using System;
using UnityEngine;

namespace JRogue.Input
{
    public enum InputState { Normal, Targeting }

    /// <summary>Where a player-triggered ability is resolved when replaying commands.</summary>
    public enum PlayerAbilitySource : byte
    {
        Essence = 0,
        EquipmentItem = 1,
        HumanMageSpell = 2,
    }

    /// <summary>Discriminant for <see cref="PlayerCommand"/>; stable for serialization.</summary>
    public enum PlayerCommandKind : byte
    {
        MoveGrid,
        Wait,
        ConfirmTarget,
        CancelTarget,
        AbilitySlot,
        ToggleFormation,
        SwapPartyMember,
        PickupFloorItems,
    }

    /// <summary>
    /// Replay-friendly snapshot of one player-side decision after device input is interpreted.
    /// Intentionally POD-style: no delegates or Unity callbacks.
    /// </summary>
    [Serializable]
    public struct PlayerCommand
    {
        public PlayerCommandKind Kind;
        public Vector3Int Direction;
        public int SlotIndex;
        /// <summary>True = shift-bound secondary ability.</summary>
        public bool AbilitySecondary;
        /// <summary>True = ctrl-bound item ability from equipment.</summary>
        public bool AbilityFromEquipment;
        /// <summary>F-key index: F1 → 0.</summary>
        public int PartyMemberIndex;
        /// <summary>Shift+wait: end entire player phase.</summary>
        public bool PartyWait;

        public static PlayerCommand MoveGrid(Vector3Int direction) =>
            new PlayerCommand { Kind = PlayerCommandKind.MoveGrid, Direction = direction };

        public static PlayerCommand Wait(bool partyWait) =>
            new PlayerCommand { Kind = PlayerCommandKind.Wait, PartyWait = partyWait };

        public static PlayerCommand ConfirmTarget() =>
            new PlayerCommand { Kind = PlayerCommandKind.ConfirmTarget };

        public static PlayerCommand CancelTarget() =>
            new PlayerCommand { Kind = PlayerCommandKind.CancelTarget };

        public static PlayerCommand AbilitySlot(int slotIndex, bool secondary, bool fromEquipment) =>
            new PlayerCommand
            {
                Kind = PlayerCommandKind.AbilitySlot,
                SlotIndex = slotIndex,
                AbilitySecondary = secondary,
                AbilityFromEquipment = fromEquipment,
            };

        public static PlayerCommand ToggleFormation() =>
            new PlayerCommand { Kind = PlayerCommandKind.ToggleFormation };

        public static PlayerCommand SwapPartyMember(int zeroBasedPartyIndex) =>
            new PlayerCommand
            {
                Kind = PlayerCommandKind.SwapPartyMember,
                PartyMemberIndex = zeroBasedPartyIndex,
            };

        public static PlayerCommand PickupFloorItems() =>
            new PlayerCommand { Kind = PlayerCommandKind.PickupFloorItems };
    }
}
