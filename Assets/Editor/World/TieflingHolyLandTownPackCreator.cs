#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.GridFeatures;
using JRogue.Manager.Grid;
using JRogue.Racial;
using JRogue.World.Generation;
using JRogue.World.Generation.Zones;
using JRogue.World.Town;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    /// <summary>Tiefling Holy Land — dirt grounds, chief NPC, sanctum fleshmetal forgemaster.</summary>
    public static class TieflingHolyLandTownPackCreator
    {
        const string WallTilePath = "Assets/TileMaps/Town/Town_WallBuilding.asset";
        const string BuildingWallTilePath = "Assets/TileMaps/Town/Town_Building_StoneWall.asset";
        const string BuildingDoorTilePath = "Assets/TileMaps/Town/Town_Building_Door.asset";
        const string DcssTileFolder = "Assets/TileMaps/Town/Dcss";
        const string DirtTileFolder = "Assets/TileMaps/Dcss/Cavern";
        const string TieflingNpcPrefabPath = "Assets/Prefabs/Actor/Npc/TieflingNpc.prefab";
        const string ChiefDialogPath = "Assets/Resources/Dialog/Profiles/NpcDialog_ChiefTiefling.asset";
        const string ChiefPrefabPath = "Assets/Resources/Town/Npc/TownNpc_ChiefTiefling.prefab";
        const string ChiefPortraitPath = "Assets/Resources/Dialog/Portraits/Portrait_FleshmetalForgemaster.asset";
        const string ChiefSpritePath = "Assets/Art/NPC/Sprites/NPC_Tiefling_Smith.png";
        const string ForgemasterSpritePath = "Assets/Art/NPC/Sprites/NPC_FleshmetalForgemaster.png";
        const string ForgemasterPortraitPath = "Assets/Resources/Dialog/Portraits/Portrait_FleshmetalForgemaster.asset";
        const string ForgemasterCatalogPath = "Assets/Resources/Racial/Tiefling/DefaultFleshmetalForgemaster.asset";
        const string FormationPath = "Assets/Resources/Dungeon/PartyFormation_Default.asset";

        static readonly string[] StoneFloorTileAssets =
        {
            "Dcss_Floor_RectGray0.asset",
            "Dcss_Floor_RectGray1.asset",
            "Dcss_Floor_RectGray2.asset",
            "Dcss_Floor_RectGray3.asset",
        };

        static readonly string[] DirtFloorTileAssets =
        {
            "grey_dirt_0_new.asset",
            "grey_dirt_1_new.asset",
            "grey_dirt_2_new.asset",
            "grey_dirt_3_new.asset",
            "grey_dirt_4_new.asset",
            "grey_dirt_5_new.asset",
            "grey_dirt_b_0.asset",
            "grey_dirt_b_1.asset",
        };

        [MenuItem("JRogue/Town/Setup Tiefling Holy Land")]
        public static void SetupTieflingHolyLand()
        {
            EnsureFolders();
            FleshmetalForgemasterPackCreator.CreateFleshmetalForgemasterPack();
            EnsureChiefDialogAndPrefab();
            EnsureSanctumForgemasterPrefab();
            EnsureTieflingHolyLandFloorDefinition();
            EnsureTieflingSanctumInteriorFloorDefinition();
            HolyLandTownPackCreator.SetupHolyLand();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TieflingHolyLand] Data ready. Run JRogue → Town → Fix District Town Test Scene.");
        }

        [MenuItem("JRogue/Town/Create / Update Tiefling Holy Land Floors")]
        public static void CreateOrUpdateTieflingHolyLandFloors()
        {
            SetupTieflingHolyLand();
            DimensionSquareSceneCreator.FixDimensionSquareTestScene();
        }

        public static void IntegrateTieflingHolyLandScene(DungeonFloorInstance instance) =>
            PaintTieflingHolyLandLayout(instance);

        public static void IntegrateTieflingSanctumInteriorScene(DungeonFloorInstance instance) =>
            PaintTieflingSanctumInteriorLayout(instance);

        public static void AppendToDimensionSquareCatalog(
            List<DungeonFloorDefinition> hubFloors,
            DungeonFloorDefinition tieflingHolyLandDef,
            DungeonFloorDefinition tieflingSanctumDef)
        {
            HolyLandTownPackCreator.AppendIfMissingPublic(hubFloors, tieflingHolyLandDef);
            HolyLandTownPackCreator.AppendIfMissingPublic(hubFloors, tieflingSanctumDef);
        }

        static void EnsureFolders()
        {
            EnsureFolder(TownDistrictTestPaths.HolyLandFolder);
            EnsureFolder("Assets/Resources/Town/Npc");
            EnsureFolder("Assets/Resources/Dialog/Profiles");
            EnsureFolder("Assets/Resources/Dialog/Portraits");
        }

        static void EnsureChiefDialogAndPrefab()
        {
            var dialog = LoadOrCreate<NpcDialogProfile>(ChiefDialogPath);
            var dialogSo = new SerializedObject(dialog);
            dialogSo.FindProperty("npcId").stringValue = TieflingHolyLandLayout.ChiefMarkerId;
            dialogSo.FindProperty("rootNodeIndex").intValue = 0;
            SerializedProperty nodes = dialogSo.FindProperty("nodes");
            nodes.arraySize = 3;
            WriteChiefLine(nodes.GetArrayElementAtIndex(0),
                "Welcome to the ember sanctum. Here the pact-bound temper their flesh in fire.",
                1);
            WriteChiefLine(nodes.GetArrayElementAtIndex(1),
                "Walk the ash paths with care. What we graft is never given lightly.",
                2);
            WriteChiefLine(nodes.GetArrayElementAtIndex(2),
                "The forge within offers grafts to those who bear our blood.",
                -1);
            dialogSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dialog);

            GameObject tieflingNpc = AssetDatabase.LoadAssetAtPath<GameObject>(TieflingNpcPrefabPath);
            if (tieflingNpc == null)
                return;

            bool createdNew = !File.Exists(ChiefPrefabPath);
            GameObject instance = createdNew
                ? (GameObject)PrefabUtility.InstantiatePrefab(tieflingNpc)
                : PrefabUtility.LoadPrefabContents(ChiefPrefabPath);

            try
            {
                instance.name = "TownNpc_ChiefTiefling";
                NpcController npc = instance.GetComponent<NpcController>() ?? instance.AddComponent<NpcController>();
                var npcSo = new SerializedObject(npc);
                npcSo.FindProperty("npcId").stringValue = TieflingHolyLandLayout.ChiefMarkerId;
                npcSo.FindProperty("displayName").stringValue = "Chief of the Tieflings";
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

        static void EnsureSanctumForgemasterPrefab()
        {
            TieflingForgemasterDefinition catalog =
                AssetDatabase.LoadAssetAtPath<TieflingForgemasterDefinition>(ForgemasterCatalogPath);
            PortraitDefinition portrait =
                AssetDatabase.LoadAssetAtPath<PortraitDefinition>(ForgemasterPortraitPath);
            GameObject tieflingNpc = AssetDatabase.LoadAssetAtPath<GameObject>(TieflingNpcPrefabPath);
            if (tieflingNpc == null || catalog == null)
                return;

            const string prefabPath = "Assets/Resources/Town/Npc/TownNpc_TieflingHolyLandForgemaster.prefab";
            NpcDialogPackCreator.DeletePrefabIfPresent(prefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(tieflingNpc) as GameObject;
            instance.name = "TownNpc_TieflingHolyLandForgemaster";

            NpcController dialogNpc = instance.GetComponent<NpcController>();
            if (dialogNpc != null)
                Object.DestroyImmediate(dialogNpc, true);

            TieflingForgemasterNpcController forgemaster = instance.AddComponent<TieflingForgemasterNpcController>();
            SerializedObject npcSo = new SerializedObject(forgemaster);
            npcSo.FindProperty("npcId").stringValue = TieflingForgemasterIds.HolyLandForgemasterNpcId;
            npcSo.FindProperty("portrait").objectReferenceValue = portrait;
            npcSo.FindProperty("forgemasterCatalog").objectReferenceValue = catalog;
            npcSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject actorSo = new SerializedObject(forgemaster);
            actorSo.FindProperty("displayName").stringValue = "Sanctum Fleshmetal Forgemaster";
            actorSo.ApplyModifiedPropertiesWithoutUndo();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ForgemasterSpritePath);
            SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
            if (renderer != null && sprite != null)
                renderer.sprite = sprite;

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
        }

        static DungeonFloorDefinition EnsureTieflingHolyLandFloorDefinition()
        {
            EnsureTieflingHolyLandPalettes();
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{DirtTileFolder}/{DirtFloorTileAssets[0]}");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            PartyFormationSpawnProfile formation = AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(FormationPath);

            var def = LoadOrCreate<DungeonFloorDefinition>(TownDistrictTestPaths.TieflingHolyLandProperFloorDef);
            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = HolyLandFloorIds.TieflingHolyLandProper;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("defaultFloorPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.TieflingHolyLandProperFloorPalette);
            so.FindProperty("defaultWallPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.HolyLandWallPalette);
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;

            SerializedProperty portals = so.FindProperty("portals");
            portals.arraySize = 2;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.TieflingHolyLandToNexus;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("targetFloorId").stringValue = HolyLandFloorIds.Nexus;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("portalCell").vector3IntValue =
                TieflingHolyLandLayout.ReturnToNexusCell;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("listLabel").stringValue = "Nexus";
            portals.GetArrayElementAtIndex(1).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.TieflingSanctumEnter;
            portals.GetArrayElementAtIndex(1).FindPropertyRelative("targetFloorId").stringValue =
                HolyLandFloorIds.TieflingSanctumInterior;
            portals.GetArrayElementAtIndex(1).FindPropertyRelative("portalCell").vector3IntValue =
                TieflingHolyLandLayout.SanctumDoorCell;
            portals.GetArrayElementAtIndex(1).FindPropertyRelative("listLabel").stringValue = "Sanctum";

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 2;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.NexusToTieflingHolyLand;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("arrivalAnchor").vector3IntValue =
                HolyLandNexusLayout.HolyLandArrivalCell;
            arrivals.GetArrayElementAtIndex(1).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.TieflingSanctumExit;
            arrivals.GetArrayElementAtIndex(1).FindPropertyRelative("arrivalAnchor").vector3IntValue =
                TieflingHolyLandLayout.SanctumDoorCell;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static DungeonFloorDefinition EnsureTieflingSanctumInteriorFloorDefinition()
        {
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{DcssTileFolder}/{StoneFloorTileAssets[0]}");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            PartyFormationSpawnProfile formation = AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(FormationPath);

            var def = LoadOrCreate<DungeonFloorDefinition>(TownDistrictTestPaths.TieflingSanctumInteriorFloorDef);
            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = HolyLandFloorIds.TieflingSanctumInterior;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("defaultFloorPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.TieflingSanctumInteriorFloorPalette);
            so.FindProperty("defaultWallPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.HolyLandWallPalette);
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;

            SerializedProperty portals = so.FindProperty("portals");
            portals.arraySize = 1;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.TieflingSanctumExit;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("targetFloorId").stringValue =
                HolyLandFloorIds.TieflingHolyLandProper;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("portalCell").vector3IntValue =
                TieflingHolyLandSanctumLayout.InteriorExitCell;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("listLabel").stringValue = "Tiefling Holy Land";

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 1;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.TieflingSanctumEnter;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("arrivalAnchor").vector3IntValue =
                TieflingHolyLandSanctumLayout.InteriorArrivalCell;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static void EnsureTieflingHolyLandPalettes()
        {
            var dirtTiles = new List<(string tilePath, int weight)>();
            for (int i = 0; i < DirtFloorTileAssets.Length; i++)
                dirtTiles.Add(($"{DirtTileFolder}/{DirtFloorTileAssets[i]}", 5));

            DistrictTownTestSceneCreator.CreateOrUpdatePalette(
                TownDistrictTestPaths.TieflingHolyLandProperFloorPalette,
                "tiefling_holy_land_floor",
                DungeonTilePaletteLayer.Floor,
                dirtTiles.ToArray());

            var stoneTiles = new List<(string tilePath, int weight)>();
            for (int i = 0; i < StoneFloorTileAssets.Length; i++)
                stoneTiles.Add(($"{DcssTileFolder}/{StoneFloorTileAssets[i]}", 5));

            DistrictTownTestSceneCreator.CreateOrUpdatePalette(
                TownDistrictTestPaths.TieflingSanctumInteriorFloorPalette,
                "tiefling_holy_land_sanctum_interior_floor",
                DungeonTilePaletteLayer.Floor,
                stoneTiles.ToArray());
        }

        static void PaintTieflingHolyLandLayout(DungeonFloorInstance instance)
        {
            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Tilemap wallMap = instance.Tilemaps.WallMap;
            TileBase[] dirtTiles = LoadPaletteTiles(TownDistrictTestPaths.TieflingHolyLandProperFloorPalette);
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            TileBase buildingWall = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingWallTilePath);
            TileBase buildingDoor = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingDoorTilePath);
            if (floorMap == null || wallMap == null || dirtTiles == null || wallTile == null)
                return;

            Undo.RecordObject(floorMap, "Paint tiefling holy land floor");
            Undo.RecordObject(wallMap, "Paint tiefling holy land walls");
            TieflingHolyLandLayout.Paint(floorMap, wallMap, dirtTiles, wallTile, buildingWall, buildingDoor);
            FinalizePaint(floorMap, wallMap);
            EnsureTieflingHolyLandMarkers(instance);
            instance.MarkNeedsRegeneration();
        }

        static void PaintTieflingSanctumInteriorLayout(DungeonFloorInstance instance)
        {
            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Tilemap wallMap = instance.Tilemaps.WallMap;
            TileBase[] floorTiles = LoadPaletteTiles(TownDistrictTestPaths.TieflingSanctumInteriorFloorPalette);
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            TileBase doorTile = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingDoorTilePath);
            if (floorMap == null || wallMap == null || floorTiles == null || wallTile == null)
                return;

            Undo.RecordObject(floorMap, "Paint tiefling sanctum interior floor");
            Undo.RecordObject(wallMap, "Paint tiefling sanctum interior walls");
            BarbarianShamanTentLayout.Paint(floorMap, wallMap, floorTiles, wallTile, doorTile);
            FinalizePaint(floorMap, wallMap);
            EnsureTieflingSanctumInteriorMarkers(instance);
            instance.MarkNeedsRegeneration();
        }

        static void EnsureTieflingHolyLandMarkers(DungeonFloorInstance instance)
        {
            Transform markersRoot = EnsureMarkersRoot(instance);
            ClearChildren(markersRoot);
            CreateMarker(markersRoot, instance, "PlayerStart", StaticHubMarkerKind.PlayerStart, TieflingHolyLandLayout.PlayerStartCell);
            CreateMarker(markersRoot, instance, "Chief", StaticHubMarkerKind.NpcSlot, TieflingHolyLandLayout.ChiefNpcCell, TieflingHolyLandLayout.ChiefMarkerId);
        }

        static void EnsureTieflingSanctumInteriorMarkers(DungeonFloorInstance instance)
        {
            Transform markersRoot = EnsureMarkersRoot(instance);
            ClearChildren(markersRoot);
            CreateMarker(markersRoot, instance, "PlayerStart", StaticHubMarkerKind.PlayerStart, TieflingHolyLandSanctumLayout.InteriorArrivalCell);
            CreateMarker(
                markersRoot,
                instance,
                "Forgemaster",
                StaticHubMarkerKind.NpcSlot,
                TieflingHolyLandSanctumLayout.ForgemasterNpcCell,
                TieflingHolyLandSanctumLayout.ForgemasterMarkerId);
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
