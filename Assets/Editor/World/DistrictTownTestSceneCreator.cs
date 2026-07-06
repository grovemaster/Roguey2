#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.GridFeatures;
using JRogue.Manager.Door;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Visibility;
using JRogue.View;
using JRogue.World.Generation;
using JRogue.World.Generation.Zones;
using JRogue.World.Lighting;
using JRogue.World.MapInteract;
using JRogue.World.Town;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    /// <summary>
    /// DistrictTest hub scene: scene-painted Dimension Square under Resources/Town/DistrictTest/.
    /// </summary>
    public static class DistrictTownTestSceneCreator
    {
        const string TemplateScenePath = "Assets/Scenes/Dungeon/DungeonFloorTest.unity";
        const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        const string WallTilePath = "Assets/TileMaps/Town/Town_WallBuilding.asset";
        const string DcssTileFolder = "Assets/TileMaps/Town/Dcss";

        static readonly string[] DcssFloorTileAssets =
        {
            "Dcss_Floor_RectGray0.asset",
            "Dcss_Floor_RectGray1.asset",
            "Dcss_Floor_RectGray2.asset",
            "Dcss_Floor_RectGray3.asset",
        };

        [MenuItem("JRogue/Town/Create District Town Test Scene")]
        public static void CreateDistrictTownTestScene() => CreateOrFixInternal(repaintTiles: true);

        [MenuItem("JRogue/Town/Fix District Town Test Scene")]
        public static void FixDistrictTownTestScene() => CreateOrFixInternal(repaintTiles: true);

        static void CreateOrFixInternal(bool repaintTiles)
        {
            if (!File.Exists(TemplateScenePath))
            {
                Debug.LogError(
                    $"[DistrictTownTest] Missing template {TemplateScenePath}. Run JRogue → Dungeon → Create DungeonFloorTest Scene.");
                return;
            }

            EnsureDistrictFolders();
            DcssRectGrayFloorVarietyEditor.ConfigureRectGrayFloorTiles();
            EnsureDimensionSquarePalettes();
            DungeonFloorDefinition squareDef = EnsureDimensionSquareFloorDefinition();
            AdventureGuildExchangePackCreator.SetupAdventureGuildExchange();
            AdventureGuildHallPackCreator.SetupAdventureGuildHall();
            MarketTownPackCreator.SetupMarketTown();
            ResidentialTownPackCreator.SetupResidentialTown();
            ResidentialInnPackCreator.SetupResidentialInn();
            MarketGeneralStorePackCreator.SetupMarketGeneralStore();
            MarketItemShopPackCreator.SetupMarketItemShop();
            BlacksmithShopPackCreator.SetupBlacksmithShop();
            HolyLandTownPackCreator.SetupHolyLand();
            ElfHolyLandTownPackCreator.SetupElfHolyLand();
            DungeonFloorDefinition guildInteriorDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.AdventureGuildInteriorFloorDef);
            DungeonFloorDefinition guildHallInteriorDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.AdventureGuildHallInteriorFloorDef);
            DungeonFloorDefinition marketDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.MarketFloorDef);
            DungeonFloorDefinition residentialDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.ResidentialFloorDef);
            DungeonFloorDefinition storeInteriorDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.MarketGeneralStoreInteriorFloorDef);
            DungeonFloorDefinition itemShopInteriorDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.MarketItemShopInteriorFloorDef);
            DungeonFloorDefinition blacksmithInteriorDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.MarketBlacksmithInteriorFloorDef);
            DungeonFloorDefinition innInteriorDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.ResidentialInnInteriorFloorDef);
            DungeonFloorDefinition nexusDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.HolyLandNexusFloorDef);
            DungeonFloorDefinition barbarianHolyLandDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.HolyLandProperFloorDef);
            DungeonFloorDefinition shamanTentDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.HolyLandTentInteriorFloorDef);
            DungeonFloorDefinition elfHolyLandDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.ElfHolyLandProperFloorDef);
            DungeonFloorDefinition elfHouseDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.ElfHolyLandHouseInteriorFloorDef);
            List<DungeonFloorDefinition> hubFloors = CollectDistrictHubFloorDefinitions(
                squareDef,
                marketDef,
                residentialDef,
                guildInteriorDef,
                guildHallInteriorDef,
                storeInteriorDef,
                itemShopInteriorDef,
                blacksmithInteriorDef,
                innInteriorDef,
                nexusDef,
                barbarianHolyLandDef,
                shamanTentDef,
                elfHolyLandDef,
                elfHouseDef);
            DistrictTestCatalogUpdater.UpdateCatalog(hubFloors.ToArray());

            if (!File.Exists(TownDistrictTestPaths.DistrictTownTestScene))
            {
                AssetDatabase.CopyAsset(TemplateScenePath, TownDistrictTestPaths.DistrictTownTestScene);
                AssetDatabase.Refresh();
            }

            Scene scene = EditorSceneManager.OpenScene(TownDistrictTestPaths.DistrictTownTestScene, OpenSceneMode.Single);
            ConfigureSceneHierarchy(
                scene,
                hubFloors,
                squareDef,
                marketDef,
                residentialDef,
                guildInteriorDef,
                guildHallInteriorDef,
                storeInteriorDef,
                itemShopInteriorDef,
                blacksmithInteriorDef,
                innInteriorDef,
                nexusDef,
                barbarianHolyLandDef,
                shamanTentDef,
                elfHolyLandDef,
                elfHouseDef);
            if (repaintTiles)
            {
                PaintDimensionSquareLayout();
                PaintMarketTownLayout();
                PaintResidentialTownLayout();
                PaintHolyLandLayouts();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[DistrictTownTest] Saved {TownDistrictTestPaths.DistrictTownTestScene}. " +
                $"Data: {TownDistrictTestPaths.DimensionSquareFolder}. Press Play to walk the hub.");
        }

        static void EnsureDistrictFolders()
        {
            EnsureFolder("Assets/Scenes/Town");
            EnsureFolder(TownDistrictTestPaths.DistrictTestRoot);
            EnsureFolder(TownDistrictTestPaths.DistrictTestRoot + "/TownArea");
            EnsureFolder(TownDistrictTestPaths.DimensionSquareFolder);
            EnsureFolder(TownDistrictTestPaths.MarketFolder);
            EnsureFolder(TownDistrictTestPaths.ResidentialFolder);
            EnsureFolder(TownDistrictTestPaths.MarketGeneralStoreFolder);
            EnsureFolder(TownDistrictTestPaths.MarketItemShopFolder);
            EnsureFolder(TownDistrictTestPaths.MarketBlacksmithFolder);
            EnsureFolder(TownDistrictTestPaths.ResidentialInnFolder);
            EnsureFolder(TownDistrictTestPaths.DistrictTestRoot + "/Building");
        }

        static void EnsureDimensionSquarePalettes()
        {
            var floorTiles = new List<(string tilePath, int weight)>();
            for (int i = 0; i < DcssFloorTileAssets.Length; i++)
                floorTiles.Add(($"{DcssTileFolder}/{DcssFloorTileAssets[i]}", 5));

            CreateOrUpdatePalette(
                TownDistrictTestPaths.DimensionSquareFloorPalette,
                "dimension_square_floor",
                DungeonTilePaletteLayer.Floor,
                floorTiles.ToArray());

            CreateOrUpdatePalette(
                TownDistrictTestPaths.DimensionSquareWallPalette,
                "dimension_square_wall",
                DungeonTilePaletteLayer.Wall,
                new[] { (WallTilePath, 5) });
        }

        static DungeonFloorDefinition EnsureDimensionSquareFloorDefinition()
        {
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{DcssTileFolder}/{DcssFloorTileAssets[0]}");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            DungeonTilePalette floorPalette =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.DimensionSquareFloorPalette);
            DungeonTilePalette wallPalette =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.DimensionSquareWallPalette);
            PartyFormationSpawnProfile formation =
                AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(
                    "Assets/Resources/Dungeon/PartyFormation_Default.asset");

            var def = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.DimensionSquareFloorDef);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<DungeonFloorDefinition>();
                AssetDatabase.CreateAsset(def, TownDistrictTestPaths.DimensionSquareFloorDef);
            }

            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = DimensionSquareFloorIds.FloorId;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("layoutStamp").objectReferenceValue = null;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("defaultFloorPalette").objectReferenceValue = floorPalette;
            so.FindProperty("defaultWallPalette").objectReferenceValue = wallPalette;
            so.FindProperty("playerSafeRadius").intValue = 8;
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        // Catalog updated via MarketTownPackCreator.UpdateDistrictCatalog (7 hub floors).

        static List<DungeonFloorDefinition> CollectDistrictHubFloorDefinitions(
            DungeonFloorDefinition squareDef,
            DungeonFloorDefinition marketDef,
            DungeonFloorDefinition residentialDef,
            DungeonFloorDefinition guildInteriorDef,
            DungeonFloorDefinition guildHallInteriorDef,
            DungeonFloorDefinition storeInteriorDef,
            DungeonFloorDefinition itemShopInteriorDef,
            DungeonFloorDefinition blacksmithInteriorDef,
            DungeonFloorDefinition innInteriorDef,
            DungeonFloorDefinition nexusDef,
            DungeonFloorDefinition barbarianHolyLandDef,
            DungeonFloorDefinition shamanTentDef,
            DungeonFloorDefinition elfHolyLandDef,
            DungeonFloorDefinition elfHouseDef)
        {
            var floors = new List<DungeonFloorDefinition>(14);
            AppendIfPresent(floors, squareDef);
            AppendIfPresent(floors, marketDef);
            AppendIfPresent(floors, residentialDef);
            AppendIfPresent(floors, guildInteriorDef);
            AppendIfPresent(floors, guildHallInteriorDef);
            AppendIfPresent(floors, storeInteriorDef);
            AppendIfPresent(floors, itemShopInteriorDef);
            AppendIfPresent(floors, blacksmithInteriorDef);
            AppendIfPresent(floors, innInteriorDef);
            AppendIfPresent(floors, nexusDef);
            AppendIfPresent(floors, barbarianHolyLandDef);
            AppendIfPresent(floors, shamanTentDef);
            AppendIfPresent(floors, elfHolyLandDef);
            AppendIfPresent(floors, elfHouseDef);
            return floors;
        }

        static void AppendIfPresent(List<DungeonFloorDefinition> floors, DungeonFloorDefinition def)
        {
            if (def == null)
                return;

            for (int i = 0; i < floors.Count; i++)
            {
                if (floors[i] != null && floors[i].FloorId == def.FloorId)
                    return;
            }

            floors.Add(def);
        }

        static void ConfigureSceneHierarchy(
            Scene scene,
            List<DungeonFloorDefinition> hubFloors,
            DungeonFloorDefinition squareDef,
            DungeonFloorDefinition marketDef,
            DungeonFloorDefinition residentialDef,
            DungeonFloorDefinition guildInteriorDef,
            DungeonFloorDefinition guildHallInteriorDef,
            DungeonFloorDefinition storeInteriorDef,
            DungeonFloorDefinition itemShopInteriorDef,
            DungeonFloorDefinition blacksmithInteriorDef,
            DungeonFloorDefinition innInteriorDef,
            DungeonFloorDefinition nexusDef,
            DungeonFloorDefinition barbarianHolyLandDef,
            DungeonFloorDefinition shamanTentDef,
            DungeonFloorDefinition elfHolyLandDef,
            DungeonFloorDefinition elfHouseDef)
        {
            GameObject systems = GameObject.Find(DungeonFloorTestSceneValidator.SystemsObjectName);
            if (systems == null)
            {
                Debug.LogError("[DistrictTownTest] DungeonTestSystems missing — recreate from template.");
                return;
            }

            TownPackCreatorLighting.EnsureLightingOnSystems(systems);
            DungeonWorldFeatureServices.EnsureOn(systems);
            if (systems.GetComponent<DoorService>() == null)
                systems.AddComponent<DoorService>();
            if (systems.GetComponent<VisibilityManager>() == null)
                systems.AddComponent<VisibilityManager>();
            if (systems.GetComponent<PortalEntryService>() == null)
                systems.AddComponent<PortalEntryService>();
            if (systems.GetComponent<DistrictTownCalendarBootstrap>() == null)
                systems.AddComponent<DistrictTownCalendarBootstrap>();

            DungeonFloorDefinitionCatalog catalog =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinitionCatalog>(TownDistrictTestPaths.DistrictTestCatalog);

            DungeonFloorInstanceManager floorManager = systems.GetComponent<DungeonFloorInstanceManager>()
                ?? systems.AddComponent<DungeonFloorInstanceManager>();

            var managerSo = new SerializedObject(floorManager);
            managerSo.FindProperty("useDontDestroyOnLoad").boolValue = false;
            managerSo.FindProperty("floorDefinitions").arraySize = hubFloors.Count;
            for (int i = 0; i < hubFloors.Count; i++)
                managerSo.FindProperty("floorDefinitions").GetArrayElementAtIndex(i).objectReferenceValue = hubFloors[i];
            Transform floorsRoot = EnsureFloorsRoot(systems, floorManager);
            managerSo.ApplyModifiedPropertiesWithoutUndo();

            RemoveChildFloorsExcept(
                floorsRoot,
                CollectDistrictHubFloorIds(
                    squareDef,
                    marketDef,
                    residentialDef,
                    guildInteriorDef,
                    guildHallInteriorDef,
                    storeInteriorDef,
                    itemShopInteriorDef,
                    blacksmithInteriorDef,
                    innInteriorDef,
                    nexusDef,
                    barbarianHolyLandDef,
                    shamanTentDef,
                    elfHolyLandDef,
                    elfHouseDef));

            DungeonFloorInstance squareInstance = EnsureScenePaintedFloor(floorsRoot, squareDef);
            squareInstance.gameObject.SetActive(true);

            if (marketDef != null)
            {
                DungeonFloorInstance marketInstance = EnsureScenePaintedFloor(floorsRoot, marketDef);
                marketInstance.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError(
                    $"[DistrictTownTest] Missing market floor definition at {TownDistrictTestPaths.MarketFloorDef}. " +
                    "Run JRogue → Town → Setup Market Town Area.");
            }

            if (residentialDef != null)
            {
                DungeonFloorInstance residentialInstance = EnsureScenePaintedFloor(floorsRoot, residentialDef);
                residentialInstance.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError(
                    $"[DistrictTownTest] Missing residential floor definition at {TownDistrictTestPaths.ResidentialFloorDef}. " +
                    "Run JRogue → Town → Setup Residential Town Area.");
            }

            if (guildInteriorDef != null)
            {
                DungeonFloorInstance guildInteriorInstance = EnsureScenePaintedFloor(floorsRoot, guildInteriorDef);
                guildInteriorInstance.gameObject.SetActive(false);
                AdventureGuildExchangePackCreator.IntegrateDistrictTownScene(guildInteriorInstance);
            }

            if (guildHallInteriorDef != null)
            {
                DungeonFloorInstance guildHallInteriorInstance = EnsureScenePaintedFloor(floorsRoot, guildHallInteriorDef);
                guildHallInteriorInstance.gameObject.SetActive(false);
                AdventureGuildHallPackCreator.IntegrateDistrictTownScene(guildHallInteriorInstance);
            }

            if (storeInteriorDef != null)
            {
                DungeonFloorInstance storeInteriorInstance = EnsureScenePaintedFloor(floorsRoot, storeInteriorDef);
                storeInteriorInstance.gameObject.SetActive(false);
                MarketGeneralStorePackCreator.IntegrateDistrictTownScene(storeInteriorInstance);
            }

            if (itemShopInteriorDef != null)
            {
                DungeonFloorInstance itemShopInteriorInstance = EnsureScenePaintedFloor(floorsRoot, itemShopInteriorDef);
                itemShopInteriorInstance.gameObject.SetActive(false);
                MarketItemShopPackCreator.IntegrateDistrictTownScene(itemShopInteriorInstance);
            }

            if (blacksmithInteriorDef != null)
            {
                DungeonFloorInstance blacksmithInteriorInstance = EnsureScenePaintedFloor(floorsRoot, blacksmithInteriorDef);
                blacksmithInteriorInstance.gameObject.SetActive(false);
                BlacksmithShopPackCreator.IntegrateDistrictTownScene(blacksmithInteriorInstance);
            }

            if (innInteriorDef != null)
            {
                DungeonFloorInstance innInteriorInstance = EnsureScenePaintedFloor(floorsRoot, innInteriorDef);
                innInteriorInstance.gameObject.SetActive(false);
                ResidentialInnPackCreator.IntegrateDistrictTownScene(innInteriorInstance);
            }

            if (nexusDef != null)
            {
                DungeonFloorInstance nexusInstance = EnsureScenePaintedFloor(floorsRoot, nexusDef);
                nexusInstance.gameObject.SetActive(false);
                HolyLandTownPackCreator.IntegrateNexusScene(nexusInstance);
            }

            if (barbarianHolyLandDef != null)
            {
                DungeonFloorInstance holyLandInstance = EnsureScenePaintedFloor(floorsRoot, barbarianHolyLandDef);
                holyLandInstance.gameObject.SetActive(false);
                HolyLandTownPackCreator.IntegrateHolyLandScene(holyLandInstance);
            }

            if (shamanTentDef != null)
            {
                DungeonFloorInstance tentInstance = EnsureScenePaintedFloor(floorsRoot, shamanTentDef);
                tentInstance.gameObject.SetActive(false);
                HolyLandTownPackCreator.IntegrateTentInteriorScene(tentInstance);
            }

            if (elfHolyLandDef != null)
            {
                DungeonFloorInstance elfHolyLandInstance = EnsureScenePaintedFloor(floorsRoot, elfHolyLandDef);
                elfHolyLandInstance.gameObject.SetActive(false);
                ElfHolyLandTownPackCreator.IntegrateElfHolyLandScene(elfHolyLandInstance);
            }

            if (elfHouseDef != null)
            {
                DungeonFloorInstance elfHouseInstance = EnsureScenePaintedFloor(floorsRoot, elfHouseDef);
                elfHouseInstance.gameObject.SetActive(false);
                ElfHolyLandTownPackCreator.IntegrateElfHouseInteriorScene(elfHouseInstance);
            }

            DungeonFloorTestController test = systems.GetComponent<DungeonFloorTestController>()
                ?? systems.AddComponent<DungeonFloorTestController>();

            var testSo = new SerializedObject(test);
            testSo.FindProperty("floorInstanceManager").objectReferenceValue = floorManager;
            testSo.FindProperty("floorCatalog").objectReferenceValue = catalog;
            testSo.FindProperty("startFloorId").stringValue = DimensionSquareFloorIds.FloorId;
            testSo.FindProperty("runSeed").intValue = 200001;
            testSo.FindProperty("autoGenerateOnPlay").boolValue = true;
            testSo.FindProperty("validateSceneOnPlay").boolValue = true;
            testSo.FindProperty("tryRepairSceneAtRuntime").boolValue = true;
            testSo.FindProperty("showGenerateButton").boolValue = false;

            GameObject party = GameObject.Find(DungeonFloorTestSceneValidator.PartyObjectName);
            DungeonRunBootstrap bootstrap = party != null ? party.GetComponent<DungeonRunBootstrap>() : null;
            if (bootstrap != null)
                testSo.FindProperty("runBootstrap").objectReferenceValue = bootstrap;
            testSo.ApplyModifiedPropertiesWithoutUndo();

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = AdventureGuildExchangeLayout.DistrictHubCameraOrthoSize;
                if (cam.GetComponent<CameraFollow>() == null)
                    cam.gameObject.AddComponent<CameraFollow>();
            }

            EnsureGameplayUiFromSampleScene(scene);
            EnsureMarkers(squareInstance);
        }

        static DungeonFloorInstance EnsureScenePaintedFloor(Transform floorsRoot, DungeonFloorDefinition floorDef)
        {
            string floorId = floorDef.FloorId;
            Transform existing = floorsRoot.Find(floorId);
            GameObject floorGo = existing != null ? existing.gameObject : new GameObject(floorId);
            if (existing == null)
                floorGo.transform.SetParent(floorsRoot, false);

            DungeonFloorInstance instance = floorGo.GetComponent<DungeonFloorInstance>()
                ?? floorGo.AddComponent<DungeonFloorInstance>();
            instance.Configure(floorDef);
            instance.EnsureHierarchyBuilt();
            return instance;
        }

        static void PaintDimensionSquareLayout()
        {
            DungeonFloorInstance instance = FindDimensionSquareInstance();
            if (instance == null)
            {
                Debug.LogError("[DistrictTownTest] No dimension_square DungeonFloorInstance to paint.");
                return;
            }

            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Tilemap wallMap = instance.Tilemaps.WallMap;
            if (floorMap == null || wallMap == null)
            {
                Debug.LogError("[DistrictTownTest] Floor/Wall tilemaps missing.");
                return;
            }

            TileBase[] floorTiles = LoadFloorTilesFromPalette();
            TileBase wallTile = LoadWallTileFromPalette();
            if (floorTiles == null || floorTiles.Length == 0 || wallTile == null)
            {
                Debug.LogError("[DistrictTownTest] Missing floor or wall palette tiles.");
                return;
            }

            Undo.RecordObject(floorMap, "Paint dimension square floor");
            Undo.RecordObject(wallMap, "Paint dimension square walls");
            DimensionSquareLayout.Paint(floorMap, wallMap, floorTiles, wallTile);
            AdventureGuildExchangePackCreator.PaintAdventureGuildExteriorFacade(floorMap, wallMap);
            AdventureGuildHallPackCreator.PaintAdventureGuildHallExteriorFacade(floorMap, wallMap);

            GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(floorMap);
            GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(wallMap);

            floorMap.CompressBounds();
            wallMap.CompressBounds();
            EditorUtility.SetDirty(floorMap);
            EditorUtility.SetDirty(wallMap);
        }

        static void PaintMarketTownLayout()
        {
            DungeonFloorInstance instance = FindFloorInstance(MarketTownFloorIds.FloorId);
            if (instance == null)
            {
                Debug.LogError("[DistrictTownTest] No town_market DungeonFloorInstance to paint.");
                return;
            }

            MarketTownPackCreator.IntegrateDistrictTownScene(instance);
        }

        static void PaintResidentialTownLayout()
        {
            DungeonFloorInstance instance = FindFloorInstance(ResidentialTownFloorIds.FloorId);
            if (instance == null)
            {
                Debug.LogError("[DistrictTownTest] No town_residential DungeonFloorInstance to paint.");
                return;
            }

            ResidentialTownPackCreator.IntegrateDistrictTownScene(instance);
        }

        static void PaintHolyLandLayouts()
        {
            DungeonFloorInstance nexus = FindFloorInstance(HolyLandFloorIds.Nexus);
            if (nexus != null)
                HolyLandTownPackCreator.IntegrateNexusScene(nexus);

            DungeonFloorInstance holyLand = FindFloorInstance(HolyLandFloorIds.HolyLandProper);
            if (holyLand != null)
                HolyLandTownPackCreator.IntegrateHolyLandScene(holyLand);

            DungeonFloorInstance tent = FindFloorInstance(HolyLandFloorIds.ShamanTentInterior);
            if (tent != null)
                HolyLandTownPackCreator.IntegrateTentInteriorScene(tent);

            DungeonFloorInstance elfHolyLand = FindFloorInstance(HolyLandFloorIds.ElfHolyLandProper);
            if (elfHolyLand != null)
                ElfHolyLandTownPackCreator.IntegrateElfHolyLandScene(elfHolyLand);

            DungeonFloorInstance elfHouse = FindFloorInstance(HolyLandFloorIds.ElfHouseInterior);
            if (elfHouse != null)
                ElfHolyLandTownPackCreator.IntegrateElfHouseInteriorScene(elfHouse);
        }

        static string[] CollectDistrictHubFloorIds(
            DungeonFloorDefinition squareDef,
            DungeonFloorDefinition marketDef,
            DungeonFloorDefinition residentialDef,
            DungeonFloorDefinition guildInteriorDef,
            DungeonFloorDefinition guildHallInteriorDef,
            DungeonFloorDefinition storeInteriorDef,
            DungeonFloorDefinition itemShopInteriorDef,
            DungeonFloorDefinition blacksmithInteriorDef,
            DungeonFloorDefinition innInteriorDef,
            DungeonFloorDefinition nexusDef,
            DungeonFloorDefinition barbarianHolyLandDef,
            DungeonFloorDefinition shamanTentDef,
            DungeonFloorDefinition elfHolyLandDef,
            DungeonFloorDefinition elfHouseDef)
        {
            var ids = new List<string>(14);
            AppendFloorId(ids, squareDef);
            AppendFloorId(ids, marketDef);
            AppendFloorId(ids, residentialDef);
            AppendFloorId(ids, guildInteriorDef);
            AppendFloorId(ids, guildHallInteriorDef);
            AppendFloorId(ids, storeInteriorDef);
            AppendFloorId(ids, itemShopInteriorDef);
            AppendFloorId(ids, blacksmithInteriorDef);
            AppendFloorId(ids, innInteriorDef);
            AppendFloorId(ids, nexusDef);
            AppendFloorId(ids, barbarianHolyLandDef);
            AppendFloorId(ids, shamanTentDef);
            AppendFloorId(ids, elfHolyLandDef);
            AppendFloorId(ids, elfHouseDef);
            return ids.ToArray();
        }

        static void AppendFloorId(List<string> ids, DungeonFloorDefinition def)
        {
            if (def != null && !string.IsNullOrEmpty(def.FloorId))
                ids.Add(def.FloorId);
        }

        static DungeonFloorInstance FindFloorInstance(string floorId)
        {
            GameObject systems = GameObject.Find(DungeonFloorTestSceneValidator.SystemsObjectName);
            if (systems != null)
            {
                Transform floorsRoot = systems.transform.Find("Floors");
                if (floorsRoot != null)
                {
                    Transform child = floorsRoot.Find(floorId);
                    if (child != null && child.TryGetComponent(out DungeonFloorInstance hierarchyInstance))
                        return hierarchyInstance;
                }
            }

            DungeonFloorInstance[] instances =
                Object.FindObjectsByType<DungeonFloorInstance>(FindObjectsInactive.Include);
            for (int i = 0; i < instances.Length; i++)
            {
                DungeonFloorInstance instance = instances[i];
                if (instance != null && instance.FloorId == floorId)
                    return instance;
            }

            return null;
        }

        static DungeonFloorInstance FindDimensionSquareInstance() =>
            FindFloorInstance(DimensionSquareFloorIds.FloorId);

        static TileBase[] LoadFloorTilesFromPalette()
        {
            var palette = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(
                TownDistrictTestPaths.DimensionSquareFloorPalette);
            return ExtractTilesFromPalette(palette);
        }

        static TileBase LoadWallTileFromPalette()
        {
            var palette = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(
                TownDistrictTestPaths.DimensionSquareWallPalette);
            TileBase[] tiles = ExtractTilesFromPalette(palette);
            return tiles != null && tiles.Length > 0 ? tiles[0] : null;
        }

        static TileBase[] ExtractTilesFromPalette(DungeonTilePalette palette)
        {
            if (palette == null || palette.Entries == null)
                return null;

            var tiles = new List<TileBase>();
            DungeonTilePaletteEntry[] entries = palette.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].tile != null)
                    tiles.Add(entries[i].tile);
            }

            return tiles.Count > 0 ? tiles.ToArray() : null;
        }

        static void EnsureMarkers(DungeonFloorInstance instance)
        {
            Transform grid = instance.transform.Find("Grid");
            if (grid == null)
                return;

            Transform markersRoot = instance.transform.Find("Markers");
            if (markersRoot == null)
            {
                var markersGo = new GameObject("Markers");
                markersGo.transform.SetParent(instance.transform, false);
                markersRoot = markersGo.transform;
            }

            ClearChildren(markersRoot);
            Grid gridComponent = grid.GetComponent<Grid>();
            Tilemap floorMap = instance.Tilemaps.FloorMap;

            CreateMarker(markersRoot, gridComponent, floorMap, "PlayerStart", StaticHubMarkerKind.PlayerStart, DimensionSquareLayout.PlayerStartCell);
            CreateMarker(markersRoot, gridComponent, floorMap, "DungeonPortal", StaticHubMarkerKind.DungeonPortal, DimensionSquareLayout.DungeonPortalCell);
            CreateMarker(markersRoot, gridComponent, floorMap, "NpcSlot_North", StaticHubMarkerKind.NpcSlot, DimensionSquareLayout.NpcSlotNorthCell, "npc_slot_north");
            CreateMarker(markersRoot, gridComponent, floorMap, "NpcSlot_South", StaticHubMarkerKind.NpcSlot, DimensionSquareLayout.NpcSlotSouthCell, "npc_slot_south");
            CreateMarker(markersRoot, gridComponent, floorMap, "NpcSlot_East", StaticHubMarkerKind.NpcSlot, DimensionSquareLayout.NpcSlotEastCell, "npc_slot_east");
            CreateMarker(markersRoot, gridComponent, floorMap, "NpcSlot_West", StaticHubMarkerKind.NpcSlot, DimensionSquareLayout.NpcSlotWestCell, "npc_slot_west");
        }

        static void CreateMarker(
            Transform parent,
            Grid grid,
            Tilemap floorMap,
            string objectName,
            StaticHubMarkerKind kind,
            Vector3Int cell,
            string markerId = null)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            StaticHubMarker marker = go.AddComponent<StaticHubMarker>();
            marker.EditorConfigure(kind, cell, markerId);

            Vector3 world = grid != null
                ? grid.GetCellCenterWorld(cell)
                : floorMap != null
                    ? GridCellWorld.GetCellCenter(floorMap, cell)
                    : new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
            go.transform.position = world;
        }

        internal static void CreateOrUpdatePalette(
            string path,
            string paletteId,
            DungeonTilePaletteLayer layer,
            (string tilePath, int weight)[] tiles)
        {
            var palette = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(path);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<DungeonTilePalette>();
                AssetDatabase.CreateAsset(palette, path);
            }

            SerializedObject so = new SerializedObject(palette);
            so.FindProperty("paletteId").stringValue = paletteId;
            so.FindProperty("layer").enumValueIndex = (int)layer;
            so.FindProperty("defaultVariationMode").enumValueIndex =
                (int)DungeonTileVariationMode.WeightedRandom;

            SerializedProperty entries = so.FindProperty("entries");
            entries.arraySize = tiles.Length;
            for (int i = 0; i < tiles.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("tile").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<TileBase>(tiles[i].tilePath);
                entry.FindPropertyRelative("registryKey").stringValue = string.Empty;
                entry.FindPropertyRelative("weight").intValue = tiles[i].weight;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(palette);
        }

        static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        static Transform EnsureFloorsRoot(GameObject systems, DungeonFloorInstanceManager floorManager)
        {
            Transform floors = systems.transform.Find("Floors");
            if (floors == null)
            {
                var floorsGo = new GameObject("Floors");
                floorsGo.transform.SetParent(systems.transform, false);
                floors = floorsGo.transform;
            }

            var managerSo = new SerializedObject(floorManager);
            managerSo.FindProperty("floorsRoot").objectReferenceValue = floors;
            managerSo.ApplyModifiedPropertiesWithoutUndo();
            return floors;
        }

        static void RemoveChildFloorsExcept(Transform floorsRoot, params string[] keepFloorIds)
        {
            if (floorsRoot == null || keepFloorIds == null || keepFloorIds.Length == 0)
                return;

            var keep = new HashSet<string>(keepFloorIds);
            for (int i = floorsRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = floorsRoot.GetChild(i);
                if (!keep.Contains(child.name))
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        static void EnsureGameplayUiFromSampleScene(Scene targetScene)
        {
            if (SceneHasRootNamed(targetScene, "Canvas") && SceneHasRootNamed(targetScene, "EventSystem"))
                return;

            if (!File.Exists(SampleScenePath))
                return;

            Scene sampleScene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Additive);
            try
            {
                foreach (GameObject root in sampleScene.GetRootGameObjects())
                {
                    if (root.name != "Canvas" && root.name != "EventSystem")
                        continue;

                    if (SceneHasRootNamed(targetScene, root.name))
                        continue;

                    GameObject copy = Object.Instantiate(root);
                    copy.name = root.name;
                    SceneManager.MoveGameObjectToScene(copy, targetScene);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(sampleScene, true);
            }
        }

        static bool SceneHasRootNamed(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                    return true;
            }

            return false;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
