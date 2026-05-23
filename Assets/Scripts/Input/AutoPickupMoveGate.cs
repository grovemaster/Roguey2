using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Item.World;
using JRogue.Manager.Floor;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Input
{
    /// <summary>Pre-move check for confirm-gated floor auto-pickup items.</summary>
    public static class AutoPickupMoveGate
    {
        public static bool TryInterceptMove(
            BaseActor mover,
            Vector3Int destination,
            bool isEnemyBump,
            System.Action onConfirmedMove)
        {
            if (isEnemyBump || mover == null || onConfirmedMove == null)
                return false;

            if (!HasConfirmGatedPickup(destination, out IReadOnlyList<FloorItemEntry> pileEntries, out IReadOnlyList<WorldItem> worldItems))
                return false;

            AutoPickupConfirmDialogUI dialog = AutoPickupConfirmDialogUI.EnsureInstance();
            dialog.Show(mover, destination, pileEntries, worldItems, onConfirmedMove);
            return true;
        }

        public static bool HasConfirmGatedPickup(
            Vector3Int tile,
            out IReadOnlyList<FloorItemEntry> pileEntries,
            out IReadOnlyList<WorldItem> worldItems)
        {
            pileEntries = FloorItemPileService.Instance != null
                ? FloorItemPileService.Instance.GetConfirmGatedAutoPickupEntries(tile)
                : System.Array.Empty<FloorItemEntry>();
            worldItems = FloorPickupQuery.GetConfirmGatedWorldItems(tile);
            return pileEntries.Count > 0 || worldItems.Count > 0;
        }
    }
}
