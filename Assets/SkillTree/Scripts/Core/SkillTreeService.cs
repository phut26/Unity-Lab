using System;
using System.Collections.Generic;
using System.Linq;


namespace SkillTree.Core
{
    public enum SkillUpgradeResult
    {
        Success,
        PrerequisiteNotMet,
        CannotAfford,
        TransactionFailed,
        Maxed,
    }

    public class SkillTreeService
    {
        private readonly Dictionary<string, Skill> _skillCache = new();
        private readonly ISkillProgressStore _store;
        public event Action<Skill> OnLevelChanged;
        public event Action OnLevelsReset;

        public SkillTreeService(IEnumerable<SkillSO> skillData, ISkillProgressStore store)
        {
            if (skillData == null) throw new ArgumentNullException(nameof(skillData));
            _store = store ?? throw new ArgumentNullException(nameof(store));

            var skills = ValidateSkillData(skillData);

            InitSkills(skills);
        }

        private static List<SkillSO> ValidateSkillData(IEnumerable<SkillSO> skillData)
        {
            List<SkillSO> skills = skillData is ICollection<SkillSO> skillCollection
                ? new List<SkillSO>(skillCollection.Count)
                : new List<SkillSO>();

            foreach (SkillSO skill in skillData)
            {
                if (skill == null)
                    throw new ArgumentException("Skill list contains null entry.", nameof(skillData));

                if (string.IsNullOrWhiteSpace(skill.SkillId))
                    throw new InvalidOperationException("Skill has empty skillId.");

                if (skill.MaxLevel < 1)
                    throw new InvalidOperationException("Skill has invalid maxLevel.");

                skills.Add(skill);
            }

            return skills;
        }

        private void InitSkills(IEnumerable<SkillSO> skillSoList)
        {
            var allIds = skillSoList.Select(s => s.SkillId);
            var savedLevels = _store.LoadAll(allIds);
            foreach (SkillSO skillSO in skillSoList)
            {
                var level = savedLevels.GetValueOrDefault(skillSO.SkillId);
                Skill newSkill = new(skillSO, ClampLevel(level, skillSO.MaxLevel));

                if (!_skillCache.TryAdd(skillSO.SkillId, newSkill))
                    throw new InvalidOperationException($"Duplicated skill id: {skillSO.SkillId}");
            }

            ValidatePrerequisites();
        }

        private void ValidatePrerequisites()
        {
            foreach (Skill skill in _skillCache.Values)
            {
                foreach (string prerequisiteId in skill.PrerequisiteIds)
                {
                    if (!_skillCache.ContainsKey(prerequisiteId))
                    {
                        throw new InvalidOperationException(
                            $"Skill '{skill.SkillId}' has unknown prerequisite id '{prerequisiteId}'.");
                    }
                }
            }

            ValidateNoPrerequisiteCycle();
        }

        private void ValidateNoPrerequisiteCycle()
        {
            var visitState = new Dictionary<string, int>();
            var currentPath = new Stack<string>();

            foreach (var skillId in _skillCache.Keys)
            {
                if (visitState.GetValueOrDefault(skillId) == 0)
                    DetectPrerequisiteCycle(skillId, visitState, currentPath);
            }
        }

        private void DetectPrerequisiteCycle(
            string skillId,
            Dictionary<string, int> visitState,
            Stack<string> currentPath)
        {
            visitState[skillId] = 1;
            currentPath.Push(skillId);

            Skill skill = _skillCache[skillId];
            foreach (string prerequisiteId in skill.PrerequisiteIds)
            {
                int prerequisiteState = visitState.GetValueOrDefault(prerequisiteId);
                if (prerequisiteState == 1)
                {
                    var pathList = currentPath.Reverse().ToList();
                    int cycleStartIndex = pathList.IndexOf(prerequisiteId);
                    var cycle = pathList.Skip(cycleStartIndex).Append(prerequisiteId);
                    string cyclePath = string.Join(" -> ", cycle);
                    throw new InvalidOperationException($"Skill prerequisite cycle detected: {cyclePath}");
                }

                if (prerequisiteState == 0)
                    DetectPrerequisiteCycle(prerequisiteId, visitState, currentPath);
            }

            currentPath.Pop();
            visitState[skillId] = 2;
        }

        private static int ClampLevel(int level, int maxLevel) => Math.Clamp(level, 0, maxLevel);

        public SkillUpgradeResult TryUpgrade(string skillId, ISkillContext skillContext)
        {
            if (skillContext == null)
                throw new ArgumentNullException(nameof(skillContext));

            if (!_skillCache.TryGetValue(skillId, out Skill skill))
                throw new KeyNotFoundException($"Skill not found: {skillId}");

            if (!ArePrerequisitesMet(skill))
                return SkillUpgradeResult.PrerequisiteNotMet;

            if (skill.IsMaxedLevel)
                return SkillUpgradeResult.Maxed;

            if (!skillContext.CanPay(skill.UpgradeCosts))
                return SkillUpgradeResult.CannotAfford;

            if (skillContext.TryPay(skill.UpgradeCosts))
            {
                skill.IncreaseLevel();
                SaveProgression();
                OnLevelChanged?.Invoke(skill);
                return SkillUpgradeResult.Success;
            }
            else return SkillUpgradeResult.TransactionFailed;
        }

        public bool ArePrerequisitesMet(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                throw new ArgumentException("Skill id cannot be null or whitespace.", nameof(skillId));

            if (!_skillCache.TryGetValue(skillId, out Skill skill))
                throw new KeyNotFoundException($"Skill not found: {skillId}");

            return ArePrerequisitesMet(skill);
        }

        private bool ArePrerequisitesMet(Skill skill)
        {
            foreach (string prerequisiteId in skill.PrerequisiteIds)
            {
                if (!_skillCache.TryGetValue(prerequisiteId, out Skill prerequisiteSkill))
                    return false;

                // TODO: Support tiered skill in the future (level == MaxLevel)
                if (prerequisiteSkill.Level < 1)
                    return false;
            }

            return true;
        }

        public Skill GetSkillById(string skillId)
        {
            if (_skillCache.TryGetValue(skillId, out Skill skill))
                return skill;

            throw new KeyNotFoundException();
        }

        public IReadOnlyCollection<Skill> GetAllSkills() => _skillCache.Values;

        public void SaveProgression()
        {
            Dictionary<string, int> levels = new();
            foreach (var (skillId, skill) in _skillCache)
            {
                levels[skillId] = skill.Level;
            }
            _store.SaveAll(levels);
        }

        public void ResetProgression()
        {
            foreach (var skill in _skillCache.Values)
            {
                skill.SetLevel(0);
            }
            SaveProgression();
            OnLevelsReset?.Invoke();
        }
    }
}
