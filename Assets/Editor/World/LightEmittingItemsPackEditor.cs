#if UNITY_EDITOR
using System.IO;
using JRogue.Ability.HelmetOfLight;
using JRogue.Item;
using JRogue.Shop;
using JRogue.World.Generation;
using JRogue.World.Lighting;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    public static class LightEmittingItemsPackEditor
    {
        const string TorchIconPath = "Assets/Art/Items/Sprites/Accessory_HandheldTorch.png";
        const string HelmetIconPath = "Assets/Art/Items/Sprites/Armor_HelmetOfLight.png";
        const string TorchEmitterPath = "Assets/Resources/Lighting/Torch.asset";
        const string HandheldTorchPath = "Assets/Resources/Item/Accessory/Accessory_HandheldTorch.asset";
        const string HelmetPath = "Assets/Resources/Item/Armor/Armor_HelmetOfLight.asset";
        const string RadianceAbilityPath = "Assets/Resources/Item/Ability/HelmetOfLight_Radiance.asset";
        const string Floor1Path = "Assets/Resources/Dungeon/Floor_dungeon_floor_01.asset";
        const string GretaShopPath = "Assets/Resources/Shop/ShopNpc_Greta.asset";

        [MenuItem("JRogue/Lighting/Create Light-Emitting Items Pack")]
        public static void CreateLightEmittingItemsPack()
        {
            EnsureFolders();

            LightEmitterDefinition torchEmitter = AssetDatabase.LoadAssetAtPath<LightEmitterDefinition>(TorchEmitterPath);
            Sprite torchIcon = AssetDatabase.LoadAssetAtPath<Sprite>(TorchIconPath);
            Sprite helmetIcon = AssetDatabase.LoadAssetAtPath<Sprite>(HelmetIconPath);

            HelmetOfLightRadianceAbility radiance = CreateRadianceAbility();
            LightSourceItemData handheldTorch = CreateHandheldTorch(torchEmitter, torchIcon);
            LightSourceItemData helmet = CreateHelmet(torchEmitter, helmetIcon, radiance);

            WireFloor1HandheldTorch(handheldTorch);
            WireGretaShop(handheldTorch, helmet);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LightItems] Created Handheld Torch + Helmet of Light pack.");
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Resources/Item/Accessory");
            Directory.CreateDirectory("Assets/Resources/Item/Armor");
            Directory.CreateDirectory("Assets/Resources/Item/Ability");
        }

        static HelmetOfLightRadianceAbility CreateRadianceAbility()
        {
            var ability = LoadOrCreate<HelmetOfLightRadianceAbility>(RadianceAbilityPath);
            ability.abilityName = "Radiance";
            ability.description = "Emit light for 5 turns.";
            ability.soulPowerCost = 0;
            ability.magicPowerCost = 0;
            ability.divinePowerCost = 0;
            ability.cooldownTurns = 3;
            ability.requiresTarget = false;
            ability.range = 0;
            ability.lightDurationTurns = LightSourceItemRules.DefaultHelmetLightDurationTurns;
            EditorUtility.SetDirty(ability);
            return ability;
        }

        static LightSourceItemData CreateHandheldTorch(LightEmitterDefinition emitter, Sprite icon)
        {
            var item = LoadOrCreate<LightSourceItemData>(HandheldTorchPath);
            item.itemName = "Handheld Torch";
            item.category = ItemCategory.Accessory;
            item.slotType = EquipmentSlot.Accessory_MainHand;
            item.weight = 1f;
            item.icon = icon;
            item.goldValue = 8;
            item.buyValue = 12;
            item.sellValue = 4;
            item.requiresAppraisal = true;
            item.autoPickupOnStep = false;
            item.emitterDefinition = emitter;
            item.emitsWhenEquipped = true;
            item.startsLit = true;
            item.canIgniteWallTorches = false;
            item.activeAbilities = new System.Collections.Generic.List<JRogue.Ability.AbilityAction>();
            EditorUtility.SetDirty(item);
            return item;
        }

        static LightSourceItemData CreateHelmet(
            LightEmitterDefinition emitter,
            Sprite icon,
            HelmetOfLightRadianceAbility radiance)
        {
            var item = LoadOrCreate<LightSourceItemData>(HelmetPath);
            item.itemName = "Helmet of Light";
            item.category = ItemCategory.Armor;
            item.slotType = EquipmentSlot.Head;
            item.weight = 2.5f;
            item.icon = icon;
            item.goldValue = 40;
            item.buyValue = 55;
            item.sellValue = 20;
            item.requiresAppraisal = true;
            item.autoPickupOnStep = false;
            item.emitterDefinition = emitter;
            item.emitsWhenEquipped = false;
            item.startsLit = false;
            item.canIgniteWallTorches = false;
            item.activeAbilities = new System.Collections.Generic.List<JRogue.Ability.AbilityAction> { radiance };
            EditorUtility.SetDirty(item);
            return item;
        }

        static void WireFloor1HandheldTorch(LightSourceItemData handheldTorch)
        {
            if (handheldTorch == null)
                return;

            var floor = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(Floor1Path);
            if (floor == null)
            {
                Debug.LogWarning($"[LightItems] Missing {Floor1Path}");
                return;
            }

            SerializedObject so = new SerializedObject(floor);
            SerializedProperty items = so.FindProperty("floorItemPopulation");
            int torchIndex = FindPopulationIndex(items, handheldTorch);
            if (torchIndex < 0)
            {
                int newIndex = items.arraySize;
                items.InsertArrayElementAtIndex(newIndex);
                torchIndex = newIndex;
            }

            SerializedProperty entry = items.GetArrayElementAtIndex(torchIndex);
            entry.FindPropertyRelative("itemData").objectReferenceValue = handheldTorch;
            entry.FindPropertyRelative("minCount").intValue = 1;
            entry.FindPropertyRelative("maxCount").intValue = 1;
            entry.FindPropertyRelative("minQuantity").intValue = 1;
            entry.FindPropertyRelative("maxQuantity").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(floor);
        }

        static int FindPopulationIndex(SerializedProperty items, ItemData target)
        {
            for (int i = 0; i < items.arraySize; i++)
            {
                var item = items.GetArrayElementAtIndex(i).FindPropertyRelative("itemData").objectReferenceValue as ItemData;
                if (item == target)
                    return i;
            }

            return -1;
        }

        static void WireGretaShop(LightSourceItemData handheldTorch, LightSourceItemData helmet)
        {
            var shop = AssetDatabase.LoadAssetAtPath<ShopNpcDefinition>(GretaShopPath);
            if (shop == null)
            {
                Debug.LogWarning($"[LightItems] Missing {GretaShopPath}");
                return;
            }

            ItemData giantsBlade = AssetDatabase.LoadAssetAtPath<ItemData>(
                "Assets/Resources/Item/Weapon/Giants_Blade.asset");

            var stock = new System.Collections.Generic.List<ShopStockEntry>();
            if (giantsBlade != null)
                stock.Add(new ShopStockEntry { item = giantsBlade, quantity = 2 });
            if (handheldTorch != null)
                stock.Add(new ShopStockEntry { item = handheldTorch, quantity = 2 });
            if (helmet != null)
                stock.Add(new ShopStockEntry { item = helmet, quantity = 1 });

            shop.initialStock = stock.ToArray();
            EditorUtility.SetDirty(shop);
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
#endif
