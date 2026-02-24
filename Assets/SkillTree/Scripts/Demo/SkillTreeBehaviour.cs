using System;
using System.Collections.Generic;
using SkillTree.Core;
using UnityEngine;


namespace SkillTree.Demo
{
    public class SkillTreeBehaviour : MonoBehaviour
    {
        [SerializeField] private List<SkillSO> _skillData;
        [SerializeField] private ResourceCatalogSO _resourceCatalog;
        [SerializeField] private int _startingGold = 1200;
        [SerializeField] private int _startingEssence = 30;

        private SkillTreeSession _session;

        public SkillTreeSession Session => _session;
        public SkillTreeService Service => _session?.Service;
        public WalletContext Wallet => _session?.Wallet;
        public ICostCatalog Catalog => _session?.Catalog;
        public ISkillProgressStore Store => _session?.Store;
        public IReadOnlyList<SkillSO> SkillData => _skillData;

        private void Awake()
        {
            Dictionary<string, int> initialBalances = new()
            {
                ["gold"] = Mathf.Max(0, _startingGold),
                ["essence"] = Mathf.Max(0, _startingEssence),
            };

            _session = new SkillTreeSession(_skillData, _resourceCatalog, initialBalances);
        }

        public SkillTreePresenter CreatePresenter()
        {
            if (_session == null)
                throw new InvalidOperationException("SkillTreeSession is not initialized.");

            return new SkillTreePresenter(_skillData, _session.Service, _session.Wallet);
        }
    }
}
