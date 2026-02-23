using System;
using SkillTree.Core;
using UnityEngine;

namespace SkillTree.Demo
{
    public sealed class SkillEffectAdapter : MonoBehaviour
    {
        [SerializeField] private SkillTreeBehaviour _skillTreeBehaviour;
        [SerializeField] private StatSystem _statSystem;

        private bool _isBound;

        private void Reset()
        {
            AutoResolveReferences();
        }

        private void Awake()
        {
            AutoResolveReferences();
        }

        private void OnEnable()
        {
            TryBind();
        }

        private void Start()
        {
            TryBind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void AutoResolveReferences()
        {
            if (_skillTreeBehaviour == null)
                _skillTreeBehaviour = FindFirstObjectByType<SkillTreeBehaviour>();

            if (_statSystem == null)
                _statSystem = FindFirstObjectByType<StatSystem>();
        }

        private void TryBind()
        {
            if (_isBound) return;

            AutoResolveReferences();
            if (_skillTreeBehaviour == null || _statSystem == null) return;

            SkillTreeService service = _skillTreeBehaviour.Service;
            if (service == null) return;

            service.OnLevelChanged += OnSkillLevelChanged;
            service.OnLevelsReset += OnLevelsReset;
            _isBound = true;

            SyncAllSkills();
        }

        private void Unbind()
        {
            if (!_isBound) return;

            if (_skillTreeBehaviour == null || _skillTreeBehaviour.Service == null)
            {
                _isBound = false;
                return;
            }

            SkillTreeService service = _skillTreeBehaviour.Service;
            service.OnLevelChanged -= OnSkillLevelChanged;
            service.OnLevelsReset -= OnLevelsReset;
            _isBound = false;
        }

        private void SyncAllSkills()
        {
            foreach (Skill skill in _skillTreeBehaviour.Service.GetAllSkills())
                ReapplySkill(skill);
        }

        private void OnSkillLevelChanged(Skill skill)
        {
            ReapplySkill(skill);
        }

        private void OnLevelsReset()
        {
            foreach (Skill skill in _skillTreeBehaviour.Service.GetAllSkills())
                _statSystem.RemoveModifiersBySource(skill.SkillId);
        }

        private void ReapplySkill(Skill skill)
        {
            _statSystem.RemoveModifiersBySource(skill.SkillId);
            if (skill.Level <= 0) return;

            foreach (EffectDefinition effect in skill.EffectDefinitions)
            {
                if (!string.Equals(effect.TargetType, "Stat", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.IsNullOrWhiteSpace(effect.StatId))
                    continue;

                float value = ResolveValueByLevel(effect.Operation, effect.Value, skill.Level);
                if (Mathf.Approximately(value, 0f))
                    continue;

                EffectModifier modifier = new()
                {
                    SourceId = skill.SkillId,
                    StatId = effect.StatId.Trim(),
                    Operation = effect.Operation,
                    Value = value
                };

                _statSystem.ApplyModifier(modifier);
            }
        }

        private static float ResolveValueByLevel(ModifierOperation operation, float baseValue, int level)
        {
            if (level <= 0) return 0f;

            return operation switch
            {
                ModifierOperation.Add => baseValue * level,
                ModifierOperation.Subtract => baseValue * level,
                ModifierOperation.Multiply => Mathf.Pow(baseValue, level),
                ModifierOperation.Divide => Mathf.Pow(baseValue, level),
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };
        }
    }
}
