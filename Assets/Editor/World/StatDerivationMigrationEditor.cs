using JRogue.Controller.Enemy;
using JRogue.Data.Progression;
using JRogue.Racial;
using JRogue.Stats;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    /// <summary>
    /// Migrates race base HP packages and enemy combat numbers into the dual-track HP band.
    /// Menu: JRogue → Stats → Migrate Stat Derivation v0 (Race HP + Enemies)
    /// </summary>
    public static class StatDerivationMigrationEditor
    {
        [MenuItem("JRogue/Stats/Migrate Stat Derivation v0 (Race HP + Enemies)")]
        public static void Migrate()
        {
            MigrateRaceLoadouts();
            MigrateEnemyPrefabs();
            MigrateExperienceCurve();
            AssetDatabase.SaveAssets();
            Debug.Log("[StatDerivation] Migration complete.");
        }

        static void MigrateExperienceCurve()
        {
            ExperienceCurve curve = AssetDatabase.LoadAssetAtPath<ExperienceCurve>(
                "Assets/Resources/Progression/DefaultExperienceCurve.asset");
            if (curve == null)
                return;

            SerializedObject so = new SerializedObject(curve);
            so.FindProperty("hpPerLevel").intValue = 4;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(curve);
        }

        static void MigrateRaceLoadouts()
        {
            SetLoadout("Assets/Data/Racial/DefaultHumanRacialLoadout.asset", 12, null);
            SetLoadout("Assets/Data/Racial/DefaultBarbarianRacialLoadout.asset", 18, new[]
            {
                (StatType.Strength, 3),
                (StatType.Dexterity, -2),
                (StatType.Agility, -1),
                (StatType.Constitution, 2),
                (StatType.Intelligence, -2),
                (StatType.Wisdom, -1),
                (StatType.Charisma, -1)
            });
            SetLoadout("Assets/Data/Racial/Elf/DefaultElfRacialLoadout.asset", 10, new[]
            {
                (StatType.Strength, -1),
                (StatType.Dexterity, 2),
                (StatType.Agility, 1),
                (StatType.Constitution, -2),
                (StatType.Intelligence, 1),
                (StatType.Wisdom, 1)
            });
            SetLoadout("Assets/Data/Racial/Dwarf/DefaultDwarfRacialLoadout.asset", 14, new[]
            {
                (StatType.Strength, 1),
                (StatType.Dexterity, -1),
                (StatType.Agility, -2),
                (StatType.Constitution, 2),
                (StatType.Intelligence, -1),
                (StatType.Wisdom, 1),
                (StatType.Charisma, -1)
            });
        }

        static void SetLoadout(string path, int raceBaseHp, (StatType attr, int value)[] mods)
        {
            RacialLoadoutDefinition loadout = AssetDatabase.LoadAssetAtPath<RacialLoadoutDefinition>(path);
            if (loadout == null)
            {
                Debug.LogWarning($"[StatDerivation] Missing loadout at {path}");
                return;
            }

            SerializedObject so = new SerializedObject(loadout);
            so.FindProperty("raceBaseHp").intValue = raceBaseHp;

            SerializedProperty modsProp = so.FindProperty("statModifiers");
            modsProp.ClearArray();
            if (mods != null)
            {
                for (int i = 0; i < mods.Length; i++)
                {
                    modsProp.InsertArrayElementAtIndex(i);
                    SerializedProperty element = modsProp.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("attribute").enumValueIndex = (int)mods[i].attr;
                    element.FindPropertyRelative("value").intValue = mods[i].value;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(loadout);
        }

        static void MigrateEnemyPrefabs()
        {
            SetEnemyStats("Assets/Prefabs/Actor/Enemy/Production/GoblinEnemy.prefab", 12, 1, 2);
            SetEnemyStats("Assets/Prefabs/Actor/Enemy/Production/GhoulEnemy.prefab", 12, 1, 2);
            SetEnemyStats("Assets/Prefabs/Actor/Enemy/Production/DireWolfEnemy.prefab", 14, 2, 3);
            SetEnemyStats("Assets/Prefabs/Actor/Enemy/Enemy.prefab", 12, 10, 2);
            SetEnemyStatsViaContents("Assets/Prefabs/Actor/Enemy/GiantSkeletonEnemy.prefab", 28, 10, 5);
            SetEnemyStatsViaContents("Assets/Prefabs/Actor/Enemy/Production/GiantSkeletonKingEnemy.prefab", 50, 12, 8);
        }

        static void SetEnemyStats(string prefabPath, int raceBaseHp, int constitution, int attackPower)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[StatDerivation] Missing prefab {prefabPath}");
                return;
            }

            CharacterStats stats = prefab.GetComponent<CharacterStats>();
            EnemyController enemy = prefab.GetComponent<EnemyController>();
            if (stats != null)
            {
                SerializedObject so = new SerializedObject(stats);
                so.FindProperty("raceBaseHP").intValue = raceBaseHp;
                so.FindProperty("Constitution").FindPropertyRelative("baseValue").intValue = constitution;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(stats);
            }

            if (enemy != null)
            {
                SerializedObject eso = new SerializedObject(enemy);
                eso.FindProperty("attackPower").intValue = attackPower;
                eso.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(enemy);
            }

            EditorUtility.SetDirty(prefab);
        }

        static void SetEnemyStatsViaContents(string prefabPath, int raceBaseHp, int constitution, int attackPower)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                CharacterStats stats = root.GetComponentInChildren<CharacterStats>();
                EnemyController enemy = root.GetComponentInChildren<EnemyController>();
                if (stats != null)
                {
                    SerializedObject so = new SerializedObject(stats);
                    so.FindProperty("raceBaseHP").intValue = raceBaseHp;
                    so.FindProperty("Constitution").FindPropertyRelative("baseValue").intValue = constitution;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                if (enemy != null)
                {
                    SerializedObject eso = new SerializedObject(enemy);
                    eso.FindProperty("attackPower").intValue = attackPower;
                    eso.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
