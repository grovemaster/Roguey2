using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Manager.Loot;
using JRogue.Manager.Party;
using JRogue.Organizations;
using JRogue.Shop;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.Party.Recruitment
{
  public readonly struct PartyRecruitOptionView
  {
    public PartyRecruitOptionView(
      PartyRecruitDefinition recruit,
      int goldCost,
      bool canSelect,
      string denyReason)
    {
      Recruit = recruit;
      GoldCost = goldCost;
      CanSelect = canSelect;
      DenyReason = denyReason;
    }

    public PartyRecruitDefinition Recruit { get; }
    public int GoldCost { get; }
    public bool CanSelect { get; }
    public string DenyReason { get; }
  }

  public static class PartyRecruitmentService
  {
    static readonly IRecruitCostCalculator CostCalculator = new EssenceCountGoldRecruitCostCalculator();

    public static IRecruitCostCalculator DefaultCostCalculator => CostCalculator;

    public static int GetRecruitCost(PartyRecruitDefinition recruit)
    {
      if (!CostCalculator.TryCalculate(recruit, out int goldCost, out _))
        return 0;

      return goldCost;
    }

    public static List<PartyRecruitOptionView> GetRecruitOptions(
      OrganizationDefinition organization,
      PartyManager party,
      PartyRecruitCatalog catalog = null,
      PartyRecruitBoardState board = null)
    {
      var options = new List<PartyRecruitOptionView>();
      if (organization == null || party == null)
        return options;

      catalog ??= PartyRecruitCatalog.LoadDefault();
      if (catalog == null)
        return options;

      board ??= PartyRecruitBoardState.Instance;
      PartyCapacityService capacity = PartyCapacityService.Instance;
      bool hasCapacity = capacity == null || capacity.CanAddMember(party);
      int partyRank = OrganizationRankService.GetPartyRank(organization, party);
      int partyGold = ShopGoldUtility.GetPartyGoldTotal();

      List<PartyRecruitDefinition> available = catalog.GetAvailableEntries(board);
      for (int i = 0; i < available.Count; i++)
      {
        PartyRecruitDefinition recruit = available[i];
        if (recruit == null)
          continue;

        if (!CostCalculator.TryCalculate(recruit, out int goldCost, out _))
          continue;

        string denyReason = null;
        if (!hasCapacity)
          denyReason = GetPartyFullReason(capacity, party);
        else if (!PartyRecruitmentLogic.IsRankEligible(partyRank, recruit.guildRank))
          denyReason = PartyRecruitmentLogic.GetRankDenyReason(partyRank, recruit.guildRank);
        else if (!PartyRecruitmentLogic.IsGoldEligible(partyGold, goldCost))
          denyReason = PartyRecruitmentLogic.GetGoldDenyReason(partyGold, goldCost);

        bool canSelect = denyReason == null;
        options.Add(new PartyRecruitOptionView(recruit, goldCost, canSelect, denyReason));
      }

      return options;
    }

    public static bool HasAvailableRecruitsOnBoard(
      PartyRecruitCatalog catalog = null,
      PartyRecruitBoardState board = null)
    {
      catalog ??= PartyRecruitCatalog.LoadDefault();
      if (catalog == null)
        return false;

      board ??= PartyRecruitBoardState.Instance;
      return catalog.GetAvailableEntries(board).Count > 0;
    }

    public static bool CanOpenRecruitMenu(PartyManager party)
    {
      if (party == null)
        return false;

      PartyCapacityService capacity = PartyCapacityService.Instance;
      if (capacity != null && !capacity.CanAddMember(party))
        return false;

      return HasAvailableRecruitsOnBoard();
    }

    public static bool TryRecruit(
      OrganizationDefinition organization,
      PartyManager party,
      string recruitId,
      out string message,
      PartyRecruitCatalog catalog = null,
      PartyRecruitBoardState board = null)
    {
      message = null;
      if (organization == null || party == null || string.IsNullOrEmpty(recruitId))
      {
        message = "Recruitment is unavailable.";
        return false;
      }

      catalog ??= PartyRecruitCatalog.LoadDefault();
      if (catalog == null)
      {
        message = "The guild recruit roster is unavailable.";
        return false;
      }

      board ??= PartyRecruitBoardState.Instance;
      if (board == null)
      {
        message = "The guild recruit board is unavailable.";
        return false;
      }

      PartyRecruitDefinition recruit = catalog.FindById(recruitId);
      if (recruit == null)
      {
        message = "That adventurer is not on the roster.";
        return false;
      }

      if (board.IsRecruited(recruitId))
      {
        message = "That adventurer has already joined a party.";
        return false;
      }

      PartyCapacityService capacity = PartyCapacityService.Instance;
      if (capacity != null && !capacity.CanAddMember(party))
      {
        message = GetPartyFullReason(capacity, party);
        return false;
      }

      int partyRank = OrganizationRankService.GetPartyRank(organization, party);
      if (!PartyRecruitmentLogic.IsRankEligible(partyRank, recruit.guildRank))
      {
        message = PartyRecruitmentLogic.GetRankDenyReason(partyRank, recruit.guildRank);
        return false;
      }

      if (!CostCalculator.TryCalculate(recruit, out int goldCost, out _))
      {
        message = "Unable to determine recruitment cost.";
        return false;
      }

      int partyGold = ShopGoldUtility.GetPartyGoldTotal();
      if (!PartyRecruitmentLogic.IsGoldEligible(partyGold, goldCost))
      {
        message = PartyRecruitmentLogic.GetGoldDenyReason(partyGold, goldCost);
        return false;
      }

      if (!ShopGoldUtility.TrySpendPartyGold(goldCost))
      {
        message = PartyRecruitmentLogic.GetGoldDenyReason(
          ShopGoldUtility.GetPartyGoldTotal(),
          goldCost);
        return false;
      }

      Transform parent = ResolvePartyParent(party);
      BaseActor actor = PartyRecruitActorFactory.Create(recruit, parent, organization);
      if (actor == null)
      {
        ShopGoldUtility.AddPartyGold(goldCost);
        message = "Failed to prepare that adventurer.";
        return false;
      }

      party.partyMembers.Add(actor);
      board.MarkRecruited(recruitId);

      if (!PartySpawnService.TryPlaceRecruitNearParty(actor, party))
      {
        Debug.LogWarning(
          $"[PartyRecruit] Could not find an open tile near the party for {actor.DisplayName}.");
      }

      party.InitializeRosterAfterDeferredSpawn();
      party.RefreshCameraFollow();
      ManaStoneAutoPickupService.Instance?.SubscribePartyMembers();
      PortalEntryService.Instance?.SubscribePartyMembers();

      message = $"{actor.DisplayName} has joined your party.";
      return true;
    }

    static string GetPartyFullReason(PartyCapacityService capacity, PartyManager party)
    {
      int living = capacity.GetLivingMemberCount(party);
      return $"Your party is full ({living}/{capacity.MaxPartyMembers}).";
    }

    static Transform ResolvePartyParent(PartyManager party)
    {
      DungeonRunBootstrap bootstrap = Object.FindAnyObjectByType<DungeonRunBootstrap>();
      if (bootstrap != null && bootstrap.PartyContainer != null)
        return bootstrap.PartyContainer;

      return party != null ? party.transform : null;
    }
  }
}
