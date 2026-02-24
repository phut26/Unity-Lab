using System;
using System.Collections.Generic;
using SkillTree.Core;
using UnityEngine;

namespace SkillTree.Demo
{
    public sealed class SkillNodeViewModel
    {
        public SkillNodeViewModel(
            string skillId,
            string displayName,
            string description,
            Vector2 nodePosition,
            int level,
            int maxLevel,
            bool isLocked,
            bool isUnlocked,
            bool isMaxed,
            bool canAfford,
            bool canUpgrade,
            IReadOnlyList<string> prerequisiteIds,
            IReadOnlyList<CostDefinition> upgradeCosts)
        {
            SkillId = skillId;
            DisplayName = displayName;
            Description = description;
            NodePosition = nodePosition;
            Level = level;
            MaxLevel = maxLevel;
            IsLocked = isLocked;
            IsUnlocked = isUnlocked;
            IsMaxed = isMaxed;
            CanAfford = canAfford;
            CanUpgrade = canUpgrade;
            PrerequisiteIds = prerequisiteIds ?? Array.Empty<string>();
            UpgradeCosts = upgradeCosts ?? Array.Empty<CostDefinition>();
        }

        public string SkillId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Vector2 NodePosition { get; }
        public int Level { get; }
        public int MaxLevel { get; }
        public bool IsLocked { get; }
        public bool IsUnlocked { get; }
        public bool IsMaxed { get; }
        public bool CanAfford { get; }
        public bool CanUpgrade { get; }
        public IReadOnlyList<string> PrerequisiteIds { get; }
        public IReadOnlyList<CostDefinition> UpgradeCosts { get; }
    }
}
