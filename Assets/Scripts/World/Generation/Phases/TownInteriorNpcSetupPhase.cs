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
            (
                AdventureGuildHallLayout.InteriorFloorId,
                AdventureGuildHallLayout.NpcMarkerId,
                "Town/Npc/TownNpc_AdventureGuildSecretary",
                "Assets/Resources/Town/Npc/TownNpc_AdventureGuildSecretary.prefab"),
            (
                MarketGeneralStoreLayout.InteriorFloorId,
                MarketGeneralStoreLayout.NpcMarkerId,
                "Town/Npc/TownNpc_MarketGeneralStoreKeeper",
                "Assets/Resources/Town/Npc/TownNpc_MarketGeneralStoreKeeper.prefab"),
            (
                MarketItemShopLayout.InteriorFloorId,
                MarketItemShopLayout.NpcMarkerId,
                "Town/Npc/TownNpc_MarketItemShopClerk",
                "Assets/Resources/Town/Npc/TownNpc_MarketItemShopClerk.prefab"),
            (
                MarketBlacksmithLayout.InteriorFloorId,
                MarketBlacksmithLayout.NpcMarkerId,
                "Town/Npc/TownNpc_MarketBlacksmith",
                "Assets/Resources/Town/Npc/TownNpc_MarketBlacksmith.prefab"),
            (
                ResidentialInnLayout.InteriorFloorId,
                ResidentialInnLayout.NpcMarkerId,
                "Town/Npc/TownNpc_ResidentialInnKeeper",
                "Assets/Resources/Town/Npc/TownNpc_ResidentialInnKeeper.prefab"),
            (
                HolyLandFloorIds.ShamanTentInterior,
                BarbarianShamanTentLayout.ShamanMarkerId,
                "Town/Npc/TownNpc_ShamanBarbarian",
                "Assets/Resources/Town/Npc/TownNpc_ShamanBarbarian.prefab"),
            (
                HolyLandFloorIds.HolyLandProper,
                BarbarianHolyLandLayout.ChiefMarkerId,
                "Town/Npc/TownNpc_ChiefBarbarian",
                "Assets/Resources/Town/Npc/TownNpc_ChiefBarbarian.prefab"),
            (
                HolyLandFloorIds.ElfHolyLandProper,
                ElfHolyLandLayout.ChiefMarkerId,
                "Town/Npc/TownNpc_ChiefElf",
                "Assets/Resources/Town/Npc/TownNpc_ChiefElf.prefab"),
            (
                HolyLandFloorIds.ElfHouseInterior,
                ElfHolyLandHouseLayout.FairyMerchantMarkerId,
                "Town/Npc/TownNpc_ElfGroveFairyMerchant",
                "Assets/Resources/Town/Npc/TownNpc_ElfGroveFairyMerchant.prefab"),
            (
                HolyLandFloorIds.BeastmanHolyLandProper,
                BeastmanHolyLandLayout.ChiefMarkerId,
                "Town/Npc/TownNpc_ChiefBeastman",
                "Assets/Resources/Town/Npc/TownNpc_ChiefBeastman.prefab"),
            (
                HolyLandFloorIds.BeastmanDenInterior,
                BeastmanHolyLandDenLayout.BeastBloodMerchantMarkerId,
                "Town/Npc/TownNpc_BeastmanDenBeastBloodMerchant",
                "Assets/Resources/Town/Npc/TownNpc_BeastmanDenBeastBloodMerchant.prefab"),
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
                if (counterBinding != null)
                    ConfigureCounterTalkBinding(counterBinding, floorId);

                if (instance.GetComponent<ShopNpcController>() != null
                    || instance.GetComponent<InnkeeperNpcController>() != null
                    || instance.GetComponent<AdventurersGuildSecretaryNpcController>() != null)
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

        static void ConfigureCounterTalkBinding(NpcCounterTalkBinding counterBinding, string floorId)
        {
            if (floorId == AdventureGuildExchangeLayout.InteriorFloorId)
            {
                counterBinding.Configure(
                    AdventureGuildExchangeLayout.CustomerRowY,
                    AdventureGuildExchangeLayout.CounterRowY);
                return;
            }

            if (floorId == AdventureGuildHallLayout.InteriorFloorId)
            {
                counterBinding.Configure(
                    AdventureGuildHallLayout.CustomerRowY,
                    AdventureGuildHallLayout.CounterRowY);
                return;
            }

            if (floorId == MarketGeneralStoreLayout.InteriorFloorId)
            {
                counterBinding.Configure(
                    MarketGeneralStoreLayout.CustomerRowY,
                    MarketGeneralStoreLayout.CounterRowY);
                return;
            }

            if (floorId == MarketItemShopLayout.InteriorFloorId)
            {
                counterBinding.Configure(
                    MarketItemShopLayout.CustomerRowY,
                    MarketItemShopLayout.CounterRowY);
                return;
            }

            if (floorId == MarketBlacksmithLayout.InteriorFloorId)
            {
                counterBinding.Configure(
                    MarketBlacksmithLayout.CustomerRowY,
                    MarketBlacksmithLayout.CounterRowY);
                return;
            }

            if (floorId == ResidentialInnLayout.InteriorFloorId)
            {
                counterBinding.Configure(
                    ResidentialInnLayout.CustomerRowY,
                    ResidentialInnLayout.CounterRowY);
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
