#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.GridFeatures;
using JRogue.Manager.Grid;
using JRogue.Organizations;
using JRogue.Shop;
using JRogue.World.Generation;
using JRogue.World.Town;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    /// <summary>
    /// Adventurer's Guild Hall on dimension_square west (5×5 exterior) + scene-painted interior.
    /// </summary>
    public static class AdventureGuildHallPackCreator
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
        const string HallFormationPath =
            TownDistrictTestPaths.AdventureGuildHallFolder + "/PartyFormation_ShopInterior.asset";
        const string HumanNpcPrefabPath = "Assets/Prefabs/Actor/Npc/HumanNpc.prefab";
        const string SecretaryPrefabPath = "Assets/Resources/Town/Npc/TownNpc_AdventureGuildSecretary.prefab";
        const string SecretaryPortraitPath = "Assets/Resources/Dialog/Portraits/Portrait_Greta.asset";
        const string SecretarySpritePath = "Assets/Art/NPC/Sprites/NPC_Greta.png";
        const string OrganizationAssetPath = "Assets/Resources/Organizations/Organization_AdventurersGuild.asset";

        static readonly string[] PlayerPrefabPaths =
        {
            PartyCompositionPresets.BarbarianPrefabPath,
            PartyCompositionPresets.HumanPrefabPath,
            PartyCompositionPresets.ElfPrefabPath,
            PartyCompositionPresets.UndeadPrefabPath,
            PartyCompositionPresets.TieflingPrefabPath,
            PartyCompositionPresets.BeastmanPrefabPath,
            PartyCompositionPresets.DragonianPrefabPath,
            PartyCompositionPresets.DwarfPrefabPath,
        };

        [MenuItem("JRogue/Town/Setup Adventure Guild Hall")]
        public static void SetupAdventureGuildHall()
        {
            EnsureFolders();
            DcssRectGrayFloorVarietyEditor.ConfigureRectGrayFloorTiles();
            OrganizationDefinition organization = EnsureOrganizationDefinition();
            EnsureSecretaryPrefab(organization);
            EnsurePlayerGuildMembership();
            PartyFormationSpawnProfile formation = EnsureHallInteriorFormationProfile();
            DungeonFloorDefinition interiorDef = EnsureInteriorFloorDefinition(formation);
            EnsureDimensionSquareHallPortals();
            EnsureInteriorFacadeOverlay();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AdventureGuildHall] Data ready. Run JRogue → Town → Fix Dimension Square Test Scene.");
        }

        public static void IntegrateDistrictTownScene(DungeonFloorInstance interiorInstance)
        {
            if (interiorInstance == null)
                return;

            PaintInteriorHall(interiorInstance);
            EnsureInteriorMarkers(interiorInstance);
        }

        static void EnsureFolders()
        {
            EnsureFolder(TownDistrictTestPaths.AdventureGuildHallFolder);
            EnsureFolder("Assets/Resources/Town/Npc");
            EnsureFolder("Assets/Resources/Organizations");
            EnsureFolder("Assets/Data/Organizations");
        }

        static OrganizationDefinition EnsureOrganizationDefinition()
        {
            var organization = LoadOrCreate<OrganizationDefinition>(OrganizationAssetPath);
            organization.organizationId = OrganizationIds.AdventurersGuild;
            organization.displayName = "Adventurer's Guild";
            organization.rankBest = 1;
            organization.rankWorst = 9;
            organization.defaultStartingRank = 9;
            organization.allowsRankDecrease = false;
            organization.rankThresholds = new[] { 0, 3, 6, 9, 12, 15, 18, 21, 24 };
            EditorUtility.SetDirty(organization);
            return organization;
        }

        static void EnsurePlayerGuildMembership()
        {
            OrganizationDefinition organization =
                AssetDatabase.LoadAssetAtPath<OrganizationDefinition>(OrganizationAssetPath);

            for (int i = 0; i < PlayerPrefabPaths.Length; i++)
            {
                string path = PlayerPrefabPaths[i];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                GameObject instance = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    OrganizationMembershipRuntime membership = instance.GetComponent<OrganizationMembershipRuntime>()
                        ?? instance.AddComponent<OrganizationMembershipRuntime>();

                    if (organization != null)
                    {
                        var so = new SerializedObject(membership);
                        SerializedProperty memberships = so.FindProperty("memberships");
                        bool hasGuild = false;
                        for (int m = 0; m < memberships.arraySize; m++)
                        {
                            SerializedProperty entry = memberships.GetArrayElementAtIndex(m);
                            if (entry.FindPropertyRelative("organizationId").stringValue == OrganizationIds.AdventurersGuild)
                            {
                                hasGuild = true;
                                break;
                            }
                        }

                        if (!hasGuild)
                        {
                            memberships.arraySize++;
                            SerializedProperty added = memberships.GetArrayElementAtIndex(memberships.arraySize - 1);
                            added.FindPropertyRelative("organizationId").stringValue = OrganizationIds.AdventurersGuild;
                            added.FindPropertyRelative("rank").intValue = 9;
                            added.FindPropertyRelative("isActiveMember").boolValue = true;
                        }

                        so.ApplyModifiedPropertiesWithoutUndo();
                    }

                    PrefabUtility.SaveAsPrefabAsset(instance, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(instance);
                }
            }
        }

        static void EnsureSecretaryPrefab(OrganizationDefinition organization)
        {
            GameObject humanNpc = AssetDatabase.LoadAssetAtPath<GameObject>(HumanNpcPrefabPath);
            if (humanNpc == null)
            {
                Debug.LogWarning("[AdventureGuildHall] Missing HumanNpc prefab.");
                return;
            }

            Sprite secretarySprite = AssetDatabase.LoadAssetAtPath<Sprite>(SecretarySpritePath);
            PortraitDefinition portrait = AssetDatabase.LoadAssetAtPath<PortraitDefinition>(SecretaryPortraitPath);
            bool createdNew = !File.Exists(SecretaryPrefabPath);
            GameObject instance = createdNew
                ? (GameObject)PrefabUtility.InstantiatePrefab(humanNpc)
                : PrefabUtility.LoadPrefabContents(SecretaryPrefabPath);

            try
            {
                instance.name = "TownNpc_AdventureGuildSecretary";

                NpcController existingNpc = instance.GetComponent<NpcController>();
                if (existingNpc != null && existingNpc.GetType() != typeof(AdventurersGuildSecretaryNpcController))
                    Object.DestroyImmediate(existingNpc);

                ShopNpcController shopNpc = instance.GetComponent<ShopNpcController>();
                if (shopNpc != null)
                    Object.DestroyImmediate(shopNpc);

                AdventurersGuildSecretaryNpcController secretary =
                    instance.GetComponent<AdventurersGuildSecretaryNpcController>()
                    ?? instance.AddComponent<AdventurersGuildSecretaryNpcController>();

                var secretarySo = new SerializedObject(secretary);
                secretarySo.FindProperty("npcId").stringValue = AdventureGuildHallLayout.NpcId;
                secretarySo.FindProperty("displayName").stringValue = "Guild Secretary";
                secretarySo.FindProperty("dialogProfile").objectReferenceValue = null;
                secretarySo.FindProperty("portrait").objectReferenceValue = portrait;
                secretarySo.FindProperty("organization").objectReferenceValue = organization;
                secretarySo.ApplyModifiedPropertiesWithoutUndo();

                if (secretarySprite != null)
                {
                    SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.sprite = secretarySprite;
                        renderer.sortingOrder = 20;
                    }
                }

                NpcCounterTalkBinding counterBinding = instance.GetComponent<NpcCounterTalkBinding>()
                    ?? instance.AddComponent<NpcCounterTalkBinding>();
                counterBinding.Configure(
                    AdventureGuildHallLayout.CustomerRowY,
                    AdventureGuildHallLayout.CounterRowY);

                PrefabUtility.SaveAsPrefabAsset(instance, SecretaryPrefabPath);
            }
            finally
            {
                if (createdNew)
                    Object.DestroyImmediate(instance);
                else
                    PrefabUtility.UnloadPrefabContents(instance);
            }
        }

        static PartyFormationSpawnProfile EnsureHallInteriorFormationProfile()
        {
            string exchangeFormation =
                TownDistrictTestPaths.AdventureGuildExchangeFolder + "/PartyFormation_ShopInterior.asset";
            PartyFormationSpawnProfile exchange =
                AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(exchangeFormation);
            if (exchange != null)
            {
                if (!File.Exists(HallFormationPath))
                    AssetDatabase.CopyAsset(exchangeFormation, HallFormationPath);
                return AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(HallFormationPath);
            }

            var profile = LoadOrCreate<PartyFormationSpawnProfile>(HallFormationPath);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static DungeonFloorDefinition EnsureInteriorFloorDefinition(PartyFormationSpawnProfile formation)
        {
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{DcssTileFolder}/Dcss_Floor_RectGray0.asset");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/TileMaps/Town/Town_WallBuilding.asset");
            formation ??= AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(
                "Assets/Resources/Dungeon/PartyFormation_Default.asset");

            var def = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(
                TownDistrictTestPaths.AdventureGuildHallInteriorFloorDef);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<DungeonFloorDefinition>();
                AssetDatabase.CreateAsset(def, TownDistrictTestPaths.AdventureGuildHallInteriorFloorDef);
            }

            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = AdventureGuildHallLayout.InteriorFloorId;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;

            SerializedProperty portals = so.FindProperty("portals");
            portals.arraySize = 1;
            SerializedProperty exitPortal = portals.GetArrayElementAtIndex(0);
            exitPortal.FindPropertyRelative("portalLinkId").stringValue = AdventureGuildHallLayout.ExitLinkId;
            exitPortal.FindPropertyRelative("targetFloorId").stringValue = DimensionSquareFloorIds.FloorId;
            exitPortal.FindPropertyRelative("portalCell").vector3IntValue = AdventureGuildHallLayout.InteriorExitCell;
            exitPortal.FindPropertyRelative("listLabel").stringValue = "Exit";

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 1;
            SerializedProperty arrival = arrivals.GetArrayElementAtIndex(0);
            arrival.FindPropertyRelative("portalLinkId").stringValue = AdventureGuildHallLayout.EnterLinkId;
            arrival.FindPropertyRelative("arrivalAnchor").vector3IntValue = AdventureGuildHallLayout.InteriorArrivalCell;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static void EnsureDimensionSquareHallPortals()
        {
            ApplyDimensionSquareHallPortals(TownDistrictTestPaths.DimensionSquareFloorDef);
            ApplyDimensionSquareHallPortals(MarketTownPackCreator.LegacyDimensionSquareFloorDefPath);
            ApplyDimensionSquareHallPortals("Assets/Resources/Town/Floor_dimension_square.asset");
        }

        static void ApplyDimensionSquareHallPortals(string floorDefAssetPath)
        {
            var def = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(floorDefAssetPath);
            if (def == null)
                return;

            var so = new SerializedObject(def);
            UpsertPortal(
                so,
                AdventureGuildHallLayout.EnterLinkId,
                AdventureGuildHallLayout.InteriorFloorId,
                AdventureGuildHallLayout.ExteriorDoorCell,
                "Adventurer's Guild Hall");
            UpsertArrival(
                so,
                AdventureGuildHallLayout.ExitLinkId,
                AdventureGuildHallLayout.ExteriorDoorCell);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
        }

        static void UpsertPortal(
            SerializedObject floorSo,
            string linkId,
            string targetFloorId,
            Vector3Int portalCell,
            string listLabel)
        {
            SerializedProperty portals = floorSo.FindProperty("portals");
            for (int i = 0; i < portals.arraySize; i++)
            {
                SerializedProperty portal = portals.GetArrayElementAtIndex(i);
                if (portal.FindPropertyRelative("portalLinkId").stringValue != linkId)
                    continue;

                portal.FindPropertyRelative("targetFloorId").stringValue = targetFloorId;
                portal.FindPropertyRelative("portalCell").vector3IntValue = portalCell;
                portal.FindPropertyRelative("listLabel").stringValue = listLabel;
                return;
            }

            int index = portals.arraySize;
            portals.InsertArrayElementAtIndex(index);
            SerializedProperty added = portals.GetArrayElementAtIndex(index);
            added.FindPropertyRelative("portalLinkId").stringValue = linkId;
            added.FindPropertyRelative("targetFloorId").stringValue = targetFloorId;
            added.FindPropertyRelative("portalCell").vector3IntValue = portalCell;
            added.FindPropertyRelative("listLabel").stringValue = listLabel;
            added.FindPropertyRelative("portalMarkerId").stringValue = string.Empty;
            added.FindPropertyRelative("adjacentConfirmOnly").boolValue = false;
        }

        static void UpsertArrival(SerializedObject floorSo, string linkId, Vector3Int arrivalAnchor)
        {
            SerializedProperty arrivals = floorSo.FindProperty("arrivalBindings");
            for (int i = 0; i < arrivals.arraySize; i++)
            {
                SerializedProperty arrival = arrivals.GetArrayElementAtIndex(i);
                if (arrival.FindPropertyRelative("portalLinkId").stringValue != linkId)
                    continue;

                arrival.FindPropertyRelative("arrivalAnchor").vector3IntValue = arrivalAnchor;
                return;
            }

            int index = arrivals.arraySize;
            arrivals.InsertArrayElementAtIndex(index);
            SerializedProperty added = arrivals.GetArrayElementAtIndex(index);
            added.FindPropertyRelative("portalLinkId").stringValue = linkId;
            added.FindPropertyRelative("arrivalAnchor").vector3IntValue = arrivalAnchor;
        }

        public static void PaintAdventureGuildHallExteriorFacade(Tilemap floorMap, Tilemap wallMap)
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
                return null;

            int originX = AdventureGuildHallLayout.ExteriorOriginX;
            int originY = AdventureGuildHallLayout.ExteriorOriginY;
            int width = AdventureGuildHallLayout.ExteriorWidth;
            int depth = AdventureGuildHallLayout.ExteriorDepth;
            int doorLocalX = AdventureGuildHallLayout.ExteriorDoorCell.x - originX;

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

        static void EnsureInteriorFacadeOverlay()
        {
            TileBase door = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingDoorTilePath);
            if (door == null)
                return;

            var cells = new[]
            {
                new TownFacadePaintCell
                {
                    cell = AdventureGuildHallLayout.InteriorExitCell,
                    tile = door,
                    layer = TownFacadePaintLayer.Floor,
                },
            };

            TownBuildingFacadeOverlay overlay =
                LoadOrCreate<TownBuildingFacadeOverlay>(TownDistrictTestPaths.AdventureGuildHallInteriorFacadeOverlay);
            overlay.Configure(AdventureGuildHallLayout.InteriorFloorId, cells);
            EditorUtility.SetDirty(overlay);
        }

        static void PaintInteriorHall(DungeonFloorInstance instance)
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

            int width = AdventureGuildHallLayout.InteriorWidth;
            int height = AdventureGuildHallLayout.InteriorHeight;

            Undo.RecordObject(floorMap, "Paint guild hall interior floor");
            Undo.RecordObject(wallMap, "Paint guild hall interior walls");
            floorMap.ClearAllTiles();
            wallMap.ClearAllTiles();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    bool isPerimeter = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    bool isExit = cell == AdventureGuildHallLayout.InteriorExitCell;
                    bool isCounter = AdventureGuildHallLayout.IsCounterCell(cell);

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

            CreateMarker(markersRoot, gridComponent, floorMap, "GuildSecretary", StaticHubMarkerKind.NpcSlot,
                AdventureGuildHallLayout.SecretaryNpcCell, AdventureGuildHallLayout.NpcMarkerId);
            CreateMarker(markersRoot, gridComponent, floorMap, "InteriorExit", StaticHubMarkerKind.BuildingExit,
                AdventureGuildHallLayout.InteriorExitCell);
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
