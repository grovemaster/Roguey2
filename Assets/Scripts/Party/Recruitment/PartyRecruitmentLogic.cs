namespace JRogue.Party.Recruitment
{
  public static class PartyRecruitmentLogic
  {
    public static bool IsRankEligible(int partyGuildRank, int recruitGuildRank) =>
      partyGuildRank > 0 && partyGuildRank <= recruitGuildRank;

    public static bool IsGoldEligible(int partyGold, int goldCost) =>
      partyGold >= goldCost;

    public static bool CanSelectRecruit(
      int partyGuildRank,
      int partyGold,
      int recruitGuildRank,
      int goldCost,
      bool partyHasCapacity) =>
      partyHasCapacity
      && IsRankEligible(partyGuildRank, recruitGuildRank)
      && IsGoldEligible(partyGold, goldCost);

    public static string GetRankDenyReason(int partyGuildRank, int recruitGuildRank)
    {
      if (partyGuildRank <= 0)
        return "Your party has no registered guild standing.";

      if (partyGuildRank <= recruitGuildRank)
        return null;

      return $"Requires party guild rank {recruitGuildRank} or better (yours: {partyGuildRank}).";
    }

    public static string GetGoldDenyReason(int partyGold, int goldCost)
    {
      if (partyGold >= goldCost)
        return null;

      return $"Not enough gold. Need {goldCost}; you have {partyGold}.";
    }
  }
}
