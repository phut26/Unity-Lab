using System;
using System.Collections.Generic;
using UnityEngine;


namespace SkillTree.Core
{
    [CreateAssetMenu(fileName = "ResourceCatalog", menuName = "Scriptable Objects/Resource Catalog")]
    public sealed class ResourceCatalogSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Key;
            public CostType CostType;
            public string DisplayName;
            public Sprite Icon;
        }

        [SerializeField] private List<Entry> _entries = new();
        public IReadOnlyList<Entry> Entries => _entries;
    }
}
