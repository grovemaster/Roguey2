#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.Ability;
using JRogue.Ability.Essence;
using JRogue.Controller.Enemy;
using JRogue.Data.Enemy;
using JRogue.Item.Essence;
using JRogue.Spawn;
using JRogue.Status;
using JRogue.Stats;
using JRogue.Traps;
using JRogue.World.Generation.MonsterSpawn;
using JRogue.World.Generation.Zones;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    /// <summary>
    /// Floor 1 production content: Goblin, Dire Wolf, Ghoul enemies, essences, spawn schedule, trap profiles.
    /// Run once (or re-run to refresh) after pulling §9 requirements.
    /// </summary>
    public static class DungeonFloor1ProductionContentPackCreator
    {
        const string MenuPath = "JRogue/Dungeon/Create Floor 1 Production Content Pack";

        const string EnemyRoot = "Assets/Data/Enemy/Production";
        const string LootRoot = "Assets/Data/Enemy/Loot/Production";
        const string SpawnRoot = "Assets/Data/Spawn/Production";
        const string PrefabRoot = "Assets/Prefabs/Actor/Enemy/Production";
        const string EssenceRoot = "Assets/Resources/Item/Essence/Production";
        const string AbilityRoot = "Assets/Resources/Item/Ability/Production";
        const string SchedulePath = "Assets/Data/Dungeon/SpawnSchedules/Schedule_Floor01_Production.asset";
        const string PopCavernPath = "Assets/Data/Dungeon/Zones/Population/Population_LuminescentCavern_Floor01.asset";
        const string PopDarkPath = "Assets/Data/Dungeon/Zones/Population/Population_NorthernDark_Floor01.asset";
        const string BaseEnemyPrefabPath = "Assets/Prefabs/Actor/Enemy/Enemy.prefab";
        const string BearTrapPath = "Assets/Data/Traps/TrapDefinition_Bear.asset";
        const string PoisonStatusPath = "Assets/Data/Status/Status_Poisoned_Default.asset";
        const string PoisonStatusResourcesPath = "Assets/Resources/Status/Status_Poisoned_Default.asset";
        const string MapIconPath = "Assets/Art/Essence/Sprites/Essence_MapIcon_YellowFlame.png";

        const string DcssRoot = "Assets/Sprites/DCSS/Dungeon Crawl Stone Soup Full";
        const string GoblinSpritePath = DcssRoot + "/monster/goblin_new.png";
        const string DireWolfSpritePath = DcssRoot + "/monster/animals/wolf.png";
        const string GhoulSpritePath = DcssRoot + "/monster/undead/ghoul.png";

        const string ZoneLuminescent = "luminescent_cavern";
        const string ZoneNorthernDark = "northern_dark";

        // Shared combat tuning — edit prefabs to change (see §9.4 in production requirements).
        const int SharedHp = 10;
        const int SharedAttackPower = 1;
        const int SharedVisionRange = 2;
        const int SharedFirstKillXp = 25;

        [MenuItem(MenuPath, false, 52)]
        public static void CreateFloor1ProductionContentPack()
        {
            EnsureFolder(EnemyRoot);
            EnsureFolder(LootRoot);
            EnsureFolder(SpawnRoot);
            EnsureFolder(PrefabRoot);
            EnsureFolder(EssenceRoot);
            EnsureFolder(AbilityRoot);
            EnsureFolder(Path.GetDirectoryName(SchedulePath)?.Replace('\\', '/'));
            EnsureFolder(Path.GetDirectoryName(PopCavernPath)?.Replace('\\', '/'));

            EnsurePoisonStatusDefinition();

            Sprite mapIcon = AssetDatabase.LoadAssetAtPath<Sprite>(MapIconPath);
            TrapDefinition bearTrap = AssetDatabase.LoadAssetAtPath<TrapDefinition>(BearTrapPath);
            EnemyController basePrefab = AssetDatabase.LoadAssetAtPath<EnemyController>(BaseEnemyPrefabPath);

            EnsureSpriteImport(GoblinSpritePath);
            EnsureSpriteImport(DireWolfSpritePath);
            EnsureSpriteImport(GhoulSpritePath);

            Sprite goblinSprite = LoadSprite(GoblinSpritePath);
            Sprite wolfSprite = LoadSprite(DireWolfSpritePath);
            Sprite ghoulSprite = LoadSprite(GhoulSpritePath);

            EssenceDesignAbility goblinAbility = CreateEssenceAbility(
                $"{AbilityRoot}/GoblinEssence_PoisonWeapon.asset",
                "Poison Weapon",
                "For the next 10 turns, weapon attacks (including ranged and thrown, not staves/wands) have a 10% chance to poison.",
                soulPowerCost: 10,
                consumesPlayerTurn: false,
                effectDurationTurns: 10,
                procChance: 0.1f);

            EssenceDesignAbility ghoulAbility = CreateEssenceAbility(
                $"{AbilityRoot}/GhoulEssence_Dash.asset",
                "Dash",
                "For the next 3 turns, the user may advance 2 tiles per turn (user only; party followers unchanged).",
                soulPowerCost: 0,
                consumesPlayerTurn: false,
                effectDurationTurns: 3,
                movementTilesPerTurn: 2);

            EssenceDesignAbility wolfAbility = CreateEssenceAbility(
                $"{AbilityRoot}/DireWolfEssence_AdrenalineRush.asset",
                "Adrenaline Rush",
                "Decreases Defense by 10, increases Strength by 10.",
                soulPowerCost: 10,
                consumesPlayerTurn: false,
                strengthDelta: 10,
                defenseDelta: -10);

            EssenceData goblinEssence = CreateEssence(
                $"{EssenceRoot}/GoblinEssence.asset",
                "Goblin Essence",
                "Tier-9 essence from goblins. +10 Dexterity.",
                mapIcon,
                goblinAbility,
                StatType.Dexterity, 10);

            EssenceData ghoulEssence = CreateEssence(
                $"{EssenceRoot}/GhoulEssence.asset",
                "Ghoul Essence",
                "Tier-9 essence from ghouls. +10 Agility, +10 Hearing.",
                mapIcon,
                ghoulAbility,
                StatType.Agility, 10,
                StatType.Hearing, 10);

            EssenceData wolfEssence = CreateEssence(
                $"{EssenceRoot}/DireWolfEssence.asset",
                "Dire Wolf Essence",
                "Tier-9 essence from dire wolves. +10 Strength, +10 Smell.",
                mapIcon,
                wolfAbility,
                StatType.Strength, 10,
                StatType.Smell, 10);

            EnemyLootTable goblinLoot = CreateLootTable(
                $"{LootRoot}/EnemyLootTable_Goblin.asset", "Goblin", goblinEssence);
            EnemyLootTable ghoulLoot = CreateLootTable(
                $"{LootRoot}/EnemyLootTable_Ghoul.asset", "Ghoul", ghoulEssence);
            EnemyLootTable wolfLoot = CreateLootTable(
                $"{LootRoot}/EnemyLootTable_DireWolf.asset", "Dire Wolf", wolfEssence);

            EnemySpeciesDefinition goblinSpecies = CreateSpecies(
                $"{EnemyRoot}/GoblinSpecies.asset", "goblin", "Goblin", goblinLoot);
            EnemySpeciesDefinition ghoulSpecies = CreateSpecies(
                $"{EnemyRoot}/GhoulSpecies.asset", "ghoul", "Ghoul", ghoulLoot);
            EnemySpeciesDefinition wolfSpecies = CreateSpecies(
                $"{EnemyRoot}/DireWolfSpecies.asset", "dire_wolf", "Dire Wolf", wolfLoot);

            EnemyController goblinPrefab = CreateEnemyPrefab(
                $"{PrefabRoot}/GoblinEnemy.prefab", "GoblinEnemy", goblinSpecies, goblinSprite, basePrefab);
            EnemyController ghoulPrefab = CreateEnemyPrefab(
                $"{PrefabRoot}/GhoulEnemy.prefab", "GhoulEnemy", ghoulSpecies, ghoulSprite, basePrefab);
            EnemyController wolfPrefab = CreateEnemyPrefab(
                $"{PrefabRoot}/DireWolfEnemy.prefab", "DireWolfEnemy", wolfSpecies, wolfSprite, basePrefab);

            EnemySpawnDefinition goblinSpawn = CreateSpawnDef(
                $"{SpawnRoot}/Spawn_Goblin_Floor01.asset", goblinPrefab);
            EnemySpawnDefinition ghoulSpawn = CreateSpawnDef(
                $"{SpawnRoot}/Spawn_Ghoul_Floor01.asset", ghoulPrefab);
            EnemySpawnDefinition wolfSpawn = CreateSpawnDef(
                $"{SpawnRoot}/Spawn_DireWolf_Floor01.asset", wolfPrefab);

            CreateProductionSchedule(goblinSpawn, ghoulSpawn, wolfSpawn);
            CreateTrapPopulationProfiles(bearTrap);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Floor1Production] Created production enemies, essences, schedule, and trap profiles. " +
                "Tune stats on prefabs under PrefabRoot; tune essences under EssenceRoot; " +
                "tune day schedules on Schedule_Floor01_Production.");
        }

        static void EnsurePoisonStatusDefinition()
        {
            EnsureFolder("Assets/Data/Status");
            EnsureFolder("Assets/Resources/Status");

            var definition = LoadOrCreate<PoisonStatusEffectDefinition>(PoisonStatusPath);
            definition.damagePerTick = 1;
            definition.damageType = DamageType.Poison;
            definition.escapeDifficulty = 12;
            EditorUtility.SetDirty(definition);

            if (!File.Exists(PoisonStatusResourcesPath))
            {
                AssetDatabase.CopyAsset(PoisonStatusPath, PoisonStatusResourcesPath);
                AssetDatabase.Refresh();
            }
        }

        static void CreateProductionSchedule(
            EnemySpawnDefinition goblinSpawn,
            EnemySpawnDefinition ghoulSpawn,
            EnemySpawnDefinition wolfSpawn)
        {
            MonsterSpawnScheduleProfile profile = LoadOrCreate<MonsterSpawnScheduleProfile>(SchedulePath);
            SerializedObject so = new SerializedObject(profile);
            SerializedProperty groups = so.FindProperty("groups");
            groups.arraySize = 16;
            int index = 0;

            Vector3Int[] cavernGoblinAnchors =
            {
                new(10, 12, 0), new(25, 12, 0), new(40, 12, 0), new(15, 45, 0), new(35, 45, 0),
            };
            for (int i = 0; i < cavernGoblinAnchors.Length; i++)
            {
                SetAnchoredGroup(
                    groups.GetArrayElementAtIndex(index++),
                    $"cavern_goblin_{i}",
                    ZoneLuminescent,
                    cavernGoblinAnchors[i],
                    goblinSpawn,
                    "goblin",
                    day1: 1,
                    day2: 1,
                    day3: i < 4 ? 2 : 1,
                    day4: i < 4 ? 2 : 1);
            }

            Vector3Int[] cavernGhoulAnchors =
            {
                new(10, 30, 0), new(25, 30, 0), new(40, 30, 0),
            };
            for (int i = 0; i < cavernGhoulAnchors.Length; i++)
            {
                SetAnchoredGroup(
                    groups.GetArrayElementAtIndex(index++),
                    $"cavern_ghoul_{i}",
                    ZoneLuminescent,
                    cavernGhoulAnchors[i],
                    ghoulSpawn,
                    "ghoul",
                    day1: 1,
                    day2: 1,
                    day3: 1,
                    day4: 1);
            }

            Vector3Int[] cavernWolfAnchors =
            {
                new(15, 22, 0), new(35, 22, 0), new(25, 38, 0),
            };
            for (int i = 0; i < cavernWolfAnchors.Length; i++)
            {
                SetAnchoredGroup(
                    groups.GetArrayElementAtIndex(index++),
                    $"cavern_dire_wolf_{i}",
                    ZoneLuminescent,
                    cavernWolfAnchors[i],
                    wolfSpawn,
                    "dire_wolf",
                    day1: 1,
                    day2: 1,
                    day3: 1,
                    day4: 1);
            }

            Vector3Int[] darkGoblinAnchors =
            {
                new(10, 68, 0), new(20, 68, 0), new(30, 68, 0), new(40, 68, 0), new(25, 72, 0),
            };
            for (int i = 0; i < darkGoblinAnchors.Length; i++)
            {
                SetAnchoredGroup(
                    groups.GetArrayElementAtIndex(index++),
                    $"dark_goblin_{i}",
                    ZoneNorthernDark,
                    darkGoblinAnchors[i],
                    goblinSpawn,
                    "goblin",
                    day1: 1,
                    day2: 1,
                    day3: 1,
                    day4: 1);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }

        static void SetAnchoredGroup(
            SerializedProperty groupElement,
            string groupId,
            string zoneId,
            Vector3Int anchor,
            EnemySpawnDefinition spawnDefinition,
            string rowId,
            int day1,
            int day2,
            int day3,
            int day4)
        {
            groupElement.FindPropertyRelative("groupId").stringValue = groupId;
            groupElement.FindPropertyRelative("displayName").stringValue = groupId;
            SerializedProperty binding = groupElement.FindPropertyRelative("areaBinding");
            binding.FindPropertyRelative("kind").enumValueIndex = (int)MonsterSpawnAreaBindingKind.ZoneId;
            binding.FindPropertyRelative("zoneId").stringValue = zoneId;
            binding.FindPropertyRelative("zoneInstanceId").stringValue = string.Empty;
            binding.FindPropertyRelative("markerIds").arraySize = 0;

            SerializedProperty anchors = groupElement.FindPropertyRelative("anchors");
            anchors.arraySize = 1;
            anchors.GetArrayElementAtIndex(0).vector3IntValue = anchor;
            groupElement.FindPropertyRelative("anchorPolicy").enumValueIndex =
                (int)MonsterSpawnAnchorPolicy.AtAnchor;

            SerializedProperty daySchedules = groupElement.FindPropertyRelative("daySchedules");
            daySchedules.arraySize = 4;
            SetRefillDaySchedule(daySchedules.GetArrayElementAtIndex(0), 1, spawnDefinition, rowId, day1);
            SetRefillDaySchedule(daySchedules.GetArrayElementAtIndex(1), 2, spawnDefinition, rowId, day2);
            SetRefillDaySchedule(daySchedules.GetArrayElementAtIndex(2), 3, spawnDefinition, rowId, day3);
            SetRefillDaySchedule(daySchedules.GetArrayElementAtIndex(3), 4, spawnDefinition, rowId, day4);
        }

        static void SetRefillDaySchedule(
            SerializedProperty daySchedule,
            int dungeonDay,
            EnemySpawnDefinition spawnDefinition,
            string rowId,
            int targetCount)
        {
            daySchedule.FindPropertyRelative("dungeonDay").intValue = dungeonDay;
            SerializedProperty composition = daySchedule.FindPropertyRelative("composition");
            composition.arraySize = 1;
            SerializedProperty row = composition.GetArrayElementAtIndex(0);
            row.FindPropertyRelative("rowId").stringValue = rowId;
            row.FindPropertyRelative("spawnDefinition").objectReferenceValue = spawnDefinition;
            row.FindPropertyRelative("targetCount").intValue = targetCount;
            row.FindPropertyRelative("fillPolicy").enumValueIndex =
                (int)MonsterSpawnFillPolicy.RefillToTarget;
            row.FindPropertyRelative("speciesFilter").stringValue = string.Empty;
        }

        static void CreateTrapPopulationProfiles(TrapDefinition bearTrap)
        {
            SetTrapOnlyPopulation(PopCavernPath, bearTrap, minCount: 2, maxCount: 3);
            SetTrapOnlyPopulation(PopDarkPath, bearTrap, minCount: 3, maxCount: 5);
        }

        static void SetTrapOnlyPopulation(string path, TrapDefinition bearTrap, int minCount, int maxCount)
        {
            DungeonZonePopulationProfile profile = LoadOrCreate<DungeonZonePopulationProfile>(path);
            SerializedObject so = new SerializedObject(profile);
            so.FindProperty("enemyPopulation").arraySize = 0;
            so.FindProperty("hazardPopulation").arraySize = 0;
            so.FindProperty("floorItemPopulation").arraySize = 0;
            so.FindProperty("interactablePopulation").arraySize = 0;

            SerializedProperty traps = so.FindProperty("trapPopulation");
            if (bearTrap == null)
            {
                traps.arraySize = 0;
            }
            else
            {
                traps.arraySize = 1;
                SerializedProperty trap = traps.GetArrayElementAtIndex(0);
                trap.FindPropertyRelative("definition").objectReferenceValue = bearTrap;
                trap.FindPropertyRelative("minCount").intValue = minCount;
                trap.FindPropertyRelative("maxCount").intValue = maxCount;
                trap.FindPropertyRelative("densityMode").enumValueIndex = 0;
                trap.FindPropertyRelative("requiresTag").stringValue = string.Empty;
                trap.FindPropertyRelative("forbiddenNearEdge").intValue = 0;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }

        static EssenceDesignAbility CreateEssenceAbility(
            string path,
            string abilityName,
            string description,
            int soulPowerCost,
            bool consumesPlayerTurn,
            int effectDurationTurns = 0,
            float procChance = 0f,
            int strengthDelta = 0,
            int defenseDelta = 0,
            int movementTilesPerTurn = 1)
        {
            var ability = LoadOrCreate<EssenceDesignAbility>(path);
            ability.abilityName = abilityName;
            ability.description = description;
            ability.soulPowerCost = soulPowerCost;
            ability.consumesPlayerTurn = consumesPlayerTurn;
            ability.effectDurationTurns = effectDurationTurns;
            ability.procChance = procChance;
            ability.strengthDelta = strengthDelta;
            ability.defenseDelta = defenseDelta;
            ability.movementTilesPerTurn = movementTilesPerTurn;
            EditorUtility.SetDirty(ability);
            return ability;
        }

        static EssenceData CreateEssence(
            string path,
            string essenceName,
            string description,
            Sprite mapIcon,
            EssenceDesignAbility ability,
            params object[] statPairs)
        {
            var essence = LoadOrCreate<EssenceData>(path);
            essence.essenceName = essenceName;
            essence.description = description;
            essence.tier = 9;
            essence.mapIcon = mapIcon;
            essence.floorLifetimePlayerPhases = 10;
            essence.statModifiers = new List<AttributeModifier>();
            for (int i = 0; i + 1 < statPairs.Length; i += 2)
            {
                essence.statModifiers.Add(new AttributeModifier
                {
                    attribute = (StatType)statPairs[i],
                    value = (int)statPairs[i + 1],
                });
            }

            essence.activeAbilities = ability != null
                ? new List<AbilityAction> { ability }
                : new List<AbilityAction>();
            EditorUtility.SetDirty(essence);
            return essence;
        }

        static EnemyLootTable CreateLootTable(string path, string displayName, EssenceData essence)
        {
            var table = LoadOrCreate<EnemyLootTable>(path);
            table.displayName = displayName;
            table.entries = new List<LootTableEntry>
            {
                new()
                {
                    dropChance = 1f,
                    payload = LootTablePayload.ManaStone,
                    manaStoneTier = 9,
                    quantity = 1,
                },
                new()
                {
                    dropChance = 0.05f,
                    payload = LootTablePayload.Essence,
                    essenceData = essence,
                    quantity = 1,
                },
            };
            EditorUtility.SetDirty(table);
            return table;
        }

        static EnemySpeciesDefinition CreateSpecies(
            string path,
            string speciesId,
            string displayName,
            EnemyLootTable lootTable)
        {
            var species = LoadOrCreate<EnemySpeciesDefinition>(path);
            species.speciesId = speciesId;
            species.displayName = displayName;
            species.firstKillExperience = SharedFirstKillXp;
            species.lootTable = lootTable;
            EditorUtility.SetDirty(species);
            return species;
        }

        static EnemyController CreateEnemyPrefab(
            string path,
            string prefabName,
            EnemySpeciesDefinition species,
            Sprite sprite,
            EnemyController basePrefab)
        {
            if (basePrefab == null)
            {
                Debug.LogError("[Floor1Production] Missing base enemy prefab.");
                return null;
            }

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing == null)
            {
                if (!AssetDatabase.CopyAsset(BaseEnemyPrefabPath, path))
                {
                    Debug.LogError($"[Floor1Production] Failed to copy prefab to {path}");
                    return null;
                }

                AssetDatabase.ImportAsset(path);
            }

            GameObject instance = PrefabUtility.LoadPrefabContents(path);
            try
            {
                instance.name = prefabName;
                EnemyController controller = instance.GetComponent<EnemyController>();
                if (controller != null)
                {
                    SerializedObject controllerSo = new SerializedObject(controller);
                    controllerSo.FindProperty("hp").intValue = SharedHp;
                    controllerSo.FindProperty("attackPower").intValue = SharedAttackPower;
                    controllerSo.FindProperty("visionRange").intValue = SharedVisionRange;
                    controllerSo.FindProperty("species").objectReferenceValue = species;
                    controllerSo.ApplyModifiedPropertiesWithoutUndo();
                }

                SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
                if (renderer != null && sprite != null)
                    renderer.sprite = sprite;

                PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(instance);
            }

            return AssetDatabase.LoadAssetAtPath<EnemyController>(path);
        }

        static EnemySpawnDefinition CreateSpawnDef(string path, EnemyController prefab)
        {
            var spawn = LoadOrCreate<EnemySpawnDefinition>(path);
            spawn.enemyPrefab = prefab;
            spawn.placementPolicy = EnemySpawnPlacementPolicy.NorthOfOriginThenNearestUnoccupiedFloor;
            spawn.primaryOffset = new Vector3Int(0, 1, 0);
            EditorUtility.SetDirty(spawn);
            return spawn;
        }

        static void EnsureSpriteImport(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        static Sprite LoadSprite(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
                Debug.LogWarning($"[Floor1Production] Sprite not found at {assetPath}");
            return sprite;
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
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
