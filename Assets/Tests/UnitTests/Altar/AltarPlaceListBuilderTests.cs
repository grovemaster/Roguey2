using System.Collections.Generic;
using JRogue.Manager.Party;
using JRogue.World.Altar;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Altar
{
    [TestFixture]
    public sealed class AltarPlaceListBuilderTests
    {
        [Test]
        public void Tier9OnAltar_ExcludesTier9FromPlaceList()
        {
            AltarInstance instance = CreateInstanceWithTierFilters();
            instance.Slots[0].Offering = new AltarManaStoneOffering(9, "skeleton");

            var ledgerGo = new GameObject("Ledger");
            var ledger = ledgerGo.AddComponent<PartyManaStoneLedger>();
            ledger.Add(9, "skeleton", 2);
            ledger.Add(8, "skeleton", 1);

            var dest = new List<AltarPlaceableStack>();
            AltarPlaceListBuilder.BuildPlaceableStacks(instance, ledger, dest);

            Assert.AreEqual(1, dest.Count);
            Assert.AreEqual(8, dest[0].Tier);

            Object.DestroyImmediate(ledgerGo);
        }

        static AltarInstance CreateInstanceWithTierFilters()
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
