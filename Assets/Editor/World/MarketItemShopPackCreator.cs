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
    /// <summary>Market item shop on town_market (5×4 exterior) + scene-painted shop interior.</summary>
    public static class MarketItemShopPackCreator
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
            TownDistrictTestPaths.MarketItemShopFolder + "/PartyFormation_ShopInterior.asset";
        const string HumanNpcPrefabPath = "Assets/Prefabs/Actor/Npc/HumanNpc.prefab";
        const string ClerkPrefabPath = "Assets/Resources/Town/Npc/TownNpc_MarketItemShopClerk.prefab";
        const string ClerkDialogPath = "Assets/Resources/Dialog/Profiles/NpcDialog_MarketItemShopClerk.asset";
        const string ClerkSpritePath = "Assets/Art/NPC/Sprites/NPC_Edda.png";

        [MenuItem("JRogue/Town/Setup Market Item Shop")]
        public static void SetupMarketItemShop()
        {
            EnsureFolders();
            DcssRectGrayFloorVarietyEditor.ConfigureRectGrayFloorTiles();
            EnsureClerkDialog();
            EnsureClerkPrefab();
            PartyFormationSpawnProfile shopFormation = EnsureShopInteriorFormationProfile();
            EnsureInteriorFloorDefinition(shopFormation);
            EnsureInteriorFacadeOverlay();
            MarketTownBuildingPortalsEditor.EnsureMarketBuildingPortals();
            MarketTownPackCreator.EnsureMarketFacadeOverlay();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MarketItemShop] Data ready. Run JRogue → Town → Fix District Town Test Scene.");
        }

        public static void IntegrateDistrictTownScene(DungeonFloorInstance interiorInstance)
        {
            if (interiorInstance == null)
                return;

            PaintInteriorShop(interiorInstance);
            EnsureInteriorMarkers(interiorInstance);
        }

        public static TownFacadePaintCell[] BuildExteriorFacadeCells() =>
            BuildExteriorFacadeCellsInternal();

        static void EnsureFolders()
        {
            EnsureFolder(TownDistrictTestPaths.MarketItemShopFolder);
            EnsureFolder("Assets/Resources/Town/Npc");
            EnsureFolder("Assets/Resources/Dialog/Profiles");
        }

        static void EnsureClerkDialog()
        {
            var profile = AssetDatabase.LoadAssetAtPath<NpcDialogProfile>(ClerkDialogPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<NpcDialogProfile>();
                AssetDatabase.CreateAsset(profile, ClerkDialogPath);
            }

            var so = new SerializedObject(profile);
            so.FindProperty("npcId").stringValue = MarketItemShopLayout.NpcId;
            so.FindProperty("rootNodeIndex").intValue = 0;
            so.FindProperty("incrementTalkCountOnStart").boolValue = true;

            SerializedProperty nodes = so.FindProperty("nodes");
            nodes.arraySize = 1;
            SerializedProperty node = nodes.GetArrayElementAtIndex(0);
            node.FindPropertyRelative("kind").enumValueIndex = (int)DialogNodeKind.Line;
            node.FindPropertyRelative("line").FindPropertyRelative("textTemplate").stringValue =
                "Browse if you like, {partyName}. Stock rotates with the dimensions.";
            node.FindPropertyRelative("nextNodeIndex").intValue = DialogGraph.NoNode;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }

        static void EnsureClerkPrefab()
        {
            GameObject humanNpc = AssetDatabase.LoadAssetAtPath<GameObject>(HumanNpcPrefabPath);
            if (humanNpc == null)
            {
                Debug.LogWarning("[MarketItemShop] Missing HumanNpc prefab — run Create NPC Dialog Pack first.");
                return;
            }

            Sprite clerkSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ClerkSpritePath);
            string prefabPath = ClerkPrefabPath;
            bool createdNew = !File.Exists(prefabPath);
            GameObject instance = createdNew
                ? (GameObject)PrefabUtility.InstantiatePrefab(humanNpc)
                : PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                instance.name = "TownNpc_MarketItemShopClerk";

                NpcController npc = instance.GetComponent<NpcController>();
                if (npc != null)
                {
                    var npcSo = new SerializedObject(npc);
                    npcSo.FindProperty("npcId").stringValue = MarketItemShopLayout.NpcId;
                    npcSo.FindProperty("displayName").stringValue = "Item Shop Clerk";
                    npcSo.FindProperty("dialogProfile").objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<NpcDialogProfile>(ClerkDialogPath);
                    npcSo.ApplyModifiedPropertiesWithoutUndo();
                }

                if (clerkSprite != null)
                {
                    SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.sprite = clerkSprite;
                        renderer.sortingOrder = 20;
                    }
                }

                NpcCounterTalkBinding counterBinding = instance.GetComponent<NpcCounterTalkBinding>()
                    ?? instance.AddComponent<NpcCounterTalkBinding>();
                counterBinding.Configure(
                    MarketItemShopLayout.CustomerRowY,
                    MarketItemShopLayout.CounterRowY);

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
                TownDistrictTestPaths.MarketItemShopInteriorFloorDef);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<DungeonFloorDefinition>();
                AssetDatabase.CreateAsset(def, TownDistrictTestPaths.MarketItemShopInteriorFloorDef);
            }

            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = MarketItemShopLayout.InteriorFloorId;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;

            SerializedProperty portals = so.FindProperty("portals");
            portals.arraySize = 1;
            SerializedProperty exitPortal = portals.GetArrayElementAtIndex(0);
            exitPortal.FindPropertyRelative("portalLinkId").stringValue = MarketItemShopLayout.ExitLinkId;
            exitPortal.FindPropertyRelative("targetFloorId").stringValue = MarketTownFloorIds.FloorId;
            exitPortal.FindPropertyRelative("portalCell").vector3IntValue = MarketItemShopLayout.InteriorExitCell;
            exitPortal.FindPropertyRelative("listLabel").stringValue = "Exit";

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 1;
            SerializedProperty arrival = arrivals.GetArrayElementAtIndex(0);
            arrival.FindPropertyRelative("portalLinkId").stringValue = MarketItemShopLayout.EnterLinkId;
            arrival.FindPropertyRelative("arrivalAnchor").vector3IntValue = MarketItemShopLayout.InteriorArrivalCell;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
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
                    cell = MarketItemShopLayout.InteriorExitCell,
                    tile = door,
                    layer = TownFacadePaintLayer.Floor,
                },
            };

            TownBuildingFacadeOverlay overlay = LoadOrCreate<TownBuildingFacadeOverlay>(
                TownDistrictTestPaths.MarketItemShopInteriorFacadeOverlay);
            overlay.Configure(MarketItemShopLayout.InteriorFloorId, cells);
            EditorUtility.SetDirty(overlay);
        }

        static TownFacadePaintCell[] BuildExteriorFacadeCellsInternal()
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
                Debug.LogWarning("[MarketItemShop] Building tiles missing — run Fix TownTest Scene.");
                return null;
            }

            int originX = MarketItemShopLayout.ExteriorOriginX;
            int originY = MarketItemShopLayout.ExteriorOriginY;
            int width = MarketItemShopLayout.ExteriorWidth;
            int depth = MarketItemShopLayout.ExteriorDepth;
            int doorLocalX = MarketItemShopLayout.ExteriorDoorLocalX;

            var cells = new List<TownFacadePaintCell>(width * depth);
            for (int dy = 0; dy < depth; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int x = originX + dx;
                    int y = originY + dy;
                    var cell = new Vector3Int(x, y, 0);
                    bool isDoor = dx == doorLocalX && dy == 0;

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

            int width = MarketItemShopLayout.InteriorWidth;
            int height = MarketItemShopLayout.InteriorHeight;

            Undo.RecordObject(floorMap, "Paint item shop interior floor");
            Undo.RecordObject(wallMap, "Paint item shop interior walls");
            floorMap.ClearAllTiles();
            wallMap.ClearAllTiles();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    bool isPerimeter = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    bool isExit = cell == MarketItemShopLayout.InteriorExitCell;
                    bool isCounter = MarketItemShopLayout.IsCounterCell(cell);

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

            CreateMarker(markersRoot, gridComponent, floorMap, "ItemShopClerk", StaticHubMarkerKind.NpcSlot,
                MarketItemShopLayout.ClerkNpcCell, MarketItemShopLayout.NpcMarkerId);
            CreateMarker(markersRoot, gridComponent, floorMap, "InteriorExit", StaticHubMarkerKind.BuildingExit,
                MarketItemShopLayout.InteriorExitCell);
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
