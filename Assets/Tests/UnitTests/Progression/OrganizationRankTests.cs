using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Item.Essence;
using JRogue.Manager.Essence;
using JRogue.Organizations;
using JRogue.Stats;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Progression
{
    [TestFixture]
    public sealed class OrganizationRankTests
    {
        readonly List<GameObject> _created = new List<GameObject>();
        readonly List<Object> _assets = new List<Object>();

        OrganizationDefinition _guild;

        [SetUp]
        public void SetUp()
        {
            OrganizationRankScoreService.ResetContributorsForTests();
            _guild = CreateGuildDefinition();
        }

        [TearDown]
        public void TearDown()
        {
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
        public void Thresholds_MatchGuildTable()
        {
            Assert.AreEqual(0, _guild.GetThresholdForRank(9));
            Assert.AreEqual(3, _guild.GetThresholdForRank(8));
            Assert.AreEqual(24, _guild.GetThresholdForRank(1));
        }

        [Test]
        public void ThreeTierNineEssences_YieldThreePoints()
        {
            BaseActor actor = CreateGuildMember(rank: 9);
            EquipEssences(actor, CreateEssence(9), CreateEssence(9), CreateEssence(9));

            Assert.AreEqual(3, OrganizationRankScoreService.GetScore(OrganizationIds.AdventurersGuild, actor));
        }

        [Test]
        public void RankNineWithThreePoints_CanTargetRankEight()
        {
            BaseActor actor = CreateGuildMember(rank: 9);
            EquipEssences(actor, CreateEssence(9), CreateEssence(9), CreateEssence(9));

            Assert.IsTrue(
                OrganizationRankService.CanRankUp(_guild, actor, out int targetRank, out _),
                "Expected rank-up eligibility.");
            Assert.AreEqual(8, targetRank);
        }

        [Test]
        public void RankNineWithTwoPoints_CannotRankUp()
        {
            BaseActor actor = CreateGuildMember(rank: 9);
            EquipEssences(actor, CreateEssence(9), CreateEssence(9));

            Assert.IsFalse(OrganizationRankService.CanRankUp(_guild, actor, out _, out _));
        }

        [Test]
        public void AfterRankUpToEight_ThreePoints_CannotReachRankSeven()
        {
            BaseActor actor = CreateGuildMember(rank: 8);
            EquipEssences(actor, CreateEssence(9), CreateEssence(9), CreateEssence(9));

            Assert.IsFalse(OrganizationRankService.CanRankUp(_guild, actor, out _, out _));
        }

        [Test]
        public void TryRankUp_AtRankOne_Fails()
        {
            BaseActor actor = CreateGuildMember(rank: 1);
            EquipEssences(actor, CreateEssence(1), CreateEssence(1), CreateEssence(1));

            Assert.IsFalse(OrganizationRankService.TryRankUp(_guild, actor));
        }

        [Test]
        public void PartyRank_ForRanksNineNineEightSeven_IsEight()
        {
            var members = new List<BaseActor>
            {
                CreateGuildMember(rank: 9),
                CreateGuildMember(rank: 9),
                CreateGuildMember(rank: 8),
                CreateGuildMember(rank: 7),
            };

            Assert.AreEqual(8, OrganizationRankLogic.GetPartyRank(members, OrganizationIds.AdventurersGuild));
        }

        [Test]
        public void PartyRank_ExcludesNonMembers()
        {
            var members = new List<BaseActor>
            {
                CreateGuildMember(rank: 9),
                CreateActorWithoutMembership(),
            };

            Assert.AreEqual(9, OrganizationRankLogic.GetPartyRank(members, OrganizationIds.AdventurersGuild));
        }

        [Test]
        public void TryRankUp_ChangesStoredRank()
        {
            BaseActor actor = CreateGuildMember(rank: 9);
            EquipEssences(actor, CreateEssence(9), CreateEssence(9), CreateEssence(9));

            Assert.IsTrue(OrganizationRankService.TryRankUp(_guild, actor));
            Assert.IsTrue(actor.GetComponent<OrganizationMembershipRuntime>().TryGetRank(
                OrganizationIds.AdventurersGuild,
                out int rank));
            Assert.AreEqual(8, rank);
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
            BaseActor actor = CreateActorWithoutMembership();
            OrganizationMembershipRuntime membership = actor.GetComponent<OrganizationMembershipRuntime>();
            membership.EnsureMembership(_guild, rank);
            return actor;
        }

        BaseActor CreateActorWithoutMembership()
        {
            GameObject go = new GameObject("TestActor");
            _created.Add(go);
            go.AddComponent<CharacterStats>();
            go.AddComponent<EssenceSlotManager>();
            OrganizationMembershipRuntime.EnsureOn(go);
            return go.AddComponent<BaseActor>();
        }

        EssenceData CreateEssence(int tier)
        {
            var essence = ScriptableObject.CreateInstance<EssenceData>();
            _assets.Add(essence);
            essence.tier = tier;
            return essence;
        }

        static void EquipEssences(BaseActor actor, params EssenceData[] essences)
        {
            EssenceSlotManager slots = actor.GetComponent<EssenceSlotManager>();
            for (int i = 0; i < essences.Length; i++)
                slots.EquipEssence(essences[i], i);
        }
    }
}
