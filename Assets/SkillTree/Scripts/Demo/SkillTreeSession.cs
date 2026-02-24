using System.Collections.Generic;
using SkillTree.Core;


namespace SkillTree.Demo
{
    public sealed class SkillTreeSession
    {
        public SkillTreeSession(
            IReadOnlyList<SkillSO> skillData,
            ResourceCatalogSO resourceCatalog,
            IReadOnlyDictionary<string, int> initialBalances = null,
            ISkillProgressStore store = null)
        {
            if (skillData == null) throw new System.ArgumentNullException(nameof(skillData));
            if (resourceCatalog == null) throw new System.ArgumentNullException(nameof(resourceCatalog));

            Store = store ?? new SkillPersistenceService();
            Catalog = new ResourceCatalogAdapter(resourceCatalog);
            Service = new SkillTreeService(skillData, Store, Catalog);
            Wallet = new WalletContext(Catalog, initialBalances);
        }

        public SkillTreeService Service { get; }
        public WalletContext Wallet { get; }
        public ICostCatalog Catalog { get; }
        public ISkillProgressStore Store { get; }
    }
}
