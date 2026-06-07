using JRogue.UI.Hotbar;
using NUnit.Framework;

namespace JRogue.Tests.UI
{
    [TestFixture]
    public sealed class HotbarEntryTests
    {
        [Test]
        public void IsEmpty_ReturnsTrueForDefaultEntry()
        {
            var entry = new HotbarEntry();
            Assert.IsTrue(entry.IsEmpty());
            Assert.AreEqual("empty", entry.EntryKey());
        }

        [Test]
        public void EntryKey_FormatsByKind()
        {
            var essence = new HotbarEntry
            {
                Kind = HotbarEntryKind.EssenceActive,
                essenceSlotIndex = 1,
                abilityIndex = 2,
            };

            Assert.AreEqual("essence:1:2", essence.EntryKey());

            var racial = new HotbarEntry
            {
                Kind = HotbarEntryKind.RacialActive,
                racialBindingKey = "SpiritImprint:node_a:0",
            };

            Assert.AreEqual("racial:SpiritImprint:node_a:0", racial.EntryKey());
        }

        [Test]
        public void EqualsEntry_MatchesSamePayloadOnly()
        {
            var left = new HotbarEntry
            {
                Kind = HotbarEntryKind.InventoryActive,
                itemInstanceId = "item-1",
                abilityIndex = 0,
            };

            var right = left.Clone();
            var different = new HotbarEntry
            {
                Kind = HotbarEntryKind.InventoryActive,
                itemInstanceId = "item-1",
                abilityIndex = 1,
            };

            Assert.IsTrue(left.EqualsEntry(right));
            Assert.IsFalse(left.EqualsEntry(different));
            Assert.IsFalse(left.EqualsEntry(null));
        }

        [Test]
        public void Clone_CreatesIndependentCopy()
        {
            var original = new HotbarEntry
            {
                Kind = HotbarEntryKind.EquipmentActive,
                equipmentSlot = 3,
                abilityIndex = 1,
                abilityAssetName = "Radiance",
            };

            HotbarEntry copy = original.Clone();
            copy.abilityIndex = 2;

            Assert.AreEqual(1, original.abilityIndex);
            Assert.AreEqual(HotbarEntryKind.EquipmentActive, copy.Kind);
            Assert.AreEqual(3, copy.equipmentSlot);
            Assert.AreEqual("Radiance", copy.abilityAssetName);
        }
    }
}
