using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Manager.Essence;
using JRogue.Manager.Party;
using JRogue.Status;
using JRogue.Stats;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Party
{
    [TestFixture]
    public sealed class PartyMemberDeathTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            PartyMemberDeathService.ResetForTests();

            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _created.Clear();

            if (PartyManager.Instance != null)
                Object.DestroyImmediate(PartyManager.Instance.gameObject);
        }

        [Test]
        public void TakeDamage_Overkill_ClampsHpToZero()
        {
            GameObject go = CreateActorWithHealth(hp: 5);
            HealthComponent health = go.GetComponent<HealthComponent>();

            health.TakeDamage(999, DamageType.Blunt);

            Assert.AreEqual(0, go.GetComponent<CharacterStats>().currentHP);
        }

        [Test]
        public void RemovePartyMember_RemovesFromList_AndPromotesLeader()
        {
            PartyManager party = CreatePartyManager();
            BaseActor leader = CreatePartyActor("Leader");
            BaseActor follower = CreatePartyActor("Follower");
            party.partyMembers.Add(leader);
            party.partyMembers.Add(follower);

            Assert.IsTrue(party.RemovePartyMember(leader));
            Assert.AreEqual(1, party.partyMembers.Count);
            Assert.AreEqual(follower, party.partyMembers[0]);
            Assert.AreEqual(follower, party.GetActiveMember());
        }

        [Test]
        public void RemovePartyMember_Follower_KeepsLeader()
        {
            PartyManager party = CreatePartyManager();
            BaseActor leader = CreatePartyActor("Leader");
            BaseActor follower = CreatePartyActor("Follower");
            party.partyMembers.Add(leader);
            party.partyMembers.Add(follower);

            Assert.IsTrue(party.RemovePartyMember(follower));
            Assert.AreEqual(1, party.partyMembers.Count);
            Assert.AreEqual(leader, party.GetActiveMember());
        }

        GameObject CreateActorWithHealth(int hp)
        {
            GameObject go = new GameObject("TestActor");
            _created.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.Constitution = new Stat(hp / 10 + 1);
            stats.currentHP = hp;
            go.AddComponent<HealthComponent>();
            return go;
        }

        BaseActor CreatePartyActor(string name)
        {
            GameObject go = new GameObject(name);
            _created.Add(go);
            go.AddComponent<CharacterStats>();
            go.AddComponent<HealthComponent>();
            go.AddComponent<EssenceSlotManager>();
            go.AddComponent<GridMover>();
            go.AddComponent<StatusEffectController>();
            return go.AddComponent<TestPartyActor>();
        }

        sealed class TestPartyActor : BaseActor
        {
            protected override void Die() { }
        }

        static PartyManager CreatePartyManager()
        {
            var go = new GameObject("PartyManager");
            Object.DontDestroyOnLoad(go);
            PartyManager pm = go.AddComponent<PartyManager>();
            pm.partyMembers = new List<BaseActor>();
            return pm;
        }
    }
}
