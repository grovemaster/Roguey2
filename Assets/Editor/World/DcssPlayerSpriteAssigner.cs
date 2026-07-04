#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    /// <summary>Assigns DCSS player race sprites to HumanPlayer and ElfPlayer prefab variants.</summary>
    public static class DcssPlayerSpriteAssigner
    {
        const string SpriteFolder = "Assets/Art/Player/Sprites";

        static readonly (string prefabPath, string spriteFile)[] Assignments =
        {
            ("Assets/Prefabs/Actor/Race/HumanPlayer.prefab", "Player_Human.png"),
            ("Assets/Prefabs/Actor/Race/ElfPlayer.prefab", "Player_Elf.png"),
        };

        [MenuItem("JRogue/Player/Assign DCSS Player Race Sprites")]
        public static void AssignDcssPlayerRaceSprites()
        {
            int assigned = 0;
            int missing = 0;

            for (int i = 0; i < Assignments.Length; i++)
            {
                (string prefabPath, string spriteFile) = Assignments[i];
                string spritePath = $"{SpriteFolder}/{spriteFile}";

                GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (prefabRoot == null || sprite == null)
                {
                    Debug.LogWarning($"[DcssPlayer] Skip {prefabPath}: prefab or sprite missing.");
                    missing++;
                    continue;
                }

                GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
                    if (renderer == null)
                    {
                        Debug.LogWarning($"[DcssPlayer] {prefabPath} has no SpriteRenderer.");
                        missing++;
                        continue;
                    }

                    renderer.sprite = sprite;
                    PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                    assigned++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(instance);
                }
            }

            DcssPlayerSpritePackCreator.WireCatalogAsset();
            AssetDatabase.SaveAssets();
            Debug.Log($"[DcssPlayer] Assigned {assigned} player race sprite(s). Missing: {missing}.");
        }
    }
}
#endif
