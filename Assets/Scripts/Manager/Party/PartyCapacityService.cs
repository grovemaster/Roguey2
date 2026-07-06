using System;
using JRogue.Actors;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Manager.Party
{
  public sealed class PartyCapacityService : MonoBehaviour
  {
    public static PartyCapacityService Instance { get; private set; }

    public const int DefaultMaxPartyMembers = 5;
    public const int FormationAbsoluteMax = 6;

    [SerializeField] int maxPartyMembers = DefaultMaxPartyMembers;

    public event Action Changed;

    public int MaxPartyMembers => maxPartyMembers;

    void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }

      Instance = this;
    }

    void OnDestroy()
    {
      if (Instance == this)
        Instance = null;
    }

    public void SetMaxPartyMembers(int value)
    {
      int clamped = Mathf.Clamp(value, 1, FormationAbsoluteMax);
      if (maxPartyMembers == clamped)
        return;

      maxPartyMembers = clamped;
      Changed?.Invoke();
    }

    public int GetLivingMemberCount(PartyManager party)
    {
      if (party?.partyMembers == null)
        return 0;

      int count = 0;
      for (int i = 0; i < party.partyMembers.Count; i++)
      {
        BaseActor member = party.partyMembers[i];
        if (member == null)
          continue;

        CharacterStats stats = member.stats;
        if (stats != null && stats.currentHP <= 0)
          continue;

        count++;
      }

      return count;
    }

    public bool CanAddMember(PartyManager party) =>
      GetLivingMemberCount(party) < MaxPartyMembers;
  }
}
