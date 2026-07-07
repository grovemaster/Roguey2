#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.World.Generation;
using JRogue.World.Generation.MonsterSpawn;
using JRogue.World.Generation.Zones;
using JRogue.World.Lighting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    /// <summary>
    /// Phase 2: 50×80 production layout, DCSS cavern tiles, zone palettes with emitters, floor fork.
    /// </summary>
    public static class DungeonFloor1ProductionPhase2PackCreator
    {
        const string MenuPath = "JRogue/Dungeon/Phase 2 — Create Production Floor Layout";

        public const string DcssRoot = "Assets/Sprites/DCSS/Dungeon Crawl Stone Soup Full";
        public const string CyanEmitterRoot = "Assets/Sprites/DCSS";
        public const string RecolorRoot = "Assets/Art/Dungeon/ThirdParty/Dcss/Cavern";
        public const string TileRoot = "Assets/TileMaps/Dcss/Cavern";
        public const string PaletteRoot = "Assets/Data/Dungeon/TilePalettes";
        public const string ZoneRoot = "Assets/Data/Dungeon/Zones";
        public const string LayoutPath = "Assets/Data/Dungeon/Layouts/Layout_Floor01_Production.asset";
        public const string FloorProdPath = "Assets/Resources/Dungeon/Floor_prod_dungeon_floor_01.asset";
        public const string FloorTestPath = "Assets/Resources/Dungeon/Floor_dungeon_floor_01.asset";
        public const string CatalogProdPath = "Assets/Resources/Dungeon/DungeonProdFloor1Catalog.asset";
        public const string Floor02Path = "Assets/Resources/Dungeon/Floor_prod_dungeon_floor_02.asset";
        public const string SchedulePath = "Assets/Data/Dungeon/SpawnSchedules/Schedule_Floor01_Production.asset";
        public const string PopCavernPath = "Assets/Data/Dungeon/Zones/Population/Population_LuminescentCavern_Floor01.asset";
        public const string PopDarkPath = "Assets/Data/Dungeon/Zones/Population/Population_NorthernDark_Floor01.asset";
        public const string TorchPath = "Assets/Resources/Lighting/Torch.asset";
        public const string CavernGlowPath = "Assets/Resources/Lighting/CavernGlow.asset";

        public const string ZoneLuminescent = "luminescent_cavern";
        public const string ZoneNorthernDark = "northern_dark";

        public const string PaletteLuminescentFloorPath = PaletteRoot + "/Palette_LuminescentCavern_Floor.asset";
        public const string PaletteLuminescentGlowFloorPath = PaletteRoot + "/Palette_LuminescentCavern_GlowFloor.asset";
        public const string PaletteLuminescentWallPath = PaletteRoot + "/Palette_LuminescentCavern_Wall.asset";
        public const string PaletteDarkFloorPath = PaletteRoot + "/Palette_NorthernDark_Floor.asset";
        public const string PaletteDarkWallPath = PaletteRoot + "/Palette_NorthernDark_Wall.asset";
        public const string ZoneLuminescentPath = ZoneRoot + "/Zone_LuminescentCavern.asset";
        public const string ZoneDarkPath = ZoneRoot + "/Zone_NorthernDark.asset";

        static readonly string[] NormalFloorSprites =
        {
            "dungeon/floor/grey_dirt_0_new.png",
            "dungeon/floor/grey_dirt_1_new.png",
            "dungeon/floor/grey_dirt_2_new.png",
            "dungeon/floor/grey_dirt_3_new.png",
            "dungeon/floor/grey_dirt_4_new.png",
            "dungeon/floor/grey_dirt_5_new.png",
            "dungeon/floor/grey_dirt_6_new.png",
            "dungeon/floor/grey_dirt_7_new.png",
            "dungeon/floor/grey_dirt_b_0.png",
            "dungeon/floor/grey_dirt_b_1.png",
            "dungeon/floor/grey_dirt_b_2.png",
            "dungeon/floor/grey_dirt_b_3.png",
        };

        static readonly string[] DarkWallSprites =
        {
            "dungeon/wall/stone2_gray_2_new.png",
            "dungeon/wall/stone2_gray_3_new.png",
            "dungeon/wall/stone2_dark_2_new.png",
            "dungeon/wall/stone2_dark_3_new.png",
        };

        static readonly (string relativePath, int weight)[] EmitterFloorSpecs =
        {
            ("_cyan_floor_nerves_2_new.png", 1),
            ("_cyan_floor_nerves_4_new.png", 1),
        };

        readonly struct PaletteTileSpec
        {
            public PaletteTileSpec(string tilePath, int weight, bool isLightEmitter = false, string registryKey = null)
            {
                TilePath = tilePath;
                Weight = weight;
                IsLightEmitter = isLightEmitter;
                RegistryKey = registryKey ?? string.Empty;
            }

            public string TilePath { get; }
            public int Weight { get; }
            public bool IsLightEmitter { get; }
            public string RegistryKey { get; }
        }

        [MenuItem(MenuPath, false, 53)]
        public static void CreateProductionFloorPhase2()
        {
            EnsureFolder(TileRoot);
            EnsureFolder(PaletteRoot);
            EnsureFolder(ZoneRoot);
            EnsureFolder(RecolorRoot);
            EnsureFolder(Path.GetDirectoryName(LayoutPath)?.Replace('\\', '/'));
            EnsureFolder(Path.GetDirectoryName(SchedulePath)?.Replace('\\', '/'));

            CreateCavernTiles();
            CreatePalettes();

            DungeonZoneDefinition zoneLuminescent = CreateZoneDefinitions();
            DungeonFloorZoneLayout layout = CreateProductionLayout(zoneLuminescent);
            CreateProductionFloor(layout);
            CreateProductionCatalog();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Floor1 Phase2] Created 50×80 layout, DCSS cavern tiles/palettes, zones, " +
                $"Floor_prod_dungeon_floor_01, and DungeonProdFloor1Catalog. " +
                "Scene wiring: run JRogue → Dungeon → Phase 1 — Setup Production Dungeon.");
        }

        /// <summary>Re-wire zone population + schedule references without regenerating tiles/layout.</summary>
        public static void RefreshProductionContentWiring()
        {
            DungeonFloorZoneLayout layout =
                AssetDatabase.LoadAssetAtPath<DungeonFloorZoneLayout>(LayoutPath);
            if (layout == null)
            {
                Debug.LogError($"[Floor1Production] Missing layout at {LayoutPath}. Run Phase 2 first.");
                return;
            }

            CreateZoneDefinitions();
            CreateProductionFloor(layout);
            CreateProductionCatalog();
            EditorUtility.SetDirty(layout);
        }

        [MenuItem("JRogue/Dungeon/Refresh Luminescent Cavern Emitters", false, 54)]
        public static void RefreshLuminescentCavernEmitters()
        {
            CreatePalettes();
            CreateZoneDefinitions();
            AssetDatabase.SaveAssets();
            Debug.Log("[Floor1 Phase2] Refreshed luminescent cavern palettes, glow gap-fill, and cave tuning.");
        }

        static void CreateCavernTiles()
        {
            for (int i = 0; i < NormalFloorSprites.Length; i++)
            {
                string relative = NormalFloorSprites[i];
                EnsureTileFromSingleSprite($"{DcssRoot}/{relative}", TileNameFromRelative(relative));
            }

            for (int i = 0; i < EmitterFloorSpecs.Length; i++)
            {
                (string fileName, _) = EmitterFloorSpecs[i];
                EnsureTileFromSingleSprite($"{CyanEmitterRoot}/{fileName}", TileNameFromRelative(fileName));
            }

            for (int i = 0; i < DarkWallSprites.Length; i++)
            {
                string relative = DarkWallSprites[i];
                EnsureTileFromSingleSprite($"{DcssRoot}/{relative}", TileNameFromRelative(relative));
                EnsureLightBlueWallTile($"{DcssRoot}/{relative}", TileNameFromRelative(relative) + "_lightblue");
            }
        }

        static void CreatePalettes()
        {
            LightEmitterDefinition cavernGlow = LoadOrCreateCavernGlow();

            var normalFloors = new List<PaletteTileSpec>();
            for (int i = 0; i < NormalFloorSprites.Length; i++)
            {
                string name = TileNameFromRelative(NormalFloorSprites[i]);
                normalFloors.Add(new PaletteTileSpec($"{TileRoot}/{name}.asset", weight: 5, registryKey: $"DcssCavern:{name}"));
            }

            var luminescentFloors = new List<PaletteTileSpec>();
            for (int i = 0; i < NormalFloorSprites.Length; i++)
            {
                string name = TileNameFromRelative(NormalFloorSprites[i]);
                luminescentFloors.Add(new PaletteTileSpec($"{TileRoot}/{name}.asset", weight: 3, registryKey: $"DcssCavern:{name}"));
            }

            CreateOrUpdatePalette(
                PaletteLuminescentFloorPath,
                "luminescent_cavern_floor",
                DungeonTilePaletteLayer.Floor,
                luminescentFloors);

            var glowFloors = new List<PaletteTileSpec>();
            for (int i = 0; i < EmitterFloorSpecs.Length; i++)
            {
                (string fileName, int weight) = EmitterFloorSpecs[i];
                string name = TileNameFromRelative(fileName);
                glowFloors.Add(new PaletteTileSpec(
                    $"{TileRoot}/{name}.asset",
                    weight,
                    isLightEmitter: true,
                    registryKey: $"DcssCavern:{name}"));
            }

            CreateOrUpdatePalette(
                PaletteLuminescentGlowFloorPath,
                "luminescent_cavern_glow_floor",
                DungeonTilePaletteLayer.Floor,
                glowFloors,
                cavernGlow);
            CreateOrUpdatePalette(PaletteDarkFloorPath, "northern_dark_floor", DungeonTilePaletteLayer.Floor, normalFloors);

            var darkWalls = new List<PaletteTileSpec>();
            var glowWalls = new List<PaletteTileSpec>();
            for (int i = 0; i < DarkWallSprites.Length; i++)
            {
                string name = TileNameFromRelative(DarkWallSprites[i]);
                darkWalls.Add(new PaletteTileSpec($"{TileRoot}/{name}.asset", weight: 4, registryKey: $"DcssCavern:{name}"));
                glowWalls.Add(new PaletteTileSpec(
                    $"{TileRoot}/{name}_lightblue.asset",
                    weight: 4,
                    isLightEmitter: true,
                    registryKey: $"DcssCavern:{name}_lightblue"));
            }

            CreateOrUpdatePalette(
                PaletteLuminescentWallPath,
                "luminescent_cavern_wall",
                DungeonTilePaletteLayer.Wall,
                glowWalls,
                cavernGlow);
            CreateOrUpdatePalette(PaletteDarkWallPath, "northern_dark_wall", DungeonTilePaletteLayer.Wall, darkWalls);
        }

        static LightEmitterDefinition LoadOrCreateCavernGlow()
        {
            var glow = AssetDatabase.LoadAssetAtPath<LightEmitterDefinition>(CavernGlowPath);
            if (glow != null)
                return glow;

            glow = ScriptableObject.CreateInstance<LightEmitterDefinition>();
            AssetDatabase.CreateAsset(glow, CavernGlowPath);
            SerializedObject so = new SerializedObject(glow);
            so.FindProperty("baseEmissionMax").intValue = 10;
            so.FindProperty("falloffRadius").intValue = 12;
            so.FindProperty("falloffPerTile").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(glow);
            return glow;
        }

        static DungeonZoneDefinition CreateZoneDefinitions()
        {
            TileBase fallbackFloor = LoadFirstTile(normalFloors: true);
            TileBase fallbackWall = LoadFirstTile(normalFloors: false);

            DungeonTilePalette lumFloor = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(PaletteLuminescentFloorPath);
            DungeonTilePalette lumGlowFloor = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(PaletteLuminescentGlowFloorPath);
            DungeonTilePalette lumWall = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(PaletteLuminescentWallPath);
            DungeonTilePalette darkFloor = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(PaletteDarkFloorPath);
            DungeonTilePalette darkWall = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(PaletteDarkWallPath);

            DungeonZonePopulationProfile popCavern =
                AssetDatabase.LoadAssetAtPath<DungeonZonePopulationProfile>(PopCavernPath);
            DungeonZonePopulationProfile popDark =
                AssetDatabase.LoadAssetAtPath<DungeonZonePopulationProfile>(PopDarkPath);
            MonsterSpawnScheduleProfile schedule =
                AssetDatabase.LoadAssetAtPath<MonsterSpawnScheduleProfile>(SchedulePath);

            DungeonZoneDefinition luminescent = CreateOrUpdateZone(
                ZoneLuminescentPath,
                ZoneLuminescent,
                "Luminescent Cavern",
                lumFloor,
                lumWall,
                fallbackFloor,
                fallbackWall,
                minWidth: 50,
                minHeight: 60,
                maxWidth: 50,
                maxHeight: 60,
                fillMode: ZoneFillMode.Cave,
                innerWallDensity: 55,
                caSmoothingIterations: 5,
                minCorridorWidth: 1,
                maxCorridorWidth: 3,
                minRoomSize: 6,
                maxRoomSize: 16,
                maxRoomCount: 8,
                popCavern,
                schedule,
                glowFloorGapFill: true,
                glowFloorPalette: lumGlowFloor,
                glowFloorMinReceivedLight: 1,
                glowFloorMinSpacing: 6);

            CreateOrUpdateZone(
                ZoneDarkPath,
                ZoneNorthernDark,
                "Northern Dark",
                darkFloor,
                darkWall,
                fallbackFloor,
                fallbackWall,
                minWidth: 50,
                minHeight: 20,
                maxWidth: 50,
                maxHeight: 20,
                fillMode: ZoneFillMode.RoomCorridor,
                innerWallDensity: 0,
                caSmoothingIterations: 3,
                minCorridorWidth: 1,
                maxCorridorWidth: 3,
                minRoomSize: 4,
                maxRoomSize: 9,
                maxRoomCount: 14,
                popDark,
                schedule);

            return luminescent;
        }

        static DungeonZoneDefinition CreateOrUpdateZone(
            string path,
            string zoneId,
            string displayName,
            DungeonTilePalette floorPalette,
            DungeonTilePalette wallPalette,
            TileBase fallbackFloor,
            TileBase fallbackWall,
            int minWidth,
            int minHeight,
            int maxWidth,
            int maxHeight,
            ZoneFillMode fillMode,
            int innerWallDensity,
            int caSmoothingIterations,
            int minCorridorWidth,
            int maxCorridorWidth,
            int minRoomSize,
            int maxRoomSize,
            int maxRoomCount,
            DungeonZonePopulationProfile populationProfile,
            MonsterSpawnScheduleProfile schedule,
            bool glowFloorGapFill = false,
            DungeonTilePalette glowFloorPalette = null,
            int glowFloorMinReceivedLight = 1,
            int glowFloorMinSpacing = 6)
        {
            var zone = LoadOrCreate<DungeonZoneDefinition>(path);
            SerializedObject so = new SerializedObject(zone);
            so.FindProperty("zoneId").stringValue = zoneId;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("floorTile").objectReferenceValue = fallbackFloor;
            so.FindProperty("wallTile").objectReferenceValue = fallbackWall;
            so.FindProperty("floorTilePalette").objectReferenceValue = floorPalette;
            so.FindProperty("wallTilePalette").objectReferenceValue = wallPalette;
            so.FindProperty("ambientRegionId").intValue = 0;
            so.FindProperty("defaultAmbientLight").intValue = 0;
            so.FindProperty("minWidth").intValue = minWidth;
            so.FindProperty("minHeight").intValue = minHeight;
            so.FindProperty("maxWidth").intValue = maxWidth;
            so.FindProperty("maxHeight").intValue = maxHeight;

            SerializedProperty fill = so.FindProperty("fillProfile");
            fill.FindPropertyRelative("mode").enumValueIndex = (int)fillMode;
            fill.FindPropertyRelative("innerWallDensity").intValue = innerWallDensity;
            fill.FindPropertyRelative("ensureConnectivity").boolValue = true;
            fill.FindPropertyRelative("minCorridorWidth").intValue = minCorridorWidth;
            fill.FindPropertyRelative("maxCorridorWidth").intValue = maxCorridorWidth;
            fill.FindPropertyRelative("caSmoothingIterations").intValue = caSmoothingIterations;
            fill.FindPropertyRelative("minRoomSize").intValue = minRoomSize;
            fill.FindPropertyRelative("maxRoomSize").intValue = maxRoomSize;
            fill.FindPropertyRelative("maxRoomCount").intValue = maxRoomCount;
            fill.FindPropertyRelative("glowFloorGapFill").boolValue = glowFloorGapFill;
            fill.FindPropertyRelative("glowFloorPalette").objectReferenceValue = glowFloorPalette;
            fill.FindPropertyRelative("glowFloorMinReceivedLight").intValue = glowFloorMinReceivedLight;
            fill.FindPropertyRelative("glowFloorMinSpacing").intValue = glowFloorMinSpacing;

            so.FindProperty("populationProfile").objectReferenceValue = populationProfile;
            so.FindProperty("monsterPopulationMode").enumValueIndex =
                schedule != null
                    ? (int)MonsterPopulationMode.ScheduledGroups
                    : (int)MonsterPopulationMode.Scatter;
            so.FindProperty("monsterSpawnSchedule").objectReferenceValue = schedule;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zone);
            return zone;
        }

        static DungeonFloorZoneLayout CreateProductionLayout(DungeonZoneDefinition zoneLuminescent)
        {
            DungeonZoneDefinition zoneDark = AssetDatabase.LoadAssetAtPath<DungeonZoneDefinition>(ZoneDarkPath);
            var layout = LoadOrCreate<DungeonFloorZoneLayout>(LayoutPath);
            SerializedObject so = new SerializedObject(layout);
            so.FindProperty("floorWidth").intValue = 50;
            so.FindProperty("floorHeight").intValue = 80;
            so.FindProperty("layoutKind").enumValueIndex = (int)ZoneLayoutKind.CompassSlots;
            so.FindProperty("defaultOuterBoundary").enumValueIndex = (int)ZoneBoundaryKind.Wall;
            so.FindProperty("fallbackZoneId").stringValue = ZoneIds.Rock;
            so.FindProperty("selectionRules").arraySize = 0;

            SerializedProperty pieces = so.FindProperty("pieces");
            pieces.arraySize = 2;
            SetNormalizedPiece(
                pieces.GetArrayElementAtIndex(0),
                pieceId: "center",
                zoneId: ZoneLuminescent,
                mandatory: true,
                isPlayerStart: true,
                xMin: 0f,
                yMin: 0f,
                xMax: 1f,
                yMax: 0.75f,
                defaultBoundary: ZoneBoundaryKind.Wall,
                neighborPieceId: "north",
                corridorCount: 3,
                corridorWidthMin: 1,
                corridorWidthMax: 3);
            SetNormalizedPiece(
                pieces.GetArrayElementAtIndex(1),
                pieceId: "north",
                zoneId: ZoneNorthernDark,
                mandatory: true,
                isPlayerStart: false,
                xMin: 0f,
                yMin: 0.75f,
                xMax: 1f,
                yMax: 1f,
                defaultBoundary: ZoneBoundaryKind.Wall,
                neighborPieceId: null,
                corridorCount: 1,
                corridorWidthMin: 1,
                corridorWidthMax: 1);

            SerializedProperty zoneDefs = so.FindProperty("zoneDefinitions");
            zoneDefs.arraySize = 2;
            zoneDefs.GetArrayElementAtIndex(0).objectReferenceValue = zoneLuminescent;
            zoneDefs.GetArrayElementAtIndex(1).objectReferenceValue = zoneDark;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(layout);
            return layout;
        }

        static void SetNormalizedPiece(
            SerializedProperty element,
            string pieceId,
            string zoneId,
            bool mandatory,
            bool isPlayerStart,
            float xMin,
            float yMin,
            float xMax,
            float yMax,
            ZoneBoundaryKind defaultBoundary,
            string neighborPieceId,
            int corridorCount,
            int corridorWidthMin,
            int corridorWidthMax)
        {
            element.FindPropertyRelative("pieceId").stringValue = pieceId;
            element.FindPropertyRelative("anchorKind").enumValueIndex = (int)ZonePieceAnchorKind.NormalizedRect;
            element.FindPropertyRelative("mandatory").boolValue = mandatory;
            element.FindPropertyRelative("isPlayerStartPiece").boolValue = isPlayerStart;
            element.FindPropertyRelative("defaultBoundary").enumValueIndex = (int)defaultBoundary;
            element.FindPropertyRelative("connectsTo").arraySize = 0;

            SerializedProperty normalized = element.FindPropertyRelative("normalizedRect");
            normalized.FindPropertyRelative("xMin").floatValue = xMin;
            normalized.FindPropertyRelative("yMin").floatValue = yMin;
            normalized.FindPropertyRelative("xMax").floatValue = xMax;
            normalized.FindPropertyRelative("yMax").floatValue = yMax;

            SerializedProperty candidates = element.FindPropertyRelative("candidates");
            candidates.arraySize = 1;
            candidates.GetArrayElementAtIndex(0).FindPropertyRelative("zoneId").stringValue = zoneId;
            candidates.GetArrayElementAtIndex(0).FindPropertyRelative("weight").intValue = 100;

            SerializedProperty edges = element.FindPropertyRelative("edgeBoundaries");
            if (string.IsNullOrEmpty(neighborPieceId))
            {
                edges.arraySize = 0;
                return;
            }

            edges.arraySize = 1;
            SerializedProperty edge = edges.GetArrayElementAtIndex(0);
            edge.FindPropertyRelative("neighborPieceId").stringValue = neighborPieceId;
            edge.FindPropertyRelative("boundaryKind").enumValueIndex = (int)ZoneBoundaryKind.Corridor;
            edge.FindPropertyRelative("corridorCount").intValue = corridorCount;
            edge.FindPropertyRelative("corridorWidth").intValue = corridorWidthMin;
            edge.FindPropertyRelative("corridorWidthMin").intValue = corridorWidthMin;
            edge.FindPropertyRelative("corridorWidthMax").intValue = corridorWidthMax;
        }

        static void CreateProductionFloor(DungeonFloorZoneLayout layout)
        {
            DungeonFloorDefinition floor = LoadOrCreate<DungeonFloorDefinition>(FloorProdPath);
            CopyFloorBaselineIfEmpty(floor);

            DungeonTilePalette lumFloor = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(PaletteLuminescentFloorPath);
            DungeonTilePalette lumWall = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(PaletteLuminescentWallPath);
            MonsterSpawnScheduleProfile schedule =
                AssetDatabase.LoadAssetAtPath<MonsterSpawnScheduleProfile>(SchedulePath);

            SerializedObject so = new SerializedObject(floor);
            so.FindProperty("floorId").stringValue = "dungeon_floor_01";
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ZoneComposite;
            so.FindProperty("zoneLayout").objectReferenceValue = layout;
            so.FindProperty("defaultFloorPalette").objectReferenceValue = lumFloor;
            so.FindProperty("defaultWallPalette").objectReferenceValue = lumWall;
            so.FindProperty("floorTile").objectReferenceValue = LoadFirstTile(normalFloors: true);
            so.FindProperty("wallTile").objectReferenceValue = LoadFirstTile(normalFloors: false);
            so.FindProperty("useFloorPopulationAsFallback").boolValue = true;
            so.FindProperty("playerSafeRadius").intValue = 5;
            so.FindProperty("participatesInDungeonTime").boolValue = true;
            so.FindProperty("baseDayNightCycles").intValue = 4;
            so.FindProperty("playerTurnsPerDay").intValue = 20;
            so.FindProperty("playerTurnsPerNight").intValue = 20;
            so.FindProperty("monsterPopulationMode").enumValueIndex =
                schedule != null
                    ? (int)MonsterPopulationMode.ScheduledGroups
                    : (int)MonsterPopulationMode.Scatter;
            so.FindProperty("monsterSpawnSchedule").objectReferenceValue = schedule;
            so.FindProperty("portalPlacementRules").arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(floor);
        }

        static void SetFixedMapRowPortalRule(
            SerializedProperty rules,
            string zoneId,
            int mapRow,
            string portalLinkId,
            string targetFloorId,
            string listLabel,
            string rngSalt)
        {
            rules.arraySize = 1;
            SerializedProperty rule = rules.GetArrayElementAtIndex(0);
            rule.FindPropertyRelative("kind").enumValueIndex = (int)PortalPlacementRuleKind.FixedMapRowEdge;
            rule.FindPropertyRelative("portalLinkId").stringValue = portalLinkId;
            rule.FindPropertyRelative("targetFloorId").stringValue = targetFloorId;
            rule.FindPropertyRelative("listLabel").stringValue = listLabel;
            rule.FindPropertyRelative("zoneId").stringValue = zoneId;
            rule.FindPropertyRelative("fixedMapRow").intValue = mapRow;
            rule.FindPropertyRelative("rngSalt").stringValue = rngSalt;
        }

        static void CreateProductionCatalog()
        {
            DungeonFloorDefinition floorProd =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(FloorProdPath);
            DungeonFloorDefinition floor02 =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(Floor02Path);

            var catalog = LoadOrCreate<DungeonFloorDefinitionCatalog>(CatalogProdPath);
            SerializedObject so = new SerializedObject(catalog);
            SerializedProperty floors = so.FindProperty("floors");
            floors.arraySize = floor02 != null ? 2 : 1;
            floors.GetArrayElementAtIndex(0).objectReferenceValue = floorProd;
            if (floor02 != null)
                floors.GetArrayElementAtIndex(1).objectReferenceValue = floor02;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        public static void WireProductionScene()
        {
            if (!File.Exists(DungeonFloorProductionSceneCreator.ProductionScenePath))
                return;

            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                DungeonFloorProductionSceneCreator.ProductionScenePath,
                UnityEditor.SceneManagement.OpenSceneMode.Single);
            DungeonV0aPackCreator.FixProductionSceneHierarchyInPlace();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }

        static void CopyFloorBaselineIfEmpty(DungeonFloorDefinition floor)
        {
            SerializedObject so = new SerializedObject(floor);
            if (so.FindProperty("formationProfile").objectReferenceValue != null)
                return;

            var template = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(FloorTestPath);
            if (template == null)
                return;

            SerializedObject from = new SerializedObject(template);
            CopyReference(from, so, "formationProfile");
            CopyReference(from, so, "vaultCatalog");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CopyReference(SerializedObject from, SerializedObject to, string propertyName)
        {
            to.FindProperty(propertyName).objectReferenceValue =
                from.FindProperty(propertyName).objectReferenceValue;
        }

        static void CreateOrUpdatePalette(
            string path,
            string paletteId,
            DungeonTilePaletteLayer layer,
            IReadOnlyList<PaletteTileSpec> tiles,
            LightEmitterDefinition emitterDefinition = null)
        {
            var palette = LoadOrCreate<DungeonTilePalette>(path);

            SerializedObject so = new SerializedObject(palette);
            so.FindProperty("paletteId").stringValue = paletteId;
            so.FindProperty("layer").enumValueIndex = (int)layer;
            so.FindProperty("defaultVariationMode").enumValueIndex =
                (int)DungeonTileVariationMode.WeightedRandom;

            SerializedProperty entries = so.FindProperty("entries");
            entries.arraySize = tiles.Count;
            for (int i = 0; i < tiles.Count; i++)
            {
                PaletteTileSpec spec = tiles[i];
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("tile").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<TileBase>(spec.TilePath);
                entry.FindPropertyRelative("registryKey").stringValue = spec.RegistryKey;
                entry.FindPropertyRelative("weight").intValue = spec.Weight;
                entry.FindPropertyRelative("isLightEmitter").boolValue = spec.IsLightEmitter;
                entry.FindPropertyRelative("emitLight").objectReferenceValue =
                    spec.IsLightEmitter ? emitterDefinition : null;
                entry.FindPropertyRelative("emissionOverride").intValue = 0;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(palette);
        }

        static TileBase LoadFirstTile(bool normalFloors)
        {
            string path = normalFloors
                ? $"{TileRoot}/{TileNameFromRelative(NormalFloorSprites[0])}.asset"
                : $"{TileRoot}/{TileNameFromRelative(DarkWallSprites[0])}.asset";
            return AssetDatabase.LoadAssetAtPath<TileBase>(path);
        }

        static void EnsureTileFromSingleSprite(string spritePath, string tileName)
        {
            EnsureSingleSpriteImport(spritePath);
            string tilePath = $"{TileRoot}/{tileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<Tile>(tilePath) != null)
                return;

            Sprite sprite = LoadSingleSprite(spritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[Floor1 Phase2] Missing sprite at {spritePath}");
                return;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            AssetDatabase.CreateAsset(tile, tilePath);
        }

        static void EnsureLightBlueWallTile(string sourceSpritePath, string tileName)
        {
            string recolorPng = $"{RecolorRoot}/{tileName}.png";
            EnsureLightBlueRecolorPng(sourceSpritePath, recolorPng);
            EnsureTileFromSingleSprite(recolorPng, tileName);
        }

        static void EnsureLightBlueRecolorPng(string sourceSpritePath, string outputPath)
        {
            if (File.Exists(outputPath))
                return;

            EnsureSingleSpriteImport(sourceSpritePath);
            byte[] bytes = File.ReadAllBytes(sourceSpritePath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            if (!texture.LoadImage(bytes))
            {
                Object.DestroyImmediate(texture);
                Debug.LogWarning($"[Floor1 Phase2] Failed to load texture {sourceSpritePath}");
                return;
            }

            Color[] pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                if (c.a < 0.01f)
                    continue;

                Color.RGBToHSV(c, out float h, out float s, out float v);
                h = 0.55f;
                s = Mathf.Clamp(s * 0.85f + 0.15f, 0f, 1f);
                v = Mathf.Clamp(v * 1.05f + 0.08f, 0f, 1f);
                pixels[i] = Color.HSVToRGB(h, s, v);
                pixels[i].a = c.a;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(outputPath);
            EnsureSingleSpriteImport(outputPath);
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

        static string TileNameFromRelative(string relativePath) =>
            Path.GetFileNameWithoutExtension(relativePath);

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
