#if UNITY_EDITOR
using System.IO;
using JRogue.Ability.ArcaneMight;
using JRogue.Ability.Fireball;
using JRogue.Ability.LightningBolt;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Item;
using JRogue.Quest;
using JRogue.Racial;
using JRogue.Shop;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.World.Generation;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    public static class MageSpellPackCreator
    {
        const string HumanNpcPrefabPath = "Assets/Prefabs/Actor/Npc/HumanNpc.prefab";
        const string HumanPlayerPath = "Assets/Prefabs/Actor/Race/HumanPlayer.prefab";
        const string StampPath = "Assets/Resources/Town/Stamp_TownPlaza_20x20.asset";
        const string ResourcesNpcFolder = "Assets/Resources/Town/Npc";
        const string ResourcesQuestFolder = "Assets/Resources/Quest";
        const string ResourcesCatalogFolder = "Assets/Resources/Racial/Human";
        const string DataHumanFolder = "Assets/Data/Racial/Human";
        const string ResourcesSpellbookFolder = "Assets/Resources/Item/Spellbook";
        const string ResourcesAbilityFolder = "Assets/Resources/Item/Ability";
        const string ResourcesShopFolder = "Assets/Resources/Shop";
        const string ResourcesPortraitsFolder = "Assets/Resources/Dialog/Portraits";
        const string TutorSpritePath = "Assets/Art/NPC/Sprites/NPC_MageTutor.png";
        const string VendorSpritePath = "Assets/Art/NPC/Sprites/NPC_ArcaneVendor.png";
        const string TutorPortraitPath = "Assets/Art/Portraits/NPC/Portrait_MageTutor.png";
        const string VendorPortraitPath = "Assets/Art/Portraits/NPC/Portrait_ArcaneVendor.png";

        [MenuItem("JRogue/Racial/Create Human Mage Spell Pack")]
        public static void CreateHumanMageSpellPack()
        {
            EnsureFolders();
            CreatePlaceholderSprite(TutorSpritePath, new Color(0.42f, 0.34f, 0.82f));
            CreatePlaceholderSprite(VendorSpritePath, new Color(0.28f, 0.52f, 0.88f));
            CreatePlaceholderPortrait(TutorPortraitPath, new Color(0.42f, 0.34f, 0.82f));
            CreatePlaceholderPortrait(VendorPortraitPath, new Color(0.28f, 0.52f, 0.88f));
            AssetDatabase.Refresh();

            ConfigureTexture(TutorSpritePath, 32, FilterMode.Point);
            ConfigureTexture(VendorSpritePath, 32, FilterMode.Point);
            ConfigureTexture(TutorPortraitPath, 128, FilterMode.Point);
            ConfigureTexture(VendorPortraitPath, 128, FilterMode.Point);

            FireballAbility fireballAbility = SyncAbility<FireballAbility>(
                $"{ResourcesAbilityFolder}/Fireball_Standard.asset",
                "Fireball");
            LightningBoltAbility lightningAbility = CreateLightningAbility();
            ArcaneMightAbility arcaneMightAbility = CreateArcaneMightAbility();

            MageSpellDefinition fireball = CreateOrUpdateSpell(
                "Spell_Fireball_Mage",
                "mage_spell_fireball",
                "Fireball",
                tier: 3,
                extraEquipCost: 0,
                magicPowerCost: 5,
                fireballAbility);
            MageSpellDefinition lightning = CreateOrUpdateSpell(
                "Spell_LightningBolt_Mage",
                "mage_spell_lightning_bolt",
                "Lightning Bolt",
                tier: 5,
                extraEquipCost: 1,
                magicPowerCost: 4,
                lightningAbility);
            MageSpellDefinition arcaneMight = CreateOrUpdateSpell(
                "Spell_ArcaneMight_Mage",
                "mage_spell_arcane_might",
                "Arcane Might",
                tier: 7,
                extraEquipCost: 0,
                magicPowerCost: 2,
                arcaneMightAbility);

            CreateSpellCatalog(fireball, lightning, arcaneMight);

            MageSpellbookDefinition bookArcaneMight = CreateSpellbook(
                "Spellbook_ArcaneMight",
                "spellbook_arcane_might",
                "Spellbook of Arcane Might",
                arcaneMight);
            MageSpellbookDefinition bookFireball = CreateSpellbook(
                "Spellbook_Fireball",
                "spellbook_fireball",
                "Spellbook of Fireball",
                fireball);
            MageSpellbookDefinition bookLightning = CreateSpellbook(
                "Spellbook_LightningBolt",
                "spellbook_lightning_bolt",
                "Spellbook of Lightning Bolt",
                lightning);

            SpellbookItemData itemArcaneMight = CreateSpellbookItem(bookArcaneMight, "SpellbookItem_ArcaneMight");
            SpellbookItemData itemFireball = CreateSpellbookItem(bookFireball, "SpellbookItem_Fireball");
            SpellbookItemData itemLightning = CreateSpellbookItem(bookLightning, "SpellbookItem_LightningBolt");

            QuestDefinition apprenticeshipQuest = CreateApprenticeshipQuest();
            PortraitDefinition tutorPortrait = CreatePortrait("Portrait_MageTutor", TutorPortraitPath);
            PortraitDefinition vendorPortrait = CreatePortrait("Portrait_ArcaneVendor", VendorPortraitPath);

            WireHumanPlayerMageSpellsRuntime();
            NpcDialogPackCreator.RefreshHumanNpcBasePrefab();

            GameObject humanNpc = AssetDatabase.LoadAssetAtPath<GameObject>(HumanNpcPrefabPath);
            if (humanNpc == null)
            {
                Debug.LogError("[MageSpell] Missing HumanNpc base prefab.");
                return;
            }

            CreateMageTutorNpcPrefab(tutorPortrait, humanNpc);
            ShopNpcDefinition arcaneShop = CreateArcaneVendorShop(
                vendorPortrait,
                itemArcaneMight,
                itemFireball,
                itemLightning);
            CreateArcaneVendorNpcPrefab(vendorPortrait, humanNpc, arcaneShop);

            UpdateTownStampMarkers();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            TownNpcPrefabRebuild.RebuildAllHumanDerived();
            Debug.Log("[MageSpell] Created Human Mage spell pack (spells, books, tutor, vendor).");
        }

        public static void RebuildTownNpcPrefabs(GameObject humanNpcBase)
        {
            if (humanNpcBase == null)
                return;

            PortraitDefinition tutorPortrait =
                AssetDatabase.LoadAssetAtPath<PortraitDefinition>(
                    $"{ResourcesPortraitsFolder}/Portrait_MageTutor.asset");
            PortraitDefinition vendorPortrait =
                AssetDatabase.LoadAssetAtPath<PortraitDefinition>(
                    $"{ResourcesPortraitsFolder}/Portrait_ArcaneVendor.asset");
            ShopNpcDefinition arcaneShop =
                AssetDatabase.LoadAssetAtPath<ShopNpcDefinition>(
                    $"{ResourcesShopFolder}/ShopNpc_ArcaneVendor.asset");

            if (tutorPortrait == null || vendorPortrait == null || arcaneShop == null)
            {
                Debug.LogWarning("[MageSpell] Missing tutor/vendor assets — run Create Human Mage Spell Pack first.");
                return;
            }

            CreateMageTutorNpcPrefab(tutorPortrait, humanNpcBase);
            CreateArcaneVendorNpcPrefab(vendorPortrait, humanNpcBase, arcaneShop);
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Art/NPC/Sprites");
            Directory.CreateDirectory("Assets/Art/Portraits/NPC");
            Directory.CreateDirectory(ResourcesNpcFolder);
            Directory.CreateDirectory(ResourcesQuestFolder);
            Directory.CreateDirectory(ResourcesCatalogFolder);
            Directory.CreateDirectory(DataHumanFolder);
            Directory.CreateDirectory(ResourcesSpellbookFolder);
            Directory.CreateDirectory(ResourcesAbilityFolder);
            Directory.CreateDirectory(ResourcesShopFolder);
            Directory.CreateDirectory(ResourcesPortraitsFolder);
        }

        static T SyncAbility<T>(string path, string abilityName) where T : JRogue.Ability.AbilityAction
        {
            var ability = LoadOrCreate<T>(path);
            ability.abilityName = abilityName;
            ability.requiresTarget = true;
            EditorUtility.SetDirty(ability);
            return ability;
        }

        static LightningBoltAbility CreateLightningAbility()
        {
            string path = $"{ResourcesAbilityFolder}/LightningBolt_Standard.asset";
            var ability = LoadOrCreate<LightningBoltAbility>(path);
            ability.abilityName = "Lightning Bolt";
            ability.requiresTarget = true;
            ability.lightningDamage = 12;
            ability.splashRadius = 0;
            ability.splashZone = null;
            EditorUtility.SetDirty(ability);
            return ability;
        }

        static ArcaneMightAbility CreateArcaneMightAbility()
        {
            string path = $"{ResourcesAbilityFolder}/ArcaneMight_Standard.asset";
            var ability = LoadOrCreate<ArcaneMightAbility>(path);
            ability.abilityName = "Arcane Might";
            ability.requiresTarget = true;
            ability.strengthBonus = 100;
            ability.durationTurns = 10;
            EditorUtility.SetDirty(ability);
            return ability;
        }

        static MageSpellDefinition CreateOrUpdateSpell(
            string fileName,
            string spellId,
            string displayName,
            int tier,
            int extraEquipCost,
            int magicPowerCost,
            JRogue.Ability.AbilityAction ability)
        {
            string dataPath = $"{DataHumanFolder}/{fileName}.asset";
            var spell = LoadOrCreate<MageSpellDefinition>(dataPath);
            spell.spellId = spellId;
            spell.displayName = displayName;
            spell.tier = tier;
            spell.extraEquipCost = extraEquipCost;
            spell.magicPowerCost = magicPowerCost;
            spell.ability = ability;
            EditorUtility.SetDirty(spell);

            string resourcesPath = $"{ResourcesCatalogFolder}/{fileName}.asset";
            var copy = LoadOrCreate<MageSpellDefinition>(resourcesPath);
            copy.spellId = spell.spellId;
            copy.displayName = spell.displayName;
            copy.description = spell.description;
            copy.tier = spell.tier;
            copy.extraEquipCost = spell.extraEquipCost;
            copy.magicPowerCost = spell.magicPowerCost;
            copy.ability = spell.ability;
            EditorUtility.SetDirty(copy);
            return copy;
        }

        static void CreateSpellCatalog(
            MageSpellDefinition fireball,
            MageSpellDefinition lightning,
            MageSpellDefinition arcaneMight)
        {
            string path = $"{ResourcesCatalogFolder}/MageSpellCatalog.asset";
            var catalog = LoadOrCreate<MageSpellCatalog>(path);
            catalog.spells.Clear();
            if (arcaneMight != null)
                catalog.spells.Add(arcaneMight);
            if (fireball != null)
                catalog.spells.Add(fireball);
            if (lightning != null)
                catalog.spells.Add(lightning);
            EditorUtility.SetDirty(catalog);
        }

        static MageSpellbookDefinition CreateSpellbook(
            string fileName,
            string spellbookId,
            string displayName,
            MageSpellDefinition spell)
        {
            string path = $"{DataHumanFolder}/{fileName}.asset";
            var book = LoadOrCreate<MageSpellbookDefinition>(path);
            book.spellbookId = spellbookId;
            book.displayName = displayName;
            book.spellIds.Clear();
            if (spell != null)
                book.spellIds.Add(spell.spellId);
            EditorUtility.SetDirty(book);
            return book;
        }

        static SpellbookItemData CreateSpellbookItem(MageSpellbookDefinition book, string fileName)
        {
            string path = $"{ResourcesSpellbookFolder}/{fileName}.asset";
            var item = LoadOrCreate<SpellbookItemData>(path);
            item.itemName = book != null ? book.displayName : fileName;
            item.spellbook = book;
            item.category = ItemCategory.Spellbook;
            item.weight = 1f;
            item.buyValue = 1;
            item.sellValue = 0;
            item.allowUseInSafeZone = true;
            EditorUtility.SetDirty(item);
            return item;
        }

        static QuestDefinition CreateApprenticeshipQuest()
        {
            string path = $"{ResourcesQuestFolder}/quest_mage_tutor_apprenticeship.asset";
            var quest = LoadOrCreate<QuestDefinition>(path);
            quest.questId = HumanMageTutorIds.ApprenticeshipQuestId;
            quest.displayTitle = "Arcane Apprenticeship";
            quest.journalDescription = "Pay the Mage Tutor 5 gold to begin arcane training.";
            quest.giverNpcId = HumanMageTutorIds.TutorNpcId;
            quest.giverDisplayName = "Mage Tutor";
            quest.ownership = QuestOwnership.PerPartyMember;
            quest.requiredMinLevel = 0;
            quest.requiredRace = Race.Human;
            quest.requiresHumanClassNone = true;
            quest.requiresNoConsumedEssences = true;
            quest.turnInGoldCost = HumanMageClassCommitService.ApprenticeshipGoldCost;
            quest.commitHumanClass = HumanClass.Mage;
            quest.learnDragonianSpellId = null;
            quest.acceptPrerequisites = System.Array.Empty<QuestPrerequisite>();
            quest.objectives = System.Array.Empty<QuestObjectiveDefinition>();
            quest.autoCompleteOnObjectives = false;
            quest.sortOrder = 5;
            EditorUtility.SetDirty(quest);
            return quest;
        }

        static PortraitDefinition CreatePortrait(string assetName, string texturePath)
        {
            string path = $"{ResourcesPortraitsFolder}/{assetName}.asset";
            var portrait = LoadOrCreate<PortraitDefinition>(path);
            portrait.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            EditorUtility.SetDirty(portrait);
            return portrait;
        }

        static void CreateMageTutorNpcPrefab(PortraitDefinition portrait, GameObject humanNpcBase)
        {
            string path = $"{ResourcesNpcFolder}/TownNpc_MageTutor.prefab";
            NpcDialogPackCreator.DeletePrefabIfPresent(path);
            GameObject instance = PrefabUtility.InstantiatePrefab(humanNpcBase) as GameObject;
            instance.name = "TownNpc_MageTutor";

            Object.DestroyImmediate(instance.GetComponent<NpcController>(), true);
            HumanMageTutorNpcController controller = instance.AddComponent<HumanMageTutorNpcController>();

            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("npcId").stringValue = HumanMageTutorIds.TutorNpcId;
            controllerSo.FindProperty("portrait").objectReferenceValue = portrait;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject actorSo = new SerializedObject(controller);
            actorSo.FindProperty("displayName").stringValue = "Mage Tutor";
            actorSo.ApplyModifiedPropertiesWithoutUndo();

            ApplySprite(instance, TutorSpritePath);
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        static ShopNpcDefinition CreateArcaneVendorShop(
            PortraitDefinition portrait,
            params SpellbookItemData[] spellbooks)
        {
            string path = $"{ResourcesShopFolder}/ShopNpc_ArcaneVendor.asset";
            var shop = LoadOrCreate<ShopNpcDefinition>(path);
            shop.shopNpcId = HumanMageTutorIds.ArcaneVendorNpcId;
            shop.displayName = "Arcane Vendor";
            shop.portrait = portrait;
            shop.allowPlayerBuy = true;
            shop.allowPlayerSell = false;
            shop.initialGold = 100;

            var stock = new System.Collections.Generic.List<ShopStockEntry>();
            for (int i = 0; i < spellbooks.Length; i++)
            {
                if (spellbooks[i] == null)
                    continue;
                stock.Add(new ShopStockEntry { item = spellbooks[i], quantity = 99 });
            }

            shop.initialStock = stock.ToArray();
            EditorUtility.SetDirty(shop);
            return shop;
        }

        static void CreateArcaneVendorNpcPrefab(
            PortraitDefinition portrait,
            GameObject humanNpcBase,
            ShopNpcDefinition shopDefinition)
        {
            string path = $"{ResourcesNpcFolder}/TownNpc_ArcaneVendor.prefab";
            NpcDialogPackCreator.DeletePrefabIfPresent(path);
            GameObject instance = PrefabUtility.InstantiatePrefab(humanNpcBase) as GameObject;
            instance.name = "TownNpc_ArcaneVendor";

            Object.DestroyImmediate(instance.GetComponent<NpcController>(), true);
            ShopNpcController shopNpc = instance.AddComponent<ShopNpcController>();

            SerializedObject shopSo = new SerializedObject(shopNpc);
            shopSo.FindProperty("npcId").stringValue = shopDefinition.shopNpcId;
            shopSo.FindProperty("shopDefinition").objectReferenceValue = shopDefinition;
            shopSo.FindProperty("portrait").objectReferenceValue = portrait;
            shopSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject actorSo = new SerializedObject(shopNpc);
            actorSo.FindProperty("displayName").stringValue = "Arcane Vendor";
            actorSo.ApplyModifiedPropertiesWithoutUndo();

            ApplySprite(instance, VendorSpritePath);
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        static void ApplySprite(GameObject instance, string spritePath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
            if (renderer != null && sprite != null)
                renderer.sprite = sprite;
        }

        static void WireHumanPlayerMageSpellsRuntime()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanPlayerPath);
            if (prefab == null)
                return;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance.GetComponent<HumanMageSpellsRuntime>() == null)
                instance.AddComponent<HumanMageSpellsRuntime>();

            PrefabUtility.SaveAsPrefabAsset(instance, HumanPlayerPath);
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

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void CreatePlaceholderSprite(string path, Color color)
        {
            if (File.Exists(path))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void CreatePlaceholderPortrait(string path, Color color)
        {
            if (File.Exists(path))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var pixels = new Color[128 * 128];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void ConfigureTexture(string path, int pixelsPerUnit, FilterMode filterMode)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = filterMode;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}
#endif
