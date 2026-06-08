using JRogue.Manager.Progression;
using JRogue.UI.Inventory;
using JRogue.UI.Quest;
using JRogue.UI.Racial;
using JRogue.UI.Character;

namespace JRogue.UI.Gameplay
{
    public static class GameplayModalGate
    {
        public static bool BlocksFloorGameplay =>
            RestSessionService.IsResting
            || GameOverModalUI.BlocksGameplay
            || InventoryUI.BlocksGameplay
            || AutoPickupConfirmDialogUI.BlocksGameplay
            || TrapConfirmDialogUI.BlocksGameplay
            || HazardConfirmDialogUI.BlocksGameplay
            || FloorPickupMenuUI.BlocksGameplay
            || PartyMemberDeathDialogUI.BlocksGameplay
            || DungeonEndedDialogUI.BlocksGameplay
            || EnterDungeonDialogUI.BlocksGameplay
            || AdjacentInteractPickerModalUI.BlocksGameplay
            || AltarOfferingModalUI.BlocksGameplay
            || AltarUsedModalUI.BlocksGameplay
            || EssencePickupConfirmDialogUI.BlocksGameplay
            || NpcDialogBoxUI.BlocksGameplay
            || ShopNpcMenuUI.BlocksGameplay
            || QuestJournalUI.BlocksGameplay
            || RacialAbilitiesUI.BlocksGameplay
            || CharacterEquipmentUI.BlocksGameplay
            || FriendlyFireConfirmDialogUI.BlocksGameplay
            || MessageHistoryUI.BlocksGameplay
            || InventoryGivePickerUI.IsOpen;
    }
}
