using System.Collections.Generic;
using SkillTree.Core;
using UnityEngine;


namespace SkillTree.Demo
{
    public class SkillTreeBehaviour : MonoBehaviour
    {
        [SerializeField] private List<SkillSO> _skillData;
        [SerializeField] private ResourceCatalogSO _resourceCatalog;
        private SkillTreeService _skillTreeService;
        private ISkillProgressStore _progressStore;

        public SkillTreeService Service => _skillTreeService;

        private void Awake()
        {
            _progressStore = new SkillPersistenceService();
            ICostCatalog costCatalog = new ResourceCatalogAdapter(_resourceCatalog);
            _skillTreeService = new SkillTreeService(_skillData, _progressStore, costCatalog);
        }
    }
}
