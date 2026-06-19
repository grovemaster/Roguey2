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
    /// <summary>town_market district — open 40×40 area linked south to dimension_square.</summary>
    public static class MarketTownPackCreator
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

        [MenuItem("JRogue/Town/Setup Market Town Area")]
        public static void SetupMarketTown()
        {
            EnsureFolders();
            DcssRectGrayFloorVarietyEditor.ConfigureRectGrayFloorTiles();
            EnsureMarketPalettes();
            EnsureMarketFloorDefinition();
            EnsureDimensionSquarePortals();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MarketTown] Data ready. Run JRogue → Town → Fix District Town Test Scene.");
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets/Resources/Town/DistrictTest/TownArea");
            EnsureFolder(TownDistrictTestPaths.MarketFolder);
        }

        public static void IntegrateDistrictTownScene(DungeonFloorInstance marketInstance)
        {
            if (marketInstance == null)
                return;

            PaintMarketLayout(marketInstance);
            EnsureMarketMarkers(marketInstance);
            MarketGeneralStorePackCreator.PaintMarketExteriorFacade(
                marketInstance.Tilemaps.FloorMap,
                marketInstance.Tilemaps.WallMap);
        }

        public static void UpdateDistrictCatalog(
            DungeonFloorDefinition squareDef,
            DungeonFloorDefinition marketDef,
            DungeonFloorDefinition guildInteriorDef,
            DungeonFloorDefinition storeInteriorDef)
        {
            DistrictTestCatalogUpdater.UpdateCatalog(squareDef, marketDef, guildInteriorDef, storeInteriorDef);
        }

        static void EnsureMarketPalettes()
        {
            var floorTiles = new List<(string tilePath, int weight)>();
            for (int i = 0; i < DcssFloorTileAssets.Length; i++)
                floorTiles.Add(($"{DcssTileFolder}/{DcssFloorTileAssets[i]}", 5));

            DistrictTownTestSceneCreator.CreateOrUpdatePalette(
                TownDistrictTestPaths.MarketFloorPalette,
                "town_market_floor",
                DungeonTilePaletteLayer.Floor,
                floorTiles.ToArray());

            DistrictTownTestSceneCreator.CreateOrUpdatePalette(
                TownDistrictTestPaths.MarketWallPalette,
                "town_market_wall",
                DungeonTilePaletteLayer.Wall,
                new[] { (WallTilePath, 5) });
        }

        static DungeonFloorDefinition EnsureMarketFloorDefinition()
        {
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{DcssTileFolder}/{DcssFloorTileAssets[0]}");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            var formation = AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(
                "Assets/Resources/Dungeon/PartyFormation_Default.asset");

            var def = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.MarketFloorDef);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<DungeonFloorDefinition>();
                AssetDatabase.CreateAsset(def, TownDistrictTestPaths.MarketFloorDef);
            }

            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = MarketTownFloorIds.FloorId;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("defaultFloorPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.MarketFloorPalette);
            so.FindProperty("defaultWallPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.MarketWallPalette);
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;

            SerializedProperty portals = so.FindProperty("portals");
            int stripWidth = DistrictSquareMarketTransition.StripMaxX - DistrictSquareMarketTransition.StripMinX + 1;
            portals.arraySize = stripWidth;
            WriteSouthStripPortals(
                portals,
                DistrictSquareMarketTransition.MarketToSquareLinkId,
                DimensionSquareFloorIds.FloorId,
                DistrictSquareMarketTransition.MarketSouthEdgeY,
                "Dimension Square");

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 1;
            SerializedProperty arrival = arrivals.GetArrayElementAtIndex(0);
            arrival.FindPropertyRelative("portalLinkId").stringValue =
                DistrictSquareMarketTransition.SquareToMarketLinkId;
            arrival.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                DistrictSquareMarketTransition.MarketArrivalCell;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static void EnsureDimensionSquarePortals()
        {
            var def = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.DimensionSquareFloorDef);
            if (def == null)
                return;

            var so = new SerializedObject(def);
            SerializedProperty portals = so.FindProperty("portals");

            int stripWidth = DistrictSquareMarketTransition.StripMaxX - DistrictSquareMarketTransition.StripMinX + 1;
            portals.arraySize = 1 + stripWidth;

            SerializedProperty guildPortal = portals.GetArrayElementAtIndex(0);
            guildPortal.FindPropertyRelative("portalLinkId").stringValue = AdventureGuildExchangeLayout.EnterLinkId;
            guildPortal.FindPropertyRelative("targetFloorId").stringValue = AdventureGuildExchangeLayout.InteriorFloorId;
            guildPortal.FindPropertyRelative("portalCell").vector3IntValue = AdventureGuildExchangeLayout.ExteriorDoorCell;
            guildPortal.FindPropertyRelative("listLabel").stringValue = "Adventure Guild Exchange";

            WriteNorthStripPortals(
                portals,
                startIndex: 1,
                DistrictSquareMarketTransition.SquareToMarketLinkId,
                MarketTownFloorIds.FloorId,
                DistrictSquareMarketTransition.SquareNorthEdgeY,
                "Market");

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 2;

            SerializedProperty guildArrival = arrivals.GetArrayElementAtIndex(0);
            guildArrival.FindPropertyRelative("portalLinkId").stringValue = AdventureGuildExchangeLayout.ExitLinkId;
            guildArrival.FindPropertyRelative("arrivalAnchor").vector3IntValue = AdventureGuildExchangeLayout.ExteriorDoorCell;

            SerializedProperty marketArrival = arrivals.GetArrayElementAtIndex(1);
            marketArrival.FindPropertyRelative("portalLinkId").stringValue =
                DistrictSquareMarketTransition.MarketToSquareLinkId;
            marketArrival.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                DistrictSquareMarketTransition.SquareArrivalCell;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
        }

        static void WriteSouthStripPortals(
            SerializedProperty portals,
            string linkId,
            string targetFloorId,
            int y,
            string label)
        {
            WriteNorthStripPortals(portals, 0, linkId, targetFloorId, y, label);
        }

        static void WriteNorthStripPortals(
            SerializedProperty portals,
            int startIndex,
            string linkId,
            string targetFloorId,
            int y,
            string label)
        {
            int stripWidth = DistrictSquareMarketTransition.StripMaxX - DistrictSquareMarketTransition.StripMinX + 1;
            for (int i = 0; i < stripWidth; i++)
            {
                int x = DistrictSquareMarketTransition.StripMinX + i;
                SerializedProperty portal = portals.GetArrayElementAtIndex(startIndex + i);
                portal.FindPropertyRelative("portalLinkId").stringValue = linkId;
                portal.FindPropertyRelative("targetFloorId").stringValue = targetFloorId;
                portal.FindPropertyRelative("portalCell").vector3IntValue = new Vector3Int(x, y, 0);
                portal.FindPropertyRelative("listLabel").stringValue = label;
            }
        }

        static void PaintMarketLayout(DungeonFloorInstance instance)
        {
            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Tilemap wallMap = instance.Tilemaps.WallMap;
            if (floorMap == null || wallMap == null)
                return;

            TileBase[] floorTiles = LoadMarketFloorTiles();
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            if (floorTiles == null || floorTiles.Length == 0 || wallTile == null)
                return;

            Undo.RecordObject(floorMap, "Paint market floor");
            Undo.RecordObject(wallMap, "Paint market walls");
            MarketTownLayout.Paint(floorMap, wallMap, floorTiles, wallTile);

            GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(floorMap);
            GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(wallMap);
            floorMap.CompressBounds();
            wallMap.CompressBounds();
            EditorUtility.SetDirty(floorMap);
            EditorUtility.SetDirty(wallMap);
        }

        static void EnsureMarketMarkers(DungeonFloorInstance instance)
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
                MarketTownLayout.PlayerStartCell);
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

        static TileBase[] LoadMarketFloorTiles()
        {
            var palette = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.MarketFloorPalette);
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
