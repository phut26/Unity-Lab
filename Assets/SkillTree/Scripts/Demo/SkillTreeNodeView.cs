using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace SkillTree.Demo
{
    public sealed class SkillTreeNodeView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform _rectTransform;
        [FormerlySerializedAs("_upgradeButton")]
        [SerializeField] private Button _selectButton;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _stateImage;
        [SerializeField] private Image _selectionFrameImage;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _statusText;

        [Header("State Colors")]
        [SerializeField] private Color _lockedColor = new(0.35f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color _unlockedColor = new(0.9f, 0.9f, 0.9f, 1f);
        [SerializeField] private Color _maxedColor = new(0.3f, 0.8f, 0.3f, 1f);
        [SerializeField] private Color _cannotAffordColor = new(0.8f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color _selectedFrameColor = new(1f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color _idleFrameColor = new(1f, 1f, 1f, 0.25f);

        private string _skillId;
        private Action<string> _selected;

        private void Awake()
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;

            if (_selectButton != null)
                _selectButton.onClick.AddListener(HandleSelected);
        }

        private void OnDestroy()
        {
            if (_selectButton != null)
                _selectButton.onClick.RemoveListener(HandleSelected);
        }

        public void Bind(SkillNodeViewModel node, bool isSelected, Action<string> onSelected)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            _skillId = node.SkillId;
            _selected = onSelected;

            if (_rectTransform != null)
                _rectTransform.anchoredPosition = node.NodePosition;

            if (_nameText != null)
                _nameText.text = node.DisplayName;

            if (_levelText != null)
                _levelText.text = $"Lv {node.Level}/{node.MaxLevel}";

            if (_statusText != null)
                _statusText.text = BuildStatus(node);

            if (_selectButton != null)
                _selectButton.interactable = true;

            if (_stateImage != null)
                _stateImage.color = ResolveStateColor(node);

            if (_selectionFrameImage != null)
                _selectionFrameImage.color = isSelected ? _selectedFrameColor : _idleFrameColor;
        }

        public void SetIcon(Sprite icon)
        {
            if (_iconImage == null)
                return;

            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
        }

        public Vector2 GetAnchoredPosition()
        {
            return _rectTransform != null ? _rectTransform.anchoredPosition : Vector2.zero;
        }

        private void HandleSelected()
        {
            if (string.IsNullOrWhiteSpace(_skillId))
                return;

            _selected?.Invoke(_skillId);
        }

        private string BuildStatus(SkillNodeViewModel node)
        {
            if (node.IsMaxed) return "Maxed";
            if (node.IsLocked) return "Locked";
            if (!node.CanAfford) return "Insufficient";
            return "Ready";
        }

        private Color ResolveStateColor(SkillNodeViewModel node)
        {
            if (node.IsMaxed) return _maxedColor;
            if (node.IsLocked) return _lockedColor;
            if (!node.CanAfford) return _cannotAffordColor;
            return _unlockedColor;
        }
    }
}
