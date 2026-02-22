using System;
using System.Collections.Generic;
using SkillTree.Core;
using UnityEngine;


namespace SkillTree.Demo
{
    public class ResourceCatalogAdapter : ICostCatalog
    {
        private readonly HashSet<(CostType, string Key)> _defined = new();
        private readonly Dictionary<CostType, HashSet<string>> _keysByType = new();

        public ResourceCatalogAdapter(ResourceCatalogSO catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            foreach (CostType type in Enum.GetValues(typeof(CostType)))
                _keysByType[type] = new (StringComparer.OrdinalIgnoreCase);

            foreach (var entry in catalog.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                    continue;

                var key = entry.Key.Trim();
                var id = (entry.CostType, key);

                if (!_defined.Add(id))
                {
#if UNITY_EDITOR
                    Debug.Log($"Duplicate resource in catalog: {entry.CostType}/{key}.First winds.");
#endif
                    continue;
                }
                _keysByType[entry.CostType].Add(key);
            }
        }


        public bool IsDefined(CostDefinition cost)
        {
            if (string.IsNullOrWhiteSpace(cost.Key)) return false;
            return _defined.Contains((cost.CostType, cost.Key.Trim()));
        }


        public IReadOnlyCollection<string> GetKeys(CostType type) =>
            _keysByType[type];

    }
}
