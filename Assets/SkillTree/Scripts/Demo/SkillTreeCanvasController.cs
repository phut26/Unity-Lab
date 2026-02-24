using System;
using System.Collections.Generic;
using SkillTree.Core;
using UnityEngine;

namespace SkillTree.Demo
{
    public sealed class SkillTreeCanvasController : MonoBehaviour
    {
        [SerializeField] private SkillTreeBehaviour _skillTreeBehaviour;
        [SerializeField] private RectTransform _connectionsRoot;
        [SerializeField] private RectTransform _nodesRoot;
        [SerializeField] private SkillTreeConnectionView _connectionViewPrefab;
        [SerializeField] private SkillTreeNodeView _nodeViewPrefab;
        [SerializeField] private SkillTreeDetailPanelView _detailPanel;
        [SerializeField] private bool _autoRefreshOnEnable = true;
        [SerializeField] private bool _autoSelectFirstNode = true;
        [SerializeField] private float _connectionThickness = 6f;
        [SerializeField] private Color _lockedConnectionColor = new(0.35f, 0.35f, 0.35f, 0.9f);
        [SerializeField] private Color _readyConnectionColor = new(0.8f, 0.8f, 0.8f, 0.9f);
        [SerializeField] private Color _selectedPathColor = new(1f, 0.85f, 0.35f, 1f);

        private readonly Dictionary<string, SkillTreeConnectionView> _connections = new(StringComparer.Ordinal);
        private readonly Dictionary<string, SkillTreeNodeView> _nodeViews = new(StringComparer.Ordinal);
        private SkillTreePresenter _presenter;
        private string _selectedSkillId;
        private bool _isInitialized;
        private bool _warnedMissingConnectionPrefab;

        private void Awake()
        {
            if (_skillTreeBehaviour == null)
                _skillTreeBehaviour = FindFirstObjectByType<SkillTreeBehaviour>();
        }

        private void OnEnable()
        {
            if (_autoRefreshOnEnable)
                TryInitializeAndRefresh();
        }

        private void Start()
        {
            if (_autoRefreshOnEnable && !_isInitialized)
                TryInitializeAndRefresh();
        }

        private void Update()
        {
            if (_autoRefreshOnEnable && !_isInitialized)
                TryInitializeAndRefresh();
        }

        private void OnDisable()
        {
            UnbindPresenter();
        }

        private void OnDestroy()
        {
            UnbindPresenter();
        }

        [ContextMenu("Refresh Skill Tree UI")]
        public void RefreshUi()
        {
            if (!_isInitialized)
            {
                TryInitializeAndRefresh();
                return;
            }

            _presenter.Refresh();
        }

        private void TryInitializeAndRefresh()
        {
            if (_isInitialized)
            {
                _presenter.Refresh();
                return;
            }

            if (_skillTreeBehaviour == null || _nodeViewPrefab == null)
            {
                Debug.LogWarning("[SkillTreeDemo] Missing references for SkillTreeCanvasController.");
                return;
            }

            if (_skillTreeBehaviour.Session == null)
            {
                return;
            }

            _presenter = _skillTreeBehaviour.CreatePresenter();
            _presenter.OnChanged += HandlePresenterChanged;
            _isInitialized = true;

            _presenter.Refresh();
        }

        private void HandlePresenterChanged(IReadOnlyList<SkillNodeViewModel> nodes)
        {
            HashSet<string> activeNodeIds = new(StringComparer.Ordinal);

            foreach (SkillNodeViewModel node in nodes)
            {
                activeNodeIds.Add(node.SkillId);
                SkillTreeNodeView view = GetOrCreateView(node.SkillId);

                view.Bind(node, string.Equals(node.SkillId, _selectedSkillId, StringComparison.Ordinal), HandleNodeSelected);
                view.SetIcon(FindIcon(node.SkillId));
            }

            List<string> removedIds = new();
            foreach (KeyValuePair<string, SkillTreeNodeView> pair in _nodeViews)
                if (!activeNodeIds.Contains(pair.Key))
                    removedIds.Add(pair.Key);

            foreach (string removedId in removedIds)
            {
                if (_nodeViews.TryGetValue(removedId, out SkillTreeNodeView removedView) && removedView != null)
                    Destroy(removedView.gameObject);

                _nodeViews.Remove(removedId);
            }

            EnsureSelection(nodes);
            SyncSelectionVisuals(nodes);
            SyncConnections(nodes);
            RefreshDetailPanel();
        }

