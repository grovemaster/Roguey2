using JRogue.Actors.Components;
using JRogue.Controller.Npc;
using JRogue.Manager.Grid;
using JRogue.Shop;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JRogue.World.Generation.Phases
{
    /// <summary>Spawns town NPC prefabs at stamp markers on town_main.</summary>
    public sealed class TownNpcSetupPhase : IDungeonGenerationPhase
    {
        public const string TownFloorId = "town_main";

        static readonly (string markerId, string resourcesPath, string editorPath)[] SpawnEntries =
            BuildSpawnEntries();

        static (string markerId, string resourcesPath, string editorPath)[] BuildSpawnEntries()
        {
            var entries = new System.Collections.Generic.List<(string, string, string)>
            {
                (StampMarkerIds.TownNpc1, "Town/Npc/TownNpc_Mira", "Assets/Resources/Town/Npc/TownNpc_Mira.prefab"),
                (StampMarkerIds.TownNpc2, "Town/Npc/TownNpc_Luc", "Assets/Resources/Town/Npc/TownNpc_Luc.prefab"),
                (StampMarkerIds.TownNpc3, "Town/Npc/TownNpc_Edda", "Assets/Resources/Town/Npc/TownNpc_Edda.prefab"),
                (StampMarkerIds.TownNpc4, "Town/Npc/TownNpc_Fenn", "Assets/Resources/Town/Npc/TownNpc_Fenn.prefab"),
                (StampMarkerIds.TownNpc5, "Town/Npc/TownNpc_Greta", "Assets/Resources/Town/Npc/TownNpc_Greta.prefab"),
                (StampMarkerIds.ShamanBarbarian, "Town/Npc/TownNpc_ShamanBarbarian", "Assets/Resources/Town/Npc/TownNpc_ShamanBarbarian.prefab"),
                (StampMarkerIds.FairyMerchant, "Town/Npc/TownNpc_FairyMerchant", "Assets/Resources/Town/Npc/TownNpc_FairyMerchant.prefab"),
                (StampMarkerIds.BeastBloodMerchant, "Town/Npc/TownNpc_BeastBloodMerchant", "Assets/Resources/Town/Npc/TownNpc_BeastBloodMerchant.prefab"),
                (StampMarkerIds.FleshmetalForgemaster, "Town/Npc/TownNpc_FleshmetalForgemaster", "Assets/Resources/Town/Npc/TownNpc_FleshmetalForgemaster.prefab"),
                (StampMarkerIds.DragonianElderVolscale, "Town/Npc/TownNpc_DragonianElderVolscale", "Assets/Resources/Town/Npc/TownNpc_DragonianElderVolscale.prefab"),
                (StampMarkerIds.MageTutor, "Town/Npc/TownNpc_MageTutor", "Assets/Resources/Town/Npc/TownNpc_MageTutor.prefab"),
                (StampMarkerIds.KnightDrillMaster, "Town/Npc/TownNpc_KnightDrillMaster", "Assets/Resources/Town/Npc/TownNpc_KnightDrillMaster.prefab"),
                (StampMarkerIds.ArcaneVendor, "Town/Npc/TownNpc_ArcaneVendor", "Assets/Resources/Town/Npc/TownNpc_ArcaneVendor.prefab"),
                (StampMarkerIds.PriestShrineSteward, "Town/Npc/TownNpc_PriestShrineSteward", "Assets/Resources/Town/Npc/TownNpc_PriestShrineSteward.prefab"),
            };

            for (int i = 0; i < DwarfClanTownEntries.Stewards.Length; i++)
            {
                DwarfClanTownEntries.StewardEntry steward = DwarfClanTownEntries.Stewards[i];
                entries.Add((steward.MarkerId, steward.PrefabResourcesPath, steward.PrefabEditorPath));
            }

            return entries.ToArray();
        }

        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def == null || def.FloorId != TownFloorId)
                return;

            Transform parent = context.Instance.DynamicViewsRoot;
            if (parent == null)
                return;

            int spawned = 0;
            for (int i = 0; i < SpawnEntries.Length; i++)
            {
                (string markerId, string resourcesPath, string editorPath) = SpawnEntries[i];
                if (!TryResolveMarkerCell(context, markerId, out Vector3Int cell))
                {
                    DungeonGenerationLog.Warn($"{nameof(TownNpcSetupPhase)} missing marker '{markerId}'.");
                    continue;
                }

                GameObject prefab = LoadNpcPrefab(resourcesPath, editorPath);
                if (prefab == null)
                {
                    DungeonGenerationLog.Warn($"{nameof(TownNpcSetupPhase)} missing prefab {resourcesPath}.");
                    continue;
                }

                GameObject instance = Object.Instantiate(prefab, parent);
                instance.name = prefab.name;

                if (instance.GetComponent<NpcController>() == null)
                {
                    Object.Destroy(instance);
                    DungeonGenerationLog.Warn($"{nameof(TownNpcSetupPhase)} prefab lacks {nameof(NpcController)}.");
                    continue;
                }

                ShopNpcController shopNpc = instance.GetComponent<ShopNpcController>();
                if (shopNpc != null && shopNpc.ShopDefinition != null)
                    TownShopStateService.EnsureRunService();

                GridMover mover = instance.GetComponent<GridMover>();
                if (mover != null)
                    mover.InitializeAtGridAnchor(cell);
                else
                    instance.transform.position = GridCellWorld.GetCellCenter(context.Instance.Tilemaps.FloorMap, cell);

                spawned++;
                DungeonGenerationLog.Phase(nameof(TownNpcSetupPhase), $"spawned {instance.name} at {cell}");
            }

            DungeonGenerationLog.Phase(nameof(TownNpcSetupPhase), $"spawned {spawned} town NPC(s).");
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
            DungeonLayoutStamp stamp = context.Definition?.LayoutStamp;
            return stamp != null && stamp.TryGetMarker(markerId, out cell);
        }
    }
}
