using System;
using System.Text;
using SkillTree.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkillTree.Demo
{
    public sealed class SkillTreeResourcePanelView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI _resourcesText;
        [SerializeField] private Button _addResourcesButton;
        [SerializeField] private TextMeshProUGUI _addResourcesButtonLabel;
        [SerializeField] private Button _resetProgressionButton;
        [SerializeField] private TextMeshProUGUI _resetProgressionButtonLabel;

        [Header("Content")]
        [SerializeField] private string _addResourcesButtonText = "Add Resources";
        [SerializeField] private string _resetProgressionButtonText = "Reset Progression";
        [SerializeField] private string _emptyText = "No resources available.";

        private ResourceCatalogSO _catalog;
        private Func<string, int> _balanceResolver;
        private Action _addResourcesRequested;
        private Action _resetProgressionRequested;

        private void Awake()
        {
            if (_addResourcesButton != null)
                _addResourcesButton.onClick.AddListener(HandleAddResourcesClicked);
            if (_resetProgressionButton != null)
                _resetProgressionButton.onClick.AddListener(HandleResetProgressionClicked);

            ApplyButtonLabel();
            Clear();
        }

        private void OnDestroy()
        {
            if (_addResourcesButton != null)
                _addResourcesButton.onClick.RemoveListener(HandleAddResourcesClicked);
            if (_resetProgressionButton != null)
                _resetProgressionButton.onClick.RemoveListener(HandleResetProgressionClicked);
        }

        public void Bind(
            ResourceCatalogSO catalog,
            Func<string, int> balanceResolver,
            Action onAddResourcesRequested,
            Action onResetProgressionRequested = null)
        {
            _catalog = catalog;
            _balanceResolver = balanceResolver;
            _addResourcesRequested = onAddResourcesRequested;
            _resetProgressionRequested = onResetProgressionRequested;

            ApplyButtonLabel();
            UpdateButtonState();
            RefreshBalances();
        }

        public void RefreshBalances()
        {
            if (_resourcesText == null)
                return;

            if (_catalog == null || _balanceResolver == null)
            {
                _resourcesText.text = _emptyText;
                return;
            }

            StringBuilder builder = new();
            bool hasCurrencyEntries = false;

            foreach (ResourceCatalogSO.Entry entry in _catalog.Entries)
            {
                if (entry.CostType != CostType.Currency || string.IsNullOrWhiteSpace(entry.Key))
                    continue;

                hasCurrencyEntries = true;

                string key = entry.Key.Trim();
                string displayName = string.IsNullOrWhiteSpace(entry.DisplayName) ? key : entry.DisplayName.Trim();
                int balance = Mathf.Max(0, _balanceResolver(key));

                builder.Append(displayName);
                builder.Append(": ");
                builder.Append(balance);
                builder.AppendLine();
            }

            _resourcesText.text = hasCurrencyEntries ? builder.ToString().TrimEnd() : _emptyText;
        }

        public void Clear()
        {
            _catalog = null;
            _balanceResolver = null;
            _addResourcesRequested = null;
            _resetProgressionRequested = null;

            if (_resourcesText != null)
                _resourcesText.text = _emptyText;

            UpdateButtonState();
        }

        private void HandleAddResourcesClicked()
        {
            _addResourcesRequested?.Invoke();
        }

        private void HandleResetProgressionClicked()
        {
            _resetProgressionRequested?.Invoke();
        }

        private void UpdateButtonState()
        {
            if (_addResourcesButton != null)
                _addResourcesButton.interactable = _addResourcesRequested != null;
            if (_resetProgressionButton != null)
                _resetProgressionButton.interactable = _resetProgressionRequested != null;
        }

        private void ApplyButtonLabel()
        {
            if (_addResourcesButtonLabel != null)
                _addResourcesButtonLabel.text = _addResourcesButtonText;
            if (_resetProgressionButtonLabel != null)
                _resetProgressionButtonLabel.text = _resetProgressionButtonText;
        }
    }
}
