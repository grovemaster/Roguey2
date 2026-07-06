using System;
using JRogue.Item.Essence;
using UnityEngine;

namespace JRogue.Party.Recruitment
{
  [Serializable]
  public sealed class PartyRecruitDefinition
  {
    public string recruitId;
    public string displayName;
    public GameObject actorPrefab;
    public int guildRank = 9;
    public EssenceData[] essences = Array.Empty<EssenceData>();

    public int EssenceCount
    {
      get
      {
        if (essences == null || essences.Length == 0)
          return 0;

        int count = 0;
        for (int i = 0; i < essences.Length; i++)
        {
          if (essences[i] != null)
            count++;
        }

        return count;
      }
    }
  }
}
