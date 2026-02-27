using System;
using System.Collections.Generic;
using SkillTree.Core;
using UnityEngine;

namespace SkillTree.Demo
{
    public sealed class SkillNodeViewModel
    {
        public SkillNodeViewModel(
            Skill skill,
            bool isLocked,
            bool canAfford,
            bool canUpgrade)
        {
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            IsLocked = isLocked;
            CanAfford = canAfford;
            CanUpgrade = canUpgrade;
        }

        public Skill Skill { get; }
        public string SkillId => Skill.SkillId;
        public string DisplayName =>
            string.IsNullOrWhiteSpace(Skill.DisplayName) ? Skill.SkillId : Skill.DisplayName.Trim();
        public string Description => Skill.Description ?? string.Empty;
        public Sprite Icon => Skill.Icon;
        public Vector2 NodePosition => Skill.NodePosition;
        public int Level => Skill.Level;
        public int MaxLevel => Skill.MaxLevel;
        public bool IsLocked { get; }
        public bool IsUnlocked => !IsLocked;
        public bool IsMaxed => Skill.IsMaxedLevel;
        public bool CanAfford { get; }
        public bool CanUpgrade { get; }
        public IReadOnlyList<string> PrerequisiteIds => Skill.PrerequisiteIds;
        public IReadOnlyList<CostDefinition> UpgradeCosts => Skill.UpgradeCosts;
    }
}
