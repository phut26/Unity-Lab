using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SkillTree.Core;
using SkillTree.Demo;
using UnityEngine;
using UnityEngine.TestTools;

namespace SkillTree.Tests.PlayMode
{
    public class SkillTreeFlowPlayModeTests
    {
        private readonly List<UnityEngine.Object> _createdAssets = new();
        private readonly List<GameObject> _createdGameObjects = new();
        private readonly List<string> _skillIds = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in _createdGameObjects)
                if (gameObject != null)
                    UnityEngine.Object.DestroyImmediate(gameObject);

            foreach (UnityEngine.Object asset in _createdAssets)
                if (asset != null)
                    UnityEngine.Object.DestroyImmediate(asset);

            foreach (string skillId in _skillIds)
                PlayerPrefs.DeleteKey(skillId);

            PlayerPrefs.Save();
            _createdAssets.Clear();
            _createdGameObjects.Clear();
            _skillIds.Clear();
        }

        [UnityTest]
        public IEnumerator TryUpgrade_Success_AppliesAdditiveStatModifier()
        {
            string skillId = NewSkillId("spell_power");
            SkillSO skill = CreateSkill(
                skillId,
                3,
                null,
                new EffectDefinition
                {
                    TargetType = "Stat",
                    StatId = "spell_power",
                    Operation = ModifierOperation.Add,
                    Value = 2f
                },
                new CostDefinition { Key = "gold", CostType = CostType.Currency, Amount = 10 });

            TestRig rig = CreateRig(
                new[] { skill },
                new[] { new StatDefinition { StatId = "spell_power", BaseValue = 10f } },
                new Dictionary<string, int> { ["gold"] = 100, ["essence"] = 0 });

            yield return null;

            SkillUpgradeResult result = rig.Service.TryUpgrade(skillId, rig.Wallet);

            Assert.That(result, Is.EqualTo(SkillUpgradeResult.Success));
            Assert.That(rig.Service.GetSkillById(skillId).Level, Is.EqualTo(1));
            Assert.That(rig.StatSystem.GetFinalValue("spell_power"), Is.EqualTo(12f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator TryUpgrade_CannotAfford_DoesNotChangeStat()
        {
            string skillId = NewSkillId("cost_gate");
            SkillSO skill = CreateSkill(
                skillId,
                1,
                null,
                new EffectDefinition
                {
                    TargetType = "Stat",
                    StatId = "spell_power",
                    Operation = ModifierOperation.Add,
                    Value = 5f
                },
                new CostDefinition { Key = "gold", CostType = CostType.Currency, Amount = 50 });

            TestRig rig = CreateRig(
                new[] { skill },
                new[] { new StatDefinition { StatId = "spell_power", BaseValue = 10f } },
                new Dictionary<string, int> { ["gold"] = 0, ["essence"] = 0 });

            yield return null;

            SkillUpgradeResult result = rig.Service.TryUpgrade(skillId, rig.Wallet);

            Assert.That(result, Is.EqualTo(SkillUpgradeResult.CannotAfford));
            Assert.That(rig.Service.GetSkillById(skillId).Level, Is.EqualTo(0));
            Assert.That(rig.StatSystem.GetFinalValue("spell_power"), Is.EqualTo(10f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator TryUpgrade_PrerequisiteNotMet_DoesNotApplyChildEffect()
        {
            string rootId = NewSkillId("root");
            string childId = NewSkillId("child");
            SkillSO root = CreateSkill(
                rootId,
                1,
                null,
                new EffectDefinition
                {
                    TargetType = "Stat",
                    StatId = "mana_regen",
                    Operation = ModifierOperation.Add,
                    Value = 1f
                },
                new CostDefinition { Key = "gold", CostType = CostType.Currency, Amount = 10 });

            SkillSO child = CreateSkill(
                childId,
                1,
                new[] { rootId },
                new EffectDefinition
                {
                    TargetType = "Stat",
                    StatId = "spell_power",
                    Operation = ModifierOperation.Add,
                    Value = 7f
                },
                new CostDefinition { Key = "gold", CostType = CostType.Currency, Amount = 10 });

            TestRig rig = CreateRig(
                new[] { root, child },
                new[] { new StatDefinition { StatId = "spell_power", BaseValue = 10f } },
                new Dictionary<string, int> { ["gold"] = 100, ["essence"] = 0 });

            yield return null;

            SkillUpgradeResult result = rig.Service.TryUpgrade(childId, rig.Wallet);

            Assert.That(result, Is.EqualTo(SkillUpgradeResult.PrerequisiteNotMet));
            Assert.That(rig.Service.GetSkillById(childId).Level, Is.EqualTo(0));
            Assert.That(rig.StatSystem.GetFinalValue("spell_power"), Is.EqualTo(10f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator ResetProgression_RemovesAppliedModifiers()
        {
            string skillId = NewSkillId("reset");
            SkillSO skill = CreateSkill(
                skillId,
                2,
                null,
                new EffectDefinition
                {
                    TargetType = "Stat",
                    StatId = "spell_power",
                    Operation = ModifierOperation.Add,
                    Value = 3f
                },
                new CostDefinition { Key = "gold", CostType = CostType.Currency, Amount = 10 });

            TestRig rig = CreateRig(
                new[] { skill },
                new[] { new StatDefinition { StatId = "spell_power", BaseValue = 10f } },
                new Dictionary<string, int> { ["gold"] = 100, ["essence"] = 0 });

            yield return null;

            SkillUpgradeResult result = rig.Service.TryUpgrade(skillId, rig.Wallet);
            Assert.That(result, Is.EqualTo(SkillUpgradeResult.Success));
            Assert.That(rig.StatSystem.GetFinalValue("spell_power"), Is.EqualTo(13f).Within(0.0001f));

            rig.Service.ResetProgression();

            Assert.That(rig.Service.GetSkillById(skillId).Level, Is.EqualTo(0));
            Assert.That(rig.StatSystem.GetFinalValue("spell_power"), Is.EqualTo(10f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator TryUpgrade_MultiplyEffect_ScalesPerLevel()
        {
            string skillId = NewSkillId("multiply");
            SkillSO skill = CreateSkill(
                skillId,
                3,
                null,
                new EffectDefinition
                {
                    TargetType = "Stat",
                    StatId = "spell_power",
                    Operation = ModifierOperation.Multiply,
                    Value = 1.1f
                },
                new CostDefinition { Key = "gold", CostType = CostType.Currency, Amount = 10 });

            TestRig rig = CreateRig(
                new[] { skill },
                new[] { new StatDefinition { StatId = "spell_power", BaseValue = 100f } },
                new Dictionary<string, int> { ["gold"] = 100, ["essence"] = 0 });

            yield return null;

            Assert.That(rig.Service.TryUpgrade(skillId, rig.Wallet), Is.EqualTo(SkillUpgradeResult.Success));
            Assert.That(rig.Service.TryUpgrade(skillId, rig.Wallet), Is.EqualTo(SkillUpgradeResult.Success));

            Assert.That(rig.Service.GetSkillById(skillId).Level, Is.EqualTo(2));
            Assert.That(rig.StatSystem.GetFinalValue("spell_power"), Is.EqualTo(121f).Within(0.001f));
        }

        private TestRig CreateRig(
            IReadOnlyList<SkillSO> skills,
            IReadOnlyList<StatDefinition> stats,
            IReadOnlyDictionary<string, int> balances)
        {
            if (skills == null) throw new ArgumentNullException(nameof(skills));
            if (stats == null) throw new ArgumentNullException(nameof(stats));
            if (balances == null) throw new ArgumentNullException(nameof(balances));

            foreach (SkillSO skill in skills)
            {
                _skillIds.Add(skill.SkillId);
                PlayerPrefs.DeleteKey(skill.SkillId);
            }
            PlayerPrefs.Save();

            ResourceCatalogSO catalog = CreateCatalog();

            GameObject root = new("PlayModeSkillTreeRig");
            root.SetActive(false);
            _createdGameObjects.Add(root);

            SkillTreeBehaviour behaviour = root.AddComponent<SkillTreeBehaviour>();
            StatSystem statSystem = root.AddComponent<StatSystem>();
            SkillEffectAdapter adapter = root.AddComponent<SkillEffectAdapter>();
            int startingGold = balances.TryGetValue("gold", out int gold) ? gold : 0;
            int startingEssence = balances.TryGetValue("essence", out int essence) ? essence : 0;

            SetPrivateField(behaviour, "_skillData", new List<SkillSO>(skills));
            SetPrivateField(behaviour, "_resourceCatalog", catalog);
            SetPrivateField(behaviour, "_startingGold", startingGold);
            SetPrivateField(behaviour, "_startingEssence", startingEssence);
            SetPrivateField(statSystem, "_baseStats", new List<StatDefinition>(stats));
            SetPrivateField(adapter, "_skillTreeBehaviour", behaviour);
            SetPrivateField(adapter, "_statSystem", statSystem);

            root.SetActive(true);

            return new TestRig
            {
                Behaviour = behaviour,
                StatSystem = statSystem,
                Adapter = adapter,
                Wallet = behaviour.Wallet
            };
        }

        private ResourceCatalogSO CreateCatalog()
        {
            ResourceCatalogSO catalog = ScriptableObject.CreateInstance<ResourceCatalogSO>();
            _createdAssets.Add(catalog);

            List<ResourceCatalogSO.Entry> entries = new()
            {
                new ResourceCatalogSO.Entry { Key = "gold", CostType = CostType.Currency, DisplayName = "Gold", Icon = null },
                new ResourceCatalogSO.Entry { Key = "essence", CostType = CostType.Currency, DisplayName = "Essence", Icon = null }
            };

            SetPrivateField(catalog, "_entries", entries);
            return catalog;
        }

        private SkillSO CreateSkill(
            string skillId,
            int maxLevel,
            IReadOnlyList<string> prerequisites,
            EffectDefinition effect,
            CostDefinition cost)
        {
            SkillSO skill = ScriptableObject.CreateInstance<SkillSO>();
            _createdAssets.Add(skill);
            skill.SkillId = skillId;
            skill.MaxLevel = maxLevel;
            skill.PrerequisiteIds = prerequisites != null ? new List<string>(prerequisites) : new List<string>();
            skill.Effects = new List<EffectDefinition> { effect };
            skill.UpgradeCosts = new List<CostDefinition> { cost };
            return skill;
        }

        private string NewSkillId(string prefix)
        {
            return $"{prefix}_{Guid.NewGuid():N}";
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target
                .GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (fieldInfo == null)
                throw new MissingFieldException(target.GetType().Name, fieldName);

            fieldInfo.SetValue(target, value);
        }

        private sealed class TestRig
        {
            public SkillTreeBehaviour Behaviour { get; set; }
            public StatSystem StatSystem { get; set; }
            public SkillEffectAdapter Adapter { get; set; }
            public WalletContext Wallet { get; set; }
            public SkillTreeService Service => Behaviour.Service;
        }
    }
}
