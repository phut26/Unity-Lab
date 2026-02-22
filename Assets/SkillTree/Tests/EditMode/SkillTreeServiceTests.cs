using System;
using System.Collections.Generic;
using NUnit.Framework;
using SkillTree.Core;
using UnityEngine;


namespace SkillTree.Tests.EditMode
{
    public class SkillTreeServiceTests
    {
        private readonly string skillId = "fireball";

        [Test]
        public void TryUpgrade_Success_IncreasesLevelAndSaves()
        {
            SkillSO skill = CreateSkill(skillId);
            FakeStore store = new();
            FakeResourceCatalog fakeResourceCatalog = new(isSupported: true);
            SkillTreeService sut = new(new[] { skill }, store, fakeResourceCatalog);
            FakeContext context = new(canPay: true, tryPay: true);

            var result = sut.TryUpgrade(skillId, context);

            Assert.That(result, Is.EqualTo(SkillUpgradeResult.Success));
            Assert.That(sut.GetSkillById(skillId).Level, Is.EqualTo(1));
            Assert.That(store.SaveAllCallCount, Is.EqualTo(1));
            Assert.That(store.LastSavedLevels[skillId], Is.EqualTo(1));
        }

        [Test]
        public void TryUpgrade_CannotAfford_DoesNotIncreaseLevelOrSave()
        {
            SkillSO skill = CreateSkill(skillId);
            FakeStore store = new();
            FakeResourceCatalog fakeResourceCatalog = new(isSupported: true);
            SkillTreeService sut = new(new[] { skill }, store, fakeResourceCatalog);
            FakeContext context = new(canPay: false, tryPay: true);

            var result = sut.TryUpgrade(skillId, context);

            Assert.That(result, Is.EqualTo(SkillUpgradeResult.CannotAfford));
            Assert.That(sut.GetSkillById(skillId).Level, Is.EqualTo(0));
            Assert.That(store.SaveAllCallCount, Is.EqualTo(0));
        }

