using System;
using System.Collections.Generic;
using SkillTree.Core;


namespace SkillTree.Demo
{
    public sealed class SkillTreePresenter : IDisposable
    {
        private readonly IReadOnlyList<SkillSO> _skillData;
        private readonly SkillTreeService _service;
        private readonly ISkillContext _skillContext;
        private readonly List<SkillNodeViewModel> _nodes = new();
        private bool _isDisposed;

        public SkillTreePresenter(
            IReadOnlyList<SkillSO> skillData,
            SkillTreeService service,
            ISkillContext skillContext)
        {
            _skillData = skillData ?? throw new ArgumentNullException(nameof(skillData));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _skillContext = skillContext ?? throw new ArgumentNullException(nameof(skillContext));

            _service.OnLevelChanged += HandleLevelChanged;
            _service.OnLevelsReset += HandleLevelsReset;
            Refresh();
        }

        public event Action<IReadOnlyList<SkillNodeViewModel>> OnChanged;
        public IReadOnlyList<SkillNodeViewModel> Nodes => _nodes;

        public SkillUpgradeResult TryUpgrade(string skillId)
        {
            SkillUpgradeResult result = _service.TryUpgrade(skillId, _skillContext);
            if (result != SkillUpgradeResult.Success)
                Refresh();

            return result;
        }

        public void Refresh()
        {
            _nodes.Clear();

            foreach (SkillSO skillData in _skillData)
            {
                if (skillData == null || string.IsNullOrWhiteSpace(skillData.SkillId))
                    continue;

                Skill skill = _service.GetSkillById(skillData.SkillId);
                bool isLocked = !_service.ArePrerequisitesMet(skill.SkillId);
                bool isUnlocked = !isLocked;
                bool isMaxed = skill.IsMaxedLevel;
                bool canAfford = isUnlocked && !isMaxed && _skillContext.CanPay(skill.UpgradeCosts);
                bool canUpgrade = isUnlocked && !isMaxed && canAfford;
                string displayName = string.IsNullOrWhiteSpace(skillData.DisplayName)
                    ? skill.SkillId
                    : skillData.DisplayName.Trim();

                _nodes.Add(new SkillNodeViewModel(
                    skill.SkillId,
                    displayName,
                    skillData.Description ?? string.Empty,
                    skillData.NodePosition,
                    skill.Level,
                    skill.MaxLevel,
                    isLocked,
                    isUnlocked,
                    isMaxed,
                    canAfford,
                    canUpgrade,
                    skill.PrerequisiteIds,
                    skill.UpgradeCosts));
            }

            OnChanged?.Invoke(_nodes);
        }

        public SkillNodeViewModel GetNodeById(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                throw new ArgumentException("Skill id cannot be null or whitespace.", nameof(skillId));

            foreach (SkillNodeViewModel node in _nodes)
                if (string.Equals(node.SkillId, skillId, StringComparison.Ordinal))
                    return node;

            throw new KeyNotFoundException($"Skill node not found: {skillId}");
        }

        public bool TryGetNodeById(string skillId, out SkillNodeViewModel node)
        {
            node = null;
            if (string.IsNullOrWhiteSpace(skillId))
                return false;

            foreach (SkillNodeViewModel current in _nodes)
            {
                if (string.Equals(current.SkillId, skillId, StringComparison.Ordinal))
                {
                    node = current;
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _service.OnLevelChanged -= HandleLevelChanged;
            _service.OnLevelsReset -= HandleLevelsReset;
            _isDisposed = true;
        }

        private void HandleLevelChanged(Skill _) => Refresh();
        private void HandleLevelsReset() => Refresh();
    }
}
