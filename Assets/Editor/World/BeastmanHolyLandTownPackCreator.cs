#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.GridFeatures;
using JRogue.Item;
using JRogue.Manager.Grid;
using JRogue.Shop;
using JRogue.World.Generation;
using JRogue.World.Generation.Zones;
using JRogue.World.Town;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    /// <summary>Beastman Holy Land — stone grounds, chief NPC, den beast blood merchant at 2 gold.</summary>
    public static class BeastmanHolyLandTownPackCreator
    {
        const string WallTilePath = "Assets/TileMaps/Town/Town_WallBuilding.asset";
        const string BuildingWallTilePath = "Assets/TileMaps/Town/Town_Building_StoneWall.asset";
        const string BuildingDoorTilePath = "Assets/TileMaps/Town/Town_Building_Door.asset";
        const string DcssTileFolder = "Assets/TileMaps/Town/Dcss";
        const string HumanNpcPrefabPath = "Assets/Prefabs/Actor/Npc/HumanNpc.prefab";
        const string ChiefDialogPath = "Assets/Resources/Dialog/Profiles/NpcDialog_ChiefBeastman.asset";
        const string ChiefPrefabPath = "Assets/Resources/Town/Npc/TownNpc_ChiefBeastman.prefab";
        const string ChiefPortraitPath = "Assets/Resources/Dialog/Portraits/Portrait_Race_Beastman.asset";
        const string ChiefSpritePath = "Assets/Art/NPC/Sprites/NPC_Beastman_Brute.png";
        const string MerchantSpritePath = "Assets/Art/NPC/Sprites/NPC_BeastBloodMerchant.png";
        const string MerchantPortraitPath = "Assets/Resources/Dialog/Portraits/Portrait_BeastBloodMerchant.asset";
        const string BeastBloodItemPath = "Assets/Resources/Item/Misc/BeastBlood.asset";
        const string FormationPath = "Assets/Resources/Dungeon/PartyFormation_Default.asset";

        static readonly string[] StoneFloorTileAssets =
        {
            "Dcss_Floor_RectGray0.asset",
            "Dcss_Floor_RectGray1.asset",
            "Dcss_Floor_RectGray2.asset",
            "Dcss_Floor_RectGray3.asset",
        };

        [MenuItem("JRogue/Town/Setup Beastman Holy Land")]
        public static void SetupBeastmanHolyLand()
        {
            EnsureFolders();
            DcssRectGrayFloorVarietyEditor.ConfigureRectGrayFloorTiles();
            EnsureChiefDialogAndPrefab();
            EnsureDenBeastBloodMerchantPack();
            EnsureBeastmanHolyLandFloorDefinition();
            EnsureBeastmanDenInteriorFloorDefinition();
            HolyLandTownPackCreator.SetupHolyLand();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BeastmanHolyLand] Data ready. Run JRogue → Town → Fix District Town Test Scene.");
        }

        [MenuItem("JRogue/Town/Create / Update Beastman Holy Land Floors")]
        public static void CreateOrUpdateBeastmanHolyLandFloors()
        {
            SetupBeastmanHolyLand();
            DimensionSquareSceneCreator.FixDimensionSquareTestScene();
        }

        public static void IntegrateBeastmanHolyLandScene(DungeonFloorInstance instance) =>
            PaintBeastmanHolyLandLayout(instance);

        public static void IntegrateBeastmanDenInteriorScene(DungeonFloorInstance instance) =>
            PaintBeastmanDenInteriorLayout(instance);

        public static void AppendToDimensionSquareCatalog(
            List<DungeonFloorDefinition> hubFloors,
            DungeonFloorDefinition beastmanHolyLandDef,
            DungeonFloorDefinition beastmanDenDef)
        {
            HolyLandTownPackCreator.AppendIfMissingPublic(hubFloors, beastmanHolyLandDef);
            HolyLandTownPackCreator.AppendIfMissingPublic(hubFloors, beastmanDenDef);
        }

        static void EnsureFolders()
        {
            EnsureFolder(TownDistrictTestPaths.HolyLandFolder);
            EnsureFolder("Assets/Resources/Town/Npc");
            EnsureFolder("Assets/Resources/Dialog/Profiles");
            EnsureFolder("Assets/Resources/Dialog/Portraits");
            EnsureFolder("Assets/Resources/Shop");
        }

        static void EnsureChiefDialogAndPrefab()
        {
            EnsureChiefPortrait();

            var dialog = LoadOrCreate<NpcDialogProfile>(ChiefDialogPath);
            var dialogSo = new SerializedObject(dialog);
            dialogSo.FindProperty("npcId").stringValue = BeastmanHolyLandLayout.ChiefMarkerId;
            dialogSo.FindProperty("rootNodeIndex").intValue = 0;
            SerializedProperty nodes = dialogSo.FindProperty("nodes");
            nodes.arraySize = 3;
            WriteChiefLine(nodes.GetArrayElementAtIndex(0),
                "Welcome to the den, kin of tooth and claw. The pack remembers your scent.",
                1);
            WriteChiefLine(nodes.GetArrayElementAtIndex(1),
                "Here the wild heart is honored. Rest your limbs before the hunt calls again.",
                2);
            WriteChiefLine(nodes.GetArrayElementAtIndex(2),
                "Walk with purpose. The stone beneath you has felt many pawprints.",
                -1);
            dialogSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dialog);

            GameObject humanNpc = AssetDatabase.LoadAssetAtPath<GameObject>(HumanNpcPrefabPath);
            if (humanNpc == null)
                return;

            bool createdNew = !File.Exists(ChiefPrefabPath);
            GameObject instance = createdNew
                ? (GameObject)PrefabUtility.InstantiatePrefab(humanNpc)
                : PrefabUtility.LoadPrefabContents(ChiefPrefabPath);

            try
            {
                instance.name = "TownNpc_ChiefBeastman";
                NpcController npc = instance.GetComponent<NpcController>() ?? instance.AddComponent<NpcController>();
                var npcSo = new SerializedObject(npc);
                npcSo.FindProperty("npcId").stringValue = BeastmanHolyLandLayout.ChiefMarkerId;
                npcSo.FindProperty("displayName").stringValue = "Chief of the Beastmen";
                npcSo.FindProperty("dialogProfile").objectReferenceValue = dialog;
                npcSo.FindProperty("portrait").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<PortraitDefinition>(ChiefPortraitPath);
                npcSo.ApplyModifiedPropertiesWithoutUndo();

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ChiefSpritePath);
                SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
                if (renderer != null && sprite != null)
                {
                    renderer.sprite = sprite;
                    renderer.sortingOrder = 20;
                }

                PrefabUtility.SaveAsPrefabAsset(instance, ChiefPrefabPath);
            }
            finally
            {
                if (!createdNew)
                    PrefabUtility.UnloadPrefabContents(instance);
            }
        }

        static void EnsureChiefPortrait()
        {
            var portrait = LoadOrCreate<PortraitDefinition>(ChiefPortraitPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ChiefSpritePath);
            if (sprite != null)
            {
                portrait.portrait = sprite;
                EditorUtility.SetDirty(portrait);
            }
        }

        static void WriteChiefLine(SerializedProperty node, string text, int nextNodeIndex)
        {
            node.FindPropertyRelative("kind").enumValueIndex = 0;
            node.FindPropertyRelative("line").FindPropertyRelative("textTemplate").stringValue = text;
            node.FindPropertyRelative("nextNodeIndex").intValue = nextNodeIndex;
        }

        static void EnsureDenBeastBloodMerchantPack()
        {
            BeastBloodItemData beastBlood = AssetDatabase.LoadAssetAtPath<BeastBloodItemData>(BeastBloodItemPath);
            if (beastBlood != null)
            {
                beastBlood.buyValue = 2;
                beastBlood.sellValue = 0;
                EditorUtility.SetDirty(beastBlood);
            }

            PortraitDefinition portrait =
                AssetDatabase.LoadAssetAtPath<PortraitDefinition>(MerchantPortraitPath);
            ShopNpcDefinition shop = EnsureDenBeastBloodMerchantShop(beastBlood, portrait);

            GameObject humanNpc = AssetDatabase.LoadAssetAtPath<GameObject>(HumanNpcPrefabPath);
            if (humanNpc == null)
                return;

            CreateDenBeastBloodMerchantPrefab(humanNpc, shop);
        }

        static ShopNpcDefinition EnsureDenBeastBloodMerchantShop(
            BeastBloodItemData beastBlood,
            PortraitDefinition portrait)
        {
            string path = "Assets/Resources/Shop/ShopNpc_BeastmanDenBeastBloodMerchant.asset";
            var shop = LoadOrCreate<ShopNpcDefinition>(path);
            shop.shopNpcId = TownShopNpcIds.BeastmanDenBeastBloodMerchant;
            shop.displayName = "Den Beast Blood Merchant";
            shop.portrait = portrait;
            shop.allowPlayerBuy = true;
            shop.allowPlayerSell = false;
            shop.initialGold = 100;
            shop.initialStock = beastBlood != null
                ? new[] { new ShopStockEntry { item = beastBlood, quantity = 99 } }
                : System.Array.Empty<ShopStockEntry>();
            EditorUtility.SetDirty(shop);
            return shop;
        }

        static void CreateDenBeastBloodMerchantPrefab(GameObject humanNpcBase, ShopNpcDefinition shopDefinition)
        {
            const string prefabPath = "Assets/Resources/Town/Npc/TownNpc_BeastmanDenBeastBloodMerchant.prefab";
            NpcDialogPackCreator.DeletePrefabIfPresent(prefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(humanNpcBase) as GameObject;
            instance.name = "TownNpc_BeastmanDenBeastBloodMerchant";

            NpcController dialogNpc = instance.GetComponent<NpcController>();
            if (dialogNpc != null)
                Object.DestroyImmediate(dialogNpc, true);

            ShopNpcController shopNpc = instance.AddComponent<ShopNpcController>();
            SerializedObject shopSo = new SerializedObject(shopNpc);
            shopSo.FindProperty("npcId").stringValue = shopDefinition.shopNpcId;
            shopSo.FindProperty("shopDefinition").objectReferenceValue = shopDefinition;
            shopSo.FindProperty("portrait").objectReferenceValue = shopDefinition.portrait;
            shopSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject actorSo = new SerializedObject(shopNpc);
            actorSo.FindProperty("displayName").stringValue = shopDefinition.displayName;
            actorSo.ApplyModifiedPropertiesWithoutUndo();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MerchantSpritePath);
            SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
            if (renderer != null && sprite != null)
                renderer.sprite = sprite;

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
        }

        static DungeonFloorDefinition EnsureBeastmanHolyLandFloorDefinition()
        {
            EnsureBeastmanHolyLandPalettes();
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{DcssTileFolder}/{StoneFloorTileAssets[0]}");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            PartyFormationSpawnProfile formation = AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(FormationPath);

            var def = LoadOrCreate<DungeonFloorDefinition>(TownDistrictTestPaths.BeastmanHolyLandProperFloorDef);
            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = HolyLandFloorIds.BeastmanHolyLandProper;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("defaultFloorPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.BeastmanHolyLandProperFloorPalette);
            so.FindProperty("defaultWallPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.HolyLandWallPalette);
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;

            SerializedProperty portals = so.FindProperty("portals");
            portals.arraySize = 2;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.BeastmanHolyLandToNexus;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("targetFloorId").stringValue = HolyLandFloorIds.Nexus;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("portalCell").vector3IntValue =
                BeastmanHolyLandLayout.ReturnToNexusCell;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("listLabel").stringValue = "Nexus";
            portals.GetArrayElementAtIndex(1).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.BeastmanDenEnter;
            portals.GetArrayElementAtIndex(1).FindPropertyRelative("targetFloorId").stringValue =
                HolyLandFloorIds.BeastmanDenInterior;
            portals.GetArrayElementAtIndex(1).FindPropertyRelative("portalCell").vector3IntValue =
                BeastmanHolyLandLayout.DenDoorCell;
            portals.GetArrayElementAtIndex(1).FindPropertyRelative("listLabel").stringValue = "Beastman Den";

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 2;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.NexusToBeastmanHolyLand;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("arrivalAnchor").vector3IntValue =
                HolyLandNexusLayout.HolyLandArrivalCell;
            arrivals.GetArrayElementAtIndex(1).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.BeastmanDenExit;
            arrivals.GetArrayElementAtIndex(1).FindPropertyRelative("arrivalAnchor").vector3IntValue =
                BeastmanHolyLandLayout.DenDoorCell;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static DungeonFloorDefinition EnsureBeastmanDenInteriorFloorDefinition()
        {
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{DcssTileFolder}/{StoneFloorTileAssets[0]}");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            PartyFormationSpawnProfile formation = AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(FormationPath);

            var def = LoadOrCreate<DungeonFloorDefinition>(TownDistrictTestPaths.BeastmanDenInteriorFloorDef);
            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = HolyLandFloorIds.BeastmanDenInterior;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("defaultFloorPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.BeastmanDenInteriorFloorPalette);
            so.FindProperty("defaultWallPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.HolyLandWallPalette);
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;

            SerializedProperty portals = so.FindProperty("portals");
            portals.arraySize = 1;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.BeastmanDenExit;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("targetFloorId").stringValue =
                HolyLandFloorIds.BeastmanHolyLandProper;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("portalCell").vector3IntValue =
                BeastmanHolyLandDenLayout.InteriorExitCell;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("listLabel").stringValue = "Beastman Holy Land";

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 1;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.BeastmanDenEnter;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("arrivalAnchor").vector3IntValue =
                BeastmanHolyLandDenLayout.InteriorArrivalCell;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static void EnsureBeastmanHolyLandPalettes()
        {
            var stoneTiles = new List<(string tilePath, int weight)>();
            for (int i = 0; i < StoneFloorTileAssets.Length; i++)
                stoneTiles.Add(($"{DcssTileFolder}/{StoneFloorTileAssets[i]}", 5));

            DistrictTownTestSceneCreator.CreateOrUpdatePalette(
                TownDistrictTestPaths.BeastmanHolyLandProperFloorPalette,
                "beastman_holy_land_floor",
                DungeonTilePaletteLayer.Floor,
                stoneTiles.ToArray());

            DistrictTownTestSceneCreator.CreateOrUpdatePalette(
                TownDistrictTestPaths.BeastmanDenInteriorFloorPalette,
                "beastman_den_interior_floor",
                DungeonTilePaletteLayer.Floor,
                stoneTiles.ToArray());
        }

        static void PaintBeastmanHolyLandLayout(DungeonFloorInstance instance)
        {
            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Tilemap wallMap = instance.Tilemaps.WallMap;
            TileBase[] stoneTiles = LoadPaletteTiles(TownDistrictTestPaths.BeastmanHolyLandProperFloorPalette);
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            TileBase buildingWall = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingWallTilePath);
            TileBase buildingDoor = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingDoorTilePath);
            if (floorMap == null || wallMap == null || stoneTiles == null || wallTile == null)
                return;

            Undo.RecordObject(floorMap, "Paint beastman holy land floor");
            Undo.RecordObject(wallMap, "Paint beastman holy land walls");
            BeastmanHolyLandLayout.Paint(floorMap, wallMap, stoneTiles, wallTile, buildingWall, buildingDoor);
            FinalizePaint(floorMap, wallMap);
            EnsureBeastmanHolyLandMarkers(instance);
            instance.MarkNeedsRegeneration();
        }

        static void PaintBeastmanDenInteriorLayout(DungeonFloorInstance instance)
        {
            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Tilemap wallMap = instance.Tilemaps.WallMap;
            TileBase[] floorTiles = LoadPaletteTiles(TownDistrictTestPaths.BeastmanDenInteriorFloorPalette);
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            TileBase doorTile = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingDoorTilePath);
            if (floorMap == null || wallMap == null || floorTiles == null || wallTile == null)
                return;

            Undo.RecordObject(floorMap, "Paint beastman den interior floor");
            Undo.RecordObject(wallMap, "Paint beastman den interior walls");
            BarbarianShamanTentLayout.Paint(floorMap, wallMap, floorTiles, wallTile, doorTile);
            FinalizePaint(floorMap, wallMap);
            EnsureBeastmanDenInteriorMarkers(instance);
            instance.MarkNeedsRegeneration();
        }

        static void EnsureBeastmanHolyLandMarkers(DungeonFloorInstance instance)
        {
            Transform markersRoot = EnsureMarkersRoot(instance);
            ClearChildren(markersRoot);
            CreateMarker(markersRoot, instance, "PlayerStart", StaticHubMarkerKind.PlayerStart, BeastmanHolyLandLayout.PlayerStartCell);
            CreateMarker(markersRoot, instance, "Chief", StaticHubMarkerKind.NpcSlot, BeastmanHolyLandLayout.ChiefNpcCell, BeastmanHolyLandLayout.ChiefMarkerId);
        }

        static void EnsureBeastmanDenInteriorMarkers(DungeonFloorInstance instance)
        {
            Transform markersRoot = EnsureMarkersRoot(instance);
            ClearChildren(markersRoot);
            CreateMarker(markersRoot, instance, "PlayerStart", StaticHubMarkerKind.PlayerStart, BeastmanHolyLandDenLayout.InteriorArrivalCell);
            CreateMarker(
                markersRoot,
                instance,
                "BeastBloodMerchant",
                StaticHubMarkerKind.NpcSlot,
                BeastmanHolyLandDenLayout.BeastBloodMerchantNpcCell,
                BeastmanHolyLandDenLayout.BeastBloodMerchantMarkerId);
        }

        static Transform EnsureMarkersRoot(DungeonFloorInstance instance)
        {
            Transform markersRoot = instance.transform.Find("Markers");
            if (markersRoot == null)
            {
                var markersGo = new GameObject("Markers");
                markersGo.transform.SetParent(instance.transform, false);
                markersRoot = markersGo.transform;
            }

            return markersRoot;
        }

        static void CreateMarker(
            Transform parent,
            DungeonFloorInstance instance,
            string objectName,
            StaticHubMarkerKind kind,
            Vector3Int cell,
            string markerId = null)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            StaticHubMarker marker = go.AddComponent<StaticHubMarker>();
            marker.EditorConfigure(kind, cell, markerId);

            Transform grid = instance.transform.Find("Grid");
            Grid gridComponent = grid != null ? grid.GetComponent<Grid>() : null;
            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Vector3 world = gridComponent != null
                ? gridComponent.GetCellCenterWorld(cell)
                : GridCellWorld.GetCellCenter(floorMap, cell);
            go.transform.position = world;
        }

        static TileBase[] LoadPaletteTiles(string palettePath)
        {
            var palette = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(palettePath);
            if (palette?.Entries == null)
                return null;

            var tiles = new List<TileBase>();
            for (int i = 0; i < palette.Entries.Length; i++)
            {
                if (palette.Entries[i].tile != null)
                    tiles.Add(palette.Entries[i].tile);
            }

            return tiles.Count > 0 ? tiles.ToArray() : null;
        }

        static void FinalizePaint(Tilemap floorMap, Tilemap wallMap)
        {
            GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(floorMap);
            GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(wallMap);
            floorMap.CompressBounds();
            wallMap.CompressBounds();
            EditorUtility.SetDirty(floorMap);
            EditorUtility.SetDirty(wallMap);
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, folderName);
        }

        static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }
}
#endif
