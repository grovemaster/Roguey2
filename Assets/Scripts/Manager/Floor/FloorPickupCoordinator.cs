using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Item.World;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Manager.Floor
{
    /// <summary>Manual floor pickup (`,` / <c>g</c>) — fast path, menu, and turn completion.</summary>
    public static class FloorPickupCoordinator
    {
        public const int PickupMenuThreshold = 1;

        public static bool TryBeginManualPickup(BaseActor picker)
        {
            if (picker == null)
                return false;

            TurnManager turn = TurnManager.Instance;
            if (turn == null || turn.currentState != GameState.PLAYER_TURN)
                return false;

            if (!turn.CanActorTakeAction(picker.gameObject))
                return false;

            Vector3Int tile = picker.GridPosition;
            List<ManualPickupTarget> targets = CollectManualTargets(tile);
            if (targets.Count == 0)
            {
                Debug.Log("[Pickup] Nothing to pick up.");
                return true;
            }

            if (targets.Count <= PickupMenuThreshold)
            {
                AttemptPickup(targets[0], picker);
                turn.OnPlayerActionComplete(picker.gameObject);
                return true;
            }

            FloorPickupMenuUI.EnsureInstance().Show(
                picker,
                tile,
                targets,
                pickedCount =>
                {
                    if (pickedCount > 0)
                        turn.OnPlayerActionComplete(picker.gameObject);
                });

            return true;
        }

        public static bool HasPickupAtTile(Vector3Int tile) => CollectManualTargets(tile).Count > 0;

        public static bool CanManualPickupNow()
        {
            PartyManager party = PartyManager.Instance;
            BaseActor active = party?.GetActiveMember();
            if (active == null)
                return false;

            TurnManager turn = TurnManager.Instance;
            if (turn == null || turn.currentState != GameState.PLAYER_TURN)
                return false;

            if (!turn.CanActorTakeAction(active.gameObject))
                return false;

            return HasPickupAtTile(active.GridPosition);
        }

        public static List<ManualPickupTarget> CollectManualTargets(Vector3Int tile)
        {
            var targets = new List<ManualPickupTarget>();

            FloorItemPileService pile = FloorItemPileService.Instance;
            if (pile != null)
            {
                IReadOnlyList<FloorItemEntry> entries = pile.GetEntries(tile);
                for (int i = 0; i < entries.Count; i++)
                {
                    FloorItemEntry entry = entries[i];
                    if (entry?.instance?.Definition == null)
                        continue;

                    targets.Add(ManualPickupTarget.FromPile(entry));
                }
            }

            IReadOnlyList<WorldItem> worldItems = FloorPickupQuery.GetAllWorldItemsOnTile(tile);
            for (int i = 0; i < worldItems.Count; i++)
            {
                WorldItem wi = worldItems[i];
                if (wi == null || wi.data == null)
                    continue;

                targets.Add(ManualPickupTarget.FromWorld(wi));
            }

            return targets;
        }

        public static bool AttemptPickup(ManualPickupTarget target, BaseActor picker)
        {
            if (picker == null)
                return false;

            GameObject go = picker.gameObject;
            FloorItemPileService pile = FloorItemPileService.Instance;

            if (target.PileEntry != null && pile != null)
            {
                bool ok = FloorPickupService.TryManualPickup(pile, target.PileEntry, go);
                if (!ok)
                    Debug.Log($"[Pickup] Could not pick up {target.DisplayName} (bag full or too heavy).");
                return ok;
            }

            if (target.WorldItem != null)
            {
                bool ok = FloorPickupService.TryManualPickup(target.WorldItem, go);
                if (!ok)
                    Debug.Log($"[Pickup] Could not pick up {target.DisplayName}.");
                return ok;
            }

            return false;
        }

        /// <summary>Picks up selected targets in order; returns count successfully taken.</summary>
        public static int AttemptPickupBatch(IReadOnlyList<ManualPickupTarget> targets, bool[] selected, BaseActor picker)
        {
            if (targets == null || selected == null || picker == null)
                return 0;

            int picked = 0;
            for (int i = 0; i < targets.Count && i < selected.Length; i++)
            {
                if (!selected[i])
                    continue;

                if (AttemptPickup(targets[i], picker))
                    picked++;
            }

            if (picked > 0 && targets.Count > picked)
                Debug.Log($"[Pickup] Picked up {picked}; some items remain on the tile.");

            return picked;
        }

        /// <summary>Picks up every carryable target on the tile (Take All).</summary>
        public static int AttemptPickupAllCarryable(IReadOnlyList<ManualPickupTarget> targets, BaseActor picker)
        {
            if (targets == null || picker == null)
                return 0;

            InventoryManager inv = picker.GetComponent<InventoryManager>();
            var carryable = new bool[targets.Count];
            int eligible = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                if (!CanPickupTarget(inv, targets[i]))
                    continue;

                carryable[i] = true;
                eligible++;
            }

            int picked = AttemptPickupBatch(targets, carryable, picker);
            if (picked > 0 && picked < eligible)
                Debug.Log($"[Pickup] Picked up {picked} of {eligible} carryable items.");

            return picked;
        }

        public static bool CanPickupTarget(InventoryManager inv, ManualPickupTarget target)
        {
            if (target == null)
                return false;

            ItemInstance inst = target.PileEntry?.instance;
            if (inst != null)
                return inv != null && (inst.IsCurrency || inst.IsManaStone || inv.CanCarry(inst));

            if (target.WorldItem?.data != null)
                return inv != null && inv.CanCarry(new ItemInstance(target.WorldItem.data));

            return false;
        }
    }

    public sealed class ManualPickupTarget
    {
        public FloorItemEntry PileEntry { get; private set; }
        public WorldItem WorldItem { get; private set; }
        public string DisplayName { get; private set; }

        public static ManualPickupTarget FromPile(FloorItemEntry entry) =>
            new ManualPickupTarget
            {
                PileEntry = entry,
                DisplayName = entry.instance.Definition.itemName
            };

        public static ManualPickupTarget FromWorld(WorldItem world) =>
            new ManualPickupTarget
            {
                WorldItem = world,
                DisplayName = world.data.itemName
            };
    }
}
