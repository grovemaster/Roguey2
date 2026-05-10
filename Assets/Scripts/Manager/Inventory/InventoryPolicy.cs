using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Combat;
using UnityEngine;

namespace JRogue.Manager.Inventory
{
    /// <summary>
    /// Central rules for who may use/move items depending on <see cref="CombatThreatCoordinator"/> tension.
    /// Inventory UI/item commands should consult these helpers (extend with item tags later).
    /// </summary>
    public static class InventoryPolicy
    {
        static bool InCombat =>
            CombatThreatCoordinator.Instance != null && CombatThreatCoordinator.Instance.IsInCombat;

        /// <summary>Another actor's loose bag item may be used (potions, etc.) if not outfitted on someone.</summary>
        public static bool CanUseCarriedFromAlly(BaseActor user, BaseActor carrier, bool itemEquippedElsewhere)
        {
            if (itemEquippedElsewhere)
                return false;

            if (!InCombat)
                return true;

            return carrier != null && user != null && carrier.gameObject == user.gameObject;
        }

        /// <summary>Handing an item to another party member spends the initiator's turn while in combat (caller must invoke TurnManager).</summary>
        public static bool TransferRequiresInitiatorTurn => InCombat;

        public static void LogCombatTransferStub(BaseActor initiator)
        {
            if (initiator == null)
                return;

            Debug.Log(
                $"[InventoryPolicy] In-combat transfer stub — hook TurnManager.OnPlayerActionComplete({initiator.name}) when item exchange is implemented.");
        }
    }
}
