#if UNITY_EDITOR
using System.IO;
using JRogue.Controller.Npc;
using JRogue.Controller.Player;
using JRogue.Dialog;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Quest;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.World.Generation;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    public static class DragonianElderPackCreator
    {
        const string DragonianPlayerPath = "Assets/Prefabs/Actor/Race/DragonianPlayer.prefab";
        const string DragonianNpcPath = "Assets/Prefabs/Actor/Npc/DragonianNpc.prefab";
        const string StampPath = "Assets/Resources/Town/Stamp_TownPlaza_20x20.asset";
        const string ResourcesNpcFolder = "Assets/Resources/Town/Npc";
        const string ResourcesQuestFolder = "Assets/Resources/Quest";
        const string ResourcesCatalogFolder = "Assets/Resources/Racial/Dragonian";
        const string DataDragonianFolder = "Assets/Data/Racial/Dragonian";
        const string ResourcesPortraitsFolder = "Assets/Resources/Dialog/Portraits";
        const string ElderSpritePath = "Assets/Art/NPC/Sprites/NPC_DragonianElderVolscale.png";
        const string ElderPortraitPath = "Assets/Art/Portraits/NPC/Portrait_DragonianElderVolscale.png";

        [MenuItem("JRogue/Racial/Create Dragonian Elder Pack")]
        public static void CreateDragonianElderPack()
        {
            EnsureFolders();
            CreatePlaceholderSprite(ElderSpritePath, new Color(0.72f, 0.28f, 0.18f));
            CreatePlaceholderPortrait(ElderPortraitPath, new Color(0.72f, 0.28f, 0.18f));
            AssetDatabase.Refresh();

            ConfigureTexture(ElderSpritePath, 32, FilterMode.Point);
            ConfigureTexture(ElderPortraitPath, 128, FilterMode.Point);

            DragonianSpellDefinition draconicSurge = AssetDatabase.LoadAssetAtPath<DragonianSpellDefinition>(
                $"{DataDragonianFolder}/Spell_DraconicSurge.asset");
            DragonianSpellDefinition dragonFlame = AssetDatabase.LoadAssetAtPath<DragonianSpellDefinition>(
                $"{DataDragonianFolder}/Spell_DragonFlame.asset");
            CreateSpellCatalog(draconicSurge, dragonFlame);
            WireDragonianPlayerPartyMemberId();

            ItemData emberScale = CreateEmberScaleItem();
            QuestDefinition quest01 = CreateVolscaleQuest01(emberScale, draconicSurge);
            QuestDefinition quest02 = CreateVolscaleQuest02(dragonFlame);
            DragonianElderDefinition elder = CreateVolscaleElder(quest01, quest02);

            PortraitDefinition portrait = CreatePortrait("Portrait_DragonianElderVolscale", ElderPortraitPath);
            GameObject dragonianNpcBase = CreateDragonianNpcBasePrefab();
            CreateVolscaleTownNpcPrefab(portrait, dragonianNpcBase, elder);
            WireEmberScaleLoot(emberScale);
            WireGretaShopEmberScales(emberScale);
            UpdateTownStampMarker();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DragonianElder] Created Elder Volscale pack (quests, NPC, catalog).");
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Art/NPC/Sprites");
            Directory.CreateDirectory("Assets/Art/Portraits/NPC");
            Directory.CreateDirectory(ResourcesNpcFolder);
            Directory.CreateDirectory(ResourcesQuestFolder);
            Directory.CreateDirectory(ResourcesCatalogFolder);
            Directory.CreateDirectory(ResourcesPortraitsFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(DragonianNpcPath)!);
            Directory.CreateDirectory("Assets/Resources/Item/Quest");
        }

        static void CreateSpellCatalog(
            DragonianSpellDefinition draconicSurge,
            DragonianSpellDefinition dragonFlame)
        {
            string path = $"{ResourcesCatalogFolder}/DragonianSpellCatalog.asset";
            DragonianSpellDefinition resourcesSurge = SyncResourcesSpellCopy(
                draconicSurge,
                "Spell_DraconicSurge.asset");
            DragonianSpellDefinition resourcesFlame = SyncResourcesSpellCopy(
                dragonFlame,
                "Spell_DragonFlame.asset");

            var catalog = LoadOrCreate<DragonianSpellCatalog>(path);
            catalog.spells.Clear();
            if (resourcesSurge != null)
                catalog.spells.Add(resourcesSurge);
            if (resourcesFlame != null)
                catalog.spells.Add(resourcesFlame);
            EditorUtility.SetDirty(catalog);
        }

        static DragonianSpellDefinition SyncResourcesSpellCopy(
            DragonianSpellDefinition source,
            string fileName)
        {
            if (source == null)
                return null;

            string path = $"{ResourcesCatalogFolder}/{fileName}";
            var copy = LoadOrCreate<DragonianSpellDefinition>(path);
            copy.spellId = source.spellId;
            copy.displayName = source.displayName;
            copy.description = source.description;
            copy.memorizeCost = source.memorizeCost;
            copy.soulPowerCastCost = source.soulPowerCastCost;
            copy.ability = source.ability;
            EditorUtility.SetDirty(copy);
            return copy;
        }

        static void WireDragonianPlayerPartyMemberId()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DragonianPlayerPath);
            if (prefab == null)
                return;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            PartyMemberId marker = instance.GetComponent<PartyMemberId>();
            if (marker == null)
                marker = instance.AddComponent<PartyMemberId>();

            marker.ConfigureMemberId("DragonianPlayer");
            EditorUtility.SetDirty(marker);
            PrefabUtility.SaveAsPrefabAsset(instance, DragonianPlayerPath);
            Object.DestroyImmediate(instance);
        }

        static ItemData CreateEmberScaleItem()
        {
            string path = "Assets/Resources/Item/Quest/EmberScale.asset";
            var item = LoadOrCreate<ItemData>(path);
            item.itemName = "Ember Scale";
            item.category = ItemCategory.QuestItem;
            item.weight = 0.1f;
            item.buyValue = 3;
            item.sellValue = 1;
            EditorUtility.SetDirty(item);
            return item;
        }

        static QuestDefinition CreateVolscaleQuest01(ItemData emberScale, DragonianSpellDefinition rewardSpell)
        {
            string path = $"{ResourcesQuestFolder}/quest_dragonian_volscale_01.asset";
            var quest = LoadOrCreate<QuestDefinition>(path);
            quest.questId = "quest_dragonian_volscale_01";
            quest.displayTitle = "Gather Ember Scales";
            quest.journalDescription =
                "Elder Volscale wants proof you can survive the dungeon. Skeletons sometimes shed "
                + "Ember Scales when slain — pick them up from the floor and keep two in your own "
                + "inventory (not a party member's bag). Return to Elder Volscale in town when "
                + "ready to learn Draconic Surge.";
            quest.giverNpcId = DragonianElderIds.VolscaleNpcId;
            quest.giverDisplayName = "Elder Volscale";
            quest.ownership = QuestOwnership.PerPartyMember;
            quest.requiredMinLevel = 1;
            quest.requiredRace = Race.Dragonian;
            quest.learnDragonianSpellId = rewardSpell != null ? rewardSpell.spellId : "dragonian_spell_sudden_strength";
            quest.acceptPrerequisites = System.Array.Empty<QuestPrerequisite>();
            quest.objectives = new[]
            {
                new QuestObjectiveDefinition
                {
                    objectiveId = "collect_ember_scales",
                    journalText = "Collect Ember Scales",
                    kind = QuestObjectiveKind.CollectItem,
                    item = emberScale,
                    itemQuantity = 2,
                },
            };
            quest.autoCompleteOnObjectives = false;
            quest.sortOrder = 10;
            EditorUtility.SetDirty(quest);
            return quest;
        }

        static QuestDefinition CreateVolscaleQuest02(DragonianSpellDefinition rewardSpell)
        {
            string path = $"{ResourcesQuestFolder}/quest_dragonian_volscale_02.asset";
            var quest = LoadOrCreate<QuestDefinition>(path);
            quest.questId = "quest_dragonian_volscale_02";
            quest.displayTitle = "Trial of Flame";
            quest.journalDescription =
                "Your second lesson with Elder Volscale. Slay five skeletons in the dungeon with "
                + "this Dragonian as the active killer. When the trial is complete, return to "
                + "Elder Volscale in town to seal Dragon Flame into your spirit. Requires level 3.";
            quest.giverNpcId = DragonianElderIds.VolscaleNpcId;
            quest.giverDisplayName = "Elder Volscale";
            quest.ownership = QuestOwnership.PerPartyMember;
            quest.requiredMinLevel = 3;
            quest.requiredRace = Race.Dragonian;
            quest.learnDragonianSpellId = rewardSpell != null ? rewardSpell.spellId : "dragonian_spell_fireball";
            quest.acceptPrerequisites = System.Array.Empty<QuestPrerequisite>();
            quest.objectives = new[]
            {
                new QuestObjectiveDefinition
                {
                    objectiveId = "kill_skeletons",
                    journalText = "Slay skeletons",
                    kind = QuestObjectiveKind.KillSpecies,
                    speciesId = "skeleton",
                    killCount = 5,
                },
            };
            quest.autoCompleteOnObjectives = false;
            quest.sortOrder = 11;
            EditorUtility.SetDirty(quest);
            return quest;
        }

        static DragonianElderDefinition CreateVolscaleElder(QuestDefinition quest01, QuestDefinition quest02)
        {
            string resourcesPath = $"{ResourcesCatalogFolder}/Elder_Volscale.asset";
            string dataPath = $"{DataDragonianFolder}/Elder_Volscale.asset";
            var elder = LoadOrCreate<DragonianElderDefinition>(resourcesPath);
            elder.elderId = "dragonian_elder_volscale";
            elder.displayName = "Elder Volscale";
            elder.description = "Teaches foundational draconic word-forms through personal trials.";
            elder.npcId = DragonianElderIds.VolscaleNpcId;
            elder.chainQuestIds = new[]
            {
                quest01 != null ? quest01.ResolvedQuestId : "quest_dragonian_volscale_01",
                quest02 != null ? quest02.ResolvedQuestId : "quest_dragonian_volscale_02",
            };
            elder.unlockStoryFlags = System.Array.Empty<string>();
            EditorUtility.SetDirty(elder);

            DragonianElderDefinition dataCopy = LoadOrCreate<DragonianElderDefinition>(dataPath);
            dataCopy.elderId = elder.elderId;
            dataCopy.displayName = elder.displayName;
            dataCopy.description = elder.description;
            dataCopy.npcId = elder.npcId;
            dataCopy.chainQuestIds = elder.chainQuestIds;
            dataCopy.unlockStoryFlags = elder.unlockStoryFlags;
            EditorUtility.SetDirty(dataCopy);

            return elder;
        }

        static PortraitDefinition CreatePortrait(string assetName, string texturePath)
        {
            string path = $"{ResourcesPortraitsFolder}/{assetName}.asset";
            PortraitDefinition portrait = LoadOrCreate<PortraitDefinition>(path);
            portrait.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            EditorUtility.SetDirty(portrait);
            return portrait;
        }

        static GameObject CreateDragonianNpcBasePrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(DragonianNpcPath);
            if (existing != null)
                return existing;

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(DragonianPlayerPath);
            if (source == null)
                throw new FileNotFoundException($"Missing {DragonianPlayerPath}");

            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            instance.name = "DragonianNpc";
            instance.tag = "Untagged";

            Object.DestroyImmediate(instance.GetComponent<PlayerController>(), true);
            instance.AddComponent<NpcController>();

            DestroyIfPresent<InventoryManager>(instance);
            DestroyIfPresent<InventoryCollector>(instance);
            DestroyIfPresent<EquipmentManager>(instance);
            DestroyIfPresent<RacialLoadoutApplier>(instance);
            DestroyIfPresent<DragonianSpellsRuntime>(instance);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, DragonianNpcPath);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        static void CreateVolscaleTownNpcPrefab(
            PortraitDefinition portrait,
            GameObject dragonianNpcBase,
            DragonianElderDefinition elder)
        {
            string path = $"{ResourcesNpcFolder}/TownNpc_DragonianElderVolscale.prefab";
            GameObject instance = PrefabUtility.InstantiatePrefab(dragonianNpcBase) as GameObject;
            instance.name = "TownNpc_DragonianElderVolscale";

            Object.DestroyImmediate(instance.GetComponent<NpcController>(), true);
            DragonianElderNpcController controller = instance.AddComponent<DragonianElderNpcController>();

            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("elderDefinition").objectReferenceValue = elder;
            controllerSo.FindProperty("npcId").stringValue = DragonianElderIds.VolscaleNpcId;
            controllerSo.FindProperty("portrait").objectReferenceValue = portrait;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject actorSo = new SerializedObject(controller);
            actorSo.FindProperty("displayName").stringValue = "Elder Volscale";
            actorSo.ApplyModifiedPropertiesWithoutUndo();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ElderSpritePath);
            SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
            if (renderer != null && sprite != null)
                renderer.sprite = sprite;

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        static void WireEmberScaleLoot(ItemData emberScale)
        {
            if (emberScale == null)
                return;

            const string lootTablePath = "Assets/Data/Enemy/Loot/EnemyLootTable_Skeleton.asset";
            var lootTable = AssetDatabase.LoadAssetAtPath<JRogue.Data.Enemy.EnemyLootTable>(lootTablePath);
            if (lootTable?.entries == null)
                return;

            for (int i = 0; i < lootTable.entries.Count; i++)
            {
                JRogue.Data.Enemy.LootTableEntry entry = lootTable.entries[i];
                if (entry?.payload == JRogue.Data.Enemy.LootTablePayload.ItemData
                    && entry.itemData == emberScale)
                {
                    return;
                }
            }

            lootTable.entries.Add(new JRogue.Data.Enemy.LootTableEntry
            {
                dropChance = 0.4f,
                payload = JRogue.Data.Enemy.LootTablePayload.ItemData,
                itemData = emberScale,
                quantity = 1,
            });
            EditorUtility.SetDirty(lootTable);
        }

        static void WireGretaShopEmberScales(ItemData emberScale)
        {
            if (emberScale == null)
                return;

            const string shopPath = "Assets/Resources/Shop/ShopNpc_Greta.asset";
            var shop = AssetDatabase.LoadAssetAtPath<JRogue.Shop.ShopNpcDefinition>(shopPath);
            if (shop == null)
                return;

            emberScale.buyValue = 3;
            emberScale.sellValue = 1;
            EditorUtility.SetDirty(emberScale);

            var stock = shop.initialStock != null
                ? new System.Collections.Generic.List<JRogue.Shop.ShopStockEntry>(shop.initialStock)
                : new System.Collections.Generic.List<JRogue.Shop.ShopStockEntry>();

            for (int i = stock.Count - 1; i >= 0; i--)
            {
                if (stock[i]?.item == emberScale)
                    stock.RemoveAt(i);
            }

            stock.Add(new JRogue.Shop.ShopStockEntry { item = emberScale, quantity = 5 });
            shop.initialStock = stock.ToArray();
            EditorUtility.SetDirty(shop);
        }

        static void UpdateTownStampMarker()
        {
            var stamp = AssetDatabase.LoadAssetAtPath<DungeonLayoutStamp>(StampPath);
            if (stamp == null)
                return;

            stamp.SetMarker(StampMarkerIds.DragonianElderVolscale, new Vector3Int(6, 5, 0));
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

        static void DestroyIfPresent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component != null)
                Object.DestroyImmediate(component, true);
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null)
                AssetDatabase.DeleteAsset(path);

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
#endif
