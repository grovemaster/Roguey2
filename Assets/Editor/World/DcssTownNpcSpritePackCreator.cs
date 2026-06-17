#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    /// <summary>Import settings for DCSS-based town NPC world sprites.</summary>
    public static class DcssTownNpcSpritePackCreator
    {
        static readonly string[] TownNpcSpritePaths =
        {
            "Assets/Art/NPC/Sprites/NPC_Mira.png",
            "Assets/Art/NPC/Sprites/NPC_Luc.png",
            "Assets/Art/NPC/Sprites/NPC_Edda.png",
        };

        [MenuItem("JRogue/Town/Configure DCSS Town NPC Sprites")]
        public static void ConfigureDcssTownNpcSprites()
        {
            int configured = 0;
            for (int i = 0; i < TownNpcSpritePaths.Length; i++)
                configured += ConfigureWorldSprite(TownNpcSpritePaths[i]) ? 1 : 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DcssTownNpc] Configured {configured} town NPC sprite(s). See Assets/Art/NPC/ThirdParty/DungeonCrawl32/README.md.");
        }

        static bool ConfigureWorldSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[DcssTownNpc] Missing sprite: {path}");
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
