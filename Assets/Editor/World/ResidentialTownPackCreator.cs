#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.GridFeatures;
using JRogue.Manager.Grid;
using JRogue.World.Generation;
using JRogue.World.Generation.Zones;
using JRogue.World.Town;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    /// <summary>town_residential district — 20×30 area west of town_market.</summary>
    public static class ResidentialTownPackCreator
    {
        const string WallTilePath = "Assets/TileMaps/Town/Town_WallBuilding.asset";
        const string DcssTileFolder = "Assets/TileMaps/Town/Dcss";

        static readonly string[] DcssFloorTileAssets =
        {
            "Dcss_Floor_RectGray0.asset",
            "Dcss_Floor_RectGray1.asset",
            "Dcss_Floor_RectGray2.asset",
            "Dcss_Floor_RectGray3.asset",
        };

        [MenuItem("JRogue/Town/Setup Residential Town Area")]
        public static void SetupResidentialTown()
        {
            EnsureFolders();
            DcssRectGrayFloorVarietyEditor.ConfigureRectGrayFloorTiles();
            EnsureResidentialPalettes();
            EnsureResidentialFloorDefinition();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ResidentialTown] Data ready. Run JRogue → Town → Fix District Town Test Scene.");
        }

        public static void IntegrateDistrictTownScene(DungeonFloorInstance residentialInstance)
        {
            if (residentialInstance == null)
                return;

            PaintResidentialLayout(residentialInstance);
            EnsureResidentialMarkers(residentialInstance);
            ResidentialInnPackCreator.PaintResidentialInnFacade(
                residentialInstance.Tilemaps.FloorMap,
                residentialInstance.Tilemaps.WallMap);
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets/Resources/Town/DistrictTest/TownArea");
            EnsureFolder(TownDistrictTestPaths.ResidentialFolder);
        }

        static void EnsureResidentialPalettes()
        {
            var floorTiles = new List<(string tilePath, int weight)>();
            for (int i = 0; i < DcssFloorTileAssets.Length; i++)
                floorTiles.Add(($"{DcssTileFolder}/{DcssFloorTileAssets[i]}", 5));

            DistrictTownTestSceneCreator.CreateOrUpdatePalette(
                TownDistrictTestPaths.ResidentialFloorPalette,
                "town_residential_floor",
                DungeonTilePaletteLayer.Floor,
                floorTiles.ToArray());

            DistrictTownTestSceneCreator.CreateOrUpdatePalette(
                TownDistrictTestPaths.ResidentialWallPalette,
                "town_residential_wall",
                DungeonTilePaletteLayer.Wall,
                new[] { (WallTilePath, 5) });
        }

        static DungeonFloorDefinition EnsureResidentialFloorDefinition()
        {
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{DcssTileFolder}/{DcssFloorTileAssets[0]}");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            var formation = AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(
                "Assets/Resources/Dungeon/PartyFormation_Default.asset");

            var def = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.ResidentialFloorDef);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<DungeonFloorDefinition>();
                AssetDatabase.CreateAsset(def, TownDistrictTestPaths.ResidentialFloorDef);
            }

            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = ResidentialTownFloorIds.FloorId;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("defaultFloorPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.ResidentialFloorPalette);
            so.FindProperty("defaultWallPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.ResidentialWallPalette);
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static void PaintResidentialLayout(DungeonFloorInstance instance)
        {
            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Tilemap wallMap = instance.Tilemaps.WallMap;
            if (floorMap == null || wallMap == null)
                return;

            TileBase[] floorTiles = LoadResidentialFloorTiles();
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            if (floorTiles == null || floorTiles.Length == 0 || wallTile == null)
                return;

            Undo.RecordObject(floorMap, "Paint residential floor");
            Undo.RecordObject(wallMap, "Paint residential walls");
            ResidentialTownLayout.Paint(floorMap, wallMap, floorTiles, wallTile);

            GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(floorMap);
            GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(wallMap);
            floorMap.CompressBounds();
            wallMap.CompressBounds();
            EditorUtility.SetDirty(floorMap);
            EditorUtility.SetDirty(wallMap);
        }

        static void EnsureResidentialMarkers(DungeonFloorInstance instance)
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

            CreateMarker(
                markersRoot,
                gridComponent,
                floorMap,
                "PlayerStart",
                StaticHubMarkerKind.PlayerStart,
                ResidentialTownLayout.PlayerStartCell);
        }

        static void CreateMarker(
            Transform parent,
            Grid grid,
            Tilemap floorMap,
            string objectName,
            StaticHubMarkerKind kind,
            Vector3Int cell)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            StaticHubMarker marker = go.AddComponent<StaticHubMarker>();
            marker.EditorConfigure(kind, cell);

            Vector3 world = grid != null
                ? grid.GetCellCenterWorld(cell)
                : GridCellWorld.GetCellCenter(floorMap, cell);
            go.transform.position = world;
        }

        static TileBase[] LoadResidentialFloorTiles()
        {
            var palette = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(
                TownDistrictTestPaths.ResidentialFloorPalette);
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

        static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                string grandParent = Path.GetDirectoryName(parent)?.Replace('\\', '/');
                string parentName = Path.GetFileName(parent);
                if (!string.IsNullOrEmpty(grandParent) && !AssetDatabase.IsValidFolder(grandParent))
                    EnsureFolder(parent);
                AssetDatabase.CreateFolder(grandParent, parentName);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
