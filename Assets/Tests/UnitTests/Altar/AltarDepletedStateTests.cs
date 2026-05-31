using JRogue.World.Altar;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Altar
{
    [TestFixture]
    public sealed class AltarDepletedStateTests
    {
        [Test]
        public void MarkRuleFired_SetsDepletedAndClearsOfferingsOnCompletionRunner()
        {
            var tier9 = ScriptableObject.CreateInstance<ManaStoneTierAcceptFilter>();
            tier9.tier = 9;
            var tier8 = ScriptableObject.CreateInstance<ManaStoneTierAcceptFilter>();
            tier8.tier = 8;

            var definition = ScriptableObject.CreateInstance<AltarDefinition>();
            definition.slots = new[]
            {
                new AltarSlotDefinition { slotId = "tier9", acceptFilter = tier9 },
                new AltarSlotDefinition { slotId = "tier8", acceptFilter = tier8 },
            };
            definition.completionRules = new[]
            {
                new AltarCompletionRule { ruleId = "test", effects = System.Array.Empty<AltarCompletionEffect>() },
            };

            var instance = new AltarInstance(Vector3Int.zero, definition);
            instance.Slots[0].Offering = new AltarManaStoneOffering(9, "skeleton");
            instance.Slots[1].Offering = new AltarManaStoneOffering(8, "skeleton");

            AltarCompletionRunner.TryFireCompletion(instance);

            Assert.IsTrue(instance.IsDepleted);
            Assert.IsTrue(instance.Slots[0].IsEmpty);
            Assert.IsTrue(instance.Slots[1].IsEmpty);
        }

        [Test]
        public void TryPlace_WhenDepleted_Fails()
        {
            var instance = new AltarInstance(Vector3Int.zero, ScriptableObject.CreateInstance<AltarDefinition>());
            instance.MarkRuleFired("used");

            Assert.AreEqual(
                AltarOfferingResult.Failed,
                AltarOfferingService.TryPlaceManaStone(instance, 9, "skeleton"));
        }
    }
}
