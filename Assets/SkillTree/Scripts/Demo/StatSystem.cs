using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillTree.Demo
{
    [Serializable]
    public struct StatDefinition
    {
        public string StatId;
        public float BaseValue;
    }

    public sealed class StatSystem : MonoBehaviour
    {
        [SerializeField] private List<StatDefinition> _baseStats = new();

        private readonly Dictionary<string, float> _baseValues =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, List<Core.EffectModifier>> _modifiersByStat =
            new(StringComparer.OrdinalIgnoreCase);

        public event Action<string, float> OnStatChanged;

        private void Awake()
        {
            RebuildBaseStats();
        }

        public void RebuildBaseStats()
        {
            _baseValues.Clear();
            foreach (StatDefinition stat in _baseStats)
            {
                if (string.IsNullOrWhiteSpace(stat.StatId))
                    continue;

                _baseValues[stat.StatId.Trim()] = stat.BaseValue;
            }
        }

        public float GetFinalValue(string statId)
        {
            if (string.IsNullOrWhiteSpace(statId)) return 0f;

            statId = statId.Trim();
            float baseValue = _baseValues.GetValueOrDefault(statId, 0f);
            if (!_modifiersByStat.TryGetValue(statId, out List<Core.EffectModifier> modifiers))
                return baseValue;

            return Evaluate(baseValue, modifiers);
        }

        public void SetBaseValue(string statId, float baseValue)
        {
            if (string.IsNullOrWhiteSpace(statId))
                return;

            statId = statId.Trim();
            _baseValues[statId] = baseValue;
            RaiseChanged(statId);
        }

        public void ApplyModifier(Core.EffectModifier modifier)
        {
            if (string.IsNullOrWhiteSpace(modifier.SourceId))
                return;

            if (string.IsNullOrWhiteSpace(modifier.StatId))
                return;

            if (modifier.Operation == Core.ModifierOperation.Divide && Mathf.Approximately(modifier.Value, 0f))
                return;

            string statId = modifier.StatId.Trim();
            if (!_modifiersByStat.TryGetValue(statId, out List<Core.EffectModifier> modifiers))
            {
                modifiers = new List<Core.EffectModifier>();
                _modifiersByStat[statId] = modifiers;
            }

            Core.EffectModifier normalized = modifier;
            normalized.StatId = statId;
            normalized.SourceId = modifier.SourceId.Trim();
            modifiers.Add(normalized);
            RaiseChanged(statId);
        }

        public void RemoveModifiersBySource(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
                return;

            sourceId = sourceId.Trim();
            List<string> changedStatIds = new();

            foreach (KeyValuePair<string, List<Core.EffectModifier>> pair in _modifiersByStat)
            {
                int removedCount = pair.Value.RemoveAll(
                    m => string.Equals(m.SourceId, sourceId, StringComparison.OrdinalIgnoreCase));

                if (removedCount > 0)
                    changedStatIds.Add(pair.Key);
            }

            foreach (string statId in changedStatIds)
                RaiseChanged(statId);
        }

        private void RaiseChanged(string statId)
        {
            OnStatChanged?.Invoke(statId, GetFinalValue(statId));
        }

        private static float Evaluate(float baseValue, List<Core.EffectModifier> modifiers)
        {
            float add = 0f;
            float subtract = 0f;
            float multiply = 1f;
            float divide = 1f;

            foreach (Core.EffectModifier modifier in modifiers)
            {
                switch (modifier.Operation)
                {
                    case Core.ModifierOperation.Add:
                        add += modifier.Value;
                        break;
                    case Core.ModifierOperation.Subtract:
                        subtract += modifier.Value;
                        break;
                    case Core.ModifierOperation.Multiply:
                        multiply *= modifier.Value;
                        break;
                    case Core.ModifierOperation.Divide:
                        if (!Mathf.Approximately(modifier.Value, 0f))
                            divide *= modifier.Value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return (baseValue + add - subtract) * multiply / divide;
        }
    }
}
