#if UNITY_EDITOR
using System.IO;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Item;
using JRogue.Shop;
using JRogue.World.Generation;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    public static class ShopNpcPackCreator
    {
        const string HumanNpcPrefabPath = "Assets/Prefabs/Actor/Npc/HumanNpc.prefab";
        const string StampPath = "Assets/Resources/Town/Stamp_TownPlaza_20x20.asset";
        const string ResourcesNpcFolder = "Assets/Resources/Town/Npc";
        const string ResourcesShopFolder = "Assets/Resources/Shop";
        const string ResourcesPortraitsFolder = "Assets/Resources/Dialog/Portraits";
        const string GiantsBladePath = "Assets/Resources/Item/Weapon/Giants_Blade.asset";
        const string HandheldTorchPath = "Assets/Resources/Item/Accessory/Accessory_HandheldTorch.asset";
        const string HelmetOfLightPath = "Assets/Resources/Item/Armor/Armor_HelmetOfLight.asset";
        const string ThrowingKnifePath = "Assets/Resources/Item/Missile/Missile_ThrowingKnife.asset";
        const string GoldCoinPath = "Assets/Resources/Item/Currency/GoldCoin.asset";

        [MenuItem("JRogue/Town/Create Shop NPC Pack")]
        public static void CreateShopNpcPack()
        {
            EnsureFolders();
            CreatePlaceholderSprite("Assets/Art/NPC/Sprites/NPC_Fenn.png", new Color(0.35f, 0.72f, 0.78f));
            CreatePlaceholderSprite("Assets/Art/NPC/Sprites/NPC_Greta.png", new Color(0.92f, 0.58f, 0.28f));
            CreatePlaceholderPortrait("Assets/Art/Portraits/NPC/Portrait_Fenn.png", new Color(0.35f, 0.72f, 0.78f));
            CreatePlaceholderPortrait("Assets/Art/Portraits/NPC/Portrait_Greta.png", new Color(0.92f, 0.58f, 0.28f));
            AssetDatabase.Refresh();

            ConfigureTexture("Assets/Art/NPC/Sprites/NPC_Fenn.png", 32, FilterMode.Point);
            ConfigureTexture("Assets/Art/NPC/Sprites/NPC_Greta.png", 32, FilterMode.Point);
            ConfigureTexture("Assets/Art/Portraits/NPC/Portrait_Fenn.png", 128, FilterMode.Point);
            ConfigureTexture("Assets/Art/Portraits/NPC/Portrait_Greta.png", 128, FilterMode.Point);

            ItemData goldCoin = CreateGoldCoinAsset();
            ItemData giantsBlade = AssetDatabase.LoadAssetAtPath<ItemData>(GiantsBladePath);
            if (giantsBlade != null)
            {
                giantsBlade.buyValue = 2;
                giantsBlade.sellValue = 1;
                EditorUtility.SetDirty(giantsBlade);
            }

            PortraitDefinition fennPortrait = CreatePortrait("Portrait_Fenn", "Assets/Art/Portraits/NPC/Portrait_Fenn.png");
            PortraitDefinition gretaPortrait = CreatePortrait("Portrait_Greta", "Assets/Art/Portraits/NPC/Portrait_Greta.png");

            ShopNpcDefinition fennShop = CreateFennShop(fennPortrait);
            ShopNpcDefinition gretaShop = CreateGretaShop(gretaPortrait);

            GameObject humanNpc = AssetDatabase.LoadAssetAtPath<GameObject>(HumanNpcPrefabPath);
            if (humanNpc == null)
            {
                Debug.LogError("[ShopNpc] Missing HumanNpc base prefab. Run JRogue → Town → Create NPC Dialog Pack first.");
                return;
            }

            CreateShopNpcPrefab("TownNpc_Fenn", "Fenn", fennShop,
                "Assets/Art/NPC/Sprites/NPC_Fenn.png", humanNpc);
            CreateShopNpcPrefab("TownNpc_Greta", "Greta", gretaShop,
                "Assets/Art/NPC/Sprites/NPC_Greta.png", humanNpc);

            UpdateTownStampMarkers();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ShopNpc] Created shop NPC pack. Run TownTest, talk to Fenn or Greta with Enter.");
        }

        public static void RebuildTownNpcPrefabs(GameObject humanNpcBase)
        {
            if (humanNpcBase == null)
                return;

            ShopNpcDefinition fennShop =
                AssetDatabase.LoadAssetAtPath<ShopNpcDefinition>($"{ResourcesShopFolder}/ShopNpc_Fenn.asset");
            ShopNpcDefinition gretaShop =
                AssetDatabase.LoadAssetAtPath<ShopNpcDefinition>($"{ResourcesShopFolder}/ShopNpc_Greta.asset");
            if (fennShop == null || gretaShop == null)
            {
                Debug.LogWarning("[ShopNpc] Missing shop definitions — run Create Shop NPC Pack first.");
                return;
            }

            CreateShopNpcPrefab("TownNpc_Fenn", "Fenn", fennShop,
                "Assets/Art/NPC/Sprites/NPC_Fenn.png", humanNpcBase);
            CreateShopNpcPrefab("TownNpc_Greta", "Greta", gretaShop,
                "Assets/Art/NPC/Sprites/NPC_Greta.png", humanNpcBase);
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Art/NPC/Sprites");
            Directory.CreateDirectory("Assets/Art/Portraits/NPC");
            Directory.CreateDirectory(ResourcesNpcFolder);
            Directory.CreateDirectory(ResourcesShopFolder);
            Directory.CreateDirectory(ResourcesPortraitsFolder);
            Directory.CreateDirectory("Assets/Resources/Item/Currency");
        }

        static ShopNpcDefinition CreateFennShop(PortraitDefinition fennPortrait)
        {
            var shop = LoadOrCreate<ShopNpcDefinition>($"{ResourcesShopFolder}/ShopNpc_Fenn.asset");
            shop.shopNpcId = TownShopNpcIds.Npc4;
            shop.displayName = "Fenn";
            shop.portrait = fennPortrait;
            shop.allowPlayerBuy = false;
            shop.allowPlayerSell = true;
            shop.initialGold = 300;
            shop.initialStock = System.Array.Empty<ShopStockEntry>();
            EditorUtility.SetDirty(shop);
            return shop;
        }

        static ShopNpcDefinition CreateGretaShop(PortraitDefinition gretaPortrait)
        {
            var shop = LoadOrCreate<ShopNpcDefinition>($"{ResourcesShopFolder}/ShopNpc_Greta.asset");
            shop.shopNpcId = TownShopNpcIds.Npc5;
            shop.displayName = "Greta";
            shop.portrait = gretaPortrait;
            shop.allowPlayerBuy = true;
            shop.allowPlayerSell = false;
            shop.initialGold = 100;

            var stock = new System.Collections.Generic.List<ShopStockEntry>();
            ItemData giantsBlade = AssetDatabase.LoadAssetAtPath<ItemData>(GiantsBladePath);
            if (giantsBlade != null)
                stock.Add(new ShopStockEntry { item = giantsBlade, quantity = 2 });

            ItemData handheldTorch = AssetDatabase.LoadAssetAtPath<ItemData>(HandheldTorchPath);
            if (handheldTorch != null)
                stock.Add(new ShopStockEntry { item = handheldTorch, quantity = 2 });

            ItemData helmetOfLight = AssetDatabase.LoadAssetAtPath<ItemData>(HelmetOfLightPath);
            if (helmetOfLight != null)
                stock.Add(new ShopStockEntry { item = helmetOfLight, quantity = 1 });

            ItemData throwingKnife = AssetDatabase.LoadAssetAtPath<ItemData>(ThrowingKnifePath);
            if (throwingKnife != null)
            {
                throwingKnife.buyValue = 3;
                throwingKnife.sellValue = 1;
                EditorUtility.SetDirty(throwingKnife);
                stock.Add(new ShopStockEntry { item = throwingKnife, quantity = 5 });
            }

            shop.initialStock = stock.ToArray();
            EditorUtility.SetDirty(shop);
            return shop;
        }

        static ItemData CreateGoldCoinAsset()
        {
            var coin = LoadOrCreate<ItemData>(GoldCoinPath);
            coin.itemName = "Gold";
            coin.category = ItemCategory.Currency;
            coin.weight = 0f;
            coin.requiresAppraisal = false;
            coin.goldValue = 1;
            coin.buyValue = 0;
            coin.sellValue = 0;
            coin.autoPickupOnStep = false;
            EditorUtility.SetDirty(coin);
            return coin;
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
            NpcDialogPackCreator.DeletePrefabIfPresent(path);
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

        static void UpdateTownStampMarkers()
        {
            var stamp = AssetDatabase.LoadAssetAtPath<DungeonLayoutStamp>(StampPath);
            if (stamp == null)
                return;

            TownPlazaMarkerLayout.ApplyAll(stamp);
            EditorUtility.SetDirty(stamp);
        }

        static void CreatePlaceholderSprite(string assetPath, Color color)
        {
            CreatePlaceholderTexture(assetPath, 32, color, true);
        }

        static void CreatePlaceholderPortrait(string assetPath, Color color)
        {
            CreatePlaceholderTexture(assetPath, 128, color, false);
        }

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

        static void ConfigureTexture(string path, int ppu, FilterMode filter)
        {
            if (!File.Exists(path))
                return;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = filter;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            if (ppu == 32)
                importer.spritePivot = new Vector2(0.5f, 0.25f);

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

    [InitializeOnLoad]
    static class ShopNpcPackAutoCreator
    {
        static ShopNpcPackAutoCreator()
        {
            EditorApplication.delayCall += TryCreatePack;
        }

        static void TryCreatePack()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Town/Npc/TownNpc_Fenn.prefab") != null)
                return;

            ShopNpcPackCreator.CreateShopNpcPack();
        }
    }
}
#endif
