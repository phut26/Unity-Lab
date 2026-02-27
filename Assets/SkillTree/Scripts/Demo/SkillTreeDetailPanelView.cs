using System;
using System.Collections.Generic;
using System.Text;
using SkillTree.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkillTree.Demo
{
    public sealed class SkillTreeDetailPanelView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Button _upgradeButton;

        [Header("Placeholders")]
        [SerializeField] private string _emptyTitle = "Select a skill";
        [SerializeField] private string _emptyDescription = "Click a node to view details.";
        [SerializeField] private string _emptyLevel = "-";
        [SerializeField] private string _emptyStatus = "-";
        [SerializeField] private string _emptyCost = "-";

        private string _selectedSkillId;
        private Action<string> _upgradeRequested;

        private void Awake()
        {
            if (_upgradeButton != null)
                _upgradeButton.onClick.AddListener(HandleUpgradeClicked);
        }

        private void OnDestroy()
        {
            if (_upgradeButton != null)
                _upgradeButton.onClick.RemoveListener(HandleUpgradeClicked);
        }

        public void Bind(SkillNodeViewModel node, Action<string> onUpgradeRequested)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            _selectedSkillId = node.SkillId;
            _upgradeRequested = onUpgradeRequested;

            if (_nameText != null)
                _nameText.text = node.DisplayName;

            if (_descriptionText != null)
                _descriptionText.text = node.Description;

            if (_levelText != null)
                _levelText.text = $"Lv {node.Level}/{node.MaxLevel}";

            if (_statusText != null)
                _statusText.text = BuildStatus(node);

            if (_costText != null)
                _costText.text = BuildCostText(node.UpgradeCosts);

            if (_upgradeButton != null)
                _upgradeButton.interactable = node.CanUpgrade;
        }

        public void Clear()
        {
            _selectedSkillId = null;
            _upgradeRequested = null;

            if (_nameText != null)
                _nameText.text = _emptyTitle;

            if (_descriptionText != null)
                _descriptionText.text = _emptyDescription;

            if (_levelText != null)
                _levelText.text = _emptyLevel;

            if (_statusText != null)
                _statusText.text = _emptyStatus;

            if (_costText != null)
                _costText.text = _emptyCost;

            if (_upgradeButton != null)
                _upgradeButton.interactable = false;
        }

        private void HandleUpgradeClicked()
        {
            if (string.IsNullOrWhiteSpace(_selectedSkillId))
                return;

            _upgradeRequested?.Invoke(_selectedSkillId);
        }

        private static string BuildStatus(SkillNodeViewModel node)
        {
            if (node.IsMaxed) return "Maxed";
            if (node.IsLocked) return "Locked";
            if (!node.CanAfford) return "Insufficient resources";
            return "Ready to upgrade";
        }

        private static string BuildCostText(IReadOnlyList<CostDefinition> costs)
        {
            if (costs == null || costs.Count == 0)
                return "No cost";

            StringBuilder builder = new();
            for (int i = 0; i < costs.Count; i++)
            {
                CostDefinition cost = costs[i];
                string key = string.IsNullOrWhiteSpace(cost.Key) ? "unknown" : cost.Key.Trim();
                builder.Append(key);
                builder.Append(": ");
                builder.Append(cost.Amount);
                builder.Append(" (");
                builder.Append(cost.CostType);
                builder.Append(')');

                if (i < costs.Count - 1)
                    builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}
