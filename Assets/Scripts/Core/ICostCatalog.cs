using System.Collections.Generic;

namespace SkillTree.Core
{
    public interface ICostCatalog
    {
        bool IsDefined(CostDefinition cost);
        IReadOnlyCollection<string> GetKeys(CostType type);
    }
}
