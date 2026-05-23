using System.Collections.Generic;
using JRogue.Item;
using JRogue.Item.World;
using UnityEngine;

namespace JRogue.Manager.Floor
{
    /// <summary>Scene <see cref="WorldItem"/> queries for floor auto-pickup (confirm-gated and silent).</summary>
    public static class FloorPickupQuery
    {
        public static IReadOnlyList<WorldItem> GetConfirmGatedWorldItems(Vector3Int tile)
        {
            var matches = new List<WorldItem>();
            CollectWorldItems(tile, def => def.RequiresConfirmBeforeAutoPickupOnStep, matches);
            return matches;
        }

        public static IReadOnlyList<WorldItem> GetSilentAutoPickupWorldItems(Vector3Int tile)
        {
            var matches = new List<WorldItem>();
            CollectWorldItems(tile, def => def.ParticipatesInSilentAutoPickupOnStep, matches);
            return matches;
        }

        static void CollectWorldItems(Vector3Int tile, System.Func<ItemData, bool> predicate, List<WorldItem> matches)
        {
            WorldItem[] worldItems = Object.FindObjectsByType<WorldItem>();
            for (int i = 0; i < worldItems.Length; i++)
            {
                WorldItem item = worldItems[i];
                if (item == null || item.data == null || !predicate(item.data))
                    continue;

                if (WorldItemTile(item) != tile)
                    continue;

                matches.Add(item);
            }
        }

        public static Vector3Int WorldItemTile(WorldItem item)
        {
            if (item == null)
                return default;

            return Vector3Int.FloorToInt(item.transform.position - new Vector3(0.5f, 0.5f, 0f));
        }
    }
}
