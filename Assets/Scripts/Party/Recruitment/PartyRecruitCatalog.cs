using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Party.Recruitment
{
  [CreateAssetMenu(fileName = "PartyRecruitCatalog", menuName = "JRogue/Party Recruit Catalog")]
  public sealed class PartyRecruitCatalog : ScriptableObject
  {
    const string DefaultResourcePath = "Party/PartyRecruitCatalog";

    [SerializeField] PartyRecruitDefinition[] entries = System.Array.Empty<PartyRecruitDefinition>();

    public IReadOnlyList<PartyRecruitDefinition> Entries => entries;

    public static PartyRecruitCatalog LoadDefault()
    {
      PartyRecruitCatalog catalog = Resources.Load<PartyRecruitCatalog>(DefaultResourcePath);
      return catalog != null ? catalog : PartyRecruitCatalogBootstrap.RuntimeInstance;
    }

    public PartyRecruitDefinition FindById(string recruitId)
    {
      if (string.IsNullOrEmpty(recruitId) || entries == null)
        return null;

      for (int i = 0; i < entries.Length; i++)
      {
        PartyRecruitDefinition entry = entries[i];
        if (entry != null && entry.recruitId == recruitId)
          return entry;
      }

      return null;
    }

    public List<PartyRecruitDefinition> GetAvailableEntries(PartyRecruitBoardState board)
    {
      var available = new List<PartyRecruitDefinition>();
      if (entries == null)
        return available;

      for (int i = 0; i < entries.Length; i++)
      {
        PartyRecruitDefinition entry = entries[i];
        if (entry == null || string.IsNullOrEmpty(entry.recruitId))
          continue;

        if (board != null && board.IsRecruited(entry.recruitId))
          continue;

        available.Add(entry);
      }

      return available;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void ConfigureEntriesForTests(PartyRecruitDefinition[] replacement) =>
      entries = replacement ?? System.Array.Empty<PartyRecruitDefinition>();
#else
    internal void ConfigureEntriesForTests(PartyRecruitDefinition[] replacement) =>
      entries = replacement ?? System.Array.Empty<PartyRecruitDefinition>();
#endif
  }
}
