using JRogue.Input;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Input
{
    [TestFixture]
    public sealed class PlayerCommandTest
    {
        [Test]
        public void MoveGrid_Factory_SetsKindAndDirection()
        {
            var d = new Vector3Int(1, -1, 0);
            PlayerCommand c = PlayerCommand.MoveGrid(d);

            Assert.AreEqual(PlayerCommandKind.MoveGrid, c.Kind);
            Assert.AreEqual(d, c.Direction);
        }

        [Test]
        public void Wait_Factory_PreservesPartyWait()
        {
            Assert.IsFalse(PlayerCommand.Wait(false).PartyWait);
            Assert.IsTrue(PlayerCommand.Wait(true).PartyWait);
            Assert.AreEqual(PlayerCommandKind.Wait, PlayerCommand.Wait(false).Kind);
        }

        [Test]
        public void AbilitySlot_Factory_SetsSlotAndModifiers()
        {
            PlayerCommand c = PlayerCommand.AbilitySlot(slotIndex: 2, secondary: true, fromEquipment: true);

            Assert.AreEqual(PlayerCommandKind.AbilitySlot, c.Kind);
            Assert.AreEqual(2, c.SlotIndex);
            Assert.IsTrue(c.AbilitySecondary);
            Assert.IsTrue(c.AbilityFromEquipment);
        }

        [Test]
        public void SwapPartyMember_Factory_SetsIndex()
        {
            PlayerCommand c = PlayerCommand.SwapPartyMember(3);
            Assert.AreEqual(PlayerCommandKind.SwapPartyMember, c.Kind);
            Assert.AreEqual(3, c.PartyMemberIndex);
        }

        [TestCase(PlayerCommandKind.MoveGrid)]
        [TestCase(PlayerCommandKind.Wait)]
        [TestCase(PlayerCommandKind.ConfirmTarget)]
        [TestCase(PlayerCommandKind.CancelTarget)]
        [TestCase(PlayerCommandKind.AbilitySlot)]
        [TestCase(PlayerCommandKind.ToggleFormation)]
        [TestCase(PlayerCommandKind.SwapPartyMember)]
        public void FactoryCommands_ReportStableKind(PlayerCommandKind expected)
        {
            PlayerCommand cmd = expected switch
            {
                PlayerCommandKind.MoveGrid => PlayerCommand.MoveGrid(Vector3Int.up),
                PlayerCommandKind.Wait => PlayerCommand.Wait(false),
                PlayerCommandKind.ConfirmTarget => PlayerCommand.ConfirmTarget(),
                PlayerCommandKind.CancelTarget => PlayerCommand.CancelTarget(),
                PlayerCommandKind.AbilitySlot => PlayerCommand.AbilitySlot(0, false, false),
                PlayerCommandKind.ToggleFormation => PlayerCommand.ToggleFormation(),
                PlayerCommandKind.SwapPartyMember => PlayerCommand.SwapPartyMember(0),
                _ => throw new System.InvalidOperationException(),
            };

            Assert.AreEqual(expected, cmd.Kind);
        }
    }
}
