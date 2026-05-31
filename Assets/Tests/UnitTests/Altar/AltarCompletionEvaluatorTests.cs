using JRogue.World.Altar;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Altar
{
    [TestFixture]
    public sealed class AltarCompletionEvaluatorTests
    {
        [Test]
        public void EmptyAltar_NotSatisfied()
        {
            AltarInstance instance = CreateInstance();
            var rule = new AltarCompletionRule { requiredSlotIds = System.Array.Empty<string>() };

            Assert.IsFalse(AltarCompletionEvaluator.IsRuleSatisfied(instance, rule));
        }

        [Test]
        public void Tier9Only_NotSatisfied()
        {
            AltarInstance instance = CreateInstance();
            instance.Slots[0].Offering = new AltarManaStoneOffering(9, "skeleton");
            var rule = new AltarCompletionRule { requiredSlotIds = System.Array.Empty<string>() };

            Assert.IsFalse(AltarCompletionEvaluator.IsRuleSatisfied(instance, rule));
        }

        [Test]
        public void Tier9And8_Satisfied()
        {
            AltarInstance instance = CreateInstance();
            instance.Slots[0].Offering = new AltarManaStoneOffering(9, "skeleton");
            instance.Slots[1].Offering = new AltarManaStoneOffering(8, "orc");
            var rule = new AltarCompletionRule { requiredSlotIds = System.Array.Empty<string>() };

            Assert.IsTrue(AltarCompletionEvaluator.IsRuleSatisfied(instance, rule));
        }

        static AltarInstance CreateInstance()
        {
            var definition = ScriptableObject.CreateInstance<AltarDefinition>();
            definition.slots = new[]
            {
                new AltarSlotDefinition { slotId = "tier9" },
                new AltarSlotDefinition { slotId = "tier8" },
            };

            return new AltarInstance(Vector3Int.zero, definition);
        }
    }
}
