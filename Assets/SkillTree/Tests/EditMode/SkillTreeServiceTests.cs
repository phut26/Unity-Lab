using System;
using System.Collections.Generic;
using NUnit.Framework;
using SkillTree.Core;
using SkillTree.Demo;
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

    public class SkillTreePresenterTests
    {
        [Test]
        public void Refresh_TracksLockedAndUnlockedStates()
        {
            SkillSO root = CreateSkill("root", costs: new[] { new CostDefinition { Key = "gold", CostType = CostType.Currency, Amount = 10 } });
            SkillSO child = CreateSkill(
                "child",
                prerequisites: new[] { "root" },
                costs: new[] { new CostDefinition { Key = "gold", CostType = CostType.Currency, Amount = 10 } });

            FakeStore store = new();
            FakeCostCatalog catalog = new(new[] { "gold" });
            FakeWalletContext wallet = new(new Dictionary<string, int> { ["gold"] = 20 });
            SkillTreeService service = new(new[] { root, child }, store, catalog);

            using SkillTreePresenter presenter = new(new[] { root, child }, service, wallet);

            SkillNodeViewModel rootNode = presenter.GetNodeById("root");
            SkillNodeViewModel childNode = presenter.GetNodeById("child");

            Assert.That(rootNode.IsLocked, Is.False);
            Assert.That(rootNode.IsUnlocked, Is.True);
            Assert.That(rootNode.IsMaxed, Is.False);
            Assert.That(rootNode.CanAfford, Is.True);
            Assert.That(rootNode.CanUpgrade, Is.True);

            Assert.That(childNode.IsLocked, Is.True);
            Assert.That(childNode.IsUnlocked, Is.False);
            Assert.That(childNode.CanAfford, Is.False);
            Assert.That(childNode.CanUpgrade, Is.False);

            Assert.That(presenter.TryUpgrade("root"), Is.EqualTo(SkillUpgradeResult.Success));

            childNode = presenter.GetNodeById("child");
            Assert.That(childNode.IsLocked, Is.False);
            Assert.That(childNode.IsUnlocked, Is.True);
            Assert.That(childNode.CanAfford, Is.True);
            Assert.That(childNode.CanUpgrade, Is.True);
        }

        [Test]
        public void Refresh_MaxedSkill_DisablesUpgrade()
        {
            SkillSO skill = CreateSkill(
                "maxed",
                maxLevel: 1,
                costs: new[] { new CostDefinition { Key = "gold", CostType = CostType.Currency, Amount = 10 } });

            FakeStore store = new(new Dictionary<string, int> { ["maxed"] = 1 });
            FakeCostCatalog catalog = new(new[] { "gold" });
            FakeWalletContext wallet = new(new Dictionary<string, int> { ["gold"] = 100 });
            SkillTreeService service = new(new[] { skill }, store, catalog);

            using SkillTreePresenter presenter = new(new[] { skill }, service, wallet);
            SkillNodeViewModel node = presenter.GetNodeById("maxed");

            Assert.That(node.IsUnlocked, Is.True);
            Assert.That(node.IsMaxed, Is.True);
            Assert.That(node.CanAfford, Is.False);
            Assert.That(node.CanUpgrade, Is.False);
        }

        [Test]
        public void Refresh_AffordabilityChangesAfterWalletUpdate()
        {
            SkillSO skill = CreateSkill(
                "costly",
                costs: new[] { new CostDefinition { Key = "gold", CostType = CostType.Currency, Amount = 50 } });

            FakeStore store = new();
            FakeCostCatalog catalog = new(new[] { "gold" });
            FakeWalletContext wallet = new(new Dictionary<string, int> { ["gold"] = 10 });
            SkillTreeService service = new(new[] { skill }, store, catalog);

            using SkillTreePresenter presenter = new(new[] { skill }, service, wallet);
            SkillNodeViewModel node = presenter.GetNodeById("costly");
            Assert.That(node.CanAfford, Is.False);
            Assert.That(node.CanUpgrade, Is.False);

            wallet.Add("gold", 40);
            presenter.Refresh();

            node = presenter.GetNodeById("costly");
            Assert.That(node.CanAfford, Is.True);
            Assert.That(node.CanUpgrade, Is.True);
        }

        [Test]
        public void ServiceReset_RefreshesNodeLevel()
        {
            SkillSO skill = CreateSkill(
                "resettable",
                maxLevel: 2,
                costs: new[] { new CostDefinition { Key = "gold", CostType = CostType.Currency, Amount = 10 } });

            FakeStore store = new();
            FakeCostCatalog catalog = new(new[] { "gold" });
            FakeWalletContext wallet = new(new Dictionary<string, int> { ["gold"] = 100 });
            SkillTreeService service = new(new[] { skill }, store, catalog);

            using SkillTreePresenter presenter = new(new[] { skill }, service, wallet);
            Assert.That(presenter.TryUpgrade("resettable"), Is.EqualTo(SkillUpgradeResult.Success));
            Assert.That(presenter.GetNodeById("resettable").Level, Is.EqualTo(1));

            service.ResetProgression();

            Assert.That(presenter.GetNodeById("resettable").Level, Is.EqualTo(0));
        }

        private static SkillSO CreateSkill(
            string id,
            int maxLevel = 1,
            IEnumerable<string> prerequisites = null,
            IEnumerable<CostDefinition> costs = null)
        {
            SkillSO skill = ScriptableObject.CreateInstance<SkillSO>();
            skill.SkillId = id;
            skill.DisplayName = id;
            skill.MaxLevel = maxLevel;
            skill.PrerequisiteIds = prerequisites != null
                ? new List<string>(prerequisites)
                : new List<string>();
            skill.UpgradeCosts = costs != null
                ? new List<CostDefinition>(costs)
                : new List<CostDefinition>();
            return skill;
        }

        private sealed class FakeWalletContext : ISkillContext
        {
            private readonly Dictionary<string, int> _balances;

            public FakeWalletContext(IReadOnlyDictionary<string, int> balances)
            {
                _balances = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, int> pair in balances)
                    _balances[pair.Key] = Math.Max(0, pair.Value);
            }

            public void Add(string key, int amount)
            {
                if (string.IsNullOrWhiteSpace(key) || amount <= 0)
                    return;

                key = key.Trim();
                _balances[key] = _balances.GetValueOrDefault(key, 0) + amount;
            }

            public bool CanPay(IEnumerable<CostDefinition> costs) =>
                TryBuildRequirements(costs, out Dictionary<string, int> required) && HasEnough(required);

            public bool TryPay(IEnumerable<CostDefinition> costs)
            {
                if (!TryBuildRequirements(costs, out Dictionary<string, int> required))
                    return false;

                if (!HasEnough(required))
                    return false;

                foreach (KeyValuePair<string, int> pair in required)
                    _balances[pair.Key] -= pair.Value;

                return true;
            }

            private bool TryBuildRequirements(
                IEnumerable<CostDefinition> costs,
                out Dictionary<string, int> required)
            {
                required = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (costs == null)
                    return true;

                foreach (CostDefinition cost in costs)
                {
                    if (cost.CostType != CostType.Currency)
                        return false;

                    if (string.IsNullOrWhiteSpace(cost.Key) || cost.Amount <= 0)
                        return false;

                    string key = cost.Key.Trim();
                    if (!_balances.ContainsKey(key))
                        return false;

                    required[key] = required.GetValueOrDefault(key, 0) + cost.Amount;
                }

                return true;
            }

            private bool HasEnough(IReadOnlyDictionary<string, int> required)
            {
                foreach (KeyValuePair<string, int> pair in required)
                    if (_balances.GetValueOrDefault(pair.Key, 0) < pair.Value)
                        return false;

                return true;
            }
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

            public int GetLevel(string skillId) => _levels.GetValueOrDefault(skillId, 0);

            public IReadOnlyDictionary<string, int> LoadAll(IEnumerable<string> skillIds)
            {
                Dictionary<string, int> result = new();
                foreach (string skillId in skillIds)
                    result[skillId] = _levels.GetValueOrDefault(skillId, 0);

                return result;
            }

            public void SaveAll(IReadOnlyDictionary<string, int> levels)
            {
                foreach (KeyValuePair<string, int> pair in levels)
                    _levels[pair.Key] = pair.Value;
            }

            public void Clear(IEnumerable<string> skillIds)
            {
                foreach (string skillId in skillIds)
                    _levels.Remove(skillId);
            }
        }

        private sealed class FakeCostCatalog : ICostCatalog
        {
            private readonly HashSet<(CostType Type, string Key)> _definedCosts = new();
            private readonly HashSet<string> _currencyKeys = new(StringComparer.OrdinalIgnoreCase);

            public FakeCostCatalog(IEnumerable<string> currencyKeys)
            {
                foreach (string key in currencyKeys)
                {
                    _definedCosts.Add((CostType.Currency, key));
                    _currencyKeys.Add(key);
                }
            }

            public bool IsDefined(CostDefinition cost)
            {
                if (string.IsNullOrWhiteSpace(cost.Key))
                    return false;

                return _definedCosts.Contains((cost.CostType, cost.Key.Trim()));
            }

            public IReadOnlyCollection<string> GetKeys(CostType type) =>
                type == CostType.Currency ? _currencyKeys : Array.Empty<string>();
        }
    }
}
