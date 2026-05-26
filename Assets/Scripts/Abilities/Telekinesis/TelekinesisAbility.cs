using JRogue.Actors;
using JRogue.Item;
using JRogue.Item.World;
using JRogue.Manager.Floor;
using JRogue.Manager.Inventory;
using UnityEngine;

namespace JRogue.Ability.Telekinesis
{
    [CreateAssetMenu(fileName = "Telekinesis_Standard", menuName = "JRogue/Abilities/Telekinesis")]
    public class TelekinesisAbility : AbilityAction
    {
        const int DefaultRange = 7;

        public override bool CanExecute(GameObject user) => user != null;

        protected override bool ExecuteCore(GameObject user) => false;

        protected override bool ExecuteCore(GameObject user, Vector3Int targetTile)
        {
            if (user == null)
                return false;

            var actor = user.GetComponent<BaseActor>();
            if (actor == null)
                return false;

            if (!IsInRange(actor.GridPosition, targetTile))
            {
                LogInvalidTarget(targetTile);
                return false;
            }

            if (!TelekinesisFloorQuery.TryGetSinglePickable(targetTile, out TelekinesisPickable pickable))
            {
                LogInvalidTarget(targetTile);
                return false;
            }

            if (!TryAcquireFromFloor(pickable, out ItemInstance instance))
            {
                LogInvalidTarget(targetTile);
                return false;
            }

            DeliverToUser(user, actor.GridPosition, instance);
            return true;
        }

        int EffectiveRange => range > 0 ? range : DefaultRange;

        bool IsInRange(Vector3Int from, Vector3Int to)
        {
            int distance = Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y) + Mathf.Abs(from.z - to.z);
            return distance <= EffectiveRange;
        }

        static void LogInvalidTarget(Vector3Int tile) =>
            Debug.Log($"[Telekinesis] Invalid target at tile ({tile.x}, {tile.y}, {tile.z}).");

        static bool TryAcquireFromFloor(TelekinesisPickable pickable, out ItemInstance instance)
        {
            instance = null;

            switch (pickable.Source)
            {
                case TelekinesisPickableSource.PileEntry:
                {
                    FloorItemEntry entry = pickable.PileEntry;
                    instance = entry?.instance;
                    if (instance == null)
                        return false;

                    FloorItemPileService pile = FloorItemPileService.Instance;
                    if (pile == null || !pile.RemoveEntry(entry.entryId))
                        return false;

                    return true;
                }
                case TelekinesisPickableSource.WorldItem:
                {
                    WorldItem worldItem = pickable.WorldItem;
                    if (worldItem == null)
                        return false;

                    instance = worldItem.CollectInstance();
                    if (instance == null)
                        return false;

                    worldItem.gameObject.SetActive(false);
                    Object.Destroy(worldItem.gameObject);
                    return true;
                }
                default:
                    return false;
            }
        }

        static void DeliverToUser(GameObject user, Vector3Int userTile, ItemInstance instance)
        {
            InventoryManager inv = user.GetComponent<InventoryManager>();

            if (inv != null && inv.CanCarry(instance) && inv.AddItem(instance))
                return;

            if (inv != null && !inv.CanCarry(instance))
            {
                string itemLabel = instance.Definition != null ? instance.Definition.itemName : "item";
                Debug.LogWarning($"[Telekinesis] Too encumbered; dropped {itemLabel} at feet.");
            }

            FloorItemPileService pile = FloorItemPileService.Instance;
            if (pile != null)
                pile.AddEntry(userTile, instance);
            else
                Debug.LogWarning("[Telekinesis] No FloorItemPileService; item lost after pull.");
        }
    }
}
