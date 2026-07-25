using JRogue.World.LotF;
using NUnit.Framework;

namespace JRogue.Tests.World
{
    public class LordOfTheFloorSummonGateTests
    {
        [Test]
        public void Passes_WhenDay3Plus_OnHostFloor_WithFourLiving_AndAvailable()
        {
            bool ok = LordOfTheFloorSummonGateLogic.Passes(
                dungeonDay: 3,
                minimumDungeonDay: 3,
                activeFloorId: "dungeon_floor_01",
                hostFloorId: "dungeon_floor_01",
                livingPartyMembers: 4,
                minimumLivingPartyMembers: 4,
                runSlot: LordOfTheFloorRunSlot.Available,
                out string fail);

            Assert.IsTrue(ok);
            Assert.IsNull(fail);
        }

        [Test]
        public void Fails_WhenDayTooEarly()
        {
            bool ok = LordOfTheFloorSummonGateLogic.Passes(
                2, 3, "dungeon_floor_01", "dungeon_floor_01", 5, 4,
                LordOfTheFloorRunSlot.Available, out string fail);

            Assert.IsFalse(ok);
            StringAssert.Contains("day", fail);
        }

        [Test]
        public void Fails_WhenWrongFloor()
        {
            bool ok = LordOfTheFloorSummonGateLogic.Passes(
                3, 3, "dungeon_floor_02", "dungeon_floor_01", 5, 4,
                LordOfTheFloorRunSlot.Available, out _);

            Assert.IsFalse(ok);
        }

        [Test]
        public void Fails_WhenFewerThanFourLiving_EvenIfDeadMembersExist()
        {
            bool ok = LordOfTheFloorSummonGateLogic.Passes(
                3, 3, "dungeon_floor_01", "dungeon_floor_01", 3, 4,
                LordOfTheFloorRunSlot.Available, out string fail);

            Assert.IsFalse(ok);
            StringAssert.Contains("living party", fail);
        }

        [Test]
        public void Passes_WithFourLiving_RegardlessOfDeadRosterSlots()
        {
            // Gate only sees living count; dead members are already excluded by the caller.
            Assert.IsTrue(LordOfTheFloorSummonGateLogic.Passes(
                5, 3, "dungeon_floor_01", "dungeon_floor_01", 4, 4,
                LordOfTheFloorRunSlot.Available, out _));
        }

        [Test]
        public void Fails_WhenSlotSummonedOrConsumed()
        {
            Assert.IsFalse(LordOfTheFloorSummonGateLogic.Passes(
                3, 3, "dungeon_floor_01", "dungeon_floor_01", 4, 4,
                LordOfTheFloorRunSlot.Summoned, out _));
            Assert.IsFalse(LordOfTheFloorSummonGateLogic.Passes(
                3, 3, "dungeon_floor_01", "dungeon_floor_01", 4, 4,
                LordOfTheFloorRunSlot.Consumed, out _));
        }
    }

    public class LordOfTheFloorRunLedgerTests
    {
        [Test]
        public void SummonThenConsume_NeverReturnsToAvailable()
        {
            var ledger = new LordOfTheFloorRunLedger();
            Assert.AreEqual(LordOfTheFloorRunSlot.Available, ledger.Get("lotf_a"));
            Assert.IsTrue(ledger.TryMarkSummoned("lotf_a"));
            Assert.AreEqual(LordOfTheFloorRunSlot.Summoned, ledger.Get("lotf_a"));
            Assert.IsFalse(ledger.TryMarkSummoned("lotf_a"));
            ledger.MarkConsumed("lotf_a");
            Assert.AreEqual(LordOfTheFloorRunSlot.Consumed, ledger.Get("lotf_a"));
            Assert.IsFalse(ledger.TryMarkSummoned("lotf_a"));
        }

        [Test]
        public void Reset_ClearsSlots()
        {
            var ledger = new LordOfTheFloorRunLedger();
            ledger.TryMarkSummoned("lotf_a");
            ledger.MarkConsumed("lotf_a");
            ledger.Reset();
            Assert.AreEqual(LordOfTheFloorRunSlot.Available, ledger.Get("lotf_a"));
        }
    }
}