        private void EnsureSelection(IReadOnlyList<SkillNodeViewModel> nodes)
        {
            bool hasSelection = !string.IsNullOrWhiteSpace(_selectedSkillId);
            if (hasSelection)
            {
                foreach (SkillNodeViewModel node in nodes)
                {
                    if (string.Equals(node.SkillId, _selectedSkillId, StringComparison.Ordinal))
                        return;
                }
            }

            if (!_autoSelectFirstNode || nodes.Count == 0)
            {
                _selectedSkillId = null;
                return;
            }

            _selectedSkillId = nodes[0].SkillId;
            SyncSelectionVisuals(nodes);
        }

        private void HandleNodeSelected(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId) || _presenter == null)
                return;

            if (string.Equals(_selectedSkillId, skillId, StringComparison.Ordinal))
            {
                RefreshDetailPanel();
                return;
            }

            _selectedSkillId = skillId;
            SyncSelectionVisuals(_presenter.Nodes);
            RefreshDetailPanel();
        }

        private void SyncSelectionVisuals(IReadOnlyList<SkillNodeViewModel> nodes)
        {
            foreach (SkillNodeViewModel node in nodes)
            {
                if (_nodeViews.TryGetValue(node.SkillId, out SkillTreeNodeView view) && view != null)
                    view.Bind(node, string.Equals(node.SkillId, _selectedSkillId, StringComparison.Ordinal), HandleNodeSelected);
            }
        }

        private void SyncConnections(IReadOnlyList<SkillNodeViewModel> nodes)
        {
            HashSet<string> activeConnectionIds = new(StringComparer.Ordinal);

            foreach (SkillNodeViewModel node in nodes)
            {
                if (node.PrerequisiteIds == null || node.PrerequisiteIds.Count == 0)
                    continue;

                foreach (string prerequisiteId in node.PrerequisiteIds)
                {
                    if (!_nodeViews.TryGetValue(prerequisiteId, out SkillTreeNodeView prerequisiteView) || prerequisiteView == null)
                        continue;

                    if (!_nodeViews.TryGetValue(node.SkillId, out SkillTreeNodeView childView) || childView == null)
                        continue;

                    string connectionId = BuildConnectionId(prerequisiteId, node.SkillId);
                    activeConnectionIds.Add(connectionId);

                    SkillTreeConnectionView connection = GetOrCreateConnection(connectionId);
                    if (connection == null)
                        continue;

                    connection.SetEndpoints(
                        prerequisiteView.GetAnchoredPosition(),
                        childView.GetAnchoredPosition(),
                        _connectionThickness);
                    connection.SetColor(ResolveConnectionColor(node, prerequisiteId));
                }
            }

            List<string> removedConnectionIds = new();
            foreach (KeyValuePair<string, SkillTreeConnectionView> pair in _connections)
                if (!activeConnectionIds.Contains(pair.Key))
                    removedConnectionIds.Add(pair.Key);

            foreach (string removedConnectionId in removedConnectionIds)
            {
                if (_connections.TryGetValue(removedConnectionId, out SkillTreeConnectionView removed) && removed != null)
                    Destroy(removed.gameObject);

                _connections.Remove(removedConnectionId);
            }
        }

        private void RefreshDetailPanel()
        {
            if (_detailPanel == null)
                return;

            if (_presenter == null || string.IsNullOrWhiteSpace(_selectedSkillId))
            {
                _detailPanel.Clear();
                return;
            }

            if (_presenter.TryGetNodeById(_selectedSkillId, out SkillNodeViewModel selected))
            {
                _detailPanel.Bind(selected, HandleUpgradeRequested);
                return;
            }

            _detailPanel.Clear();
        }

        private void HandleUpgradeRequested(string skillId)
        {
            if (_presenter == null)
                return;

            SkillUpgradeResult result = _presenter.TryUpgrade(skillId);
            if (result != SkillUpgradeResult.Success)
            {
                Debug.Log($"[SkillTreeDemo] Upgrade '{skillId}' failed: {result}");
                RefreshDetailPanel();
            }
        }

        private SkillTreeNodeView GetOrCreateView(string skillId)
        {
            if (_nodeViews.TryGetValue(skillId, out SkillTreeNodeView existing) && existing != null)
                return existing;

            Transform parent = _nodesRoot != null ? _nodesRoot : transform;
            SkillTreeNodeView created = Instantiate(_nodeViewPrefab, parent);
            created.name = $"Node_{skillId}";

            _nodeViews[skillId] = created;
            return created;
        }

        private SkillTreeConnectionView GetOrCreateConnection(string connectionId)
        {
            if (_connections.TryGetValue(connectionId, out SkillTreeConnectionView existing) && existing != null)
                return existing;

            if (_connectionViewPrefab == null)
            {
                if (!_warnedMissingConnectionPrefab)
                {
                    Debug.LogWarning("[SkillTreeDemo] Connection view prefab is not assigned. Skip drawing connections.");
                    _warnedMissingConnectionPrefab = true;
                }

                return null;
            }

            Transform parent = _connectionsRoot != null
                ? _connectionsRoot
                : (_nodesRoot != null ? _nodesRoot : transform as RectTransform);
            SkillTreeConnectionView created = Instantiate(_connectionViewPrefab, parent);
            created.name = $"Conn_{connectionId}";

            _connections[connectionId] = created;
            return created;
        }

        private static string BuildConnectionId(string prerequisiteId, string skillId)
        {
            return $"{prerequisiteId}->{skillId}";
        }

        private Color ResolveConnectionColor(SkillNodeViewModel node, string prerequisiteId)
        {
            bool isSelectedPath =
                !string.IsNullOrWhiteSpace(_selectedSkillId)
                && string.Equals(node.SkillId, _selectedSkillId, StringComparison.Ordinal)
                && HasPrerequisite(node.PrerequisiteIds, prerequisiteId);

            if (isSelectedPath)
                return _selectedPathColor;

            return node.IsLocked ? _lockedConnectionColor : _readyConnectionColor;
        }

        private static bool HasPrerequisite(IReadOnlyList<string> prerequisiteIds, string prerequisiteId)
        {
            if (prerequisiteIds == null || string.IsNullOrWhiteSpace(prerequisiteId))
                return false;

            foreach (string current in prerequisiteIds)
                if (string.Equals(current, prerequisiteId, StringComparison.Ordinal))
                    return true;

            return false;
        }

        private Sprite FindIcon(string skillId)
        {
            IReadOnlyList<SkillSO> skillData = _skillTreeBehaviour.SkillData;
            if (skillData == null)
                return null;

            foreach (SkillSO skill in skillData)
                if (skill != null && string.Equals(skill.SkillId, skillId, StringComparison.Ordinal))
                    return skill.Icon;

            return null;
        }

        private void UnbindPresenter()
        {
            if (_presenter == null)
                return;

            _presenter.OnChanged -= HandlePresenterChanged;
            _presenter.Dispose();
            _presenter = null;
            _selectedSkillId = null;
            _isInitialized = false;

            if (_detailPanel != null)
                _detailPanel.Clear();

            foreach (KeyValuePair<string, SkillTreeConnectionView> pair in _connections)
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);

            _connections.Clear();
        }
    }
}
