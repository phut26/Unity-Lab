using System;
using System.Collections.Generic;
using SkillTree.Core;


namespace SkillTree.Demo
{
    public class WalletContext : ISkillContext
    {
        private readonly Dictionary<string, int> _balances =
            new(StringComparer.OrdinalIgnoreCase);

        public WalletContext(ICostCatalog catalog, IReadOnlyDictionary<string, int> initialBalances = null)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            foreach (string key in catalog.GetKeys(CostType.Currency))
                _balances[key] = 0;

            if (initialBalances == null) return;

            foreach (var kv in initialBalances)
            {
                if (!_balances.ContainsKey(kv.Key))
                    throw new InvalidOperationException($"Unknown currency key: '{kv.Key}'");
                
                _balances[kv.Key] = Math.Max(0, kv.Value);
            }
        }
        
        public int GetBalance(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return 0;
            return _balances.GetValueOrDefault(key.Trim(), 0);
        }
        

        public void Add(string key, int amount)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(key)) return;
            key = key.Trim();
            if (!_balances.ContainsKey(key))
                throw new InvalidOperationException($"Currency '{key}' is not defined in catalog.");
            _balances[key] += amount;
        }
        

        public bool CanPay(IEnumerable<CostDefinition> costs)
        {
            return TryBuildCurrencyRequirements(costs, out var required) && HasEnough(required);
        }


        public bool TryPay(IEnumerable<CostDefinition> costs)
        {
            if (!TryBuildCurrencyRequirements(costs, out var required)) return false;
            if (!HasEnough(required)) return false;

            foreach (var kv in required)
                _balances[kv.Key] -= kv.Value;
            
            return true;
        }
        

        private bool TryBuildCurrencyRequirements(
            IEnumerable<CostDefinition> costs,
            out Dictionary<string, int> required
        )
        {
            required = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (costs == null) return true;

            foreach (var c in costs)
            {
                if (c.CostType != CostType.Currency) return false;
                if (string.IsNullOrWhiteSpace(c.Key) || c.Amount <= 0) return false;

                string key = c.Key.Trim();
                if (!_balances.ContainsKey(key)) return false;
                required[key] = required.GetValueOrDefault(key, 0) + c.Amount;
            }
            return true;
        }
        

        private bool HasEnough(Dictionary<string, int> required)
        {
            foreach (var kv in required)
                if (_balances.GetValueOrDefault(kv.Key, 0) < kv.Value)
                    return false;

            return true;
        }
    }
}
