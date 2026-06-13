#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.Input;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Data.Progression;
using JRogue.Dialog;
using JRogue.Controller.Npc;
using JRogue.Controller.Player;
using JRogue.UI.Targeting;
using JRogue.View;
using JRogue.Manager.Door;
using JRogue.World.Generation;
using JRogue.World.Generation.Phases;
using JRogue.World.Lighting;
using JRogue.World.MapInteract;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    /// <summary>
    /// Creates town floor data, Kenney Tiny Town tiles, and a playable TownTest scene
    /// (20×20 paved plaza, border walls, full dungeon services, no combat content).
    /// </summary>
    public static class TownPackCreator
    {
        const string DataRoot = "Assets/Resources/Town";
        const string SceneFolder = "Assets/Scenes/Town";
        const string ScenePath = SceneFolder + "/TownTest.unity";
        const string DungeonTestScenePath = "Assets/Scenes/Dungeon/DungeonFloorTest.unity";
        const string SpriteSheetPath = "Assets/Sprites/Environment/Town/KenneyTinyTown/tilemap_packed.png";
        const string TileFolder = "Assets/TileMaps/Town";
        const string FloorTilePath = TileFolder + "/Town_FloorPavement.asset";
        const string WallTilePath = TileFolder + "/Town_WallBuilding.asset";
        const string BuildingStoneWallTilePath = TileFolder + "/Town_Building_StoneWall.asset";
        const string BuildingStoneCornerTilePath = TileFolder + "/Town_Building_StoneCorner.asset";
        const string BuildingStoneWindowTilePath = TileFolder + "/Town_Building_StoneWindow.asset";
        const string BuildingRoofTilePath = TileFolder + "/Town_Building_Roof.asset";
        const string BuildingRoofLeftTilePath = TileFolder + "/Town_Building_RoofLeft.asset";
        const string BuildingRoofRightTilePath = TileFolder + "/Town_Building_RoofRight.asset";
        const string BuildingDoorTilePath = TileFolder + "/Town_Building_Door.asset";
        const string FacadeOverlayPath = DataRoot + "/FacadeOverlay_town_main.asset";
        const string PartyFormationPath = "Assets/Resources/Dungeon/PartyFormation_Default.asset";
        const string GameControlsPath = "Assets/Controls/GameControls.inputactions";
        const string ExperienceCurvePath = "Assets/Resources/Progression/DefaultExperienceCurve.asset";
        const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        const string BarbarianPrefabPath = "Assets/Prefabs/Actor/Race/BarbarianPlayer.prefab";
        const string HumanPrefabPath = "Assets/Prefabs/Actor/Race/HumanPlayer.prefab";
        const string HumanNpcPrefabPath = "Assets/Prefabs/Actor/Npc/HumanNpc.prefab";
        const string ResourcesDialogProfilesFolder = "Assets/Resources/Dialog/Profiles";
        const string ResourcesNpcFolder = "Assets/Resources/Town/Npc";
        const string DemoHostSpritePath = "Assets/Art/NPC/Sprites/NPC_Edda.png";
        const string DemoHostPortraitPath = "Assets/Art/Portraits/NPC/Portrait_Edda.png";

        const int SheetColumns = 12;
        const int SheetRows = 11;
        const int TilePixelSize = 16;
        const int FloorSpriteCol = 3;
        const int FloorSpriteRow = 3;
        const int WallSpriteCol = 0;
        const int WallSpriteRow = 4;
        const int BuildingStoneWallCol = 1;
        const int BuildingStoneWallRow = 4;
        const int BuildingStoneCornerCol = 0;
        const int BuildingStoneCornerRow = 4;
        const int BuildingStoneWindowCol = 3;
        const int BuildingStoneWindowRow = 4;
        const int BuildingRoofCol = 5;
        const int BuildingRoofRow = 4;
        const int BuildingRoofLeftCol = 4;
        const int BuildingRoofLeftRow = 4;
        const int BuildingRoofRightCol = 6;
        const int BuildingRoofRightRow = 4;
        const int BuildingDoorCol = 8;
        const int BuildingDoorRow = 4;

        [MenuItem("JRogue/Town/Create Town Test Data")]
        public static void CreateTownTestData() => CreateTownTestDataInternal();

        [MenuItem("JRogue/Town/Create TownTest Scene")]
        public static void CreateTownTestScene()
        {
            CreateTownTestDataInternal();
            CreateOrUpdateTownScene();
        }

        [MenuItem("JRogue/Town/Fix TownTest Scene")]
        public static void FixTownTestScene()
        {
            CreateTownTestDataInternal();
            if (!File.Exists(ScenePath))
            {
                CreateOrUpdateTownScene();
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            FixTownSceneHierarchyInPlace();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Town] Fixed {ScenePath}. Save and press Play.");
        }

        static void CreateTownTestDataInternal()
        {
            EnsureFolder("Assets/Sprites/Environment/Town/KenneyTinyTown");
            EnsureFolder(TileFolder);
            EnsureFolder(DataRoot);
            EnsureFolder(SceneFolder);

            EnsureTownSpriteSheetImported();
            EnsureTownTiles();
            EnsureTownBuildingTiles();
            JRogue.Editor.Interactables.TownTimeLeverAssetPackCreator.CreateTownTimeLeverAssets();
            JRogue.Editor.Interactables.MeditationShrineAssetPackCreator.CreateMeditationShrineAssets();

            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>(FloorTilePath);
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            PartyFormationSpawnProfile formation =
                AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(PartyFormationPath);

            DungeonLayoutStamp stamp = CreateTownPlazaStamp(
                $"{DataRoot}/Stamp_TownPlaza_20x20.asset",
                width: 20,
                height: 20,
                playerStart: new Vector3Int(10, 8, 0),
                dungeonPortalCell: new Vector3Int(10, 10, 0));

            const int demoBuildingOriginX = 12;
            const int demoBuildingOriginY = 8;
            const int demoBuildingWidth = 5;
            const int demoBuildingDepth = 3;
            const int demoDoorLocalX = 2;
            Vector3Int demoDoorCell = new Vector3Int(demoBuildingOriginX + demoDoorLocalX, demoBuildingOriginY, 0);
            DungeonLayoutStamp interiorStamp = CreateDemoInteriorStamp(
                $"{DataRoot}/Stamp_TownInteriorDemo_5x5.asset");

            EnsureDemoHostNpcAssets();

            DungeonFloorDefinition interiorFloor = CreatePeacefulFloorDefinition(
                $"{DataRoot}/Floor_town_interior_demo.asset",
                floorId: TownBuildingIds.DemoInteriorFloorId,
                interiorStamp,
                floorTile,
                wallTile,
                formation,
                portals: new[]
                {
                    new DungeonPortalSpec
                    {
                        portalLinkId = TownBuildingIds.DemoExitLink,
                        targetFloorId = TownPortalSetupPhase.TownFloorId,
                        portalMarkerId = StampMarkerIds.BuildingDemoExit,
                        listLabel = "Exit",
                    },
                },
                arrivals: new[]
                {
                    new PortalArrivalBinding
                    {
                        portalLinkId = TownBuildingIds.DemoEnterLink,
                        arrivalAnchor = new Vector3Int(2, 1, 0),
                    },
                });

            DungeonFloorDefinition townFloor = CreatePeacefulFloorDefinition(
                $"{DataRoot}/Floor_town_main.asset",
                floorId: "town_main",
                stamp,
                floorTile,
                wallTile,
                formation,
                portals: new[]
                {
                    new DungeonPortalSpec
                    {
                        portalLinkId = TownBuildingIds.DemoEnterLink,
                        targetFloorId = TownBuildingIds.DemoInteriorFloorId,
                        portalMarkerId = StampMarkerIds.BuildingDemoDoor,
                        listLabel = "Demo building",
                    },
                },
                arrivals: new[]
                {
                    new PortalArrivalBinding
                    {
                        portalLinkId = TownBuildingIds.DemoExitLink,
                        arrivalAnchor = demoDoorCell,
                    },
                });

            EnsureDemoBuildingFacadeOverlay(
                demoBuildingOriginX,
                demoBuildingOriginY,
                demoBuildingWidth,
                demoBuildingDepth,
                demoDoorLocalX);

            var catalog = LoadOrCreate<DungeonFloorDefinitionCatalog>($"{DataRoot}/TownCatalog.asset");
            SerializedObject catalogSo = new SerializedObject(catalog);
            catalogSo.FindProperty("floors").arraySize = 2;
            catalogSo.FindProperty("floors").GetArrayElementAtIndex(0).objectReferenceValue = townFloor;
            catalogSo.FindProperty("floors").GetArrayElementAtIndex(1).objectReferenceValue = interiorFloor;
            catalogSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[Town] Data at {DataRoot}. Floors: town_main + {TownBuildingIds.DemoInteriorFloorId}. " +
                $"Demo building (stone house + red roof) east of NPCs, door at ({demoDoorCell.x},{demoDoorCell.y}). " +
                "Run Fix TownTest Scene before Play.");
        }

        static void EnsureTownSpriteSheetImported()
        {
            if (!File.Exists(SpriteSheetPath))
            {
                Debug.LogError($"[Town] Missing sprite sheet at {SpriteSheetPath}. Re-run asset download.");
                return;
            }

            var importer = AssetImporter.GetAtPath(SpriteSheetPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = TilePixelSize;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;

            ApplyTownSpriteSheetSlices(importer);
            importer.SaveAndReimport();
        }

        static void ApplyTownSpriteSheetSlices(TextureImporter importer)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null)
            {
                Debug.LogWarning($"[Town] No sprite data provider for {SpriteSheetPath}.");
                return;
            }

            dataProvider.InitSpriteEditorDataProvider();

            var existingByName = new Dictionary<string, GUID>();
            foreach (SpriteRect existing in dataProvider.GetSpriteRects())
            {
                if (!string.IsNullOrEmpty(existing.name))
                    existingByName[existing.name] = existing.spriteID;
            }

            var spriteRects = new List<SpriteRect>(SheetColumns * SheetRows);
            var nameFileIdPairs = new List<SpriteNameFileIdPair>(SheetColumns * SheetRows);
            ISpriteNameFileIdDataProvider nameIdProvider = dataProvider.HasDataProvider(typeof(ISpriteNameFileIdDataProvider))
                ? dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                : null;

            for (int row = 0; row < SheetRows; row++)
            {
                for (int col = 0; col < SheetColumns; col++)
                {
                    int textureRow = SheetRows - 1 - row;
                    string spriteName = SpriteName(col, row);
                    var spriteRect = new SpriteRect
                    {
                        name = spriteName,
                        rect = new Rect(col * TilePixelSize, textureRow * TilePixelSize, TilePixelSize, TilePixelSize),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        spriteID = existingByName.TryGetValue(spriteName, out GUID existingId)
                            ? existingId
                            : GUID.Generate(),
                    };
                    spriteRects.Add(spriteRect);
                    if (nameIdProvider != null)
                        nameFileIdPairs.Add(new SpriteNameFileIdPair(spriteName, spriteRect.spriteID));
                }
            }

            dataProvider.SetSpriteRects(spriteRects.ToArray());
            if (nameIdProvider != null)
                nameIdProvider.SetNameFileIdPairs(nameFileIdPairs);

            dataProvider.Apply();
        }

        static void EnsureTownTiles()
        {
            EnsureTileAsset(FloorTilePath, SpriteName(FloorSpriteCol, FloorSpriteRow));
            EnsureTileAsset(WallTilePath, SpriteName(WallSpriteCol, WallSpriteRow));
        }

        static void EnsureTownBuildingTiles()
        {
            EnsureTileAsset(BuildingStoneCornerTilePath, SpriteName(BuildingStoneCornerCol, BuildingStoneCornerRow));
            EnsureTileAsset(BuildingStoneWallTilePath, SpriteName(BuildingStoneWallCol, BuildingStoneWallRow));
            EnsureTileAsset(BuildingStoneWindowTilePath, SpriteName(BuildingStoneWindowCol, BuildingStoneWindowRow));
            EnsureTileAsset(BuildingRoofLeftTilePath, SpriteName(BuildingRoofLeftCol, BuildingRoofLeftRow));
            EnsureTileAsset(BuildingRoofTilePath, SpriteName(BuildingRoofCol, BuildingRoofRow));
            EnsureTileAsset(BuildingRoofRightTilePath, SpriteName(BuildingRoofRightCol, BuildingRoofRightRow));
            EnsureTileAsset(BuildingDoorTilePath, SpriteName(BuildingDoorCol, BuildingDoorRow));
        }

        static string SpriteName(int col, int row) => $"Town_{col}_{row}";

        static void EnsureTileAsset(string assetPath, string spriteName)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (existing != null)
                return;

            Sprite sprite = FindSprite(SpriteSheetPath, spriteName);
            if (sprite == null)
            {
                Debug.LogWarning($"[Town] Sprite '{spriteName}' not found on {SpriteSheetPath}.");
                return;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            AssetDatabase.CreateAsset(tile, assetPath);
        }

        static Sprite FindSprite(string texturePath, string spriteName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name == spriteName)
                    return sprite;
            }

            return null;
        }

        static DungeonLayoutStamp CreateTownPlazaStamp(
            string path,
            int width,
            int height,
            Vector3Int playerStart,
            Vector3Int dungeonPortalCell)
        {
            var stamp = LoadOrCreate<DungeonLayoutStamp>(path);
            stamp.InitializeGrid(width, height, borderWalls: true);
            stamp.SetMarker(StampMarkerIds.PlayerStart, playerStart);
            stamp.SetMarker(StampMarkerIds.TownDungeonPortal, dungeonPortalCell);
            stamp.SetMarker(StampMarkerIds.TownNpc1, new Vector3Int(4, 8, 0));
            stamp.SetMarker(StampMarkerIds.TownNpc2, new Vector3Int(6, 8, 0));
            stamp.SetMarker(StampMarkerIds.TownNpc3, new Vector3Int(8, 8, 0));
            stamp.SetMarker(StampMarkerIds.TownTimeLeverA, new Vector3Int(8, 6, 0));
            stamp.SetMarker(StampMarkerIds.TownTimeLeverB, new Vector3Int(9, 6, 0));
            stamp.SetMarker(StampMarkerIds.MeditationShrine, new Vector3Int(4, 5, 0));
            stamp.SetMarker(StampMarkerIds.SoulBeastRitualCircle, new Vector3Int(14, 5, 0));
            stamp.SetMarker(StampMarkerIds.BeastBloodMerchant, new Vector3Int(2, 5, 0));
            stamp.SetMarker(StampMarkerIds.ShamanBarbarian, new Vector3Int(10, 5, 0));
            stamp.SetMarker(StampMarkerIds.FairyMerchant, new Vector3Int(12, 5, 0));
            stamp.SetMarker(StampMarkerIds.FleshmetalForgemaster, new Vector3Int(8, 5, 0));
            PaintDemoBuildingFacade(stamp, originX: 12, originY: 8, width: 5, depth: 3, doorLocalX: 2, doorLocalY: 0);
            EditorUtility.SetDirty(stamp);
            return stamp;
        }

        static DungeonLayoutStamp CreateDemoInteriorStamp(string path)
        {
            var stamp = LoadOrCreate<DungeonLayoutStamp>(path);
            stamp.InitializeGrid(5, 5, borderWalls: true);
            stamp.SetMarker(StampMarkerIds.BuildingDemoArrival, new Vector3Int(2, 1, 0));
            stamp.SetMarker(StampMarkerIds.BuildingDemoExit, new Vector3Int(2, 3, 0));
            stamp.SetMarker(StampMarkerIds.BuildingDemoNpc, new Vector3Int(2, 2, 0));
            EditorUtility.SetDirty(stamp);
            return stamp;
        }

        static void PaintDemoBuildingFacade(
            DungeonLayoutStamp stamp,
            int originX,
            int originY,
            int width,
            int depth,
            int doorLocalX,
            int doorLocalY)
        {
            for (int dy = 0; dy < depth; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int x = originX + dx;
                    int y = originY + dy;
                    bool isDoor = dx == doorLocalX && dy == doorLocalY;
                    if (isDoor)
                        stamp.SetCell(x, y, floor: true, wall: false);
                    else
                        stamp.SetCell(x, y, floor: false, wall: true);
                }
            }

            stamp.SetMarker(
                StampMarkerIds.BuildingDemoDoor,
                new Vector3Int(originX + doorLocalX, originY + doorLocalY, 0));
        }

        static void EnsureDemoBuildingFacadeOverlay(
            int originX,
            int originY,
            int width,
            int depth,
            int doorLocalX)
        {
            TileBase stoneCorner = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingStoneCornerTilePath);
            TileBase stoneWall = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingStoneWallTilePath);
            TileBase stoneWindow = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingStoneWindowTilePath);
            TileBase roofLeft = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingRoofLeftTilePath);
            TileBase roof = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingRoofTilePath);
            TileBase roofRight = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingRoofRightTilePath);
            TileBase door = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingDoorTilePath);
            if (stoneCorner == null || stoneWall == null || stoneWindow == null
                || roofLeft == null || roof == null || roofRight == null || door == null)
            {
                Debug.LogWarning("[Town] Building facade tiles missing — re-run sprite import.");
                return;
            }

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

                    TileBase wallTile = ResolveFacadeWallTile(
                        dx,
                        dy,
                        width,
                        depth,
                        stoneCorner,
                        stoneWall,
                        stoneWindow,
                        roofLeft,
                        roof,
                        roofRight);
                    cells.Add(new TownFacadePaintCell
                    {
                        cell = cell,
                        tile = wallTile,
                        layer = TownFacadePaintLayer.Wall,
                    });
                }
            }

            TownBuildingFacadeOverlay overlay = LoadOrCreate<TownBuildingFacadeOverlay>(FacadeOverlayPath);
            overlay.Configure("town_main", cells.ToArray());
            EditorUtility.SetDirty(overlay);
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

        static void EnsureDemoHostNpcAssets()
        {
            EnsureFolder(ResourcesDialogProfilesFolder);
            EnsureFolder(ResourcesNpcFolder);

            NpcDialogProfile profile = LoadOrCreate<NpcDialogProfile>(
                $"{ResourcesDialogProfilesFolder}/NpcDialog_DemoHost.asset");
            profile.npcId = TownNpcIds.DemoHost;
            profile.rootNodeIndex = 0;
            profile.nodes = new[]
            {
                new DialogNodeData
                {
                    kind = DialogNodeKind.Line,
                    line = new DialogLineData { textTemplate = "Hello." },
                },
            };
            EditorUtility.SetDirty(profile);

            PortraitDefinition portrait =
                AssetDatabase.LoadAssetAtPath<PortraitDefinition>(DemoHostPortraitPath);
            GameObject humanNpc = AssetDatabase.LoadAssetAtPath<GameObject>(HumanNpcPrefabPath);
            if (humanNpc == null)
            {
                Debug.LogWarning(
                    $"[Town] Missing {HumanNpcPrefabPath}. Run JRogue/Town/Create NPC Dialog Pack first.");
                return;
            }

            string prefabPath = $"{ResourcesNpcFolder}/TownNpc_DemoHost.prefab";
            GameObject instance = PrefabUtility.InstantiatePrefab(humanNpc) as GameObject;
            instance.name = "TownNpc_DemoHost";

            NpcController npc = instance.GetComponent<NpcController>();
            SerializedObject npcSo = new SerializedObject(npc);
            npcSo.FindProperty("npcId").stringValue = TownNpcIds.DemoHost;
            npcSo.FindProperty("dialogProfile").objectReferenceValue = profile;
            npcSo.FindProperty("portrait").objectReferenceValue = portrait;
            npcSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject actorSo = new SerializedObject(npc);
            actorSo.FindProperty("displayName").stringValue = "Host";
            actorSo.ApplyModifiedPropertiesWithoutUndo();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DemoHostSpritePath);
            SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
            if (renderer != null && sprite != null)
                renderer.sprite = sprite;

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
        }

        static DungeonFloorDefinition CreatePeacefulFloorDefinition(
            string path,
            string floorId,
            DungeonLayoutStamp stamp,
            TileBase floorTile,
            TileBase wallTile,
            PartyFormationSpawnProfile formation,
            DungeonPortalSpec[] portals = null,
            PortalArrivalBinding[] arrivals = null)
        {
            var def = LoadOrCreate<DungeonFloorDefinition>(path);
            SerializedObject so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = floorId;
            so.FindProperty("layoutStamp").objectReferenceValue = stamp;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("playerSafeRadius").intValue = 8;
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("enemyPopulation").arraySize = 0;
            so.FindProperty("hazardPopulation").arraySize = 0;
            so.FindProperty("trapPopulation").arraySize = 0;
            so.FindProperty("interactablePopulation").arraySize = 0;
            so.FindProperty("floorItemPopulation").arraySize = 0;
            WritePortalSpecs(so.FindProperty("portals"), portals);
            so.FindProperty("edgePortals").arraySize = 0;
            WriteArrivalBindings(so.FindProperty("arrivalBindings"), arrivals);
            so.FindProperty("orthogonalEdgePortalCount").intValue = 0;
            so.FindProperty("orthogonalEdgeInset").intValue = 2;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("doorPolicy").enumValueIndex = (int)DungeonDoorPolicy.None;
            so.FindProperty("vaults").arraySize = 0;
            so.FindProperty("vaultCatalog").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static void WritePortalSpecs(SerializedProperty portalsProp, DungeonPortalSpec[] portals)
        {
            portals ??= System.Array.Empty<DungeonPortalSpec>();
            portalsProp.arraySize = portals.Length;
            for (int i = 0; i < portals.Length; i++)
            {
                SerializedProperty element = portalsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("portalLinkId").stringValue = portals[i].portalLinkId;
                element.FindPropertyRelative("targetFloorId").stringValue = portals[i].targetFloorId;
                element.FindPropertyRelative("portalMarkerId").stringValue = portals[i].portalMarkerId;
                element.FindPropertyRelative("portalCell").vector3IntValue = portals[i].portalCell;
                element.FindPropertyRelative("listLabel").stringValue = portals[i].listLabel;
                element.FindPropertyRelative("adjacentConfirmOnly").boolValue = portals[i].adjacentConfirmOnly;
            }
        }

        static void WriteArrivalBindings(SerializedProperty arrivalsProp, PortalArrivalBinding[] arrivals)
        {
            arrivals ??= System.Array.Empty<PortalArrivalBinding>();
            arrivalsProp.arraySize = arrivals.Length;
            for (int i = 0; i < arrivals.Length; i++)
            {
                SerializedProperty element = arrivalsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("portalLinkId").stringValue = arrivals[i].portalLinkId;
                element.FindPropertyRelative("arrivalAnchor").vector3IntValue = arrivals[i].arrivalAnchor;
            }
        }

        static void CreateOrUpdateTownScene()
        {
            CreateTownTestDataInternal();

            if (!File.Exists(DungeonTestScenePath))
            {
                Debug.LogError($"[Town] Template missing: {DungeonTestScenePath}. Run JRogue → Dungeon → Create DungeonFloorTest Scene first.");
                return;
            }

            if (!File.Exists(ScenePath))
            {
                AssetDatabase.CopyAsset(DungeonTestScenePath, ScenePath);
                AssetDatabase.Refresh();
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            FixTownSceneHierarchyInPlace();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Town] Saved {ScenePath}. Press Play for 20×20 town plaza with party movement.");
        }

        static void FixTownSceneHierarchyInPlace()
        {
            GameObject systems = GameObject.Find(DungeonFloorTestSceneValidator.SystemsObjectName);
            if (systems == null)
            {
                Debug.LogError("[Town] DungeonTestSystems missing — recreate scene from menu.");
                return;
            }

            EnsureLightingOnSystems(systems);
            DungeonWorldFeatureServices.EnsureOn(systems);
            if (systems.GetComponent<DoorService>() == null)
                systems.AddComponent<DoorService>();
            if (systems.GetComponent<VisibilityManager>() == null)
                systems.AddComponent<VisibilityManager>();
            if (systems.GetComponent<PortalEntryService>() == null)
                systems.AddComponent<PortalEntryService>();

            DungeonFloorDefinition townDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>($"{DataRoot}/Floor_town_main.asset");
            DungeonFloorDefinition interiorDef =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>($"{DataRoot}/Floor_town_interior_demo.asset");
            DungeonFloorDefinitionCatalog townCatalog =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinitionCatalog>($"{DataRoot}/TownCatalog.asset");

            DungeonFloorInstanceManager floorManager = systems.GetComponent<DungeonFloorInstanceManager>();
            if (floorManager == null)
                floorManager = systems.AddComponent<DungeonFloorInstanceManager>();

            SerializedObject managerSo = new SerializedObject(floorManager);
            managerSo.FindProperty("useDontDestroyOnLoad").boolValue = false;
            managerSo.FindProperty("floorDefinitions").arraySize = 2;
            managerSo.FindProperty("floorDefinitions").GetArrayElementAtIndex(0).objectReferenceValue = townDef;
            managerSo.FindProperty("floorDefinitions").GetArrayElementAtIndex(1).objectReferenceValue = interiorDef;
            Transform floorsRoot = EnsureFloorsRoot(systems, floorManager);

            RemoveChildFloorsExcept(floorsRoot, "town_main", TownBuildingIds.DemoInteriorFloorId);
            EnsureFloorScaffold(floorsRoot, "town_main", townDef);
            EnsureFloorScaffold(floorsRoot, TownBuildingIds.DemoInteriorFloorId, interiorDef);
            managerSo.ApplyModifiedPropertiesWithoutUndo();

            if (systems.GetComponent<DungeonFloorTestController>() == null)
                systems.AddComponent<DungeonFloorTestController>();

            DungeonFloorTestController test = systems.GetComponent<DungeonFloorTestController>();
            SerializedObject testSo = new SerializedObject(test);
            testSo.FindProperty("floorInstanceManager").objectReferenceValue = floorManager;
            testSo.FindProperty("floorCatalog").objectReferenceValue = townCatalog;
            testSo.FindProperty("startFloorId").stringValue = "town_main";
            testSo.FindProperty("runSeed").intValue = 100001;
            testSo.FindProperty("autoGenerateOnPlay").boolValue = true;
            testSo.FindProperty("validateSceneOnPlay").boolValue = true;
            testSo.FindProperty("tryRepairSceneAtRuntime").boolValue = true;
            testSo.FindProperty("showGenerateButton").boolValue = true;

            GameObject party = GameObject.Find(DungeonFloorTestSceneValidator.PartyObjectName);
            DungeonRunBootstrap bootstrap = party != null ? party.GetComponent<DungeonRunBootstrap>() : null;
            if (bootstrap != null)
                testSo.FindProperty("runBootstrap").objectReferenceValue = bootstrap;
            testSo.ApplyModifiedPropertiesWithoutUndo();

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                if (cam.orthographicSize < 10f)
                    cam.orthographicSize = 14f;
                if (cam.GetComponent<CameraFollow>() == null)
                    cam.gameObject.AddComponent<CameraFollow>();
            }

            EnsureGameplayUiFromSampleScene(EditorSceneManager.GetActiveScene());
        }

        static void RemoveChildFloorsExcept(Transform floorsRoot, params string[] keepFloorIds)
        {
            if (floorsRoot == null || keepFloorIds == null || keepFloorIds.Length == 0)
                return;

            var keep = new HashSet<string>(keepFloorIds);
            for (int i = floorsRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = floorsRoot.GetChild(i);
                if (keep.Contains(child.name))
                    continue;

                Object.DestroyImmediate(child.gameObject);
            }
        }

        static void EnsureLightingOnSystems(GameObject systems)
        {
            LightingService service = systems.GetComponent<LightingService>() ?? systems.AddComponent<LightingService>();
            SerializedObject serviceSo = new SerializedObject(service);
            serviceSo.FindProperty("defaultFloorAmbientRegionId").intValue = 0;
            serviceSo.FindProperty("defaultFloorAmbientLight").intValue = LightLevel.FullDaylightAmbient;
            serviceSo.FindProperty("verboseReceiveLogs").boolValue = false;
            serviceSo.ApplyModifiedPropertiesWithoutUndo();

            LightingBootstrap bootstrap = systems.GetComponent<LightingBootstrap>() ?? systems.AddComponent<LightingBootstrap>();
            SerializedObject bootstrapSo = new SerializedObject(bootstrap);
            SerializedProperty regions = bootstrapSo.FindProperty("ambientRegions");
            regions.arraySize = 1;
            SerializedProperty region = regions.GetArrayElementAtIndex(0);
            region.FindPropertyRelative("regionId").intValue = 0;
            region.FindPropertyRelative("currentAmbientLight").intValue = LightLevel.FullDaylightAmbient;
            region.FindPropertyRelative("cycleLengthTurns").intValue = 0;
            region.FindPropertyRelative("phases").arraySize = 0;
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();
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

        static Transform EnsureFloorsRoot(GameObject systems, DungeonFloorInstanceManager floorManager)
        {
            Transform floors = systems.transform.Find("Floors");
            if (floors == null)
            {
                var floorsGo = new GameObject("Floors");
                floorsGo.transform.SetParent(systems.transform, false);
                floors = floorsGo.transform;
            }

            SerializedObject managerSo = new SerializedObject(floorManager);
            managerSo.FindProperty("floorsRoot").objectReferenceValue = floors;
            managerSo.ApplyModifiedPropertiesWithoutUndo();
            return floors;
        }

        static void EnsureFloorScaffold(Transform floorsRoot, string floorId, DungeonFloorDefinition definition)
        {
            if (floorsRoot == null || string.IsNullOrEmpty(floorId))
                return;

            Transform existing = floorsRoot.Find(floorId);
            GameObject floorGo = existing != null ? existing.gameObject : new GameObject(floorId);
            if (existing == null)
                floorGo.transform.SetParent(floorsRoot, false);

            DungeonFloorInstance instance = floorGo.GetComponent<DungeonFloorInstance>();
            if (instance == null)
                instance = floorGo.AddComponent<DungeonFloorInstance>();

            if (definition != null)
            {
                instance.Configure(definition);
                instance.EnsureHierarchyBuilt();
            }

            floorGo.SetActive(false);
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
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
