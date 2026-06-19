#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.GridFeatures;
using JRogue.Interactables;
using JRogue.Manager.Grid;
using JRogue.Shop;
using JRogue.World.Generation;
using JRogue.World.Town;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    /// <summary>Residential inn on town_residential (8×3, triple door) + U-counter interior with beds.</summary>
    public static class ResidentialInnPackCreator
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
        const string BedSpritePath = "Assets/Art/Environment/Town/Inn/Town_InnBed.png";
        const string BedTilePath = "Assets/TileMaps/Town/Town_InnBed.asset";
        const string InnBedInteractablePath = "Assets/Resources/Interactables/InnBed_Town.asset";
        const string InnBedEffectPath = "Assets/Data/Interactables/Effects/InnBedSleepPrompt.asset";
        const string ShopFormationPath = TownDistrictTestPaths.ResidentialInnFolder + "/PartyFormation_InnInterior.asset";
        const string HumanNpcPrefabPath = "Assets/Prefabs/Actor/Npc/HumanNpc.prefab";
        const string KeeperPrefabPath = "Assets/Resources/Town/Npc/TownNpc_ResidentialInnKeeper.prefab";
        const string KeeperShopPath = "Assets/Resources/Shop/ShopNpc_ResidentialInnKeeper.asset";
        const string KeeperSpritePath = "Assets/Art/NPC/Sprites/NPC_Luc.png";

        [MenuItem("JRogue/Town/Setup Residential Inn")]
        public static void SetupResidentialInn()
        {
            EnsureFolders();
            DcssRectGrayFloorVarietyEditor.ConfigureRectGrayFloorTiles();
            EnsureInnBedAssets();
            EnsureKeeperShopDefinition();
            EnsureKeeperPrefab();
            PartyFormationSpawnProfile formation = EnsureInnInteriorFormationProfile();
            EnsureInteriorFloorDefinition(formation);
            EnsureInteriorFacadeOverlay();
            EnsureResidentialFacadeOverlay();
            ResidentialDistrictPortalsEditor.EnsureResidentialDistrictPortals();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ResidentialInn] Data ready. Run JRogue → Town → Fix District Town Test Scene.");
        }

        public static void IntegrateDistrictTownScene(DungeonFloorInstance interiorInstance)
        {
            if (interiorInstance == null)
                return;

            PaintInteriorInn(interiorInstance);
            EnsureInteriorMarkers(interiorInstance);
        }

        public static TownFacadePaintCell[] BuildExteriorFacadeCells() =>
            BuildExteriorFacadeCellsInternal();

        public static void PaintResidentialInnFacade(Tilemap floorMap, Tilemap wallMap)
        {
            if (floorMap == null || wallMap == null)
                return;

            TownFacadePaintCell[] cells = BuildExteriorFacadeCellsInternal();
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
            EnsureFolder(TownDistrictTestPaths.ResidentialInnFolder);
            EnsureFolder("Assets/Art/Environment/Town/Inn");
            EnsureFolder("Assets/Data/Interactables/Effects");
            EnsureFolder("Assets/Resources/Interactables");
            EnsureFolder("Assets/Resources/Town/Npc");
            EnsureFolder("Assets/Resources/Dialog/Profiles");
        }

        static void EnsureInnBedAssets()
        {
            ConfigureBedSpriteImport(BedSpritePath);
            Sprite bedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BedSpritePath);

            Tile bedTile = AssetDatabase.LoadAssetAtPath<Tile>(BedTilePath);
            if (bedTile == null)
            {
                bedTile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(bedTile, BedTilePath);
            }

            bedTile.sprite = bedSprite;
            EditorUtility.SetDirty(bedTile);

            InnBedSleepPromptEffect effect = LoadOrCreate<InnBedSleepPromptEffect>(InnBedEffectPath);

            AlwaysTruePrecondition alwaysTrue = AssetDatabase.LoadAssetAtPath<AlwaysTruePrecondition>(
                "Assets/Data/Interactables/Preconditions/AlwaysTrue.asset");
            if (alwaysTrue == null)
            {
                alwaysTrue = ScriptableObject.CreateInstance<AlwaysTruePrecondition>();
                AssetDatabase.CreateAsset(alwaysTrue, "Assets/Data/Interactables/Preconditions/AlwaysTrue.asset");
            }

            InteractableTileDefinition bed = LoadOrCreate<InteractableTileDefinition>(InnBedInteractablePath);
            bed.interactableId = InteractableTileId.InnBed;
            bed.displayName = "Bed";
            bed.kind = InteractableTileKind.Bed;
            bed.blocksOccupancy = true;
            bed.bumpEnabled = true;
            bed.allowRepeatActivation = true;
            bed.preconditions = new InteractablePrecondition[] { alwaysTrue };
            bed.onActivateEffects = new InteractableEffect[] { effect };
            bed.spriteOff = bedSprite;
            bed.spriteOn = bedSprite;
            EditorUtility.SetDirty(bed);
        }

        static void ConfigureBedSpriteImport(string spritePath)
        {
            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        static void EnsureKeeperShopDefinition()
        {
            var shop = LoadOrCreate<ShopNpcDefinition>(KeeperShopPath);
            shop.shopNpcId = ResidentialInnLayout.NpcId;
            shop.displayName = "Innkeeper";
            shop.allowPlayerBuy = false;
            shop.allowPlayerSell = false;
            shop.initialGold = 0;
            shop.initialStock = System.Array.Empty<ShopStockEntry>();
            EditorUtility.SetDirty(shop);
        }

        static void EnsureKeeperPrefab()
        {
            GameObject humanNpc = AssetDatabase.LoadAssetAtPath<GameObject>(HumanNpcPrefabPath);
            if (humanNpc == null)
            {
                Debug.LogWarning("[ResidentialInn] Missing HumanNpc prefab.");
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
                instance.name = "TownNpc_ResidentialInnKeeper";

                RemovePlainNpcControllers(instance);

                InnkeeperNpcController keeper = instance.GetComponent<InnkeeperNpcController>();
                if (keeper == null)
                {
                    NpcCounterTalkBinding existingBinding = instance.GetComponent<NpcCounterTalkBinding>();
                    if (existingBinding != null)
                        Object.DestroyImmediate(existingBinding);

                    NpcController existingNpc = instance.GetComponent<NpcController>();
                    if (existingNpc != null)
                        Object.DestroyImmediate(existingNpc);

                    keeper = instance.AddComponent<InnkeeperNpcController>();
                }

                ShopNpcDefinition shopDefinition =
                    AssetDatabase.LoadAssetAtPath<ShopNpcDefinition>(KeeperShopPath);

                var keeperSo = new SerializedObject(keeper);
                keeperSo.FindProperty("npcId").stringValue = ResidentialInnLayout.NpcId;
                keeperSo.FindProperty("displayName").stringValue = "Innkeeper";
                keeperSo.FindProperty("dialogProfile").objectReferenceValue = null;
                keeperSo.FindProperty("shopDefinition").objectReferenceValue = shopDefinition;
                keeperSo.FindProperty("lodgingCostGold").intValue = InnLodgingService.DefaultLodgingCostGold;
                keeperSo.ApplyModifiedPropertiesWithoutUndo();

                RemovePlainNpcControllers(instance);

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
                    ResidentialInnLayout.CustomerRowY,
                    ResidentialInnLayout.CounterRowY);

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

        static void RemovePlainNpcControllers(GameObject instance)
        {
            NpcController[] controllers = instance.GetComponents<NpcController>();
            for (int i = 0; i < controllers.Length; i++)
            {
                NpcController controller = controllers[i];
                if (controller != null && controller.GetType() == typeof(NpcController))
                    Object.DestroyImmediate(controller);
            }
        }

        static PartyFormationSpawnProfile EnsureInnInteriorFormationProfile()
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
                TownDistrictTestPaths.ResidentialInnInteriorFloorDef);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<DungeonFloorDefinition>();
                AssetDatabase.CreateAsset(def, TownDistrictTestPaths.ResidentialInnInteriorFloorDef);
            }

            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = ResidentialInnLayout.InteriorFloorId;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;

            SerializedProperty portals = so.FindProperty("portals");
            portals.arraySize = 3;

            WriteExitPortal(portals, 0, ResidentialInnLayout.ExitWestLinkId, ResidentialInnLayout.InteriorWestExitCell);
            WriteExitPortal(portals, 1, ResidentialInnLayout.ExitCenterLinkId, ResidentialInnLayout.InteriorCenterExitCell);
            WriteExitPortal(portals, 2, ResidentialInnLayout.ExitEastLinkId, ResidentialInnLayout.InteriorEastExitCell);

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 3;

            WriteArrival(arrivals, 0, ResidentialInnLayout.EnterWestLinkId, ResidentialInnLayout.InteriorWestArrivalCell);
            WriteArrival(arrivals, 1, ResidentialInnLayout.EnterCenterLinkId, ResidentialInnLayout.InteriorCenterArrivalCell);
            WriteArrival(arrivals, 2, ResidentialInnLayout.EnterEastLinkId, ResidentialInnLayout.InteriorEastArrivalCell);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static void WriteExitPortal(SerializedProperty portals, int index, string linkId, Vector3Int cell)
        {
            SerializedProperty portal = portals.GetArrayElementAtIndex(index);
            portal.FindPropertyRelative("portalLinkId").stringValue = linkId;
            portal.FindPropertyRelative("targetFloorId").stringValue = ResidentialTownFloorIds.FloorId;
            portal.FindPropertyRelative("portalCell").vector3IntValue = cell;
            portal.FindPropertyRelative("listLabel").stringValue = "Exit";
        }

        static void WriteArrival(SerializedProperty arrivals, int index, string linkId, Vector3Int cell)
        {
            SerializedProperty arrival = arrivals.GetArrayElementAtIndex(index);
            arrival.FindPropertyRelative("portalLinkId").stringValue = linkId;
            arrival.FindPropertyRelative("arrivalAnchor").vector3IntValue = cell;
        }

        static void EnsureInteriorFacadeOverlay()
        {
            TileBase door = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingDoorTilePath);
            if (door == null)
                return;

            var cells = new[]
            {
                new TownFacadePaintCell { cell = ResidentialInnLayout.InteriorWestExitCell, tile = door, layer = TownFacadePaintLayer.Floor },
                new TownFacadePaintCell { cell = ResidentialInnLayout.InteriorCenterExitCell, tile = door, layer = TownFacadePaintLayer.Floor },
                new TownFacadePaintCell { cell = ResidentialInnLayout.InteriorEastExitCell, tile = door, layer = TownFacadePaintLayer.Floor },
            };

            TownBuildingFacadeOverlay overlay = LoadOrCreate<TownBuildingFacadeOverlay>(
                TownDistrictTestPaths.ResidentialInnInteriorFacadeOverlay);
            overlay.Configure(ResidentialInnLayout.InteriorFloorId, cells);
            EditorUtility.SetDirty(overlay);
        }

        static void EnsureResidentialFacadeOverlay()
        {
            TownFacadePaintCell[] cells = BuildExteriorFacadeCellsInternal();
            if (cells == null || cells.Length == 0)
                return;

            TownBuildingFacadeOverlay overlay =
                LoadOrCreate<TownBuildingFacadeOverlay>(TownDistrictTestPaths.ResidentialFacadeOverlay);
            overlay.Configure(ResidentialTownFloorIds.FloorId, cells);
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
                return null;

            int originX = ResidentialInnLayout.ExteriorOriginX;
            int originY = ResidentialInnLayout.ExteriorOriginY;
            int width = ResidentialInnLayout.ExteriorWidth;
            int depth = ResidentialInnLayout.ExteriorDepth;
            int westDoor = ResidentialInnLayout.ExteriorWestDoorLocalX;
            int centerDoor = ResidentialInnLayout.ExteriorCenterDoorLocalX;
            int eastDoor = ResidentialInnLayout.ExteriorEastDoorLocalX;

            var cells = new List<TownFacadePaintCell>(width * depth);
            for (int dy = 0; dy < depth; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int x = originX + dx;
                    int y = originY + dy;
                    var cell = new Vector3Int(x, y, 0);
                    bool isDoor = dy == 0 && (dx == westDoor || dx == centerDoor || dx == eastDoor);

                    if (isDoor)
                    {
                        cells.Add(new TownFacadePaintCell { cell = cell, tile = door, layer = TownFacadePaintLayer.Floor });
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

        static void PaintInteriorInn(DungeonFloorInstance instance)
        {
            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Tilemap wallMap = instance.Tilemaps.WallMap;
            if (floorMap == null || wallMap == null)
                return;

            TileBase[] floorTiles = LoadInteriorFloorTiles();
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/TileMaps/Town/Town_WallBuilding.asset");
            TileBase counterTile = AssetDatabase.LoadAssetAtPath<TileBase>(CounterFloorTilePath);
            TileBase bedTile = AssetDatabase.LoadAssetAtPath<TileBase>(BedTilePath);
            if (floorTiles == null || floorTiles.Length == 0 || wallTile == null || counterTile == null || bedTile == null)
                return;

            int width = ResidentialInnLayout.InteriorWidth;
            int height = ResidentialInnLayout.InteriorHeight;

            Undo.RecordObject(floorMap, "Paint inn interior floor");
            Undo.RecordObject(wallMap, "Paint inn interior walls");
            floorMap.ClearAllTiles();
            wallMap.ClearAllTiles();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    bool isPerimeter = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    bool isExit = ResidentialInnLayout.IsInteriorExitCell(cell);
                    bool isCounter = ResidentialInnLayout.IsCounterCell(cell);
                    bool isBed = ResidentialInnLayout.IsBedCell(cell);

                    if (isBed)
                    {
                        floorMap.SetTile(cell, bedTile);
                        wallMap.SetTile(cell, null);
                        continue;
                    }

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

            CreateMarker(markersRoot, gridComponent, floorMap, "Innkeeper", StaticHubMarkerKind.NpcSlot,
                ResidentialInnLayout.InnkeeperNpcCell, ResidentialInnLayout.NpcMarkerId);
            CreateMarker(markersRoot, gridComponent, floorMap, "InteriorWestExit", StaticHubMarkerKind.BuildingExit,
                ResidentialInnLayout.InteriorWestExitCell);
            CreateMarker(markersRoot, gridComponent, floorMap, "InteriorCenterExit", StaticHubMarkerKind.BuildingExit,
                ResidentialInnLayout.InteriorCenterExitCell);
            CreateMarker(markersRoot, gridComponent, floorMap, "InteriorEastExit", StaticHubMarkerKind.BuildingExit,
                ResidentialInnLayout.InteriorEastExitCell);
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
            int dx, int dy, int width, int depth,
            TileBase stoneCorner, TileBase stoneWall, TileBase stoneWindow,
            TileBase roofLeft, TileBase roof, TileBase roofRight)
        {
            if (dy == depth - 1)
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
