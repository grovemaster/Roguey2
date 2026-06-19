#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.GridFeatures;
using JRogue.Manager.Grid;
using JRogue.World.Generation;
using JRogue.World.Town;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    /// <summary>Market General Store on town_market (8×8 twin-door exterior) + scene-painted shop interior.</summary>
    public static class MarketGeneralStorePackCreator
    {
        const string TileFolder = "Assets/TileMaps/Town";
        const string BuildingStoneWallTilePath = TileFolder + "/Town_Building_StoneWall.asset";
        const string BuildingStoneCornerTilePath = TileFolder + "/Town_Building_StoneCorner.asset";
        const string BuildingStoneWindowTilePath = TileFolder + "/Town_Building_StoneWindow.asset";
        const string BuildingRoofTilePath = TileFolder + "/Town_Building_Roof.asset";
        const string BuildingRoofLeftTilePath = TileFolder + "/Town_Building_RoofLeft.asset";
        const string BuildingRoofRightTilePath = TileFolder + "/Town_Building_RoofRight.asset";
        const string BuildingDoorTilePath = TileFolder + "/Town_Building_Door.asset";
        const string DcssTileFolder = "Assets/TileMaps/Town/Dcss";
        const string CounterFloorTilePath = "Assets/TileMaps/Town/Town_FloorPavement.asset";
        const string ShopFormationPath =
            TownDistrictTestPaths.MarketGeneralStoreFolder + "/PartyFormation_ShopInterior.asset";
        const string HumanNpcPrefabPath = "Assets/Prefabs/Actor/Npc/HumanNpc.prefab";
        const string KeeperPrefabPath = "Assets/Resources/Town/Npc/TownNpc_MarketGeneralStoreKeeper.prefab";
        const string KeeperDialogPath = "Assets/Resources/Dialog/Profiles/NpcDialog_MarketGeneralStoreKeeper.asset";
        const string KeeperSpritePath = "Assets/Art/NPC/Sprites/NPC_Mira.png";

        [MenuItem("JRogue/Town/Setup Market General Store")]
        public static void SetupMarketGeneralStore()
        {
            EnsureFolders();
            DcssRectGrayFloorVarietyEditor.ConfigureRectGrayFloorTiles();
            EnsureKeeperDialog();
            EnsureKeeperPrefab();
            PartyFormationSpawnProfile shopFormation = EnsureShopInteriorFormationProfile();
            EnsureInteriorFloorDefinition(shopFormation);
            EnsureMarketPortals();
            EnsureMarketFacadeOverlay();
            EnsureInteriorFacadeOverlay();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MarketGeneralStore] Data ready. Run JRogue → Town → Fix District Town Test Scene.");
        }

        public static void IntegrateDistrictTownScene(DungeonFloorInstance interiorInstance)
        {
            if (interiorInstance == null)
                return;

            PaintInteriorShop(interiorInstance);
            EnsureInteriorMarkers(interiorInstance);
        }

        public static void PaintMarketExteriorFacade(Tilemap floorMap, Tilemap wallMap)
        {
            if (floorMap == null || wallMap == null)
                return;

            TownFacadePaintCell[] cells = BuildExteriorFacadeCells();
            if (cells == null || cells.Length == 0)
                return;

            for (int i = 0; i < cells.Length; i++)
            {
                TownFacadePaintCell entry = cells[i];
                if (entry.tile == null)
                    continue;

                if (entry.layer == TownFacadePaintLayer.Floor)
                {
                    floorMap.SetTile(entry.cell, entry.tile);
                    wallMap.SetTile(entry.cell, null);
                }
                else
                {
                    wallMap.SetTile(entry.cell, entry.tile);
                    floorMap.SetTile(entry.cell, null);
                }
            }

            GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(floorMap);
            GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(wallMap);
        }

        static void EnsureFolders()
        {
            EnsureFolder(TownDistrictTestPaths.MarketGeneralStoreFolder);
            EnsureFolder("Assets/Resources/Town/Npc");
            EnsureFolder("Assets/Resources/Dialog/Profiles");
        }

        static void EnsureKeeperDialog()
        {
            var profile = AssetDatabase.LoadAssetAtPath<NpcDialogProfile>(KeeperDialogPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<NpcDialogProfile>();
                AssetDatabase.CreateAsset(profile, KeeperDialogPath);
            }

            var so = new SerializedObject(profile);
            so.FindProperty("npcId").stringValue = MarketGeneralStoreLayout.NpcId;
            so.FindProperty("rootNodeIndex").intValue = 0;
            so.FindProperty("incrementTalkCountOnStart").boolValue = true;

            SerializedProperty nodes = so.FindProperty("nodes");
            nodes.arraySize = 1;
            SerializedProperty node = nodes.GetArrayElementAtIndex(0);
            node.FindPropertyRelative("kind").enumValueIndex = (int)DialogNodeKind.Line;
            node.FindPropertyRelative("line").FindPropertyRelative("textTemplate").stringValue =
                "Fresh goods from every dimension, {partyName}. Take a look at today's stock.";
            node.FindPropertyRelative("nextNodeIndex").intValue = DialogGraph.NoNode;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }

        static void EnsureKeeperPrefab()
        {
            GameObject humanNpc = AssetDatabase.LoadAssetAtPath<GameObject>(HumanNpcPrefabPath);
            if (humanNpc == null)
            {
                Debug.LogWarning("[MarketGeneralStore] Missing HumanNpc prefab — run Create NPC Dialog Pack first.");
                return;
            }

            Sprite keeperSprite = AssetDatabase.LoadAssetAtPath<Sprite>(KeeperSpritePath);
            string prefabPath = KeeperPrefabPath;
            bool createdNew = !File.Exists(prefabPath);
            GameObject instance = createdNew
                ? (GameObject)PrefabUtility.InstantiatePrefab(humanNpc)
                : PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                instance.name = "TownNpc_MarketGeneralStoreKeeper";

                NpcController npc = instance.GetComponent<NpcController>();
                if (npc != null)
                {
                    var npcSo = new SerializedObject(npc);
                    npcSo.FindProperty("npcId").stringValue = MarketGeneralStoreLayout.NpcId;
                    npcSo.FindProperty("displayName").stringValue = "Market Keeper";
                    npcSo.FindProperty("dialogProfile").objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<NpcDialogProfile>(KeeperDialogPath);
                    npcSo.ApplyModifiedPropertiesWithoutUndo();
                }

                if (keeperSprite != null)
                {
                    SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.sprite = keeperSprite;
                        renderer.sortingOrder = 20;
                    }
                }

                NpcCounterTalkBinding counterBinding = instance.GetComponent<NpcCounterTalkBinding>()
                    ?? instance.AddComponent<NpcCounterTalkBinding>();
                counterBinding.Configure(
                    MarketGeneralStoreLayout.CustomerRowY,
                    MarketGeneralStoreLayout.CounterRowY);

                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            }
            finally
            {
                if (createdNew)
                    Object.DestroyImmediate(instance);
                else
                    PrefabUtility.UnloadPrefabContents(instance);
            }
        }

        static PartyFormationSpawnProfile EnsureShopInteriorFormationProfile()
        {
            var profile = LoadOrCreate<PartyFormationSpawnProfile>(ShopFormationPath);
            var so = new SerializedObject(profile);
            SerializedProperty layouts = so.FindProperty("layouts");
            layouts.arraySize = 4;

            SetFormationLayout(layouts, 0, 1, new[] { Vector3Int.zero });
            SetFormationLayout(layouts, 1, 2, new[] { Vector3Int.zero, new Vector3Int(-1, 0, 0) });
            SetFormationLayout(
                layouts,
                2,
                3,
                new[] { new Vector3Int(-1, 0, 0), Vector3Int.zero, new Vector3Int(1, 0, 0) });
            SetFormationLayout(
                layouts,
                3,
                4,
                new[]
                {
                    new Vector3Int(-1, 0, 0),
                    Vector3Int.zero,
                    new Vector3Int(1, 0, 0),
                    new Vector3Int(0, -1, 0),
                });

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static void SetFormationLayout(
            SerializedProperty layouts,
            int layoutIndex,
            int memberCount,
            Vector3Int[] offsets)
        {
            SerializedProperty layout = layouts.GetArrayElementAtIndex(layoutIndex);
            layout.FindPropertyRelative("memberCount").intValue = memberCount;
            SerializedProperty relativeOffsets = layout.FindPropertyRelative("relativeOffsets");
            relativeOffsets.arraySize = offsets.Length;
            for (int i = 0; i < offsets.Length; i++)
                relativeOffsets.GetArrayElementAtIndex(i).vector3IntValue = offsets[i];
        }

        static DungeonFloorDefinition EnsureInteriorFloorDefinition(PartyFormationSpawnProfile formation)
        {
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{DcssTileFolder}/Dcss_Floor_RectGray0.asset");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/TileMaps/Town/Town_WallBuilding.asset");
            formation ??= AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(
                "Assets/Resources/Dungeon/PartyFormation_Default.asset");

            var def = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(
                TownDistrictTestPaths.MarketGeneralStoreInteriorFloorDef);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<DungeonFloorDefinition>();
                AssetDatabase.CreateAsset(def, TownDistrictTestPaths.MarketGeneralStoreInteriorFloorDef);
            }

            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = MarketGeneralStoreLayout.InteriorFloorId;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;

            SerializedProperty portals = so.FindProperty("portals");
            portals.arraySize = 2;

            SerializedProperty westExit = portals.GetArrayElementAtIndex(0);
            westExit.FindPropertyRelative("portalLinkId").stringValue = MarketGeneralStoreLayout.ExitWestLinkId;
            westExit.FindPropertyRelative("targetFloorId").stringValue = MarketTownFloorIds.FloorId;
            westExit.FindPropertyRelative("portalCell").vector3IntValue = MarketGeneralStoreLayout.InteriorWestExitCell;
            westExit.FindPropertyRelative("listLabel").stringValue = "Exit";

            SerializedProperty eastExit = portals.GetArrayElementAtIndex(1);
            eastExit.FindPropertyRelative("portalLinkId").stringValue = MarketGeneralStoreLayout.ExitEastLinkId;
            eastExit.FindPropertyRelative("targetFloorId").stringValue = MarketTownFloorIds.FloorId;
            eastExit.FindPropertyRelative("portalCell").vector3IntValue = MarketGeneralStoreLayout.InteriorEastExitCell;
            eastExit.FindPropertyRelative("listLabel").stringValue = "Exit";

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 2;

            SerializedProperty westArrival = arrivals.GetArrayElementAtIndex(0);
            westArrival.FindPropertyRelative("portalLinkId").stringValue = MarketGeneralStoreLayout.EnterWestLinkId;
            westArrival.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                MarketGeneralStoreLayout.InteriorWestArrivalCell;

            SerializedProperty eastArrival = arrivals.GetArrayElementAtIndex(1);
            eastArrival.FindPropertyRelative("portalLinkId").stringValue = MarketGeneralStoreLayout.EnterEastLinkId;
            eastArrival.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                MarketGeneralStoreLayout.InteriorEastArrivalCell;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static void EnsureMarketPortals()
        {
            var def = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(TownDistrictTestPaths.MarketFloorDef);
            if (def == null)
                return;

            int stripWidth = DistrictSquareMarketTransition.StripMaxX - DistrictSquareMarketTransition.StripMinX + 1;

            var so = new SerializedObject(def);
            SerializedProperty portals = so.FindProperty("portals");
            portals.arraySize = stripWidth + 2;

            WriteSouthStripPortals(
                portals,
                0,
                DistrictSquareMarketTransition.MarketToSquareLinkId,
                DimensionSquareFloorIds.FloorId,
                DistrictSquareMarketTransition.MarketSouthEdgeY,
                "Dimension Square");

            SerializedProperty westEnter = portals.GetArrayElementAtIndex(stripWidth);
            westEnter.FindPropertyRelative("portalLinkId").stringValue = MarketGeneralStoreLayout.EnterWestLinkId;
            westEnter.FindPropertyRelative("targetFloorId").stringValue = MarketGeneralStoreLayout.InteriorFloorId;
            westEnter.FindPropertyRelative("portalCell").vector3IntValue = MarketGeneralStoreLayout.ExteriorWestDoorCell;
            westEnter.FindPropertyRelative("listLabel").stringValue = "Market General Store";

            SerializedProperty eastEnter = portals.GetArrayElementAtIndex(stripWidth + 1);
            eastEnter.FindPropertyRelative("portalLinkId").stringValue = MarketGeneralStoreLayout.EnterEastLinkId;
            eastEnter.FindPropertyRelative("targetFloorId").stringValue = MarketGeneralStoreLayout.InteriorFloorId;
            eastEnter.FindPropertyRelative("portalCell").vector3IntValue = MarketGeneralStoreLayout.ExteriorEastDoorCell;
            eastEnter.FindPropertyRelative("listLabel").stringValue = "Market General Store";

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 3;

            SerializedProperty squareArrival = arrivals.GetArrayElementAtIndex(0);
            squareArrival.FindPropertyRelative("portalLinkId").stringValue =
                DistrictSquareMarketTransition.SquareToMarketLinkId;
            squareArrival.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                DistrictSquareMarketTransition.MarketArrivalCell;

            SerializedProperty westExitArrival = arrivals.GetArrayElementAtIndex(1);
            westExitArrival.FindPropertyRelative("portalLinkId").stringValue = MarketGeneralStoreLayout.ExitWestLinkId;
            westExitArrival.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                MarketGeneralStoreLayout.ExteriorWestDoorCell;

            SerializedProperty eastExitArrival = arrivals.GetArrayElementAtIndex(2);
            eastExitArrival.FindPropertyRelative("portalLinkId").stringValue = MarketGeneralStoreLayout.ExitEastLinkId;
            eastExitArrival.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                MarketGeneralStoreLayout.ExteriorEastDoorCell;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
        }

        static void WriteSouthStripPortals(
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

        static void EnsureMarketFacadeOverlay()
        {
            TownFacadePaintCell[] cells = BuildExteriorFacadeCells();
            if (cells == null || cells.Length == 0)
                return;

            TownBuildingFacadeOverlay overlay =
                LoadOrCreate<TownBuildingFacadeOverlay>(TownDistrictTestPaths.MarketFacadeOverlay);
            overlay.Configure(MarketTownFloorIds.FloorId, cells);
            EditorUtility.SetDirty(overlay);
        }

        static TownFacadePaintCell[] BuildExteriorFacadeCells()
        {
            TileBase stoneCorner = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingStoneCornerTilePath);
            TileBase stoneWall = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingStoneWallTilePath);
            TileBase stoneWindow = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingStoneWindowTilePath);
            TileBase roofLeft = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingRoofLeftTilePath);
            TileBase roof = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingRoofTilePath);
            TileBase roofRight = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingRoofRightTilePath);
            TileBase door = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingDoorTilePath);
            if (stoneCorner == null || stoneWall == null || door == null)
            {
                Debug.LogWarning("[MarketGeneralStore] Building tiles missing — run Fix TownTest Scene.");
                return null;
            }

            int originX = MarketGeneralStoreLayout.ExteriorOriginX;
            int originY = MarketGeneralStoreLayout.ExteriorOriginY;
            int width = MarketGeneralStoreLayout.ExteriorWidth;
            int depth = MarketGeneralStoreLayout.ExteriorDepth;
            int westDoorLocalX = MarketGeneralStoreLayout.ExteriorWestDoorCell.x - originX;
            int eastDoorLocalX = MarketGeneralStoreLayout.ExteriorEastDoorCell.x - originX;

            var cells = new List<TownFacadePaintCell>(width * depth);
            for (int dy = 0; dy < depth; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int x = originX + dx;
                    int y = originY + dy;
                    var cell = new Vector3Int(x, y, 0);
                    bool isDoor = dy == 0 && (dx == westDoorLocalX || dx == eastDoorLocalX);

                    if (isDoor)
                    {
                        cells.Add(new TownFacadePaintCell
                        {
                            cell = cell,
                            tile = door,
                            layer = TownFacadePaintLayer.Floor,
                        });
                        continue;
                    }

                    cells.Add(new TownFacadePaintCell
                    {
                        cell = cell,
                        tile = ResolveFacadeWallTile(dx, dy, width, depth, stoneCorner, stoneWall, stoneWindow, roofLeft, roof, roofRight),
                        layer = TownFacadePaintLayer.Wall,
                    });
                }
            }

            return cells.ToArray();
        }

        static void EnsureInteriorFacadeOverlay()
        {
            TileBase door = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingDoorTilePath);
            if (door == null)
                return;

            var cells = new[]
            {
                new TownFacadePaintCell
                {
                    cell = MarketGeneralStoreLayout.InteriorWestExitCell,
                    tile = door,
                    layer = TownFacadePaintLayer.Floor,
                },
                new TownFacadePaintCell
                {
                    cell = MarketGeneralStoreLayout.InteriorEastExitCell,
                    tile = door,
                    layer = TownFacadePaintLayer.Floor,
                },
            };

            TownBuildingFacadeOverlay overlay = LoadOrCreate<TownBuildingFacadeOverlay>(
                TownDistrictTestPaths.MarketGeneralStoreInteriorFacadeOverlay);
            overlay.Configure(MarketGeneralStoreLayout.InteriorFloorId, cells);
            EditorUtility.SetDirty(overlay);
        }

        static void PaintInteriorShop(DungeonFloorInstance instance)
        {
            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Tilemap wallMap = instance.Tilemaps.WallMap;
            if (floorMap == null || wallMap == null)
                return;

            TileBase[] floorTiles = LoadInteriorFloorTiles();
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/TileMaps/Town/Town_WallBuilding.asset");
            TileBase counterTile = AssetDatabase.LoadAssetAtPath<TileBase>(CounterFloorTilePath);
            if (floorTiles == null || floorTiles.Length == 0 || wallTile == null || counterTile == null)
                return;

            int width = MarketGeneralStoreLayout.InteriorWidth;
            int height = MarketGeneralStoreLayout.InteriorHeight;

            Undo.RecordObject(floorMap, "Paint market store interior floor");
            Undo.RecordObject(wallMap, "Paint market store interior walls");
            floorMap.ClearAllTiles();
            wallMap.ClearAllTiles();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    bool isPerimeter = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    bool isExit = MarketGeneralStoreLayout.IsInteriorExitCell(cell);
                    bool isCounter = MarketGeneralStoreLayout.IsCounterCell(cell);

                    if (isCounter)
                    {
                        floorMap.SetTile(cell, counterTile);
                        wallMap.SetTile(cell, null);
                        continue;
                    }

                    if (isPerimeter && !isExit)
                    {
                        wallMap.SetTile(cell, wallTile);
                        continue;
                    }

                    if (isExit)
                    {
                        floorMap.SetTile(cell, floorTiles[0]);
                        continue;
                    }

                    floorMap.SetTile(cell, PickFloorTile(x, y, floorTiles));
                }
            }

            GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(floorMap);
            GridOverlayPainter.ApplyCenterPivotAlignmentToPaintedCells(wallMap);
            floorMap.CompressBounds();
            wallMap.CompressBounds();
            EditorUtility.SetDirty(floorMap);
            EditorUtility.SetDirty(wallMap);
        }

        static void EnsureInteriorMarkers(DungeonFloorInstance instance)
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

            CreateMarker(markersRoot, gridComponent, floorMap, "MarketKeeper", StaticHubMarkerKind.NpcSlot,
                MarketGeneralStoreLayout.ClerkNpcCell, MarketGeneralStoreLayout.NpcMarkerId);
            CreateMarker(markersRoot, gridComponent, floorMap, "InteriorWestExit", StaticHubMarkerKind.BuildingExit,
                MarketGeneralStoreLayout.InteriorWestExitCell);
            CreateMarker(markersRoot, gridComponent, floorMap, "InteriorEastExit", StaticHubMarkerKind.BuildingExit,
                MarketGeneralStoreLayout.InteriorEastExitCell);
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
                : GridCellWorld.GetCellCenter(floorMap, cell);
            go.transform.position = world;
        }

        static TileBase[] LoadInteriorFloorTiles()
        {
            string[] names =
            {
                "Dcss_Floor_RectGray0.asset",
                "Dcss_Floor_RectGray1.asset",
                "Dcss_Floor_RectGray2.asset",
            };
            var tiles = new TileBase[names.Length];
            for (int i = 0; i < names.Length; i++)
                tiles[i] = AssetDatabase.LoadAssetAtPath<TileBase>($"{DcssTileFolder}/{names[i]}");
            return tiles;
        }

        static TileBase PickFloorTile(int x, int y, TileBase[] tiles)
        {
            int hash = unchecked((x * 73856093) ^ (y * 19349663));
            return tiles[Mathf.Abs(hash) % tiles.Length];
        }

        static TileBase ResolveFacadeWallTile(
            int dx,
            int dy,
            int width,
            int depth,
            TileBase stoneCorner,
            TileBase stoneWall,
            TileBase stoneWindow,
            TileBase roofLeft,
            TileBase roof,
            TileBase roofRight)
        {
            bool isRoofRow = dy == depth - 1;
            if (isRoofRow)
            {
                if (dx == 0)
                    return roofLeft;
                if (dx == width - 1)
                    return roofRight;
                return roof;
            }

            if (dx == 0 || dx == width - 1)
                return stoneCorner;

            return dy == 1 && (dx == 1 || dx == width - 2) ? stoneWindow : stoneWall;
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
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
