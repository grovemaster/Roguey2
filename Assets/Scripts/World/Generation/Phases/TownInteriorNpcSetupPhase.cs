using JRogue.Actors.Components;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Manager.Grid;
using JRogue.Shop;
using JRogue.World.Town;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JRogue.World.Generation.Phases
{
    /// <summary>Spawns town interior NPC prefabs at stamp or scene-painted markers.</summary>
    public sealed class TownInteriorNpcSetupPhase : IDungeonGenerationPhase
    {
        static readonly (string floorId, string markerId, string resourcesPath, string editorPath)[] SpawnEntries =
        {
            (
                TownBuildingIds.DemoInteriorFloorId,
                StampMarkerIds.BuildingDemoNpc,
                "Town/Npc/TownNpc_DemoHost",
                "Assets/Resources/Town/Npc/TownNpc_DemoHost.prefab"),
            (
                AdventureGuildExchangeLayout.InteriorFloorId,
                AdventureGuildExchangeLayout.NpcMarkerId,
                "Town/Npc/TownNpc_AdventureGuildClerk",
                "Assets/Resources/Town/Npc/TownNpc_AdventureGuildClerk.prefab"),
        };

        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def == null)
                return;

            Transform parent = context.Instance.DynamicViewsRoot;
            if (parent == null)
                return;

            int spawned = 0;
            for (int i = 0; i < SpawnEntries.Length; i++)
            {
                (string floorId, string markerId, string resourcesPath, string editorPath) = SpawnEntries[i];
                if (def.FloorId != floorId)
                    continue;

                if (!TryResolveMarkerCell(context, markerId, out Vector3Int cell))
                {
                    DungeonGenerationLog.Warn($"{nameof(TownInteriorNpcSetupPhase)} missing marker '{markerId}'.");
                    continue;
                }

                GameObject prefab = LoadNpcPrefab(resourcesPath, editorPath);
                if (prefab == null)
                {
                    DungeonGenerationLog.Warn($"{nameof(TownInteriorNpcSetupPhase)} missing prefab {resourcesPath}.");
                    continue;
                }

                GameObject instance = Object.Instantiate(prefab, parent);
                instance.name = prefab.name;

                if (instance.GetComponent<NpcController>() == null)
                {
                    Object.Destroy(instance);
                    continue;
                }

                NpcCounterTalkBinding counterBinding = instance.GetComponent<NpcCounterTalkBinding>();
                if (counterBinding != null && floorId == AdventureGuildExchangeLayout.InteriorFloorId)
                {
                    counterBinding.Configure(
                        AdventureGuildExchangeLayout.CustomerRowY,
                        AdventureGuildExchangeLayout.CounterRowY);
                }

                if (instance.GetComponent<ShopNpcController>() != null)
                    TownShopStateService.EnsureRunService();

                GridMover mover = instance.GetComponent<GridMover>();
                if (mover != null)
                    mover.InitializeAtGridAnchor(cell);
                else
                    instance.transform.position = GridCellWorld.GetCellCenter(context.Instance.Tilemaps.FloorMap, cell);

                SpriteRenderer spriteRenderer = instance.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                    spriteRenderer.sortingOrder = 20;

                spawned++;
            }

            if (spawned > 0)
            {
                DungeonGenerationLog.Phase(
                    nameof(TownInteriorNpcSetupPhase),
                    $"spawned {spawned} interior NPC(s).");
            }
        }

        static GameObject LoadNpcPrefab(string resourcesPath, string editorPath)
        {
            GameObject prefab = Resources.Load<GameObject>(resourcesPath);
#if UNITY_EDITOR
            if (prefab == null && !string.IsNullOrEmpty(editorPath))
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(editorPath);
#endif
            return prefab;
        }

        static bool TryResolveMarkerCell(DungeonGenerationContext context, string markerId, out Vector3Int cell)
        {
            cell = default;
            if (context.Instance != null
                && context.Definition?.LayoutMode == FloorLayoutMode.ScenePainted
                && ScenePaintedMarkerUtility.TryGetCellByMarkerId(context.Instance.transform, markerId, out cell))
            {
                return true;
            }

            DungeonLayoutStamp stamp = context.Definition?.LayoutStamp;
            return stamp != null && stamp.TryGetMarker(markerId, out cell);
        }
    }
}
