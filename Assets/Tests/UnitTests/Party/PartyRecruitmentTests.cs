using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Controller.Player;
using JRogue.Item.Essence;
using JRogue.Manager.Essence;
using JRogue.Manager.Party;
using JRogue.Organizations;
using JRogue.Party.Recruitment;
using JRogue.Shop;
using JRogue.Stats;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Party
{
  [TestFixture]
  public sealed class PartyRecruitmentTests
  {
    readonly List<GameObject> _created = new List<GameObject>();
    readonly List<Object> _assets = new List<Object>();

    OrganizationDefinition _guild;
    PartyRecruitCatalog _catalog;

    [SetUp]
    public void SetUp()
    {
      OrganizationRankScoreService.ResetContributorsForTests();
      _guild = CreateGuildDefinition();
      _catalog = CreateCatalog();
    }

    [TearDown]
    public void TearDown()
    {
      ClearSingleton<PartyCapacityService>();
      ClearSingleton<PartyRecruitBoardState>();
      ClearSingleton<PartyCurrencyLedger>();
      PartyManager.Instance = null;

      foreach (GameObject go in _created)
      {
        if (go != null)
          Object.DestroyImmediate(go);
      }

      _created.Clear();

      foreach (Object asset in _assets)
      {
        if (asset != null)
          Object.DestroyImmediate(asset);
      }

      _assets.Clear();
    }

    [Test]
    public void EssenceCountGoldCalculator_RankNineCost_IsOne()
    {
      var recruit = new PartyRecruitDefinition
      {
        recruitId = "test",
        essences = System.Array.Empty<EssenceData>(),
      };

      var calculator = new EssenceCountGoldRecruitCostCalculator();
      Assert.IsTrue(calculator.TryCalculate(recruit, out int goldCost, out _));
      Assert.AreEqual(1, goldCost);
    }

    [Test]
    public void EssenceCountGoldCalculator_RankEightCost_IsFour()
    {
      var recruit = new PartyRecruitDefinition
      {
        recruitId = "test",
        essences = new[] { CreateEssence(9), CreateEssence(9), CreateEssence(9) },
      };

      var calculator = new EssenceCountGoldRecruitCostCalculator();
      Assert.IsTrue(calculator.TryCalculate(recruit, out int goldCost, out _));
      Assert.AreEqual(4, goldCost);
    }

    [Test]
    public void RankEligibility_PartyNine_CannotRecruitRankEight()
    {
      Assert.IsFalse(PartyRecruitmentLogic.IsRankEligible(9, 8));
    }

    [Test]
    public void RankEligibility_PartyEight_CanRecruitRankEight()
    {
      Assert.IsTrue(PartyRecruitmentLogic.IsRankEligible(8, 8));
    }

    [Test]
    public void RankEligibility_PartySeven_CanRecruitRankEight()
    {
      Assert.IsTrue(PartyRecruitmentLogic.IsRankEligible(7, 8));
    }

    [Test]
    public void PartyCapacity_DefaultMax_IsFive()
    {
      PartyCapacityService capacity = CreateCapacityService();
      Assert.AreEqual(PartyCapacityService.DefaultMaxPartyMembers, capacity.MaxPartyMembers);
    }

    [Test]
    public void PartyCapacity_SetMaxPartyMembers_ClampsToSix()
    {
      PartyCapacityService capacity = CreateCapacityService();
      capacity.SetMaxPartyMembers(99);
      Assert.AreEqual(6, capacity.MaxPartyMembers);
    }

    [Test]
    public void PartyCapacity_LivingCount_IgnoresDeadMembers()
    {
      PartyManager party = CreatePartyManager(out PartyCapacityService capacity);
      BaseActor living = CreateGuildMember(rank: 9);
      BaseActor dead = CreateGuildMember(rank: 9);
      dead.stats.currentHP = 0;

      party.partyMembers.Add(living);
      party.partyMembers.Add(dead);

      Assert.AreEqual(1, capacity.GetLivingMemberCount(party));
    }

    [Test]
    public void CanAddMember_BlockedAtCapacity()
    {
      PartyManager party = CreatePartyManager(out PartyCapacityService capacity);
      capacity.SetMaxPartyMembers(2);

      party.partyMembers.Add(CreateGuildMember(rank: 9));
      party.partyMembers.Add(CreateGuildMember(rank: 9));

      Assert.IsFalse(capacity.CanAddMember(party));
    }

    [Test]
    public void GetRecruitOptions_RankEightRecruitDisabledWhenPartyRankNine()
    {
      PartyManager party = CreatePartyManager(out _);
      party.partyMembers.Add(CreateGuildMember(rank: 9));

      List<PartyRecruitOptionView> options = PartyRecruitmentService.GetRecruitOptions(
        _guild,
        party,
        _catalog,
        CreateBoardState());

      PartyRecruitOptionView rankEight = FindOption(options, "guild_recruit_08_human");
      Assert.IsFalse(rankEight.CanSelect);
      Assert.IsNotNull(rankEight.DenyReason);
    }

    [Test]
    public void GetRecruitOptions_RankEightRecruitEnabledWhenPartyRankEight()
    {
      PartyManager party = CreatePartyManager(out _);
      party.partyMembers.Add(CreateGuildMember(rank: 8));
      ShopGoldUtility.AddPartyGold(10);

      List<PartyRecruitOptionView> options = PartyRecruitmentService.GetRecruitOptions(
        _guild,
        party,
        _catalog,
        CreateBoardState());

      PartyRecruitOptionView rankEight = FindOption(options, "guild_recruit_08_human");
      Assert.IsTrue(rankEight.CanSelect);
      Assert.AreEqual(4, rankEight.GoldCost);
    }

    [Test]
    public void TryRecruit_SpendsGoldAndMarksBoard()
    {
      PartyManager party = CreatePartyManager(out PartyCapacityService capacity);
      party.partyMembers.Add(CreateGuildMember(rank: 9));
      PartyRecruitBoardState board = CreateBoardState();
      ShopGoldUtility.AddPartyGold(10);

      GameObject prefab = CreateRecruitPrefab();
      PatchCatalogPrefab("guild_recruit_09_human", prefab);

      int goldBefore = ShopGoldUtility.GetPartyGoldTotal();
      bool success = PartyRecruitmentService.TryRecruit(
        _guild,
        party,
        "guild_recruit_09_human",
        out string message,
        _catalog,
        board);

      Assert.IsTrue(success, message);
      Assert.AreEqual(goldBefore - 1, ShopGoldUtility.GetPartyGoldTotal());
      Assert.IsTrue(board.IsRecruited("guild_recruit_09_human"));
      Assert.AreEqual(2, party.partyMembers.Count);
      Assert.IsFalse(PartyRecruitmentService.TryRecruit(
        _guild,
        party,
        "guild_recruit_09_human",
        out _,
        _catalog,
        board));
    }

    [Test]
    public void TryRecruit_InsufficientGold_DoesNotMarkBoard()
    {
      PartyManager party = CreatePartyManager(out _);
      party.partyMembers.Add(CreateGuildMember(rank: 9));
      PartyRecruitBoardState board = CreateBoardState();
      ShopGoldUtility.AddPartyGold(0);

      GameObject prefab = CreateRecruitPrefab();
      PatchCatalogPrefab("guild_recruit_09_human", prefab);

      Assert.IsFalse(PartyRecruitmentService.TryRecruit(
        _guild,
        party,
        "guild_recruit_09_human",
        out _,
        _catalog,
        board));
      Assert.IsFalse(board.IsRecruited("guild_recruit_09_human"));
      Assert.AreEqual(1, party.partyMembers.Count);
    }

    static PartyRecruitOptionView FindOption(List<PartyRecruitOptionView> options, string recruitId)
    {
      for (int i = 0; i < options.Count; i++)
      {
        if (options[i].Recruit.recruitId == recruitId)
          return options[i];
      }

      Assert.Fail($"Missing recruit option {recruitId}.");
      return default;
    }

    void PatchCatalogPrefab(string recruitId, GameObject prefab)
    {
      PartyRecruitDefinition recruit = _catalog.FindById(recruitId);
      Assert.IsNotNull(recruit);
      recruit.actorPrefab = prefab;
    }

    PartyRecruitCatalog CreateCatalog()
    {
      var catalog = ScriptableObject.CreateInstance<PartyRecruitCatalog>();
      _assets.Add(catalog);
      catalog.ConfigureEntriesForTests(new[]
      {
        CreateRecruitDefinition("guild_recruit_09_human", "Human Adventurer", 9, null),
        CreateRecruitDefinition("guild_recruit_09_elf", "Elf Adventurer", 9, null),
        CreateRecruitDefinition("guild_recruit_09_barbarian", "Barbarian Adventurer", 9, null),
        CreateRecruitDefinition("guild_recruit_08_human", "Human Adventurer", 8, new[]
        {
          CreateEssence(9),
          CreateEssence(9),
          CreateEssence(9),
        }),
        CreateRecruitDefinition("guild_recruit_08_elf", "Elf Adventurer", 8, new[]
        {
          CreateEssence(9),
          CreateEssence(9),
          CreateEssence(9),
        }),
      });
      return catalog;
    }

    PartyRecruitDefinition CreateRecruitDefinition(
      string recruitId,
      string displayName,
      int guildRank,
      EssenceData[] essences) =>
      new()
      {
        recruitId = recruitId,
        displayName = displayName,
        guildRank = guildRank,
        essences = essences ?? System.Array.Empty<EssenceData>(),
      };

    PartyCapacityService CreateCapacityService()
    {
      var go = new GameObject("PartyCapacity");
      _created.Add(go);
      return go.AddComponent<PartyCapacityService>();
    }

    PartyRecruitBoardState CreateBoardState()
    {
      var go = new GameObject("PartyRecruitBoard");
      _created.Add(go);
      return go.AddComponent<PartyRecruitBoardState>();
    }

    PartyManager CreatePartyManager(out PartyCapacityService capacity)
    {
      var go = new GameObject("PartyManager");
      _created.Add(go);
      PartyManager party = go.AddComponent<PartyManager>();
      PartyManager.Instance = party;
      capacity = go.AddComponent<PartyCapacityService>();
      go.AddComponent<PartyRecruitBoardState>();
      var ledgerGo = new GameObject("Ledger");
      _created.Add(ledgerGo);
      ledgerGo.AddComponent<PartyCurrencyLedger>();
      return party;
    }

    GameObject CreateRecruitPrefab()
    {
      GameObject go = new GameObject("RecruitPrefab");
      _created.Add(go);
      go.AddComponent<CharacterStats>();
      go.AddComponent<EssenceSlotManager>();
      go.AddComponent<JRogue.Actors.Components.GridMover>();
      go.AddComponent<PlayerController>();
      OrganizationMembershipRuntime.EnsureOn(go);
      return go;
    }

    static void ClearSingleton<T>() where T : MonoBehaviour
    {
      typeof(T)
        .GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        ?.SetValue(null, null);
    }

    OrganizationDefinition CreateGuildDefinition()
    {
      var organization = ScriptableObject.CreateInstance<OrganizationDefinition>();
      _assets.Add(organization);
      organization.organizationId = OrganizationIds.AdventurersGuild;
      organization.rankBest = 1;
      organization.rankWorst = 9;
      organization.defaultStartingRank = 9;
      organization.rankThresholds = new[] { 0, 3, 6, 9, 12, 15, 18, 21, 24 };
      return organization;
    }

    BaseActor CreateGuildMember(int rank)
    {
      GameObject go = new GameObject("GuildMember");
      _created.Add(go);
      go.AddComponent<CharacterStats>();
      go.AddComponent<EssenceSlotManager>();
      OrganizationMembershipRuntime membership = OrganizationMembershipRuntime.EnsureOn(go);
      membership.EnsureMembership(_guild, rank);
      return go.AddComponent<PlayerController>();
    }

    EssenceData CreateEssence(int tier)
    {
      var essence = ScriptableObject.CreateInstance<EssenceData>();
      _assets.Add(essence);
      essence.tier = tier;
      return essence;
    }
  }
}
