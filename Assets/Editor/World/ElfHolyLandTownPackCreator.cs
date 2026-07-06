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
    /// <summary>Elf Holy Land — grass grove, chief NPC, grove fairy stone merchant at 2 gold.</summary>
    public static class ElfHolyLandTownPackCreator
    {
        const string WallTilePath = "Assets/TileMaps/Town/Town_WallBuilding.asset";
        const string BuildingWallTilePath = "Assets/TileMaps/Town/Town_Building_StoneWall.asset";
        const string BuildingDoorTilePath = "Assets/TileMaps/Town/Town_Building_Door.asset";
        const string DcssTileFolder = "Assets/TileMaps/Town/Dcss";
        const string GrassSpriteFolder =
            "Assets/Sprites/DCSS/Dungeon Crawl Stone Soup Full/dungeon/floor/grass";
        const string GrassTileFolder = "Assets/TileMaps/Dcss/Grass";
        const string HumanNpcPrefabPath = "Assets/Prefabs/Actor/Npc/HumanNpc.prefab";
        const string ChiefDialogPath = "Assets/Resources/Dialog/Profiles/NpcDialog_ChiefElf.asset";
        const string ChiefPrefabPath = "Assets/Resources/Town/Npc/TownNpc_ChiefElf.prefab";
        const string ChiefPortraitPath = "Assets/Resources/Dialog/Portraits/Portrait_Race_Elf.asset";
        const string ChiefSpritePath = "Assets/Art/NPC/Sprites/NPC_Elf_Sage.png";
        const string FairyMerchantSpritePath = "Assets/Art/NPC/Sprites/NPC_FairyMerchant.png";
        const string FairyMerchantPortraitPath = "Assets/Resources/Dialog/Portraits/Portrait_FairyMerchant.asset";
        const string FormationPath = "Assets/Resources/Dungeon/PartyFormation_Default.asset";

        static readonly string[] StoneFloorTileAssets =
        {
            "Dcss_Floor_RectGray0.asset",
            "Dcss_Floor_RectGray1.asset",
            "Dcss_Floor_RectGray2.asset",
            "Dcss_Floor_RectGray3.asset",
        };

        static readonly (string spriteFile, string tileName)[] GrassFloorSprites =
        {
            ("grass_0_new.png", "grass_0_new"),
            ("grass_1_new.png", "grass_1_new"),
            ("grass_2_new.png", "grass_2_new"),
            ("grass_flowers_blue_1_new.png", "grass_flowers_blue_1_new"),
            ("grass_flowers_blue_2_new.png", "grass_flowers_blue_2_new"),
            ("grass_flowers_yellow_1_new.png", "grass_flowers_yellow_1_new"),
            ("grass_flowers_yellow_2_new.png", "grass_flowers_yellow_2_new"),
            ("grass_flowers_red_1_new.png", "grass_flowers_red_1_new"),
        };

        [MenuItem("JRogue/Town/Setup Elf Holy Land")]
        public static void SetupElfHolyLand()
        {
            EnsureFolders();
            EnsureGrassFloorTiles();
            EnsureChiefDialogAndPrefab();
            EnsureGroveFairyMerchantPack();
            DungeonFloorDefinition holyLandDef = EnsureElfHolyLandFloorDefinition();
            DungeonFloorDefinition houseDef = EnsureElfHouseInteriorFloorDefinition();
            HolyLandTownPackCreator.SetupHolyLand();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ElfHolyLand] Data ready. Run JRogue → Town → Fix District Town Test Scene.");
        }

        [MenuItem("JRogue/Town/Create / Update Elf Holy Land Floors")]
        public static void CreateOrUpdateElfHolyLandFloors()
        {
            SetupElfHolyLand();
            DimensionSquareSceneCreator.FixDimensionSquareTestScene();
        }

        public static void IntegrateElfHolyLandScene(DungeonFloorInstance instance) => PaintElfHolyLandLayout(instance);
        public static void IntegrateElfHouseInteriorScene(DungeonFloorInstance instance) => PaintElfHouseInteriorLayout(instance);

        public static void AppendToDimensionSquareCatalog(
            List<DungeonFloorDefinition> hubFloors,
            DungeonFloorDefinition elfHolyLandDef,
            DungeonFloorDefinition elfHouseDef)
        {
            HolyLandTownPackCreator.AppendIfMissingPublic(hubFloors, elfHolyLandDef);
            HolyLandTownPackCreator.AppendIfMissingPublic(hubFloors, elfHouseDef);
        }

        static void EnsureFolders()
        {
            EnsureFolder(TownDistrictTestPaths.HolyLandFolder);
            EnsureFolder(GrassTileFolder);
            EnsureFolder("Assets/Resources/Town/Npc");
            EnsureFolder("Assets/Resources/Dialog/Profiles");
            EnsureFolder("Assets/Resources/Shop");
            EnsureFolder("Assets/Resources/Item/Misc");
        }

        static void EnsureGrassFloorTiles()
        {
            for (int i = 0; i < GrassFloorSprites.Length; i++)
            {
                (string spriteFile, string tileName) = GrassFloorSprites[i];
                EnsureTileFromSingleSprite($"{GrassSpriteFolder}/{spriteFile}", tileName);
            }
        }

        static void EnsureChiefDialogAndPrefab()
        {
            var dialog = LoadOrCreate<NpcDialogProfile>(ChiefDialogPath);
            var dialogSo = new SerializedObject(dialog);
            dialogSo.FindProperty("npcId").stringValue = ElfHolyLandLayout.ChiefMarkerId;
            dialogSo.FindProperty("rootNodeIndex").intValue = 0;
            SerializedProperty nodes = dialogSo.FindProperty("nodes");
            nodes.arraySize = 3;
            WriteChiefLine(nodes.GetArrayElementAtIndex(0),
                "Welcome to the grove, child of the greenwood. Rest your feet among the elders' roots.",
                1);
            WriteChiefLine(nodes.GetArrayElementAtIndex(1),
                "Long have we kept watch where the wild things stir and the spirits walk between boughs.",
                2);
            WriteChiefLine(nodes.GetArrayElementAtIndex(2),
                "Walk softly here. The forest remembers every step.",
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
                instance.name = "TownNpc_ChiefElf";
                NpcController npc = instance.GetComponent<NpcController>() ?? instance.AddComponent<NpcController>();
                var npcSo = new SerializedObject(npc);
                npcSo.FindProperty("npcId").stringValue = ElfHolyLandLayout.ChiefMarkerId;
                npcSo.FindProperty("displayName").stringValue = "Chief of the Elves";
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

        static void WriteChiefLine(SerializedProperty node, string text, int nextNodeIndex)
        {
            node.FindPropertyRelative("kind").enumValueIndex = 0;
            node.FindPropertyRelative("line").FindPropertyRelative("textTemplate").stringValue = text;
            node.FindPropertyRelative("nextNodeIndex").intValue = nextNodeIndex;
        }

        static void EnsureGroveFairyMerchantPack()
        {
            FairyStoneItemData fairyStone = EnsureGroveFairyStoneAsset();
            PortraitDefinition portrait =
                AssetDatabase.LoadAssetAtPath<PortraitDefinition>(FairyMerchantPortraitPath);
            ShopNpcDefinition shop = EnsureGroveFairyMerchantShop(fairyStone, portrait);

            GameObject humanNpc = AssetDatabase.LoadAssetAtPath<GameObject>(HumanNpcPrefabPath);
            if (humanNpc == null)
                return;

            CreateGroveFairyMerchantPrefab(humanNpc, shop);
        }

        static FairyStoneItemData EnsureGroveFairyStoneAsset()
        {
            string path = "Assets/Resources/Item/Misc/FairyStone_ElfGrove.asset";
            var stone = LoadOrCreate<FairyStoneItemData>(path);
            stone.itemName = "Fairy Stone";
            stone.category = ItemCategory.Junk;
            stone.buyValue = 2;
            stone.sellValue = 0;
            stone.weight = 0.1f;
            stone.allowUseInSafeZone = true;
            EditorUtility.SetDirty(stone);
            return stone;
        }

        static ShopNpcDefinition EnsureGroveFairyMerchantShop(FairyStoneItemData fairyStone, PortraitDefinition portrait)
        {
            string path = "Assets/Resources/Shop/ShopNpc_ElfGroveFairyMerchant.asset";
            var shop = LoadOrCreate<ShopNpcDefinition>(path);
            shop.shopNpcId = TownShopNpcIds.ElfGroveFairyMerchant;
            shop.displayName = "Grove Fairy Merchant";
            shop.portrait = portrait;
            shop.allowPlayerBuy = true;
            shop.allowPlayerSell = false;
            shop.initialGold = 100;
            shop.initialStock = fairyStone != null
                ? new[] { new ShopStockEntry { item = fairyStone, quantity = 99 } }
                : System.Array.Empty<ShopStockEntry>();
            EditorUtility.SetDirty(shop);
            return shop;
        }

        static void CreateGroveFairyMerchantPrefab(GameObject humanNpcBase, ShopNpcDefinition shopDefinition)
        {
            const string prefabPath = "Assets/Resources/Town/Npc/TownNpc_ElfGroveFairyMerchant.prefab";
            NpcDialogPackCreator.DeletePrefabIfPresent(prefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(humanNpcBase) as GameObject;
            instance.name = "TownNpc_ElfGroveFairyMerchant";

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

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FairyMerchantSpritePath);
            SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
            if (renderer != null && sprite != null)
                renderer.sprite = sprite;

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
        }

        static DungeonFloorDefinition EnsureElfHolyLandFloorDefinition()
        {
            EnsureElfHolyLandPalettes();
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{GrassTileFolder}/{GrassFloorSprites[0].tileName}.asset");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            PartyFormationSpawnProfile formation = AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(FormationPath);

            var def = LoadOrCreate<DungeonFloorDefinition>(TownDistrictTestPaths.ElfHolyLandProperFloorDef);
            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = HolyLandFloorIds.ElfHolyLandProper;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("defaultFloorPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.ElfHolyLandProperFloorPalette);
            so.FindProperty("defaultWallPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.HolyLandWallPalette);
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;

            SerializedProperty portals = so.FindProperty("portals");
            portals.arraySize = 2;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.ElfHolyLandToNexus;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("targetFloorId").stringValue = HolyLandFloorIds.Nexus;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("portalCell").vector3IntValue =
                ElfHolyLandLayout.ReturnToNexusCell;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("listLabel").stringValue = "Nexus";
            portals.GetArrayElementAtIndex(1).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.ElfHouseEnter;
            portals.GetArrayElementAtIndex(1).FindPropertyRelative("targetFloorId").stringValue =
                HolyLandFloorIds.ElfHouseInterior;
            portals.GetArrayElementAtIndex(1).FindPropertyRelative("portalCell").vector3IntValue =
                ElfHolyLandLayout.HouseDoorCell;
            portals.GetArrayElementAtIndex(1).FindPropertyRelative("listLabel").stringValue = "Grove House";

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 2;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.NexusToElfHolyLand;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("arrivalAnchor").vector3IntValue =
                HolyLandNexusLayout.HolyLandArrivalCell;
            arrivals.GetArrayElementAtIndex(1).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.ElfHouseExit;
            arrivals.GetArrayElementAtIndex(1).FindPropertyRelative("arrivalAnchor").vector3IntValue =
                ElfHolyLandLayout.HouseDoorCell;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static DungeonFloorDefinition EnsureElfHouseInteriorFloorDefinition()
        {
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{DcssTileFolder}/{StoneFloorTileAssets[0]}");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            PartyFormationSpawnProfile formation = AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(FormationPath);

            var def = LoadOrCreate<DungeonFloorDefinition>(TownDistrictTestPaths.ElfHolyLandHouseInteriorFloorDef);
            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = HolyLandFloorIds.ElfHouseInterior;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("defaultFloorPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.ElfHolyLandHouseInteriorFloorPalette);
            so.FindProperty("defaultWallPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.HolyLandWallPalette);
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;

            SerializedProperty portals = so.FindProperty("portals");
            portals.arraySize = 1;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.ElfHouseExit;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("targetFloorId").stringValue =
                HolyLandFloorIds.ElfHolyLandProper;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("portalCell").vector3IntValue =
                ElfHolyLandHouseLayout.InteriorExitCell;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("listLabel").stringValue = "Elf Holy Land";

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 1;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.ElfHouseEnter;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("arrivalAnchor").vector3IntValue =
                ElfHolyLandHouseLayout.InteriorArrivalCell;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static void EnsureElfHolyLandPalettes()
        {
            var grassTiles = new List<(string tilePath, int weight)>();
            for (int i = 0; i < GrassFloorSprites.Length; i++)
                grassTiles.Add(($"{GrassTileFolder}/{GrassFloorSprites[i].tileName}.asset", 5));

            DistrictTownTestSceneCreator.CreateOrUpdatePalette(
                TownDistrictTestPaths.ElfHolyLandProperFloorPalette,
                "elf_holy_land_floor",
                DungeonTilePaletteLayer.Floor,
                grassTiles.ToArray());

            var houseFloorTiles = new List<(string tilePath, int weight)>();
            for (int i = 0; i < StoneFloorTileAssets.Length; i++)
                houseFloorTiles.Add(($"{DcssTileFolder}/{StoneFloorTileAssets[i]}", 5));

            DistrictTownTestSceneCreator.CreateOrUpdatePalette(
                TownDistrictTestPaths.ElfHolyLandHouseInteriorFloorPalette,
                "elf_holy_land_house_interior_floor",
                DungeonTilePaletteLayer.Floor,
                houseFloorTiles.ToArray());
        }

        static void PaintElfHolyLandLayout(DungeonFloorInstance instance)
        {
            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Tilemap wallMap = instance.Tilemaps.WallMap;
            TileBase[] grassTiles = LoadPaletteTiles(TownDistrictTestPaths.ElfHolyLandProperFloorPalette);
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            TileBase buildingWall = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingWallTilePath);
            TileBase buildingDoor = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingDoorTilePath);
            if (floorMap == null || wallMap == null || grassTiles == null || wallTile == null)
                return;

            Undo.RecordObject(floorMap, "Paint elf holy land floor");
            Undo.RecordObject(wallMap, "Paint elf holy land walls");
            ElfHolyLandLayout.Paint(floorMap, wallMap, grassTiles, wallTile, buildingWall, buildingDoor);
            FinalizePaint(floorMap, wallMap);
            EnsureElfHolyLandMarkers(instance);
            instance.MarkNeedsRegeneration();
        }

        static void PaintElfHouseInteriorLayout(DungeonFloorInstance instance)
        {
            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Tilemap wallMap = instance.Tilemaps.WallMap;
            TileBase[] floorTiles = LoadPaletteTiles(TownDistrictTestPaths.ElfHolyLandHouseInteriorFloorPalette);
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            TileBase doorTile = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingDoorTilePath);
            if (floorMap == null || wallMap == null || floorTiles == null || wallTile == null)
                return;

            Undo.RecordObject(floorMap, "Paint elf grove house interior floor");
            Undo.RecordObject(wallMap, "Paint elf grove house interior walls");
            BarbarianShamanTentLayout.Paint(floorMap, wallMap, floorTiles, wallTile, doorTile);
            FinalizePaint(floorMap, wallMap);
            EnsureElfHouseInteriorMarkers(instance);
            instance.MarkNeedsRegeneration();
        }

        static void EnsureElfHolyLandMarkers(DungeonFloorInstance instance)
        {
            Transform markersRoot = EnsureMarkersRoot(instance);
            ClearChildren(markersRoot);
            CreateMarker(markersRoot, instance, "PlayerStart", StaticHubMarkerKind.PlayerStart, ElfHolyLandLayout.PlayerStartCell);
            CreateMarker(markersRoot, instance, "Chief", StaticHubMarkerKind.NpcSlot, ElfHolyLandLayout.ChiefNpcCell, ElfHolyLandLayout.ChiefMarkerId);
        }

        static void EnsureElfHouseInteriorMarkers(DungeonFloorInstance instance)
        {
            Transform markersRoot = EnsureMarkersRoot(instance);
            ClearChildren(markersRoot);
            CreateMarker(markersRoot, instance, "PlayerStart", StaticHubMarkerKind.PlayerStart, ElfHolyLandHouseLayout.InteriorArrivalCell);
            CreateMarker(
                markersRoot,
                instance,
                "FairyMerchant",
                StaticHubMarkerKind.NpcSlot,
                ElfHolyLandHouseLayout.FairyMerchantNpcCell,
                ElfHolyLandHouseLayout.FairyMerchantMarkerId);
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

        static void EnsureTileFromSingleSprite(string spritePath, string tileName)
        {
            EnsureSingleSpriteImport(spritePath);
            string tilePath = $"{GrassTileFolder}/{tileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<Tile>(tilePath) != null)
                return;

            Sprite sprite = LoadSingleSprite(spritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[ElfHolyLand] Missing sprite at {spritePath}");
                return;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            AssetDatabase.CreateAsset(tile, tilePath);
        }

        static void EnsureSingleSpriteImport(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        static Sprite LoadSingleSprite(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
                return sprite;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite loaded)
                    return loaded;
            }

            return null;
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
