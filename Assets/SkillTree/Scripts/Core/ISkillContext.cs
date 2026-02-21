using System.Collections.Generic;


namespace SkillTree.Core
{
    public interface ISkillContext
    {
        bool CanPay(IEnumerable<CostDefinition> costs);
        bool TryPay(IEnumerable<CostDefinition> costs);
    }
}
