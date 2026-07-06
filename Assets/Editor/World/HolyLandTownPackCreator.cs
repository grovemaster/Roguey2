#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.Controller.Npc;
using JRogue.Dialog;
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
    /// <summary>Barbarian Holy Land — nexus decagon, dirt camp, shaman tent interior.</summary>
    public static class HolyLandTownPackCreator
    {
        const string WallTilePath = "Assets/TileMaps/Town/Town_WallBuilding.asset";
        const string BuildingWallTilePath = "Assets/TileMaps/Town/Town_Building_StoneWall.asset";
        const string BuildingDoorTilePath = "Assets/TileMaps/Town/Town_Building_Door.asset";
        const string DcssTileFolder = "Assets/TileMaps/Town/Dcss";
        const string DirtTileFolder = "Assets/TileMaps/Dcss/Cavern";
        const string BarbarianNpcPrefabPath = "Assets/Prefabs/Actor/Npc/BarbarianNpc.prefab";
        const string ChiefDialogPath = "Assets/Resources/Dialog/Profiles/NpcDialog_ChiefBarbarian.asset";
        const string ChiefPrefabPath = "Assets/Resources/Town/Npc/TownNpc_ChiefBarbarian.prefab";
        const string ChiefPortraitPath = "Assets/Resources/Dialog/Portraits/Portrait_Race_Barbarian.asset";
        const string ChiefSpritePath = "Assets/Art/NPC/Sprites/NPC_ShamanBarbarian.png";
        const string FormationPath = "Assets/Resources/Dungeon/PartyFormation_Default.asset";
        const string LegacySquareDefPath = "Assets/Resources/Town/Floor_dimension_square.asset";
        const string LegacyCatalogPath = "Assets/Resources/Town/DimensionSquareCatalog.asset";

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

        [MenuItem("JRogue/Town/Setup Barbarian Holy Land")]
        public static void SetupHolyLand()
        {
            EnsureFolders();
            DcssRectGrayFloorVarietyEditor.ConfigureRectGrayFloorTiles();
            EnsureChiefDialogAndPrefab();
            DungeonFloorDefinition nexusDef = EnsureNexusFloorDefinition();
            DungeonFloorDefinition holyLandDef = EnsureHolyLandFloorDefinition();
            DungeonFloorDefinition tentDef = EnsureTentInteriorFloorDefinition();
            ApplyDimensionSquareHolyLandPortalLinks(TownDistrictTestPaths.DimensionSquareFloorDef);
            ApplyDimensionSquareHolyLandPortalLinks(LegacySquareDefPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HolyLand] Data ready. Run JRogue → Town → Fix Dimension Square Test Scene or Fix District Town Test Scene.");
        }

        [MenuItem("JRogue/Town/Create / Update Holy Land Floors")]
        public static void CreateOrUpdateHolyLandFloors()
        {
            SetupHolyLand();
            DimensionSquareSceneCreator.FixDimensionSquareTestScene();
        }

        public static void IntegrateNexusScene(DungeonFloorInstance instance) => PaintNexusLayout(instance);
        public static void IntegrateHolyLandScene(DungeonFloorInstance instance) => PaintHolyLandLayout(instance);
        public static void IntegrateTentInteriorScene(DungeonFloorInstance instance) => PaintTentInteriorLayout(instance);

        public static void AppendToDimensionSquareCatalog(
            List<DungeonFloorDefinition> hubFloors,
            DungeonFloorDefinition nexusDef,
            DungeonFloorDefinition holyLandDef,
            DungeonFloorDefinition tentDef)
        {
            AppendIfMissing(hubFloors, nexusDef);
            AppendIfMissing(hubFloors, holyLandDef);
            AppendIfMissing(hubFloors, tentDef);
        }

        static void AppendIfMissing(List<DungeonFloorDefinition> floors, DungeonFloorDefinition def)
        {
            AppendIfMissingPublic(floors, def);
        }

        public static void AppendIfMissingPublic(List<DungeonFloorDefinition> floors, DungeonFloorDefinition def)
        {
            if (def == null)
                return;

            for (int i = 0; i < floors.Count; i++)
            {
                if (floors[i] != null && floors[i].FloorId == def.FloorId)
                    return;
            }

            floors.Add(def);
        }

        static void EnsureFolders()
        {
            EnsureFolder(TownDistrictTestPaths.HolyLandFolder);
            EnsureFolder("Assets/Resources/Town/Npc");
            EnsureFolder("Assets/Resources/Dialog/Profiles");
        }

        static void EnsureChiefDialogAndPrefab()
        {
            var dialog = LoadOrCreate<NpcDialogProfile>(ChiefDialogPath);
            var dialogSo = new SerializedObject(dialog);
            dialogSo.FindProperty("npcId").stringValue = BarbarianHolyLandLayout.ChiefMarkerId;
            dialogSo.FindProperty("rootNodeIndex").intValue = 0;
            SerializedProperty nodes = dialogSo.FindProperty("nodes");
            nodes.arraySize = 1;
            SerializedProperty node = nodes.GetArrayElementAtIndex(0);
            node.FindPropertyRelative("kind").enumValueIndex = 0;
            node.FindPropertyRelative("line").FindPropertyRelative("textTemplate").stringValue =
                "Welcome, child of the wild. The camp honors your strength.";
            node.FindPropertyRelative("nextNodeIndex").intValue = -1;
            dialogSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dialog);

            GameObject barbarianNpc = AssetDatabase.LoadAssetAtPath<GameObject>(BarbarianNpcPrefabPath);
            if (barbarianNpc == null)
                return;

            bool createdNew = !File.Exists(ChiefPrefabPath);
            GameObject instance = createdNew
                ? (GameObject)PrefabUtility.InstantiatePrefab(barbarianNpc)
                : PrefabUtility.LoadPrefabContents(ChiefPrefabPath);

            try
            {
                instance.name = "TownNpc_ChiefBarbarian";
                NpcController npc = instance.GetComponent<NpcController>() ?? instance.AddComponent<NpcController>();
                var npcSo = new SerializedObject(npc);
                npcSo.FindProperty("npcId").stringValue = BarbarianHolyLandLayout.ChiefMarkerId;
                npcSo.FindProperty("displayName").stringValue = "Chief";
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

                if (createdNew)
                    PrefabUtility.SaveAsPrefabAsset(instance, ChiefPrefabPath);
                else
                    PrefabUtility.SaveAsPrefabAsset(instance, ChiefPrefabPath);
            }
            finally
            {
                if (!createdNew)
                    PrefabUtility.UnloadPrefabContents(instance);
            }
        }

        static DungeonFloorDefinition EnsureNexusFloorDefinition()
        {
            EnsureNexusPalettes();
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{DcssTileFolder}/{StoneFloorTileAssets[0]}");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            PartyFormationSpawnProfile formation = AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(FormationPath);

            var def = LoadOrCreate<DungeonFloorDefinition>(TownDistrictTestPaths.HolyLandNexusFloorDef);
            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = HolyLandFloorIds.Nexus;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("defaultFloorPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.HolyLandNexusFloorPalette);
            so.FindProperty("defaultWallPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.HolyLandWallPalette);
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;

            int stripWidth = DistrictSquareHolyNexusTransition.StripMaxX - DistrictSquareHolyNexusTransition.StripMinX + 1;
            SerializedProperty portals = so.FindProperty("portals");
            portals.arraySize = stripWidth + 3;
            WriteStripPortals(
                portals,
                0,
                HolyLandTransitionIds.NexusToSquare,
                DimensionSquareFloorIds.FloorId,
                DistrictSquareHolyNexusTransition.NexusNorthEdgeY,
                "Dimension Square");
            SerializedProperty holyGate = portals.GetArrayElementAtIndex(stripWidth);
            holyGate.FindPropertyRelative("portalLinkId").stringValue = HolyLandTransitionIds.NexusToHolyLand;
            holyGate.FindPropertyRelative("targetFloorId").stringValue = HolyLandFloorIds.HolyLandProper;
            holyGate.FindPropertyRelative("portalCell").vector3IntValue = HolyLandNexusLayout.HolyLandGateCell;
            holyGate.FindPropertyRelative("listLabel").stringValue = "Barbarian Holy Land";
            SerializedProperty elfGate = portals.GetArrayElementAtIndex(stripWidth + 1);
            elfGate.FindPropertyRelative("portalLinkId").stringValue = HolyLandTransitionIds.NexusToElfHolyLand;
            elfGate.FindPropertyRelative("targetFloorId").stringValue = HolyLandFloorIds.ElfHolyLandProper;
            elfGate.FindPropertyRelative("portalCell").vector3IntValue = HolyLandNexusLayout.ElfHolyLandGateCell;
            elfGate.FindPropertyRelative("listLabel").stringValue = "Elf Holy Land";
            SerializedProperty beastmanGate = portals.GetArrayElementAtIndex(stripWidth + 2);
            beastmanGate.FindPropertyRelative("portalLinkId").stringValue = HolyLandTransitionIds.NexusToBeastmanHolyLand;
            beastmanGate.FindPropertyRelative("targetFloorId").stringValue = HolyLandFloorIds.BeastmanHolyLandProper;
            beastmanGate.FindPropertyRelative("portalCell").vector3IntValue = HolyLandNexusLayout.BeastmanHolyLandGateCell;
            beastmanGate.FindPropertyRelative("listLabel").stringValue = "Beastman Holy Land";

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 4;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.SquareToNexus;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("arrivalAnchor").vector3IntValue =
                DistrictSquareHolyNexusTransition.NexusArrivalCell;
            arrivals.GetArrayElementAtIndex(1).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.HolyLandToNexus;
            arrivals.GetArrayElementAtIndex(1).FindPropertyRelative("arrivalAnchor").vector3IntValue =
                HolyLandNexusLayout.BarbarianHolyLandNexusArrivalCell;
            arrivals.GetArrayElementAtIndex(2).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.ElfHolyLandToNexus;
            arrivals.GetArrayElementAtIndex(2).FindPropertyRelative("arrivalAnchor").vector3IntValue =
                HolyLandNexusLayout.ElfHolyLandNexusArrivalCell;
            arrivals.GetArrayElementAtIndex(3).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.BeastmanHolyLandToNexus;
            arrivals.GetArrayElementAtIndex(3).FindPropertyRelative("arrivalAnchor").vector3IntValue =
                HolyLandNexusLayout.BeastmanHolyLandNexusArrivalCell;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static DungeonFloorDefinition EnsureHolyLandFloorDefinition()
        {
            EnsureHolyLandPalettes();
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{DirtTileFolder}/{DirtFloorTileAssets[0]}");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            PartyFormationSpawnProfile formation = AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(FormationPath);

            var def = LoadOrCreate<DungeonFloorDefinition>(TownDistrictTestPaths.HolyLandProperFloorDef);
            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = HolyLandFloorIds.HolyLandProper;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("defaultFloorPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.HolyLandProperFloorPalette);
            so.FindProperty("defaultWallPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.HolyLandWallPalette);
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;

            SerializedProperty portals = so.FindProperty("portals");
            portals.arraySize = 2;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.HolyLandToNexus;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("targetFloorId").stringValue = HolyLandFloorIds.Nexus;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("portalCell").vector3IntValue =
                BarbarianHolyLandLayout.ReturnToNexusCell;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("listLabel").stringValue = "Nexus";
            portals.GetArrayElementAtIndex(1).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.TentEnter;
            portals.GetArrayElementAtIndex(1).FindPropertyRelative("targetFloorId").stringValue =
                HolyLandFloorIds.ShamanTentInterior;
            portals.GetArrayElementAtIndex(1).FindPropertyRelative("portalCell").vector3IntValue =
                BarbarianHolyLandLayout.TentDoorCell;
            portals.GetArrayElementAtIndex(1).FindPropertyRelative("listLabel").stringValue = "Shaman Tent";

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 2;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.NexusToHolyLand;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("arrivalAnchor").vector3IntValue =
                HolyLandNexusLayout.HolyLandArrivalCell;
            arrivals.GetArrayElementAtIndex(1).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.TentExit;
            arrivals.GetArrayElementAtIndex(1).FindPropertyRelative("arrivalAnchor").vector3IntValue =
                BarbarianHolyLandLayout.TentDoorCell;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static DungeonFloorDefinition EnsureTentInteriorFloorDefinition()
        {
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{DcssTileFolder}/{StoneFloorTileAssets[0]}");
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            PartyFormationSpawnProfile formation = AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(FormationPath);

            var def = LoadOrCreate<DungeonFloorDefinition>(TownDistrictTestPaths.HolyLandTentInteriorFloorDef);
            var so = new SerializedObject(def);
            so.FindProperty("floorId").stringValue = HolyLandFloorIds.ShamanTentInterior;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ScenePainted;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("defaultFloorPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.HolyLandTentInteriorFloorPalette);
            so.FindProperty("defaultWallPalette").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(TownDistrictTestPaths.HolyLandWallPalette);
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("participatesInDungeonTime").boolValue = false;
            so.FindProperty("combatPolicy").enumValueIndex = (int)FloorCombatPolicy.SafeZone;

            SerializedProperty portals = so.FindProperty("portals");
            portals.arraySize = 1;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.TentExit;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("targetFloorId").stringValue =
                HolyLandFloorIds.HolyLandProper;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("portalCell").vector3IntValue =
                BarbarianShamanTentLayout.InteriorExitCell;
            portals.GetArrayElementAtIndex(0).FindPropertyRelative("listLabel").stringValue = "Holy Land";

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 1;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                HolyLandTransitionIds.TentEnter;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("arrivalAnchor").vector3IntValue =
                BarbarianShamanTentLayout.InteriorArrivalCell;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        public static void ApplyDimensionSquareHolyLandPortalLinks(string floorDefAssetPath)
        {
            var def = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(floorDefAssetPath);
            if (def == null)
                return;

            var so = new SerializedObject(def);
            SerializedProperty portals = so.FindProperty("portals");
            for (int i = 0; i < portals.arraySize; i++)
            {
                if (portals.GetArrayElementAtIndex(i).FindPropertyRelative("portalLinkId").stringValue
                    == HolyLandTransitionIds.SquareToNexus)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }

            int stripWidth = DistrictSquareHolyNexusTransition.StripMaxX - DistrictSquareHolyNexusTransition.StripMinX + 1;
            int existing = portals.arraySize;
            portals.arraySize = existing + stripWidth;

            WriteStripPortals(
                portals,
                existing,
                HolyLandTransitionIds.SquareToNexus,
                HolyLandFloorIds.Nexus,
                DistrictSquareHolyNexusTransition.SquareSouthEdgeY,
                "Holy Land Nexus");

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            int arrivalCount = arrivals.arraySize;
            bool hasHolyNexusArrival = false;
            for (int i = 0; i < arrivalCount; i++)
            {
                if (arrivals.GetArrayElementAtIndex(i).FindPropertyRelative("portalLinkId").stringValue
                    == HolyLandTransitionIds.NexusToSquare)
                {
                    hasHolyNexusArrival = true;
                    break;
                }
            }

            if (!hasHolyNexusArrival)
            {
                arrivals.arraySize = arrivalCount + 1;
                SerializedProperty arrival = arrivals.GetArrayElementAtIndex(arrivalCount);
                arrival.FindPropertyRelative("portalLinkId").stringValue = HolyLandTransitionIds.NexusToSquare;
                arrival.FindPropertyRelative("arrivalAnchor").vector3IntValue =
                    DistrictSquareHolyNexusTransition.SquareArrivalCell;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
        }

        static void WriteStripPortals(
            SerializedProperty portals,
            int startIndex,
            string linkId,
            string targetFloorId,
            int y,
            string label)
        {
            int stripWidth = DistrictSquareHolyNexusTransition.StripMaxX - DistrictSquareHolyNexusTransition.StripMinX + 1;
            for (int i = 0; i < stripWidth; i++)
            {
                int x = DistrictSquareHolyNexusTransition.StripMinX + i;
                SerializedProperty portal = portals.GetArrayElementAtIndex(startIndex + i);
                portal.FindPropertyRelative("portalLinkId").stringValue = linkId;
                portal.FindPropertyRelative("targetFloorId").stringValue = targetFloorId;
                portal.FindPropertyRelative("portalCell").vector3IntValue = new Vector3Int(x, y, 0);
                portal.FindPropertyRelative("listLabel").stringValue = label;
            }
        }

        static void EnsureNexusPalettes()
        {
            var floorTiles = new List<(string tilePath, int weight)>();
            for (int i = 0; i < StoneFloorTileAssets.Length; i++)
                floorTiles.Add(($"{DcssTileFolder}/{StoneFloorTileAssets[i]}", 5));

            DistrictTownTestSceneCreator.CreateOrUpdatePalette(
                TownDistrictTestPaths.HolyLandNexusFloorPalette,
                "holy_land_nexus_floor",
                DungeonTilePaletteLayer.Floor,
                floorTiles.ToArray());
            EnsureWallPalette();
        }

        static void EnsureHolyLandPalettes()
        {
            var floorTiles = new List<(string tilePath, int weight)>();
            for (int i = 0; i < DirtFloorTileAssets.Length; i++)
                floorTiles.Add(($"{DirtTileFolder}/{DirtFloorTileAssets[i]}", 5));

            DistrictTownTestSceneCreator.CreateOrUpdatePalette(
                TownDistrictTestPaths.HolyLandProperFloorPalette,
                "barbarian_holy_land_floor",
                DungeonTilePaletteLayer.Floor,
                floorTiles.ToArray());

            var tentFloorTiles = new List<(string tilePath, int weight)>();
            for (int i = 0; i < StoneFloorTileAssets.Length; i++)
                tentFloorTiles.Add(($"{DcssTileFolder}/{StoneFloorTileAssets[i]}", 5));

            DistrictTownTestSceneCreator.CreateOrUpdatePalette(
                TownDistrictTestPaths.HolyLandTentInteriorFloorPalette,
                "barbarian_shaman_tent_interior_floor",
                DungeonTilePaletteLayer.Floor,
                tentFloorTiles.ToArray());
            EnsureWallPalette();
        }

        static void EnsureWallPalette()
        {
            DistrictTownTestSceneCreator.CreateOrUpdatePalette(
                TownDistrictTestPaths.HolyLandWallPalette,
                "holy_land_wall",
                DungeonTilePaletteLayer.Wall,
                new[] { (WallTilePath, 5) });
        }

        static void PaintNexusLayout(DungeonFloorInstance instance)
        {
            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Tilemap wallMap = instance.Tilemaps.WallMap;
            TileBase[] floorTiles = LoadPaletteTiles(TownDistrictTestPaths.HolyLandNexusFloorPalette);
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            if (floorMap == null || wallMap == null || floorTiles == null || wallTile == null)
                return;

            Undo.RecordObject(floorMap, "Paint holy land nexus floor");
            Undo.RecordObject(wallMap, "Paint holy land nexus walls");
            HolyLandNexusLayout.Paint(floorMap, wallMap, floorTiles, wallTile);
            FinalizePaint(floorMap, wallMap);
            instance.MarkNeedsRegeneration();
            EnsureNexusMarkers(instance);
        }

        static void PaintHolyLandLayout(DungeonFloorInstance instance)
        {
            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Tilemap wallMap = instance.Tilemaps.WallMap;
            TileBase[] dirtTiles = LoadPaletteTiles(TownDistrictTestPaths.HolyLandProperFloorPalette);
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            TileBase buildingWall = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingWallTilePath);
            TileBase buildingDoor = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingDoorTilePath);
            if (floorMap == null || wallMap == null || dirtTiles == null || wallTile == null)
                return;

            Undo.RecordObject(floorMap, "Paint barbarian holy land floor");
            Undo.RecordObject(wallMap, "Paint barbarian holy land walls");
            BarbarianHolyLandLayout.Paint(floorMap, wallMap, dirtTiles, wallTile, buildingWall, buildingDoor);
            FinalizePaint(floorMap, wallMap);
            EnsureHolyLandMarkers(instance);
            instance.MarkNeedsRegeneration();
        }

        static void PaintTentInteriorLayout(DungeonFloorInstance instance)
        {
            Tilemap floorMap = instance.Tilemaps.FloorMap;
            Tilemap wallMap = instance.Tilemaps.WallMap;
            TileBase[] floorTiles = LoadPaletteTiles(TownDistrictTestPaths.HolyLandTentInteriorFloorPalette);
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(WallTilePath);
            TileBase doorTile = AssetDatabase.LoadAssetAtPath<TileBase>(BuildingDoorTilePath);
            if (floorMap == null || wallMap == null || floorTiles == null || wallTile == null)
                return;

            Undo.RecordObject(floorMap, "Paint shaman tent interior floor");
            Undo.RecordObject(wallMap, "Paint shaman tent interior walls");
            BarbarianShamanTentLayout.Paint(floorMap, wallMap, floorTiles, wallTile, doorTile);
            FinalizePaint(floorMap, wallMap);
            EnsureTentInteriorMarkers(instance);
        }

        static void EnsureNexusMarkers(DungeonFloorInstance instance) =>
            EnsureMarkers(instance, HolyLandNexusLayout.PlayerStartCell);

        static void EnsureHolyLandMarkers(DungeonFloorInstance instance)
        {
            Transform markersRoot = EnsureMarkersRoot(instance);
            ClearChildren(markersRoot);
            CreateMarker(markersRoot, instance, "PlayerStart", StaticHubMarkerKind.PlayerStart, BarbarianHolyLandLayout.PlayerStartCell);
            CreateMarker(markersRoot, instance, "Chief", StaticHubMarkerKind.NpcSlot, BarbarianHolyLandLayout.ChiefNpcCell, BarbarianHolyLandLayout.ChiefMarkerId);
        }

        static void EnsureTentInteriorMarkers(DungeonFloorInstance instance)
        {
            Transform markersRoot = EnsureMarkersRoot(instance);
            ClearChildren(markersRoot);
            CreateMarker(markersRoot, instance, "PlayerStart", StaticHubMarkerKind.PlayerStart, BarbarianShamanTentLayout.InteriorArrivalCell);
            CreateMarker(markersRoot, instance, "Shaman", StaticHubMarkerKind.NpcSlot, BarbarianShamanTentLayout.ShamanNpcCell, BarbarianShamanTentLayout.ShamanMarkerId);
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

        static void EnsureMarkers(DungeonFloorInstance instance, Vector3Int playerStart)
        {
            Transform markersRoot = EnsureMarkersRoot(instance);
            ClearChildren(markersRoot);
            CreateMarker(markersRoot, instance, "PlayerStart", StaticHubMarkerKind.PlayerStart, playerStart);
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
