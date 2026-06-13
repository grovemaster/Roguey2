#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Interactables;
using JRogue.Item;
using JRogue.Item.Essence;
using JRogue.Racial;
using JRogue.Shop;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.World.Generation;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    public static class BeastmanSoulBeastPackCreator
    {
        const string HumanNpcPrefabPath = "Assets/Prefabs/Actor/Npc/HumanNpc.prefab";
        const string BeastmanPlayerPath = "Assets/Prefabs/Actor/Race/BeastmanPlayer.prefab";
        const string StampPath = "Assets/Resources/Town/Stamp_TownPlaza_20x20.asset";
        const string ResourcesNpcFolder = "Assets/Resources/Town/Npc";
        const string ResourcesShopFolder = "Assets/Resources/Shop";
        const string ResourcesPortraitsFolder = "Assets/Resources/Dialog/Portraits";
        const string ResourcesItemFolder = "Assets/Resources/Item/Misc";
        const string ResourcesRegistryFolder = "Assets/Resources/Racial/Beastman";
        const string DataBeastmanFolder = "Assets/Data/Racial/Beastman";
        const string DataSoulBeastsFolder = "Assets/Data/Racial/Beastman/SoulBeasts";
        const string ResourcesInteractablesFolder = "Assets/Resources/Interactables";
        const string EffectsPath = "Assets/Data/Interactables/Effects";
        const string SpritesOffPath = "Assets/Art/Interactables/Sprites/LeverSwitch_Off.png";
        const string MerchantSpritePath = "Assets/Art/NPC/Sprites/NPC_BeastBloodMerchant.png";
        const string MerchantPortraitPath = "Assets/Art/Portraits/NPC/Portrait_BeastBloodMerchant.png";

        [MenuItem("JRogue/Town/Create Beastman Soul Beast Pack")]
        public static void CreateBeastmanSoulBeastPack()
        {
            EnsureFolders();
            CreatePlaceholderSprite(MerchantSpritePath, new Color(0.72f, 0.28f, 0.22f));
            CreatePlaceholderPortrait(MerchantPortraitPath, new Color(0.72f, 0.28f, 0.22f));
            AssetDatabase.Refresh();

            ConfigureTexture(MerchantSpritePath, 32, FilterMode.Point);
            ConfigureTexture(MerchantPortraitPath, 128, FilterMode.Point);

            SoulBeastDefinition emberWolf = CreateEmberWolf();
            SoulBeastDefinition stoneTortoise = CreateStoneTortoise();
            SoulBeastRegistry registry = CreateRegistry(emberWolf, stoneTortoise);

            List<SoulBeastRitualTypeDefinition> ritualTypes = CreateRitualTypes();
            SoulBeastRitualGateDefinition gate = CreateRitualGate(ritualTypes);
            CreateRitualCircleInteractable(gate);

            BeastBloodItemData beastBlood = CreateBeastBloodAsset();
            CreateRitualOfferingAsset(registry);

            PortraitDefinition portrait = CreatePortrait("Portrait_BeastBloodMerchant", MerchantPortraitPath);
            ShopNpcDefinition shop = CreateBeastBloodMerchantShop(beastBlood, portrait);

            GameObject humanNpc = AssetDatabase.LoadAssetAtPath<GameObject>(HumanNpcPrefabPath);
            if (humanNpc != null)
                CreateShopNpcPrefab("TownNpc_BeastBloodMerchant", "Beast Blood Merchant", shop, MerchantSpritePath, humanNpc);

            WireBeastmanPlayerRuntime();
            UpdateTownStampMarkers();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BeastmanSoulBeast] Created Beastman Soul Beast pack (Beast Blood = 2 gold).");
        }

        public static void RebuildTownNpcPrefab(GameObject humanNpcBase)
        {
            if (humanNpcBase == null)
                return;

            ShopNpcDefinition shop =
                AssetDatabase.LoadAssetAtPath<ShopNpcDefinition>(
                    $"{ResourcesShopFolder}/ShopNpc_BeastBloodMerchant.asset");
            if (shop == null)
            {
                Debug.LogWarning("[BeastmanSoulBeast] Missing shop definition — run Create Beastman Soul Beast Pack first.");
                return;
            }

            CreateShopNpcPrefab("TownNpc_BeastBloodMerchant", "Beast Blood Merchant", shop, MerchantSpritePath, humanNpcBase);
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
            Directory.CreateDirectory(DataBeastmanFolder);
            Directory.CreateDirectory(DataSoulBeastsFolder);
            Directory.CreateDirectory(ResourcesInteractablesFolder);
            Directory.CreateDirectory(EffectsPath);
            Directory.CreateDirectory("Assets/Data/Interactables/Effects");
        }

        static SoulBeastDefinition CreateEmberWolf()
        {
            var beast = LoadOrCreate<SoulBeastDefinition>($"{DataSoulBeastsFolder}/EmberWolf.asset");
            beast.soulBeastId = "ember_wolf";
            beast.displayName = "Ember Wolf";
            beast.description = "A fiery wolf spirit that enhances the Beastman's body.";
            beast.soulBeastType = SoulBeastType.Enhancement;
            beast.maxLevel = 5;
            beast.tags = new List<string> { "wolf", "fire" };
            beast.levels = new List<SoulBeastLevelData>
            {
                CreateLevelRow(StatType.Strength, 1),
                CreateLevelRow(StatType.Constitution, 1),
                CreateLevelRow(StatType.Strength, 2),
            };
            EditorUtility.SetDirty(beast);
            return beast;
        }

        static SoulBeastDefinition CreateStoneTortoise()
        {
            var beast = LoadOrCreate<SoulBeastDefinition>($"{DataSoulBeastsFolder}/StoneTortoise.asset");
            beast.soulBeastId = "stone_tortoise";
            beast.displayName = "Stone Tortoise";
            beast.description = "A patient earth spirit that hardens the contractor's frame.";
            beast.soulBeastType = SoulBeastType.Specialist;
            beast.maxLevel = 5;
            beast.tags = new List<string> { "tortoise", "earth" };
            beast.levels = new List<SoulBeastLevelData>
            {
                CreateLevelRow(StatType.Constitution, 1),
                CreateLevelRow(StatType.Strength, 1),
                CreateLevelRow(StatType.Constitution, 2),
            };
            EditorUtility.SetDirty(beast);
            return beast;
        }

        static SoulBeastLevelData CreateLevelRow(StatType stat, int value) =>
            new SoulBeastLevelData
            {
                statModifiers = new List<AttributeModifier>
                {
                    new AttributeModifier { attribute = stat, value = value },
                },
                resistanceModifiers = new List<DamageResistanceModifier>(),
                passiveEffects = new List<PassiveEffect>(),
                activeAbilities = new List<JRogue.Ability.AbilityAction>(),
            };

        static SoulBeastRegistry CreateRegistry(SoulBeastDefinition emberWolf, SoulBeastDefinition stoneTortoise)
        {
            var registry = LoadOrCreate<SoulBeastRegistry>($"{ResourcesRegistryFolder}/SoulBeastRegistry.asset");
            registry.beasts = new List<SoulBeastDefinition>();
            if (emberWolf != null)
                registry.beasts.Add(emberWolf);
            if (stoneTortoise != null)
                registry.beasts.Add(stoneTortoise);
            EditorUtility.SetDirty(registry);
            return registry;
        }

        static List<SoulBeastRitualTypeDefinition> CreateRitualTypes()
        {
            var types = new List<SoulBeastRitualTypeDefinition>();
            types.Add(CreateRitualType("ritual_summoning", "Summoning Rite", SoulBeastType.Summoning));
            types.Add(CreateRitualType("ritual_enhancement", "Enhancement Rite", SoulBeastType.Enhancement, "ember_wolf"));
            types.Add(CreateRitualType("ritual_special_ability", "Special Ability Rite", SoulBeastType.SpecialAbility));
            types.Add(CreateRitualType("ritual_specialist", "Specialist Rite", SoulBeastType.Specialist, "stone_tortoise"));
            return types;
        }

        static SoulBeastRitualTypeDefinition CreateRitualType(
            string id,
            string displayName,
            SoulBeastType allowedType,
            string primaryBeastId = null)
        {
            var ritualType = LoadOrCreate<SoulBeastRitualTypeDefinition>($"{DataBeastmanFolder}/{id}.asset");
            ritualType.ritualTypeId = id;
            ritualType.displayName = displayName;
            ritualType.description = $"Calls Soul Beasts of type {allowedType}.";
            ritualType.allowedSoulBeastTypes = new List<SoulBeastType> { allowedType };
            ritualType.noneOutcomeWeight = 50;
            ritualType.baseWeights = string.IsNullOrEmpty(primaryBeastId)
                ? new List<SoulBeastWeightEntry>()
                : new List<SoulBeastWeightEntry>
                {
                    new SoulBeastWeightEntry { soulBeastId = primaryBeastId, weight = 50 },
                };
            EditorUtility.SetDirty(ritualType);
            return ritualType;
        }

        static SoulBeastRitualGateDefinition CreateRitualGate(List<SoulBeastRitualTypeDefinition> ritualTypes)
        {
            var gate = LoadOrCreate<SoulBeastRitualGateDefinition>($"{DataBeastmanFolder}/SoulBeastRitualGate_Town.asset");
            gate.gateId = "soul_beast_ritual_circle";
            gate.displayName = "Soul Beast Ritual Circle";
            gate.ritualTypes = ritualTypes;
            EditorUtility.SetDirty(gate);

            var resourcesGate = LoadOrCreate<SoulBeastRitualGateDefinition>(
                $"{ResourcesRegistryFolder}/SoulBeastRitualGate_Town.asset");
            resourcesGate.gateId = gate.gateId;
            resourcesGate.displayName = gate.displayName;
            resourcesGate.ritualTypes = ritualTypes;
            EditorUtility.SetDirty(resourcesGate);
            return gate;
        }

        static void CreateRitualCircleInteractable(SoulBeastRitualGateDefinition gate)
        {
            AlwaysTruePrecondition alwaysTrue = AssetDatabase.LoadAssetAtPath<AlwaysTruePrecondition>(
                "Assets/Data/Interactables/Preconditions/AlwaysTrue.asset");
            if (alwaysTrue == null)
            {
                alwaysTrue = ScriptableObject.CreateInstance<AlwaysTruePrecondition>();
                AssetDatabase.CreateAsset(alwaysTrue, "Assets/Data/Interactables/Preconditions/AlwaysTrue.asset");
            }

            SoulBeastRitualEffect effect = LoadOrCreate<SoulBeastRitualEffect>(
                $"{EffectsPath}/SoulBeastRitual_Town.asset");
            effect.gate = gate;
            EditorUtility.SetDirty(effect);

            Sprite spriteOff = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesOffPath);
            InteractableTileDefinition ritual = LoadOrCreate<InteractableTileDefinition>(
                $"{ResourcesInteractablesFolder}/SoulBeastRitualCircle_Town.asset");
            ritual.interactableId = InteractableTileId.SoulBeastRitualCircle;
            ritual.displayName = "Soul Beast Ritual Circle";
            ritual.kind = InteractableTileKind.Shrine;
            ritual.blocksOccupancy = true;
            ritual.bumpEnabled = true;
            ritual.allowRepeatActivation = true;
            ritual.preconditions = new InteractablePrecondition[] { alwaysTrue };
            ritual.onActivateEffects = new InteractableEffect[] { effect };
            ritual.spriteOff = spriteOff;
            ritual.spriteOn = spriteOff;
            EditorUtility.SetDirty(ritual);
        }

        static BeastBloodItemData CreateBeastBloodAsset()
        {
            var blood = LoadOrCreate<BeastBloodItemData>($"{ResourcesItemFolder}/BeastBlood.asset");
            blood.itemName = "Beast Blood";
            blood.category = ItemCategory.Potion;
            blood.buyValue = 2;
            blood.sellValue = 0;
            blood.weight = 0.2f;
            blood.allowUseInSafeZone = true;
            EditorUtility.SetDirty(blood);
            return blood;
        }

        static void CreateRitualOfferingAsset(SoulBeastRegistry registry)
        {
            var offeringDef = LoadOrCreate<SoulBeastRitualOfferingDefinition>(
                $"{DataBeastmanFolder}/Offering_WolfTotem.asset");
            offeringDef.poolFilterTags = new List<string> { "wolf" };
            offeringDef.tagWeightBonuses = new List<SoulBeastTagWeightBonus>
            {
                new SoulBeastTagWeightBonus { tag = "wolf", bonusWeight = 3 },
            };
            EditorUtility.SetDirty(offeringDef);

            var item = LoadOrCreate<RitualOfferingItemData>($"{ResourcesItemFolder}/WolfTotemOffering.asset");
            item.itemName = "Wolf Totem";
            item.category = ItemCategory.Junk;
            item.buyValue = 5;
            item.sellValue = 1;
            item.weight = 0.3f;
            item.allowUseInSafeZone = true;
            item.ritualOffering = offeringDef;
            EditorUtility.SetDirty(item);
        }

        static ShopNpcDefinition CreateBeastBloodMerchantShop(
            BeastBloodItemData beastBlood,
            PortraitDefinition portrait)
        {
            var shop = LoadOrCreate<ShopNpcDefinition>($"{ResourcesShopFolder}/ShopNpc_BeastBloodMerchant.asset");
            shop.shopNpcId = TownShopNpcIds.BeastBloodMerchant;
            shop.displayName = "Beast Blood Merchant";
            shop.portrait = portrait;
            shop.allowPlayerBuy = true;
            shop.allowPlayerSell = false;
            shop.initialGold = 100;
            shop.initialStock = beastBlood != null
                ? new[] { new ShopStockEntry { item = beastBlood, quantity = 99 } }
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

        static void WireBeastmanPlayerRuntime()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BeastmanPlayerPath);
            if (prefab == null)
            {
                Debug.LogWarning("[BeastmanSoulBeast] Missing BeastmanPlayer prefab.");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance.GetComponent<BeastmanSoulBeastRuntime>() == null)
                instance.AddComponent<BeastmanSoulBeastRuntime>();

            PrefabUtility.SaveAsPrefabAsset(instance, BeastmanPlayerPath);
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
