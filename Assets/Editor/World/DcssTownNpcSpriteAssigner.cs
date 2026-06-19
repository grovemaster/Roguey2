#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    /// <summary>Assigns DCSS town NPC world sprites to plaza prefabs (one sprite per NPC).</summary>
    public static class DcssTownNpcSpriteAssigner
    {
        const string PrefabFolder = "Assets/Resources/Town/Npc";
        const string SpriteFolder = "Assets/Art/NPC/Sprites";

        /// <summary>
        /// One unique sprite per town NPC. Alternate race sprites are used where they fit the role
        /// and keep the plaza visually distinct (e.g. elf sage at the arcane stall).
        /// </summary>
        static readonly (string prefabName, string spriteFile)[] Assignments =
        {
            ("TownNpc_Mira", "NPC_Mira.png"),
            ("TownNpc_Luc", "NPC_Luc.png"),
            ("TownNpc_Edda", "NPC_Edda.png"),
            ("TownNpc_Fenn", "NPC_Fenn.png"),
            ("TownNpc_Greta", "NPC_Greta.png"),
            ("TownNpc_MageTutor", "NPC_MageTutor.png"),
            ("TownNpc_KnightDrillMaster", "NPC_KnightDrillMaster.png"),
            ("TownNpc_ArcaneVendor", "NPC_Elf_Sage.png"),
            ("TownNpc_PriestShrineSteward", "NPC_PriestShrineSteward.png"),
            ("TownNpc_DemoHost", "NPC_DemoHost.png"),
            ("TownNpc_ShamanBarbarian", "NPC_ShamanBarbarian.png"),
            ("TownNpc_ForgeBrothersSteward", "NPC_ForgeBrothersSteward.png"),
            ("TownNpc_StoneWardensSteward", "NPC_StoneWardensSteward.png"),
            ("TownNpc_BeastBloodMerchant", "NPC_BeastBloodMerchant.png"),
            ("TownNpc_FairyMerchant", "NPC_Fairy_Spriggan.png"),
            ("TownNpc_FleshmetalForgemaster", "NPC_Tiefling_Smith.png"),
            ("TownNpc_DragonianElderVolscale", "NPC_DragonianElderVolscale.png"),
            ("TownNpc_AdventureGuildClerk", "NPC_Fenn.png"),
        };

        [MenuItem("JRogue/Town/Assign DCSS Town NPC Sprites")]
        public static void AssignDcssTownNpcSprites()
        {
            int assigned = 0;
            int missing = 0;

            for (int i = 0; i < Assignments.Length; i++)
            {
                (string prefabName, string spriteFile) = Assignments[i];
                string prefabPath = $"{PrefabFolder}/{prefabName}.prefab";
                string spritePath = $"{SpriteFolder}/{spriteFile}";

                GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (prefabRoot == null || sprite == null)
                {
                    Debug.LogWarning($"[DcssTownNpc] Skip {prefabName}: prefab or sprite missing.");
                    missing++;
                    continue;
                }

                string prefabPathOnDisk = AssetDatabase.GetAssetPath(prefabRoot);
                GameObject instance = PrefabUtility.LoadPrefabContents(prefabPathOnDisk);
                try
                {
                    SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
                    if (renderer == null)
                    {
                        Debug.LogWarning($"[DcssTownNpc] {prefabName} has no SpriteRenderer.");
                        missing++;
                        continue;
                    }

                    renderer.sprite = sprite;
                    PrefabUtility.SaveAsPrefabAsset(instance, prefabPathOnDisk);
                    assigned++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(instance);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DcssTownNpc] Assigned {assigned} town NPC sprite(s). Missing: {missing}.");
        }
    }
}
#endif
