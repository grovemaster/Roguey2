#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    /// <summary>Import settings for DCSS-based playable race world sprites.</summary>
    public static class DcssPlayerSpritePackCreator
    {
        const string CatalogPath = "Assets/Resources/Player/PlayerRaceWorldSprites.asset";

        static readonly string[] PlayerSpritePaths =
        {
            "Assets/Art/Player/Sprites/Player_Human.png",
            "Assets/Art/Player/Sprites/Player_Elf.png",
        };

        [MenuItem("JRogue/Player/Configure DCSS Player Race Sprites")]
        public static void ConfigureDcssPlayerRaceSprites()
        {
            int configured = 0;
            for (int i = 0; i < PlayerSpritePaths.Length; i++)
                configured += ConfigureWorldSprite(PlayerSpritePaths[i]) ? 1 : 0;

            WireCatalogAsset();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DcssPlayer] Configured {configured} player race sprite(s).");
        }

        internal static void WireCatalogAsset()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<JRogue.View.PlayerRaceWorldSprites>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogWarning($"[DcssPlayer] Missing catalog: {CatalogPath}");
                return;
            }

            Sprite human = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePaths[0]);
            Sprite elf = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePaths[1]);
            var so = new SerializedObject(catalog);
            so.FindProperty("humanSprite").objectReferenceValue = human;
            so.FindProperty("elfSprite").objectReferenceValue = elf;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        static bool ConfigureWorldSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[DcssPlayer] Missing sprite: {path}");
                return false;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePivot = new Vector2(0.5f, 0.25f);
            importer.SaveAndReimport();
            return true;
        }
    }
}
#endif
