#if UNITY_EDITOR
using System.IO;
using JRogue.Spawn;
using JRogue.World.Altar;
using JRogue.World.Generation;
using JRogue.World.Generation.Zones;
using JRogue.World.Rift;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    /// <summary>
    /// Creates Rift Test layout, floor, definition, pedestal altar, and wires Floor 1 policy.
    /// Menu: JRogue/Dungeon/Create Rift Test Pack
    /// </summary>
    public static class RiftTestPackCreator
    {
        const string MenuPath = "JRogue/Dungeon/Create Rift Test Pack";

        public const string DataRoot = "Assets/Data/Rifts";
        public const string LayoutRoot = DataRoot + "/Layouts";
        public const string AltarRoot = "Assets/Data/Altar";
        public const string FiltersRoot = AltarRoot + "/Filters";
        public const string EffectsRoot = AltarRoot + "/Effects";

        public const string StampPath = LayoutRoot + "/Stamp_RiftTest.asset";
        public const string FloorPath = "Assets/Resources/Dungeon/Floor_rift_test.asset";
        public const string RiftDefPath = DataRoot + "/Rift_Test.asset";
        public const string AltarPath = AltarRoot + "/Altar_RiftPedestalNorthernDark.asset";
        public const string OpenEffectPath = EffectsRoot + "/OpenRiftPortal_RiftTest.asset";

        public const string Floor01Path = DungeonFloor1ProductionPhase2PackCreator.FloorProdPath;
        public const string CatalogPath = DungeonFloor1ProductionPhase2PackCreator.CatalogProdPath;

        // Stamp geometry (border walls at 0 and max).
        public const int MapWidth = 22;
        public const int MapHeight = 72;

        // Room A 10×20 at x=6..15, y=1..20
        public static readonly Vector3Int EntryAnchor = new Vector3Int(10, 3, 0);
        public static readonly Vector3Int GhoulCell = new Vector3Int(9, 8, 0); // Room A local (3,7)

        // Room B 15×15 at x=3..17, y=26..40
        public static readonly Vector3Int RoomBMin = new Vector3Int(3, 26, 0);
        public static readonly Vector3Int RoomBMax = new Vector3Int(17, 40, 0);
        public static readonly Vector3Int GoblinBL = new Vector3Int(3, 26, 0);
        public static readonly Vector3Int GoblinBR = new Vector3Int(17, 26, 0);

        // Room C 20×20 at x=1..20, y=51..70
        public static readonly Vector3Int BossCell = new Vector3Int(10, 60, 0);
        public static readonly Vector3Int ExitPortalCell = new Vector3Int(10, 69, 0);

        [MenuItem(MenuPath, false, 58)]
        public static void CreateRiftTestPack()
        {
            EnsureFolder(DataRoot);
            EnsureFolder(LayoutRoot);
            EnsureFolder(FiltersRoot);
            EnsureFolder(EffectsRoot);

            ManaStoneSpeciesAcceptFilter goblinFilter = EnsureSpeciesFilter(
                $"{FiltersRoot}/ManaStoneSpecies_Goblin.asset", "goblin");
            ManaStoneSpeciesAcceptFilter ghoulFilter = EnsureSpeciesFilter(
                $"{FiltersRoot}/ManaStoneSpecies_Ghoul.asset", "ghoul");
            ManaStoneSpeciesAcceptFilter direWolfFilter = EnsureSpeciesFilter(
                $"{FiltersRoot}/ManaStoneSpecies_DireWolf.asset", "dire_wolf");

            DungeonLayoutStamp stamp = BuildStamp();
            DungeonFloorDefinition riftFloor = CreateRiftFloor(stamp);
            RiftDefinition rift = CreateRiftDefinition(riftFloor);

            OpenRiftPortalAltarCompletionEffect openEffect =
                LoadOrCreate<OpenRiftPortalAltarCompletionEffect>(OpenEffectPath);
            openEffect.rift = rift;
            EditorUtility.SetDirty(openEffect);

            AltarDefinition altar = CreatePedestalAltar(
                goblinFilter, ghoulFilter, direWolfFilter, openEffect);

            WireFloor01Policy(rift, altar);
            EnsureCatalogIncludesRiftFloor(riftFloor);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[Rift] Rift Test pack ready. Floor={FloorPath}, Def={RiftDefPath}, " +
                $"Altar={AltarPath}. Re-enter Play Mode / regenerate Floor 1 to place pedestal altar.");
        }

        public static void CreateRiftTestPackBatch()
        {
            CreateRiftTestPack();
            EditorApplication.Exit(0);
        }

        static DungeonLayoutStamp BuildStamp()
        {
            var stamp = LoadOrCreate<DungeonLayoutStamp>(StampPath);
            stamp.InitializeGrid(MapWidth, MapHeight, borderWalls: true);

            // Fill everything as wall first, then carve rooms/halls.
            for (int y = 1; y < MapHeight - 1; y++)
            {
                for (int x = 1; x < MapWidth - 1; x++)
                    stamp.SetCell(x, y, floor: false, wall: true);
            }

            CarveRect(stamp, 6, 1, 15, 20); // Room A
            CarveHall(stamp, 10, 21, 10, 25); // A→B hall length 5
            CarveRect(stamp, 3, 26, 17, 40); // Room B
            CarveHall(stamp, 10, 41, 10, 50); // B→C hall length 10
            CarveRect(stamp, 1, 51, 20, 70); // Room C

            stamp.SetMarker(StampMarkerIds.PlayerStart, EntryAnchor);
            EditorUtility.SetDirty(stamp);
            return stamp;
        }

        static void CarveRect(DungeonLayoutStamp stamp, int x0, int y0, int x1, int y1)
        {
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                    stamp.SetCell(x, y, floor: true, wall: false);
            }
        }

        static void CarveHall(DungeonLayoutStamp stamp, int x0, int y0, int x1, int y1)
        {
            CarveRect(stamp, x0, y0, x1, y1);
        }

        static DungeonFloorDefinition CreateRiftFloor(DungeonLayoutStamp stamp)
        {
            DungeonTilePalette floorPalette = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(
                DungeonFloor1ProductionPhase2PackCreator.PaletteDarkFloorPath);
            DungeonTilePalette wallPalette = AssetDatabase.LoadAssetAtPath<DungeonTilePalette>(
                DungeonFloor1ProductionPhase2PackCreator.PaletteDarkWallPath);
            PartyFormationSpawnProfile formation = AssetDatabase.LoadAssetAtPath<PartyFormationSpawnProfile>(
                "Assets/Resources/Dungeon/PartyFormation_Default.asset");

            var floor = LoadOrCreate<DungeonFloorDefinition>(FloorPath);
            SerializedObject so = new SerializedObject(floor);
            so.FindProperty("floorId").stringValue = RiftTransitionIds.RiftTestFloorId;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.PreBakedStamp;
            so.FindProperty("layoutStamp").objectReferenceValue = stamp;
            so.FindProperty("defaultFloorPalette").objectReferenceValue = floorPalette;
            so.FindProperty("defaultWallPalette").objectReferenceValue = wallPalette;
            so.FindProperty("floorTile").objectReferenceValue = LoadFirstPaletteTile(floorPalette);
            so.FindProperty("wallTile").objectReferenceValue = LoadFirstPaletteTile(wallPalette);
            so.FindProperty("formationProfile").objectReferenceValue = formation;
            so.FindProperty("playerSafeRadius").intValue = 2;
            so.FindProperty("participatesInDungeonTime").boolValue = true;
            so.FindProperty("baseDayNightCycles").intValue = 0;
            so.FindProperty("additionalDayNightCycles").intValue = 0;
            so.FindProperty("floorDayNightCycleLimit").intValue = 0;
            so.FindProperty("playerTurnsPerDay").intValue = 5;
            so.FindProperty("playerTurnsPerNight").intValue = 5;
            so.FindProperty("enemyPopulation").arraySize = 0;
            so.FindProperty("hazardPopulation").arraySize = 0;
            so.FindProperty("trapPopulation").arraySize = 0;
            so.FindProperty("interactablePopulation").arraySize = 0;
            so.FindProperty("floorItemPopulation").arraySize = 0;
            so.FindProperty("lordsOfTheFloor").arraySize = 0;
            so.FindProperty("portals").arraySize = 0;
            so.FindProperty("portalPlacementRules").arraySize = 0;
            so.FindProperty("monsterSpawnSchedule").objectReferenceValue = null;
            so.FindProperty("vaultCatalog").objectReferenceValue = null;

            SerializedProperty arrivals = so.FindProperty("arrivalBindings");
            arrivals.arraySize = 1;
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("portalLinkId").stringValue =
                RiftTransitionIds.HostToRift("rift_test");
            arrivals.GetArrayElementAtIndex(0).FindPropertyRelative("arrivalAnchor").vector3IntValue = EntryAnchor;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(floor);
            return floor;
        }

        static RiftDefinition CreateRiftDefinition(DungeonFloorDefinition riftFloor)
        {
            EnemySpawnDefinition goblin = AssetDatabase.LoadAssetAtPath<EnemySpawnDefinition>(
                "Assets/Data/Spawn/Production/Spawn_Goblin_Floor01.asset");
            EnemySpawnDefinition ghoul = AssetDatabase.LoadAssetAtPath<EnemySpawnDefinition>(
                "Assets/Data/Spawn/Production/Spawn_Ghoul_Floor01.asset");

            var rift = LoadOrCreate<RiftDefinition>(RiftDefPath);
            rift.riftId = "rift_test";
            rift.displayName = "Rift Test";
            rift.hostFloorIds = new[] { DungeonFloorTransitionIds.Floor01Id };
            rift.riftFloorDefinition = riftFloor;
            rift.entryAnchor = EntryAnchor;
            rift.exitPortalCell = ExitPortalCell;
            rift.initialSpawns = new[]
            {
                new RiftEnemySpawnSpec { spawnDefinition = ghoul, cell = GhoulCell, isBoss = false },
                new RiftEnemySpawnSpec { spawnDefinition = goblin, cell = BossCell, isBoss = true },
            };
            rift.conditionalSummons = new[]
            {
                new RiftConditionalSummonSpec
                {
                    conditionId = "room_b_first_entry",
                    roomMinInclusive = RoomBMin,
                    roomMaxInclusive = RoomBMax,
                    spawns = new[]
                    {
                        new RiftEnemySpawnSpec { spawnDefinition = goblin, cell = GoblinBL, isBoss = false },
                        new RiftEnemySpawnSpec { spawnDefinition = goblin, cell = GoblinBR, isBoss = false },
                    },
                },
            };
            EditorUtility.SetDirty(rift);
            return rift;
        }

        static AltarDefinition CreatePedestalAltar(
            ManaStoneSpeciesAcceptFilter goblin,
            ManaStoneSpeciesAcceptFilter ghoul,
            ManaStoneSpeciesAcceptFilter direWolf,
            OpenRiftPortalAltarCompletionEffect openEffect)
        {
            Sprite overlay = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Altars/Sprites/Altar_StoneShrine.png");

            var altar = LoadOrCreate<AltarDefinition>(AltarPath);
            altar.altarId = "altar_rift_pedestal_northern_dark";
            altar.displayName = "Pedestal";
            altar.descriptionTemplate =
                "There are 3 small indentations and 1 larger indentation. " +
                "The three small ones accept mana stones of goblin, ghoul, and dire wolf. " +
                "The larger indentation is sealed for now.";
            altar.usedDescriptionTemplate =
                "The pedestal's indentations are empty. Its power has opened a rift.";
            altar.overlaySprite = overlay;
            altar.blocksOccupancy = true;
            altar.pickerSortOrder = 0;
            altar.slots = new[]
            {
                new AltarSlotDefinition
                {
                    slotId = "goblin",
                    label = "Goblin mana stone",
                    acceptFilter = goblin,
                    maxCount = 1,
                },
                new AltarSlotDefinition
                {
                    slotId = "ghoul",
                    label = "Ghoul mana stone",
                    acceptFilter = ghoul,
                    maxCount = 1,
                },
                new AltarSlotDefinition
                {
                    slotId = "dire_wolf",
                    label = "Dire wolf mana stone",
                    acceptFilter = direWolf,
                    maxCount = 1,
                },
            };
            altar.completionRules = new[]
            {
                new AltarCompletionRule
                {
                    ruleId = "open_rift_portal",
                    requiredSlotIds = System.Array.Empty<string>(),
                    effects = new AltarCompletionEffect[] { openEffect },
                },
            };
            EditorUtility.SetDirty(altar);
            return altar;
        }

        static void WireFloor01Policy(RiftDefinition rift, AltarDefinition altar)
        {
            var floor01 = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(Floor01Path);
            if (floor01 == null)
            {
                Debug.LogWarning($"[Rift] Missing Floor 1 at {Floor01Path} — skip policy wire.");
                return;
            }

            SerializedObject so = new SerializedObject(floor01);
            SerializedProperty policy = so.FindProperty("riftPolicy");
            if (policy == null)
            {
                Debug.LogError("[Rift] Floor definition missing riftPolicy field.");
                return;
            }

            policy.FindPropertyRelative("maxRiftPortalsPerRun").intValue = 1;
            policy.FindPropertyRelative("minDungeonRunsBetweenPortals").intValue = 3;
            policy.FindPropertyRelative("minDungeonDayToOpenPortal").intValue = 2;
            policy.FindPropertyRelative("minDungeonRunsBeforeWandering").intValue = 5;
            policy.FindPropertyRelative("riftPortalOpenTurns").intValue = 30;
            policy.FindPropertyRelative("wanderingRespawnDelayTurns").intValue = 20;
            policy.FindPropertyRelative("riftPedestalAltar").objectReferenceValue = altar;

            SerializedProperty rifts = policy.FindPropertyRelative("rifts");
            rifts.arraySize = 1;
            rifts.GetArrayElementAtIndex(0).objectReferenceValue = rift;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(floor01);
        }

        static void EnsureCatalogIncludesRiftFloor(DungeonFloorDefinition riftFloor)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinitionCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogWarning($"[Rift] Missing catalog {CatalogPath}");
                return;
            }

            SerializedObject so = new SerializedObject(catalog);
            SerializedProperty floors = so.FindProperty("floors");
            for (int i = 0; i < floors.arraySize; i++)
            {
                if (floors.GetArrayElementAtIndex(i).objectReferenceValue == riftFloor)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }

            int idx = floors.arraySize;
            floors.arraySize = idx + 1;
            floors.GetArrayElementAtIndex(idx).objectReferenceValue = riftFloor;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        static ManaStoneSpeciesAcceptFilter EnsureSpeciesFilter(string path, string speciesId)
        {
            var filter = LoadOrCreate<ManaStoneSpeciesAcceptFilter>(path);
            filter.requiredSpeciesId = speciesId;
            filter.requiredTierOrZero = 0;
            EditorUtility.SetDirty(filter);
            return filter;
        }

        static TileBase LoadFirstPaletteTile(DungeonTilePalette palette)
        {
            if (palette == null)
                return null;
            SerializedObject so = new SerializedObject(palette);
            SerializedProperty entries = so.FindProperty("entries");
            if (entries == null || entries.arraySize == 0)
                return null;
            return entries.GetArrayElementAtIndex(0).FindPropertyRelative("tile").objectReferenceValue as TileBase;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
