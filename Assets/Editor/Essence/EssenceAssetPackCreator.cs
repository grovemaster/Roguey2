#if UNITY_EDITOR
using JRogue.Data.Enemy;
using JRogue.Item.Essence;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.Essence
{
    public static class EssenceAssetPackCreator
    {
        const string SuddenStrengthPath = "Assets/Resources/Item/Essence/SuddenStrength.asset";
        const string MapIconPath = "Assets/Art/Essence/Sprites/Essence_MapIcon_YellowFlame.png";
        const string SkeletonLootPath = "Assets/Data/Enemy/Loot/EnemyLootTable_Skeleton.asset";

        const string MenuPath = "JRogue/Essence/Wire Enemy Essence Drop v0 Assets";

        [MenuItem(MenuPath, false, 45)]
        public static void WireEnemyEssenceDropAssets()
        {
            var suddenStrength = AssetDatabase.LoadAssetAtPath<EssenceData>(SuddenStrengthPath);
            if (suddenStrength == null)
            {
                Debug.LogError($"[Essence] Missing {SuddenStrengthPath}");
                return;
            }

            suddenStrength.tier = 9;
            suddenStrength.floorLifetimePlayerPhases = 10;
            suddenStrength.mapIcon = AssetDatabase.LoadAssetAtPath<Sprite>(MapIconPath);
            EditorUtility.SetDirty(suddenStrength);

            var skeletonTable = AssetDatabase.LoadAssetAtPath<EnemyLootTable>(SkeletonLootPath);
            if (skeletonTable == null)
            {
                Debug.LogError($"[Essence] Missing {SkeletonLootPath}");
                return;
            }

            bool hasEssenceRow = false;
            if (skeletonTable.entries != null)
            {
                for (int i = 0; i < skeletonTable.entries.Count; i++)
                {
                    LootTableEntry entry = skeletonTable.entries[i];
                    if (entry == null || entry.payload != LootTablePayload.Essence)
                        continue;

                    entry.essenceData = suddenStrength;
                    entry.dropChance = 1f;
                    entry.quantity = 1;
                    hasEssenceRow = true;
                    EditorUtility.SetDirty(skeletonTable);
                }
            }

            if (!hasEssenceRow)
            {
                if (skeletonTable.entries == null)
                    skeletonTable.entries = new System.Collections.Generic.List<LootTableEntry>();

                skeletonTable.entries.Add(new LootTableEntry
                {
                    dropChance = 1f,
                    payload = LootTablePayload.Essence,
                    essenceData = suddenStrength,
                    quantity = 1,
                });
                EditorUtility.SetDirty(skeletonTable);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Essence] Wired Sudden Strength tier/icon and Skeleton loot essence row.");
        }
    }
}
#endif
