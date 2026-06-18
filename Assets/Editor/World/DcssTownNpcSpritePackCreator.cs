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
            // Humans (10)
            "Assets/Art/NPC/Sprites/NPC_Mira.png",
            "Assets/Art/NPC/Sprites/NPC_Luc.png",
            "Assets/Art/NPC/Sprites/NPC_Edda.png",
            "Assets/Art/NPC/Sprites/NPC_Fenn.png",
            "Assets/Art/NPC/Sprites/NPC_Greta.png",
            "Assets/Art/NPC/Sprites/NPC_MageTutor.png",
            "Assets/Art/NPC/Sprites/NPC_KnightDrillMaster.png",
            "Assets/Art/NPC/Sprites/NPC_ArcaneVendor.png",
            "Assets/Art/NPC/Sprites/NPC_PriestShrineSteward.png",
            "Assets/Art/NPC/Sprites/NPC_DemoHost.png",
            // Barbarian (2)
            "Assets/Art/NPC/Sprites/NPC_ShamanBarbarian.png",
            "Assets/Art/NPC/Sprites/NPC_Barbarian_Warchief.png",
            // Dwarf (2)
            "Assets/Art/NPC/Sprites/NPC_ForgeBrothersSteward.png",
            "Assets/Art/NPC/Sprites/NPC_StoneWardensSteward.png",
            // Beastman (2)
            "Assets/Art/NPC/Sprites/NPC_BeastBloodMerchant.png",
            "Assets/Art/NPC/Sprites/NPC_Beastman_Brute.png",
            // Dragonian (2)
            "Assets/Art/NPC/Sprites/NPC_DragonianElderVolscale.png",
            "Assets/Art/NPC/Sprites/NPC_Dragonian_Guard.png",
            // Tiefling (2)
            "Assets/Art/NPC/Sprites/NPC_FleshmetalForgemaster.png",
            "Assets/Art/NPC/Sprites/NPC_Tiefling_Smith.png",
            // Fairy (2)
            "Assets/Art/NPC/Sprites/NPC_FairyMerchant.png",
            "Assets/Art/NPC/Sprites/NPC_Fairy_Spriggan.png",
            // Elf (2)
            "Assets/Art/NPC/Sprites/NPC_Elf_Ranger.png",
            "Assets/Art/NPC/Sprites/NPC_Elf_Sage.png",
            // Undead (2)
            "Assets/Art/NPC/Sprites/NPC_Undead_Wight.png",
            "Assets/Art/NPC/Sprites/NPC_Undead_Revenant.png",
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
