using System;
using System.Collections.Generic;
using System.Linq;


namespace SkillTree.Core
{
    public class Skill
    {
        private readonly int _maxLevel;
        private readonly string _skillId;
        private int _currentLevel;
        public IReadOnlyList<CostDefinition> UpgradeCosts { get; }
        public IReadOnlyList<string> PrerequisiteIds { get; }
        public IReadOnlyList<EffectDefinition> EffectDefinitions { get; }
        

        public Skill(SkillSO skillData, int level = 0)
        {
            if (skillData == null) 
                throw new ArgumentNullException(nameof(skillData));

            _skillId = skillData.SkillId;
            _maxLevel = skillData.MaxLevel;
            _currentLevel = Math.Clamp(level, 0, _maxLevel);

            UpgradeCosts = SnapshotList(skillData.upgradeCosts);
            PrerequisiteIds = SnapshotList(skillData.prerequisiteIds);
            EffectDefinitions = SnapshotList(skillData.effects);
        }


        public int Level => _currentLevel;
        public int MaxLevel => _maxLevel;
        public string SkillId => _skillId;
        public bool IsMaxedLevel => Level >= MaxLevel;


        public bool IsDiscreteSkill() => _maxLevel == 1;

        internal void IncreaseLevel()
        {
            if (IsMaxedLevel) return;
            _currentLevel++;
        }
        
        internal void SetLevel(int newLevel = 0) =>
            _currentLevel = Math.Clamp(newLevel, 0, MaxLevel);

        private static IReadOnlyList<T> SnapshotList<T>(List<T> source)
        {
            if (source == null || source.Count == 0)
                return Array.Empty<T>();

            return source.ToList().AsReadOnly();
        }
    }
}
