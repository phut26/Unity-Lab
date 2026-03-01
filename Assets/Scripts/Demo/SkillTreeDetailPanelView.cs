using System;
using TMPro;
using UnityEngine;

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
        [SerializeField] private UnityEngine.UI.Button _upgradeButton;

        [Header("Stats References")]
        [SerializeField] private TextMeshProUGUI _statsTitleText;
        [SerializeField] private TextMeshProUGUI _statsBodyText;
        [SerializeField] private StatSystem _statSystem;

        [Header("Placeholders")]
        [SerializeField] private string _emptyTitle = "Select a skill";
        [SerializeField] private string _emptyDescription = "Click a node to view details.";
        [SerializeField] private string _emptyLevel = "-";
        [SerializeField] private string _emptyStatus = "-";
        [SerializeField] private string _emptyCost = "-";
        [SerializeField] private string _emptyStatsTitle = "Stats";
        [SerializeField] private string _emptyStatsBody = "Select a skill to view stat modifiers.";
        [SerializeField] private string _noStatEffectsText = "No stat modifiers.";

        private SkillInfoSectionView _skillInfoSection;
        private SkillStatsSectionView _statsSection;

        private void Awake()
        {
            if (_statSystem == null)
                _statSystem = FindFirstObjectByType<StatSystem>();

            EnsureSectionsInitialized();
        }

        private void OnDestroy()
        {
            _skillInfoSection?.Dispose();
        }

        public void Bind(
            SkillNodeViewModel node,
            Action<string> onUpgradeRequested,
            Func<string, int> balanceResolver = null)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            EnsureSectionsInitialized();
            _skillInfoSection?.Bind(node, onUpgradeRequested, balanceResolver);
            _statsSection?.Bind(node);
        }

        public void Clear()
        {
            EnsureSectionsInitialized();
            _skillInfoSection?.Clear();
            _statsSection?.Clear();
        }

        private void EnsureSectionsInitialized()
        {
            _skillInfoSection ??= new SkillInfoSectionView(
                    _nameText,
                    _descriptionText,
                    _levelText,
                    _statusText,
                    _costText,
                    _upgradeButton,
                    _emptyTitle,
                    _emptyDescription,
                    _emptyLevel,
                    _emptyStatus,
                    _emptyCost);

            _statsSection ??= new SkillStatsSectionView(
                    _statsTitleText,
                    _statsBodyText,
                    _statSystem,
                    _emptyStatsTitle,
                    _emptyStatsBody,
                    _noStatEffectsText);
        }
    }
}