        [Test]
        public void TryUpgrade_PrerequisiteNotMet_ReturnsExpectedResult()
        {
            SkillSO root = CreateSkill("root");
            SkillSO child = CreateSkill("child", prerequisites: new[] { "root" });
            FakeStore store = new();
            FakeResourceCatalog fakeResourceCatalog = new(isSupported: true);
            SkillTreeService sut = new(new[] { root, child }, store, fakeResourceCatalog);
            FakeContext context = new(canPay: true, tryPay: true);

            var result = sut.TryUpgrade("child", context);

            Assert.That(result, Is.EqualTo(SkillUpgradeResult.PrerequisiteNotMet));
            Assert.That(sut.GetSkillById("child").Level, Is.EqualTo(0));
            Assert.That(store.SaveAllCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithPrerequisiteCycle_Throws()
        {
            SkillSO skillA = CreateSkill("A", prerequisites: new[] { "B" });
            SkillSO skillB = CreateSkill("B", prerequisites: new[] { "A" });
            FakeStore store = new();
            FakeResourceCatalog fakeResourceCatalog = new(isSupported: true);

            Assert.Throws<InvalidOperationException>(
                () => new SkillTreeService(
                    new[] { skillA, skillB }, store, fakeResourceCatalog));
        }

        [Test]
        public void ResetProgression_ResetsLevelsAndSaves()
        {
            SkillSO skill = CreateSkill(skillId);
            FakeStore store = new(new Dictionary<string, int> { [skillId] = 1 });
            FakeResourceCatalog fakeResourceCatalog = new(isSupported: true);
            SkillTreeService sut = new(new[] { skill }, store, fakeResourceCatalog);

            sut.ResetProgression();

            Assert.That(sut.GetSkillById(skillId).Level, Is.EqualTo(0));
            Assert.That(store.SaveAllCallCount, Is.EqualTo(1));
            Assert.That(store.LastSavedLevels[skillId], Is.EqualTo(0));
        }
        
        [Test]
        public void TryUpgrade_TransactionFailed_ReturnsExpectedResult()
        {
            SkillSO skill = CreateSkill(skillId);
            FakeStore store = new();
            FakeResourceCatalog fakeResourceCatalog = new(isSupported: true);
            SkillTreeService sut = new(new[] { skill }, store, fakeResourceCatalog);
            FakeContext context = new(canPay: true, tryPay: false);

            var result = sut.TryUpgrade(skillId, context);
            
            Assert.That(result, Is.EqualTo(SkillUpgradeResult.TransactionFailed));
            Assert.That(sut.GetSkillById(skillId).Level, Is.EqualTo(0));
            Assert.That(store.SaveAllCallCount, Is.EqualTo(0));
        }


        [Test]
        public void TryUpgrade_MaxedLevel_ReturnsExpectedResult()
        {
            int maxLevel = 5;
            SkillSO skill = CreateSkill(skillId, maxLevel);
            FakeResourceCatalog fakeResourceCatalog = new(isSupported: true);
            FakeStore store = new(new Dictionary<string, int> { [skillId] = maxLevel });
            SkillTreeService sut = new(new[] { skill }, store, fakeResourceCatalog);
            FakeContext context = new(canPay: true, tryPay: true);

            var result = sut.TryUpgrade(skill.SkillId, context);

            Assert.That(result, Is.EqualTo(SkillUpgradeResult.Maxed));
            Assert.That(sut.GetSkillById(skillId).Level, Is.EqualTo(maxLevel));
            Assert.That(store.SaveAllCallCount, Is.EqualTo(0));
        }

        [Test]
        public void TryUpgrade_Success_InvokesOnLevelChangedOnce_WithUpdatedSkill()
        {
            SkillSO skill = CreateSkill(skillId);
            FakeStore store = new();
            FakeResourceCatalog fakeResourceCatalog = new(isSupported: true);
            SkillTreeService sut = new(new[] { skill }, store, fakeResourceCatalog);
            FakeContext context = new(canPay: true, tryPay: true);

            int callCount = 0;
            Skill receivedSkill = null;
            sut.OnLevelChanged += changedSkill =>
            {
                callCount++;
                receivedSkill = changedSkill;
            };

            var result = sut.TryUpgrade(skillId, context);

            Assert.That(result, Is.EqualTo(SkillUpgradeResult.Success));
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(receivedSkill, Is.Not.Null);
            Assert.That(receivedSkill.SkillId, Is.EqualTo(skillId));
            Assert.That(receivedSkill.Level, Is.EqualTo(1));
        }

        [Test]
        public void TryUpgrade_CannotAfford_DoesNotInvokeOnLevelChanged()
        {
            SkillSO skill = CreateSkill(skillId);
            FakeStore store = new();
            FakeResourceCatalog fakeResourceCatalog = new(isSupported: true);
            SkillTreeService sut = new(new[] { skill }, store, fakeResourceCatalog);
            FakeContext context = new(canPay: false, tryPay: true);

            int callCount = 0;
            sut.OnLevelChanged += _ => callCount++;

            var result = sut.TryUpgrade(skillId, context);

            Assert.That(result, Is.EqualTo(SkillUpgradeResult.CannotAfford));
            Assert.That(callCount, Is.EqualTo(0));
        }

        [Test]
        public void ResetProgression_InvokesOnLevelsResetOnce()
        {
            SkillSO skill = CreateSkill(skillId);
            FakeStore store = new(new Dictionary<string, int> { [skillId] = 1 });
            FakeResourceCatalog fakeResourceCatalog = new(isSupported: true);
            SkillTreeService sut = new(new[] { skill }, store, fakeResourceCatalog);

            int callCount = 0;
            sut.OnLevelsReset += () => callCount++;

            sut.ResetProgression();

            Assert.That(callCount, Is.EqualTo(1));
        }

        private static SkillSO CreateSkill(
            string id,
            int maxLevel = 1,
            IEnumerable<string> prerequisites = null)
        {
            var so = ScriptableObject.CreateInstance<SkillSO>();
            so.SkillId = id;
            so.MaxLevel = maxLevel;
            so.PrerequisiteIds = prerequisites != null ? new List<string>(prerequisites) : new List<string>();
            so.UpgradeCosts = new List<CostDefinition>
        {
            new() { Key = "gold", CostType = CostType.Currency, Amount = 10 }
        };

            return so;
        }

        private sealed class FakeContext : ISkillContext
        {
            private readonly bool _canPay;
            private readonly bool _tryPay;

            public FakeContext(bool canPay, bool tryPay)
            {
                _canPay = canPay;
                _tryPay = tryPay;
            }

            public bool CanPay(IEnumerable<CostDefinition> costs) => _canPay;
            public bool TryPay(IEnumerable<CostDefinition> costs) => _tryPay;
        }

        private sealed class FakeStore : ISkillProgressStore
        {
            private readonly Dictionary<string, int> _levels;

            public FakeStore(Dictionary<string, int> levels = null)
            {
                _levels = levels != null
                    ? new Dictionary<string, int>(levels)
                    : new Dictionary<string, int>();
            }

            public int SaveAllCallCount { get; private set; }
            public Dictionary<string, int> LastSavedLevels { get; } = new();

            public int GetLevel(string skillId) => _levels.GetValueOrDefault(skillId, 0);

            public IReadOnlyDictionary<string, int> LoadAll(IEnumerable<string> skillIds)
            {
                Dictionary<string, int> result = new();
                foreach (var id in skillIds)
                    result[id] = _levels.GetValueOrDefault(id, 0);

                return result;
            }

            public void SaveAll(IReadOnlyDictionary<string, int> levels)
            {
                SaveAllCallCount++;
                LastSavedLevels.Clear();

                foreach (var kv in levels)
                {
                    _levels[kv.Key] = kv.Value;
                    LastSavedLevels[kv.Key] = kv.Value;
                }
            }

            public void Clear(IEnumerable<string> skillIds)
            {
                foreach (var id in skillIds)
                    _levels.Remove(id);
            }
        }


        private sealed class FakeResourceCatalog : ICostCatalog
        {
            private readonly bool _isSupported;

            public FakeResourceCatalog(bool isSupported)
            {
                _isSupported = isSupported;
            }

            public IReadOnlyCollection<string> GetKeys(CostType type)
            {
                throw new NotImplementedException();
            }


            public bool IsDefined(CostDefinition cost) => _isSupported;

            public bool IsDefined(CostType type, string key)
            {
                throw new NotImplementedException();
            }

        }
    }
}
