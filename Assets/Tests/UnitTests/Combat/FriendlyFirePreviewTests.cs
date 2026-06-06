using System.Collections.Generic;
using JRogue.Ability.Fireball;
using JRogue.Ability.Heal;
using JRogue.Ability.ThrowingKnife;
using JRogue.Actors;
using JRogue.Combat.FriendlyFire;
using JRogue.Manager.Party;
using JRogue.Stats;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Combat
{
    [TestFixture]
    public sealed class FriendlyFirePreviewTests
    {
        [Test]
        public void IsLivingPartyAlly_ExcludesCasterAndNonParty()
        {
            var party = new GameObject("Party").AddComponent<PartyManager>();
            var caster = CreateActor("Caster");
            var ally = CreateActor("Ally");
            var stranger = CreateActor("Stranger");
            party.partyMembers = new List<BaseActor> { caster, ally };

            Assert.IsFalse(FriendlyFirePreview.IsLivingPartyAlly(caster, caster, party));
            Assert.IsTrue(FriendlyFirePreview.IsLivingPartyAlly(caster, ally, party));
            Assert.IsFalse(FriendlyFirePreview.IsLivingPartyAlly(caster, stranger, party));

            Object.DestroyImmediate(party.gameObject);
            Object.DestroyImmediate(caster.gameObject);
            Object.DestroyImmediate(ally.gameObject);
            Object.DestroyImmediate(stranger.gameObject);
        }

        [Test]
        public void OrderByPartyRoster_PreservesPartyOrder()
        {
            var party = new GameObject("Party").AddComponent<PartyManager>();
            var first = CreateActor("First");
            var second = CreateActor("Second");
            var third = CreateActor("Third");
            party.partyMembers = new List<BaseActor> { first, second, third };

            var harmed = new HashSet<BaseActor> { third, first };
            List<BaseActor> ordered = FriendlyFirePreview.OrderByPartyRoster(harmed, party);

            Assert.AreEqual(2, ordered.Count);
            Assert.AreEqual(first, ordered[0]);
            Assert.AreEqual(third, ordered[1]);

            Object.DestroyImmediate(party.gameObject);
            Object.DestroyImmediate(first.gameObject);
            Object.DestroyImmediate(second.gameObject);
            Object.DestroyImmediate(third.gameObject);
        }

        [Test]
        public void FireballAbility_WouldHarm_WhenDamagePositive()
        {
            var ability = ScriptableObject.CreateInstance<FireballAbility>();
            ability.fireDamage = 15;
            var target = CreateActor("Target");

            Assert.IsTrue(ability.WouldHarm(target, null));

            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(target.gameObject);
        }

        [Test]
        public void HealingPotionAbility_WouldHarm_IsFalse()
        {
            var ability = ScriptableObject.CreateInstance<HealingPotionAbility>();
            var target = CreateActor("Target");

            Assert.IsFalse(ability.WouldHarm(target, null));

            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(target.gameObject);
        }

        [Test]
        public void ThrowingKnifeAbility_WouldHarm_RespectsCanHurtAllies()
        {
            var ability = ScriptableObject.CreateInstance<ThrowingKnifeAbility>();
            ability.pierceDamage = 10;
            ability.canHurtAllies = false;

            var party = new GameObject("Party").AddComponent<PartyManager>();
            var caster = CreateActor("Caster");
            var ally = CreateActor("Ally");
            party.partyMembers = new List<BaseActor> { caster, ally };

            Assert.IsFalse(ability.WouldHarm(ally, caster.gameObject));

            ability.canHurtAllies = true;
            Assert.IsTrue(ability.WouldHarm(ally, caster.gameObject));

            Object.DestroyImmediate(party.gameObject);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(caster.gameObject);
            Object.DestroyImmediate(ally.gameObject);
        }

        static BaseActor CreateActor(string name)
        {
            var go = new GameObject(name);
            go.AddComponent<CharacterStats>();
            return go.AddComponent<TestActor>();
        }

        sealed class TestActor : BaseActor
        {
            protected override void Die() { }
            protected override void OnBump(BaseActor target) { }
        }
    }
}
