using System.Collections.Generic;


namespace SkillTree.Core
{
    public interface ISkillProgressStore
    {
        int GetLevel(string skillId);
        IReadOnlyDictionary<string, int> LoadAll(IEnumerable<string> skillIds);
        void SaveAll(IReadOnlyDictionary<string, int> levels);
        void Clear(IEnumerable<string> skillIds);
    }
}
