using JRogue.Manager.Party;
using JRogue.World.Altar;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Altar
{
    [TestFixture]
    public sealed class AltarOfferingServiceTests
    {
        GameObject _ledgerGo;

        [SetUp]
        public void SetUp()
        {
            _ledgerGo = new GameObject("Ledger");
            _ledgerGo.AddComponent<PartyManaStoneLedger>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_ledgerGo != null)
                Object.DestroyImmediate(_ledgerGo);
        }

        [Test]
        public void Place_SpendsLedgerAndFillsSlot()
        {
            PartyManaStoneLedger.Instance.Add(9, "skeleton", 1);
            AltarInstance instance = CreateInstance();

            Assert.AreEqual(AltarOfferingResult.Placed,
                AltarOfferingService.TryPlaceManaStone(instance, 9, "skeleton"));

            Assert.AreEqual(0, PartyManaStoneLedger.Instance.GetAmount(9, "skeleton"));
            Assert.IsFalse(instance.Slots[0].IsEmpty);
        }

        [Test]
        public void Remove_ReturnsStoneToLedger()
        {
            AltarInstance instance = CreateInstance();
            instance.Slots[0].Offering = new AltarManaStoneOffering(9, "skeleton");

            Assert.AreEqual(AltarOfferingResult.Removed,
                AltarOfferingService.TryRemoveFromSlot(instance, 0));

            Assert.AreEqual(1, PartyManaStoneLedger.Instance.GetAmount(9, "skeleton"));
            Assert.IsTrue(instance.Slots[0].IsEmpty);
        }

        static AltarInstance CreateInstance()
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

            return new AltarInstance(Vector3Int.zero, definition);
        }
    }
}
