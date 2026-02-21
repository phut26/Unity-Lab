using System.Collections.Generic;
using SkillTree.Core;
using UnityEngine;


namespace SkillTree.Demo
{
    public class SkillPersistenceService : ISkillProgressStore
    {
        public int GetLevel(string skillId) => PlayerPrefs.GetInt(skillId, 0);

        public IReadOnlyDictionary<string, int> LoadAll(IEnumerable<string> skillIds)
        {
            var result = new Dictionary<string, int>();
            foreach (var skillId in skillIds)
            {
                if (PlayerPrefs.HasKey(skillId))
                    result[skillId] = PlayerPrefs.GetInt(skillId);
                else result[skillId] = 0;
            }
            return result;
        }

        public void SaveAll(IReadOnlyDictionary<string, int> levels)
        {
            foreach (var kv in levels)
                PlayerPrefs.SetInt(kv.Key, kv.Value);

            PlayerPrefs.Save();
        }

        public void Clear(IEnumerable<string> skillIds)
        {
            foreach (var id in skillIds)
                PlayerPrefs.DeleteKey(id);
            PlayerPrefs.Save();
#if UNITY_EDITOR
            Debug.Log("Player prefs reset!");
#endif
        }
    }
}
