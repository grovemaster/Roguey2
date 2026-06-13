#if UNITY_EDITOR
using System.IO;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Item;
using JRogue.Racial;
using JRogue.Shop;
using JRogue.World.Generation;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    public static class FairyMerchantPackCreator
    {
        const string HumanNpcPrefabPath = "Assets/Prefabs/Actor/Npc/HumanNpc.prefab";
        const string StampPath = "Assets/Resources/Town/Stamp_TownPlaza_20x20.asset";
        const string ResourcesNpcFolder = "Assets/Resources/Town/Npc";
        const string ResourcesShopFolder = "Assets/Resources/Shop";
        const string ResourcesPortraitsFolder = "Assets/Resources/Dialog/Portraits";
        const string ResourcesItemFolder = "Assets/Resources/Item/Misc";
        const string ResourcesRegistryFolder = "Assets/Resources/Racial/Elf";
        const string EmberWardenPath = "Assets/Data/Racial/Elf/ElementalSpirits/EmberWarden.asset";
        const string TideShardPath = "Assets/Data/Racial/Elf/ElementalSpirits/TideShard.asset";
        const string MerchantSpritePath = "Assets/Art/NPC/Sprites/NPC_FairyMerchant.png";
        const string MerchantPortraitPath = "Assets/Art/Portraits/NPC/Portrait_FairyMerchant.png";

        [MenuItem("JRogue/Town/Create Fairy Merchant Pack")]
        public static void CreateFairyMerchantPack()
        {
            EnsureFolders();
            CreatePlaceholderSprite(MerchantSpritePath, new Color(0.42f, 0.82f, 0.58f));
            CreatePlaceholderPortrait(MerchantPortraitPath, new Color(0.42f, 0.82f, 0.58f));
            AssetDatabase.Refresh();

            ConfigureTexture(MerchantSpritePath, 32, FilterMode.Point);
            ConfigureTexture(MerchantPortraitPath, 128, FilterMode.Point);

            FairyStoneItemData fairyStone = CreateFairyStoneAsset();
            CreateElementalSpiritRegistry();
            PortraitDefinition portrait = CreatePortrait("Portrait_FairyMerchant", MerchantPortraitPath);
            ShopNpcDefinition shop = CreateFairyMerchantShop(fairyStone, portrait);

            GameObject humanNpc = AssetDatabase.LoadAssetAtPath<GameObject>(HumanNpcPrefabPath);
            if (humanNpc == null)
            {
                Debug.LogError("[FairyMerchant] Missing HumanNpc base prefab.");
                return;
            }

            CreateShopNpcPrefab("TownNpc_FairyMerchant", "Fairy Merchant", shop, MerchantSpritePath, humanNpc);
            UpdateTownStampMarker();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FairyMerchant] Created Fairy Merchant town NPC pack.");
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Art/NPC/Sprites");
            Directory.CreateDirectory("Assets/Art/Portraits/NPC");
            Directory.CreateDirectory(ResourcesNpcFolder);
            Directory.CreateDirectory(ResourcesShopFolder);
            Directory.CreateDirectory(ResourcesPortraitsFolder);
            Directory.CreateDirectory(ResourcesItemFolder);
            Directory.CreateDirectory(ResourcesRegistryFolder);
        }

        static FairyStoneItemData CreateFairyStoneAsset()
        {
            string path = $"{ResourcesItemFolder}/FairyStone.asset";
            var stone = LoadOrCreate<FairyStoneItemData>(path);
            stone.itemName = "Fairy Stone";
            stone.category = ItemCategory.Junk;
            stone.buyValue = 1;
            stone.sellValue = 0;
            stone.weight = 0.1f;
            stone.allowUseInSafeZone = true;
            EditorUtility.SetDirty(stone);
            return stone;
        }

        static void CreateElementalSpiritRegistry()
        {
            string path = $"{ResourcesRegistryFolder}/ElementalSpiritRegistry.asset";
            var registry = LoadOrCreate<ElementalSpiritRegistry>(path);
            var spirits = new System.Collections.Generic.List<ElementalSpiritDefinition>();

            ElementalSpiritDefinition ember = AssetDatabase.LoadAssetAtPath<ElementalSpiritDefinition>(EmberWardenPath);
            if (ember != null)
                spirits.Add(ember);

            ElementalSpiritDefinition tide = AssetDatabase.LoadAssetAtPath<ElementalSpiritDefinition>(TideShardPath);
            if (tide != null)
                spirits.Add(tide);

            registry.spirits = spirits;
            EditorUtility.SetDirty(registry);
        }

        static ShopNpcDefinition CreateFairyMerchantShop(FairyStoneItemData fairyStone, PortraitDefinition portrait)
        {
            var shop = LoadOrCreate<ShopNpcDefinition>($"{ResourcesShopFolder}/ShopNpc_FairyMerchant.asset");
            shop.shopNpcId = TownShopNpcIds.FairyMerchant;
            shop.displayName = "Fairy Merchant";
            shop.portrait = portrait;
            shop.allowPlayerBuy = true;
            shop.allowPlayerSell = false;
            shop.initialGold = 100;
            shop.initialStock = fairyStone != null
                ? new[] { new ShopStockEntry { item = fairyStone, quantity = 99 } }
                : System.Array.Empty<ShopStockEntry>();
            EditorUtility.SetDirty(shop);
            return shop;
        }

        static PortraitDefinition CreatePortrait(string assetName, string texturePath)
        {
            string path = $"{ResourcesPortraitsFolder}/{assetName}.asset";
            var portrait = LoadOrCreate<PortraitDefinition>(path);
            portrait.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            EditorUtility.SetDirty(portrait);
            return portrait;
        }

        static void CreateShopNpcPrefab(
            string prefabName,
            string displayName,
            ShopNpcDefinition shopDefinition,
            string spritePath,
            GameObject humanNpcBase)
        {
            string path = $"{ResourcesNpcFolder}/{prefabName}.prefab";
            GameObject instance = PrefabUtility.InstantiatePrefab(humanNpcBase) as GameObject;
            instance.name = prefabName;

            NpcController dialogNpc = instance.GetComponent<NpcController>();
            if (dialogNpc != null)
                Object.DestroyImmediate(dialogNpc, true);

            ShopNpcController shopNpc = instance.AddComponent<ShopNpcController>();
            SerializedObject shopSo = new SerializedObject(shopNpc);
            shopSo.FindProperty("npcId").stringValue = shopDefinition.shopNpcId;
            shopSo.FindProperty("shopDefinition").objectReferenceValue = shopDefinition;
            shopSo.FindProperty("portrait").objectReferenceValue = shopDefinition.portrait;
            shopSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject actorSo = new SerializedObject(shopNpc);
            actorSo.FindProperty("displayName").stringValue = displayName;
            actorSo.ApplyModifiedPropertiesWithoutUndo();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
            if (renderer != null && sprite != null)
                renderer.sprite = sprite;

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        static void UpdateTownStampMarker()
        {
            var stamp = AssetDatabase.LoadAssetAtPath<DungeonLayoutStamp>(StampPath);
            if (stamp == null)
                return;

            stamp.SetMarker(StampMarkerIds.FairyMerchant, new Vector3Int(12, 5, 0));
            EditorUtility.SetDirty(stamp);
        }

        static void CreatePlaceholderSprite(string assetPath, Color color) =>
            CreatePlaceholderTexture(assetPath, 32, color, true);

        static void CreatePlaceholderPortrait(string assetPath, Color color) =>
            CreatePlaceholderTexture(assetPath, 128, color, false);

        static void CreatePlaceholderTexture(string assetPath, int size, Color color, bool feetPivot)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = size <= 32 ? 32 : 128;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            if (feetPivot)
                importer.spritePivot = new Vector2(0.5f, 0.25f);
            importer.SaveAndReimport();
        }

        static void ConfigureTexture(string assetPath, int pixelsPerUnit, FilterMode filterMode)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = filterMode;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
#endif
