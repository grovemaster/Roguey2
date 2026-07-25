#if UNITY_EDITOR
using JRogue.Controller.Enemy;
using JRogue.Data.Enemy;
using JRogue.Spawn;
using JRogue.World.Generation;
using JRogue.World.LotF;
using JRogue.World.MapPresence;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    public static class LordOfTheFloorPackEditor
    {
        const string LootPath = "Assets/Data/Enemy/Loot/EnemyLootTable_GiantSkeletonKing.asset";
        const string SpeciesPath = "Assets/Data/Enemy/Production/GiantSkeletonKingSpecies.asset";
        const string PrefabPath = "Assets/Prefabs/Actor/Enemy/Production/GiantSkeletonKingEnemy.prefab";
        const string SourcePrefabPath = "Assets/Prefabs/Actor/Enemy/GiantSkeletonEnemy.prefab";
        const string SpawnPath = "Assets/Data/Spawn/Production/Spawn_GiantSkeletonKing_LotF.asset";
        const string MistEffectPath = "Assets/Data/World/MapPresence/Effects/MistOfTheAbyss.asset";
        const string ProfilePath = "Assets/Data/World/MapPresence/Profile_GiantSkeletonKing.asset";
        const string LotfPath = "Assets/Data/World/LotF/LotF_GiantSkeletonKing.asset";
        const string FloorPath = "Assets/Resources/Dungeon/Floor_prod_dungeon_floor_01.asset";

        static readonly Color KingTint = new Color(1f, 0.35f, 0.35f, 1f);

        [MenuItem("JRogue/World/Create Lord of the Floor v0 Assets (Giant Skeleton King)")]
        public static void CreateV0Assets()
        {
            EnsureFolders();

            EnemyLootTable loot = LoadOrCreate<EnemyLootTable>(LootPath);
            loot.displayName = "Giant Skeleton King";
            loot.entries = new System.Collections.Generic.List<LootTableEntry>
            {
                new LootTableEntry
                {
                    dropChance = 1f,
                    payload = LootTablePayload.ManaStone,
                    manaStoneTier = 8,
                    quantity = 1,
                },
            };
            EditorUtility.SetDirty(loot);

            MistOfTheAbyssMapEffect mist = LoadOrCreate<MistOfTheAbyssMapEffect>(MistEffectPath);
            mist.hostFloorId = DungeonFloorTransitionIds.Floor01Id;
            EditorUtility.SetDirty(mist);

            MonsterMapPresenceProfile profile = LoadOrCreate<MonsterMapPresenceProfile>(ProfilePath);
            profile.displayName = "Giant Skeleton King — Mist of the Abyss";
            profile.effects = new MonsterMapPresenceEffect[] { mist };
            profile.permanentOnSpawn = false;
            EditorUtility.SetDirty(profile);

            EnemySpeciesDefinition species = LoadOrCreate<EnemySpeciesDefinition>(SpeciesPath);
            species.speciesId = "giant_skeleton_king";
            species.displayName = "Giant Skeleton King";
            species.firstKillExperience = 100;
            species.lootTable = loot;
            species.mapPresenceProfileAsset = profile;
            EditorUtility.SetDirty(species);

            GameObject prefabRoot = CreateOrUpdateKingPrefab(species);
            EnemyController prefabController = prefabRoot != null
                ? prefabRoot.GetComponent<EnemyController>()
                : null;

            EnemySpawnDefinition spawn = LoadOrCreate<EnemySpawnDefinition>(SpawnPath);
            var spawnSo = new SerializedObject(spawn);
            spawnSo.FindProperty("enemyPrefab").objectReferenceValue = prefabController;
            spawnSo.FindProperty("placementPolicy").enumValueIndex = 0;
            spawnSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spawn);

            LordOfTheFloorDefinition lotf = LoadOrCreate<LordOfTheFloorDefinition>(LotfPath);
            var lotfSo = new SerializedObject(lotf);
            lotfSo.FindProperty("lotfId").stringValue = "lotf_giant_skeleton_king";
            lotfSo.FindProperty("displayName").stringValue = "Giant Skeleton King";
            lotfSo.FindProperty("title").stringValue = "Lord of Giant Skeletons";
            lotfSo.FindProperty("hostFloorId").stringValue = DungeonFloorTransitionIds.Floor01Id;
            lotfSo.FindProperty("species").objectReferenceValue = species;
            lotfSo.FindProperty("spawnDefinition").objectReferenceValue = spawn;
            lotfSo.FindProperty("minimumDungeonDay").intValue = 3;
            lotfSo.FindProperty("minimumLivingPartyMembers").intValue = 4;
            lotfSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(lotf);

            DungeonFloorDefinition floor =
                AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(FloorPath);
            if (floor != null)
            {
                var floorSo = new SerializedObject(floor);
                SerializedProperty lords = floorSo.FindProperty("lordsOfTheFloor");
                if (lords != null)
                {
                    lords.arraySize = 1;
                    lords.GetArrayElementAtIndex(0).objectReferenceValue = lotf;
                    floorSo.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(floor);
                }
                else
                {
                    Debug.LogWarning(
                        "[LotF] Floor_prod_dungeon_floor_01 missing lordsOfTheFloor field — reimport scripts and re-run.");
                }
            }
            else
            {
                Debug.LogWarning($"[LotF] Missing floor asset at {FloorPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[LotF] Created Giant Skeleton King species, loot, prefab, spawn, Mist profile, LotF definition, and wired Floor 1.");
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets/Data/Enemy/Loot");
            EnsureFolder("Assets/Data/Enemy/Production");
            EnsureFolder("Assets/Data/Spawn/Production");
            EnsureFolder("Assets/Data/World/MapPresence/Effects");
            EnsureFolder("Assets/Data/World/LotF");
            EnsureFolder("Assets/Prefabs/Actor/Enemy/Production");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
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

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static GameObject CreateOrUpdateKingPrefab(EnemySpeciesDefinition species)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            if (source == null)
            {
                Debug.LogError($"[LotF] Missing source prefab {SourcePrefabPath}");
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
                instance = Object.Instantiate(source);

            instance.name = "GiantSkeletonKingEnemy";

            EnemyController enemy = instance.GetComponent<EnemyController>();
            if (enemy != null)
            {
                var so = new SerializedObject(enemy);
                SerializedProperty speciesProp = so.FindProperty("species");
                if (speciesProp != null)
                    speciesProp.objectReferenceValue = species;
                so.ApplyModifiedPropertiesWithoutUndo();
                enemy.SetDisplayName("Giant Skeleton King, Lord of Giant Skeletons");
            }

            if (instance.GetComponent<MonsterMapPresenceHost>() == null)
                instance.AddComponent<MonsterMapPresenceHost>();

            if (instance.GetComponent<LordOfTheFloorHost>() == null)
                instance.AddComponent<LordOfTheFloorHost>();

            ApplyRedTint(instance);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        static void ApplyRedTint(GameObject root)
        {
            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].color = KingTint;
        }
    }
}
#endif
