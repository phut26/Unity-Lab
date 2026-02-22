using System;
using System.Collections.Generic;
using UnityEngine;


namespace SkillTree.Core
{
    public enum CostType
    {
        Currency,
        Item,
        Energy,
    }

    [Serializable]
    public struct CostDefinition
    {
        public string Key;
        public CostType CostType;
        public int Amount;
    }


    [Serializable]
    public struct EffectDefinition
    {
        public string TargetType;
        public string StatId;
        public ModifierOperation Operation;
        public float Value;
    }


    public struct EffectModifier
    {
        public string SourceId;
        public string StatId;
        public ModifierOperation Operation;
        public float Value;
    }


    public enum ModifierOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
    }

    [CreateAssetMenu(fileName = "NewSkillSO", menuName = "Scriptable Objects/SkillSO")]
    public class SkillSO : ScriptableObject
    {
        public string SkillId;
        public string DisplayName;
        public string Description;
        public Sprite Icon;

        [Min(1)]
        public int MaxLevel;

        public List<string> PrerequisiteIds = new();
        public List<EffectDefinition> Effects = new();
        public List<CostDefinition> UpgradeCosts = new();
        
        public Vector2 NodePosition;
    }
}
