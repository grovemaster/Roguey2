#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.Controller.Npc;
using JRogue.Controller.Player;
using JRogue.Dialog;
using JRogue.Interactables;
using JRogue.Item;
using JRogue.Item.Essence;
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
    public static class StoneWardensClanPackCreator
    {
        const string DwarfPlayerPath = "Assets/Prefabs/Actor/Race/DwarfPlayer.prefab";
        const string AncestorsFolder = "Assets/Data/Racial/Dwarf/Ancestors";
        const string StoneMotherPath = "Assets/Data/Racial/Dwarf/Ancestors/StoneMother.asset";
        const string StoneMotherTreePath = "Assets/Data/Racial/Dwarf/Ancestors/StoneMotherTree.asset";
        const string ResourcesClanFolder = "Assets/Resources/Racial/Dwarf/Clans";
        const string ResourcesNpcFolder = "Assets/Resources/Town/Npc";
        const string ResourcesInteractablesFolder = "Assets/Resources/Interactables";
        const string ResourcesPortraitsFolder = "Assets/Resources/Dialog/Portraits";
        const string EffectsFolder = "Assets/Data/Interactables/Effects";
        const string ResourcesQuestFolder = "Assets/Resources/Quest";
        const string StewardSpritePath = "Assets/Art/NPC/Sprites/NPC_StoneWardensSteward.png";
        const string StewardPortraitPath = "Assets/Art/Portraits/NPC/Portrait_StoneWardensSteward.png";

        [MenuItem("JRogue/Racial/Create Stone Wardens Clan Pack")]
        public static void CreateStoneWardensClanPack()
        {
            EnsureFolders();
            CreatePlaceholderSprite(StewardSpritePath, new Color(0.58f, 0.62f, 0.68f));
            CreatePlaceholderPortrait(StewardPortraitPath, new Color(0.58f, 0.62f, 0.68f));
            AssetDatabase.Refresh();

            ConfigureTexture(StewardSpritePath, 32, FilterMode.Point);
            ConfigureTexture(StewardPortraitPath, 128, FilterMode.Point);

            SpiritImprintGraph tree = CreateStoneMotherTree();
            AncestorDefinition stoneMother = CreateStoneMotherAncestor(tree);
            DwarfClanDefinition clan = CreateClanDefinition(stoneMother);
            CreateDevotionQuest(
                DwarfClanIds.StoneWardensDevotionQuestId,
                DwarfClanIds.StoneWardensStewardNpcId,
                DwarfClanIds.StoneWardensClanId,
                "Stone Wardens Devotion",
                "Report to the Stone Wardens steward to seal a simple act of clan service.");
            CreateAltarAssets(clan);
            PortraitDefinition portrait = CreatePortrait("Portrait_StoneWardensSteward", StewardPortraitPath);
            CreateStewardNpcPrefab(clan, portrait);
            TownPlazaMarkerLayout.ApplyAll(
                AssetDatabase.LoadAssetAtPath<DungeonLayoutStamp>(TownPlazaMarkerLayout.StampPath));
            var stamp = AssetDatabase.LoadAssetAtPath<DungeonLayoutStamp>(TownPlazaMarkerLayout.StampPath);
            if (!TownPlazaMarkerLayout.ValidateMarkersOnFloor(stamp, out string floorError))
                Debug.LogWarning($"[StoneWardens] {floorError}");
            EditorUtility.SetDirty(stamp);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[StoneWardens] Created clan with exclusive branch (Mountain Fist / Earth Sight), "
                + "Hall altar (plaza cell 17,5), and steward NPC (cell 17,6). "
                + "Run JRogue/Town/Fix TownTest Scene, then play with a Dwarf party member.");
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory(AncestorsFolder);
            Directory.CreateDirectory("Assets/Art/NPC/Sprites");
            Directory.CreateDirectory("Assets/Art/Portraits/NPC");
            Directory.CreateDirectory(ResourcesClanFolder);
            Directory.CreateDirectory(ResourcesNpcFolder);
            Directory.CreateDirectory(ResourcesInteractablesFolder);
            Directory.CreateDirectory(ResourcesPortraitsFolder);
            Directory.CreateDirectory(ResourcesQuestFolder);
            Directory.CreateDirectory(EffectsFolder);
        }

        static void CreateDevotionQuest(
            string questId,
            string stewardNpcId,
            string clanId,
            string title,
            string journalDescription)
        {
            string path = $"{ResourcesQuestFolder}/{questId}.asset";
            var quest = LoadOrCreate<QuestDefinition>(path);
            quest.questId = questId;
            quest.displayTitle = title;
            quest.journalDescription = journalDescription;
            quest.giverNpcId = stewardNpcId;
            quest.giverDisplayName = title;
            quest.ownership = QuestOwnership.PartyShared;
            quest.requiredMinLevel = 1;
            quest.requiredRace = Race.Dwarf;
            quest.turnInGoldCost = 0;
            quest.acceptPrerequisites = System.Array.Empty<QuestPrerequisite>();
            quest.objectives = System.Array.Empty<QuestObjectiveDefinition>();
            quest.autoCompleteOnObjectives = false;
            quest.rewards = new QuestRewardBundle
            {
                clanPrestige = 5,
                clanPrestigeClanId = clanId,
            };
            quest.sortOrder = 21;
            EditorUtility.SetDirty(quest);
        }

        static SpiritImprintGraph CreateStoneMotherTree()
        {
            SpiritImprintGraph graph = LoadOrCreate<SpiritImprintGraph>(StoneMotherTreePath);
            graph.rootNodeId = "ancestor_root";

            var root = new SpiritImprintNodeData
            {
                nodeId = "ancestor_root",
                displayName = "Patron acknowledged (dormant)",
                description = "Rank 0 — no gameplay payload.",
            };

            var mountainFist = new SpiritImprintNodeData
            {
                nodeId = "mountain_fist",
                displayName = "Mountain Fist",
                description = "+1 Strength from the Stone Mother's warrior mark.",
                parentNodeId = "ancestor_root",
                siblingExclusivityGroup = 1,
                requiredCharacterLevel = 1,
                requiredClanMemberRank = 0,
                requiredClanPrestige = 0,
                statModifiers = new List<AttributeModifier>
                {
                    new AttributeModifier { attribute = StatType.Strength, value = 1 },
                },
            };

            var earthSight = new SpiritImprintNodeData
            {
                nodeId = "earth_sight",
                displayName = "Earth Sight",
                description = "+1 Wisdom from the deep stone's sight.",
                parentNodeId = "ancestor_root",
                siblingExclusivityGroup = 1,
                requiredCharacterLevel = 1,
                requiredClanMemberRank = 0,
                requiredClanPrestige = 0,
                statModifiers = new List<AttributeModifier>
                {
                    new AttributeModifier { attribute = StatType.Wisdom, value = 1 },
                },
            };

            var graniteGuard = new SpiritImprintNodeData
            {
                nodeId = "granite_guard",
                displayName = "Granite Guard",
                description = "+1 Constitution from ancestral endurance.",
                parentNodeId = "mountain_fist",
                requiredCharacterLevel = 1,
                requiredClanMemberRank = 1,
                requiredClanPrestige = 10,
                statModifiers = new List<AttributeModifier>
                {
                    new AttributeModifier { attribute = StatType.Constitution, value = 1 },
                },
            };

            var stoneWhisper = new SpiritImprintNodeData
            {
                nodeId = "stone_whisper",
                displayName = "Stone Whisper",
                description = "+1 Intelligence from the mountain's counsel.",
                parentNodeId = "earth_sight",
                requiredCharacterLevel = 1,
                requiredClanMemberRank = 1,
                requiredClanPrestige = 10,
                statModifiers = new List<AttributeModifier>
                {
                    new AttributeModifier { attribute = StatType.Intelligence, value = 1 },
                },
            };

            graph.nodes = new List<SpiritImprintNodeData>
            {
                root,
                mountainFist,
                earthSight,
                graniteGuard,
                stoneWhisper,
            };

            EditorUtility.SetDirty(graph);
            return graph;
        }

        static AncestorDefinition CreateStoneMotherAncestor(SpiritImprintGraph tree)
        {
            AncestorDefinition patron = LoadOrCreate<AncestorDefinition>(StoneMotherPath);
            patron.ancestorId = "stone_mother";
            patron.displayName = "Stone Mother";
            patron.description =
                "Patron of wardens and delvers; her path splits between martial fist and earth sight.";
            patron.abilityTree = tree;
            EditorUtility.SetDirty(patron);
            return patron;
        }

        static DwarfClanDefinition CreateClanDefinition(AncestorDefinition patron)
        {
            string path = $"{ResourcesClanFolder}/DwarfClan_StoneWardens.asset";
            DwarfClanDefinition clan = LoadOrCreate<DwarfClanDefinition>(path);
            clan.clanId = DwarfClanIds.StoneWardensClanId;
            clan.displayName = "Stone Wardens";
            clan.shortName = "Stone Wardens";
            clan.description =
                "Delver-clan dwarves who venerate the Stone Mother. Their hall keeps the ancestral altar "
                + "where initiates choose between martial fist and earth sight.";
            clan.patronAncestor = patron;
            clan.startingPrestige = 5;
            clan.altarFlavorTitle = "The Stone Mother's ancestors await your offering.";
            EditorUtility.SetDirty(clan);
            return clan;
        }

        static void CreateAltarAssets(DwarfClanDefinition clan)
        {
            AlwaysTruePrecondition alwaysTrue = AssetDatabase.LoadAssetAtPath<AlwaysTruePrecondition>(
                "Assets/Data/Interactables/Preconditions/AlwaysTrue.asset");
            if (alwaysTrue == null)
            {
                alwaysTrue = ScriptableObject.CreateInstance<AlwaysTruePrecondition>();
                AssetDatabase.CreateAsset(alwaysTrue, "Assets/Data/Interactables/Preconditions/AlwaysTrue.asset");
            }

            DwarfHallAncestorLearnEffect effect = LoadOrCreate<DwarfHallAncestorLearnEffect>(
                $"{EffectsFolder}/DwarfHallAncestorLearn_StoneWardens.asset");
            effect.clan = clan;
            EditorUtility.SetDirty(effect);

            Sprite spriteOff = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Interactables/Sprites/LeverSwitch_Off.png");

            InteractableTileDefinition altar = LoadOrCreate<InteractableTileDefinition>(
                $"{ResourcesInteractablesFolder}/HallOfAncestorsAltar_StoneWardens.asset");
            altar.interactableId = InteractableTileId.HallOfAncestorsAltar;
            altar.displayName = "Hall of Ancestors Altar";
            altar.kind = InteractableTileKind.Shrine;
            altar.blocksOccupancy = true;
            altar.bumpEnabled = true;
            altar.allowRepeatActivation = true;
            altar.preconditions = new InteractablePrecondition[] { alwaysTrue };
            altar.onActivateEffects = new InteractableEffect[] { effect };
            altar.spriteOff = spriteOff;
            altar.spriteOn = spriteOff;
            EditorUtility.SetDirty(altar);
        }

        static void CreateStewardNpcPrefab(DwarfClanDefinition clan, PortraitDefinition portrait)
        {
            GameObject dwarfPlayer = AssetDatabase.LoadAssetAtPath<GameObject>(DwarfPlayerPath);
            if (dwarfPlayer == null)
            {
                Debug.LogError($"[StoneWardens] Missing {DwarfPlayerPath}.");
                return;
            }

            string path = $"{ResourcesNpcFolder}/TownNpc_StoneWardensSteward.prefab";
            NpcDialogPackCreator.DeletePrefabIfPresent(path);

            GameObject instance = PrefabUtility.InstantiatePrefab(dwarfPlayer) as GameObject;
            instance.name = "TownNpc_StoneWardensSteward";
            instance.tag = "Untagged";

            Object.DestroyImmediate(instance.GetComponent<PlayerController>(), true);
            Object.DestroyImmediate(instance.GetComponent<InventoryManager>(), true);
            Object.DestroyImmediate(instance.GetComponent<InventoryCollector>(), true);
            Object.DestroyImmediate(instance.GetComponent<EquipmentManager>(), true);

            DwarfClanStewardNpcController controller = instance.AddComponent<DwarfClanStewardNpcController>();
            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("npcId").stringValue = DwarfClanIds.StoneWardensStewardNpcId;
            controllerSo.FindProperty("portrait").objectReferenceValue = portrait;
            controllerSo.FindProperty("clan").objectReferenceValue = clan;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject actorSo = new SerializedObject(controller);
            actorSo.FindProperty("displayName").stringValue = "Stone Wardens Steward";
            actorSo.ApplyModifiedPropertiesWithoutUndo();

            CharacterStats stats = instance.GetComponent<CharacterStats>();
            if (stats != null)
            {
                stats.race = Race.Dwarf;
                stats.racialSubsystem = RacialSubsystemKind.DwarfAncestry;
            }

            ApplySprite(instance, StewardSpritePath);
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        static PortraitDefinition CreatePortrait(string assetName, string texturePath)
        {
            string path = $"{ResourcesPortraitsFolder}/{assetName}.asset";
            PortraitDefinition portrait = LoadOrCreate<PortraitDefinition>(path);
            portrait.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            EditorUtility.SetDirty(portrait);
            return portrait;
        }

        static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
                return existing;

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }

        static void ApplySprite(GameObject instance, string spritePath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
            if (renderer != null && sprite != null)
                renderer.sprite = sprite;
        }

        static void CreatePlaceholderSprite(string path, Color color)
        {
            if (File.Exists(path))
                return;

            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, color);
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void CreatePlaceholderPortrait(string path, Color color) =>
            CreatePlaceholderSprite(path, color);

        static void ConfigureTexture(string path, int pixelsPerUnit, FilterMode filter)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = filter;
            importer.SaveAndReimport();
        }
    }
}
#endif
