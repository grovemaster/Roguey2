#if UNITY_EDITOR
using System.IO;
using JRogue.World.Generation;
using JRogue.World.Generation.MonsterSpawn;
using JRogue.World.Generation.Zones;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    /// <summary>
    /// Production Floor 2 v0: 10×20 rectangle, dirt floor / stone walls, south return portal only.
    /// </summary>
    public static class DungeonFloor2ProductionPackCreator
    {
        const string MenuPath = "JRogue/Dungeon/Create Floor 2 Production Pack";

        public const string FloorProdPath = "Assets/Resources/Dungeon/Floor_prod_dungeon_floor_02.asset";
        public const string StampPath = "Assets/Resources/Dungeon/Stamp_Floor02_Production_10x20.asset";
        public const string PaletteFloorPath = DungeonFloor1ProductionPhase2PackCreator.PaletteLuminescentFloorPath;
        public const string PaletteWallPath = "Assets/Data/Dungeon/TilePalettes/Palette_Floor02_Wall.asset";
        public const string CatalogProdPath = DungeonFloor1ProductionPhase2PackCreator.CatalogProdPath;
        public const string TileRoot = "Assets/TileMaps/Dcss/Cavern";
        public const string DcssRoot = DungeonFloor1ProductionPhase2PackCreator.DcssRoot;

        public const int MapWidth = 10;
        public const int MapHeight = 20;
        public const int SouthPortalX = 5;
        public const int SouthPortalY = 0;
        public const int Floor02ArrivalY = 1;

        [MenuItem(MenuPath, false, 52)]
        public static void CreateFloor2ProductionPack()
        {
            EnsureFloor02WallPalette();
            DungeonLayoutStamp stamp = CreateProductionStamp();
            DungeonFloorDefinition floor = CreateProductionFloor(stamp);
            UpdateProductionCatalog(floor);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[Dungeon] Floor 2 production pack created: {FloorProdPath} " +
                $"(portal south=({SouthPortalX},{SouthPortalY}), arrival=({SouthPortalX},{Floor02ArrivalY})).");
        }

        /// <summary>Entry point for Unity batchmode.</summary>
        public static void CreateFloor2ProductionPackBatch()
        {
            CreateFloor2ProductionPack();
            EditorApplication.Exit(0);
        }

        static void EnsureFloor02WallPalette()
        {
            EnsureFolder("Assets/Data/Dungeon/TilePalettes");
            EnsureTileFromSprite(
                $"{DcssRoot}/dungeon/wall/stone2_gray_2_new.png",
                "stone2_gray_2_new");
            EnsureTileFromSprite(
                $"{DcssRoot}/dungeon/wall/stone2_gray_3_new.png",
                "stone2_gray_3_new");

            var palette = LoadOrCreate<DungeonTilePalette>(PaletteWallPath);
            SerializedObject so = new SerializedObject(palette);
            so.FindProperty("paletteId").stringValue = "floor02_wall";
            TileBase wall0 = AssetDatabase.LoadAssetAtPath<TileBase>($"{TileRoot}/stone2_gray_2_new.asset");
            TileBase wall1 = AssetDatabase.LoadAssetAtPath<TileBase>($"{TileRoot}/stone2_gray_3_new.asset");
            SerializedProperty entries = so.FindProperty("entries");
            entries.arraySize = 2;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("tile").objectReferenceValue = wall0;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("weight").intValue = 1;
            entries.GetArrayElementAtIndex(1).FindPropertyRelative("tile").objectReferenceValue = wall1;
            entries.GetArrayElementAtIndex(1).FindPropertyRelative("weight").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(palette);
        }

        static DungeonLayoutStamp CreateProductionStamp()
        {
            var stamp = LoadOrCreate<DungeonLayoutStamp>(StampPath);
            stamp.InitializeGrid(MapWidth, MapHeight, borderWalls: true);
            Vector3Int portalCell = new Vector3Int(SouthPortalX, SouthPortalY, 0);
            stamp.SetCell(SouthPortalX, SouthPortalY, floor: true, wall: false);
            stamp.SetMarker(StampMarkerIds.PortalSouth, portalCell);
            EditorUtility.SetDirty(stamp);
            return stamp;
        }

        static DungeonFloorDefinition CreateProductionFloor(DungeonLayoutStamp stamp)
        {
            DungeonTilePalette floorPalette = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(PaletteFloorPath);
            DungeonTilePalette wallPalette = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(PaletteWallPath);
            PartyFormationSpawnProfile formation = AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(
                "Assets/Resources/Dungeon/PartyFormation_Default.asset");

            var floor = LoadOrCreate<DungeonFloorDefinition>(FloorProdPath);
            SerializedObject so = new SerializedObject(floor);
            so.FindProperty("floorId").stringValue = DungeonFloorTransitionIds.Floor02Id;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.PreBakedStamp;
            so.FindProperty("layoutStamp").objectReferenceValue = stamp;
            so.FindProperty("defaultFloorPalette").objectReferenceValue = floorPalette;
            so.FindProperty("defaultWallPalette").objectReferenceValue = wallPalette;
            so.FindProperty("floorTile").objectReferenceValue = LoadFirstPaletteTile(floorPalette);
            so.FindProperty("wallTile").objectReferenceValue = LoadFirstPaletteTile(wallPalette);
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("playerSafeRadius").intValue = 2;
            so.FindProperty("participatesInDungeonTime").boolValue = true;
            so.FindProperty("monsterPopulationMode").enumValueIndex = (int)MonsterPopulationMode.Scatter;
            so.FindProperty("enemyPopulation").arraySize = 0;
            so.FindProperty("hazardPopulation").arraySize = 0;
            so.FindProperty("trapPopulation").arraySize = 0;
            so.FindProperty("interactablePopulation").arraySize = 0;
            so.FindProperty("floorItemPopulation").arraySize = 0;
            so.FindProperty("portalPlacementRules").arraySize = 0;

            WritePortal(
                so.FindProperty("portals"),
                DungeonFloorTransitionIds.Floor02ToFloor01,
                DungeonFloorTransitionIds.Floor01Id,
                StampMarkerIds.PortalSouth,
                new Vector3Int(SouthPortalX, SouthPortalY, 0),
                "Portal (Return)");

            WriteArrival(
                so.FindProperty("arrivalBindings"),
                DungeonFloorTransitionIds.Floor01ToFloor02,
                new Vector3Int(SouthPortalX, Floor02ArrivalY, 0));

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(floor);
            return floor;
        }

        static void WritePortal(
            SerializedProperty portals,
            string linkId,
            string targetFloorId,
            string markerId,
            Vector3Int cell,
            string label)
        {
            portals.arraySize = 1;
            SerializedProperty portal = portals.GetArrayElementAtIndex(0);
            portal.FindPropertyRelative("portalLinkId").stringValue = linkId;
            portal.FindPropertyRelative("targetFloorId").stringValue = targetFloorId;
            portal.FindPropertyRelative("portalMarkerId").stringValue = markerId;
            portal.FindPropertyRelative("portalCell").vector3IntValue = cell;
            portal.FindPropertyRelative("listLabel").stringValue = label;
        }

        static void WriteArrival(SerializedProperty arrivals, string linkId, Vector3Int anchor)
        {
            arrivals.arraySize = 1;
            SerializedProperty arrival = arrivals.GetArrayElementAtIndex(0);
            arrival.FindPropertyRelative("portalLinkId").stringValue = linkId;
            arrival.FindPropertyRelative("arrivalAnchor").vector3IntValue = anchor;
        }

        static void UpdateProductionCatalog(DungeonFloorDefinition floor02)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinitionCatalog>(CatalogProdPath);
            if (catalog == null || floor02 == null)
                return;

            DungeonFloorDefinition floor01 = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(
                DungeonFloor1ProductionPhase2PackCreator.FloorProdPath);

            SerializedObject so = new SerializedObject(catalog);
            SerializedProperty floors = so.FindProperty("floors");
            floors.arraySize = 2;
            floors.GetArrayElementAtIndex(0).objectReferenceValue = floor01;
            floors.GetArrayElementAtIndex(1).objectReferenceValue = floor02;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        static TileBase LoadFirstPaletteTile(DungeonTilePalette palette)
        {
            if (palette?.Entries == null || palette.Entries.Length == 0)
                return null;

            for (int i = 0; i < palette.Entries.Length; i++)
            {
                if (palette.Entries[i].tile != null)
                    return palette.Entries[i].tile;
            }

            return null;
        }

        static void EnsureTileFromSprite(string spritePath, string tileName)
        {
            string tilePath = $"{TileRoot}/{tileName}.asset";
            Sprite sprite = LoadSingleSprite(spritePath);
            if (sprite == null)
                return;

            var existing = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (existing != null)
            {
                if (existing.sprite != sprite)
                {
                    existing.sprite = sprite;
                    EditorUtility.SetDirty(existing);
                }

                return;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            AssetDatabase.CreateAsset(tile, tilePath);
        }

        static Sprite LoadSingleSprite(string assetPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                    return sprite;
            }

            return null;
        }

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
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
