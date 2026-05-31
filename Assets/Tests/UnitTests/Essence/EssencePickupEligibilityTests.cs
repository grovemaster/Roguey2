using JRogue.Actors;
using JRogue.Item.Essence;
using JRogue.Manager.Essence;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.Tests.UnitTests.Input;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Essence
{
    [TestFixture]
    public class EssencePickupEligibilityTests
    {
        readonly System.Collections.Generic.List<Object> _created =
            new System.Collections.Generic.List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in _created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            _created.Clear();
        }

        [Test]
        public void CanGain_FalseWhenAlreadyEquipped()
        {
            var essence = CreateEssence("Sudden Strength");
            GameObject actor = CreateActorWithSlots(essence);

            bool canGain = EssencePickupEligibility.CanGain(
                actor.GetComponent<BaseActor>(),
                essence,
                out string reason);

            Assert.IsFalse(canGain);
            Assert.That(reason, Does.Contain("already have"));
        }

        [Test]
        public void CanGain_TrueWithFreeSlot()
        {
            var essence = CreateEssence("Sudden Strength");
            GameObject actor = CreateActorWithSlots();

            bool canGain = EssencePickupEligibility.CanGain(
                actor.GetComponent<BaseActor>(),
                essence,
                out _);

            Assert.IsTrue(canGain);
        }

        [Test]
        public void CanGain_FalseWhenSlotsFull()
        {
            var target = CreateEssence("Sudden Strength");
            GameObject actor = CreateActorWithSlots(
                CreateEssence("Other A"),
                CreateEssence("Other B"),
                CreateEssence("Other C"));

            bool canGain = EssencePickupEligibility.CanGain(
                actor.GetComponent<BaseActor>(),
                target,
                out string reason);

            Assert.IsFalse(canGain);
            Assert.That(reason, Does.Contain("maximum"));
        }

        EssenceData CreateEssence(string name)
        {
            var data = ScriptableObject.CreateInstance<EssenceData>();
            data.essenceName = name;
            _created.Add(data);
            return data;
        }

        GameObject CreateActorWithSlots(params EssenceData[] preEquipped)
        {
            var go = new GameObject("TestActor");
            _created.Add(go);

            go.AddComponent<InputTestSceneBuilder.TestPartyActor>();
            var stats = go.GetComponent<CharacterStats>();
            stats.humanClass = HumanClass.Knight;

            EssenceSlotManager slots = go.GetComponent<EssenceSlotManager>();
            slots.ApplyMaxSlotsFromClass();

            for (int i = 0; i < preEquipped.Length; i++)
            {
                if (preEquipped[i] != null)
                    slots.EquipEssence(preEquipped[i], i);
            }

            return go;
        }
    }
}
