#if UNITY_EDITOR
using System.IO;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Quest;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.World.Generation;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    public static class KnightDrillMasterPackCreator
    {
        const string HumanNpcPrefabPath = "Assets/Prefabs/Actor/Npc/HumanNpc.prefab";
        const string HumanPlayerPath = "Assets/Prefabs/Actor/Race/HumanPlayer.prefab";
        const string KnightTreePath = "Assets/Data/Racial/Human/KnightSkillTree_Sample.asset";
        const string ResourcesNpcFolder = "Assets/Resources/Town/Npc";
        const string ResourcesQuestFolder = "Assets/Resources/Quest";
        const string ResourcesPortraitsFolder = "Assets/Resources/Dialog/Portraits";
        const string DrillSpritePath = "Assets/Art/NPC/Sprites/NPC_KnightDrillMaster.png";
        const string DrillPortraitPath = "Assets/Art/Portraits/NPC/Portrait_KnightDrillMaster.png";

        [MenuItem("JRogue/Racial/Create Human Knight Drill Master Pack")]
        public static void CreateHumanKnightDrillMasterPack()
        {
            EnsureFolders();
            CreatePlaceholderSprite(DrillSpritePath, new Color(0.72f, 0.58f, 0.22f));
            CreatePlaceholderPortrait(DrillPortraitPath, new Color(0.72f, 0.58f, 0.22f));
            AssetDatabase.Refresh();

            ConfigureTexture(DrillSpritePath, 32, FilterMode.Point);
            ConfigureTexture(DrillPortraitPath, 128, FilterMode.Point);

            PortraitDefinition portrait = CreatePortrait("Portrait_KnightDrillMaster", DrillPortraitPath);
            CreateApprenticeshipQuest();

            GameObject humanNpc = AssetDatabase.LoadAssetAtPath<GameObject>(HumanNpcPrefabPath);
            if (humanNpc == null)
            {
                Debug.LogError($"[KnightDrill] Missing base NPC prefab at {HumanNpcPrefabPath}.");
                return;
            }

            CreateDrillMasterNpcPrefab(portrait, humanNpc);
            EnsureHumanPlayerKnightTree();
            UpdateTownStampMarkers();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[KnightDrill] Human Knight drill master pack created.");
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Art/NPC/Sprites");
            Directory.CreateDirectory("Assets/Art/Portraits/NPC");
            Directory.CreateDirectory(ResourcesNpcFolder);
            Directory.CreateDirectory(ResourcesQuestFolder);
            Directory.CreateDirectory(ResourcesPortraitsFolder);
        }

        static QuestDefinition CreateApprenticeshipQuest()
        {
            string path = $"{ResourcesQuestFolder}/quest_knight_drill_apprenticeship.asset";
            var quest = LoadOrCreate<QuestDefinition>(path);
            quest.questId = HumanKnightDrillMasterIds.ApprenticeshipQuestId;
            quest.displayTitle = "Drill Apprenticeship";
            quest.journalDescription = "Pay the Drill Master 5 gold to swear the Knight's oath.";
            quest.giverNpcId = HumanKnightDrillMasterIds.DrillMasterNpcId;
            quest.giverDisplayName = "Drill Master";
            quest.ownership = QuestOwnership.PerPartyMember;
            quest.requiredMinLevel = 0;
            quest.requiredRace = Race.Human;
            quest.requiresHumanClassNone = true;
            quest.requiresNoConsumedEssences = false;
            quest.turnInGoldCost = HumanKnightClassCommitService.DrillGoldCost;
            quest.commitHumanClass = HumanClass.Knight;
            quest.learnDragonianSpellId = null;
            quest.acceptPrerequisites = System.Array.Empty<QuestPrerequisite>();
            quest.objectives = System.Array.Empty<QuestObjectiveDefinition>();
            quest.autoCompleteOnObjectives = false;
            quest.sortOrder = 6;
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

        static void CreateDrillMasterNpcPrefab(PortraitDefinition portrait, GameObject humanNpcBase)
        {
            string path = $"{ResourcesNpcFolder}/TownNpc_KnightDrillMaster.prefab";
            NpcDialogPackCreator.DeletePrefabIfPresent(path);
            GameObject instance = PrefabUtility.InstantiatePrefab(humanNpcBase) as GameObject;
            instance.name = "TownNpc_KnightDrillMaster";

            Object.DestroyImmediate(instance.GetComponent<NpcController>(), true);
            HumanKnightDrillMasterNpcController controller =
                instance.AddComponent<HumanKnightDrillMasterNpcController>();

            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("npcId").stringValue = HumanKnightDrillMasterIds.DrillMasterNpcId;
            controllerSo.FindProperty("portrait").objectReferenceValue = portrait;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject actorSo = new SerializedObject(controller);
            actorSo.FindProperty("displayName").stringValue = "Drill Master";
            actorSo.ApplyModifiedPropertiesWithoutUndo();

            ApplySprite(instance, DrillSpritePath);
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        static void EnsureHumanPlayerKnightTree()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanPlayerPath);
            HumanClassSkillTreeDefinition tree = AssetDatabase.LoadAssetAtPath<HumanClassSkillTreeDefinition>(KnightTreePath);
            if (prefab == null || tree == null)
                return;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            HumanClassSkillTreeRuntime runtime = instance.GetComponent<HumanClassSkillTreeRuntime>();
            if (runtime == null)
                runtime = instance.AddComponent<HumanClassSkillTreeRuntime>();

            SerializedObject so = new SerializedObject(runtime);
            so.FindProperty("skillTree").objectReferenceValue = tree;
            so.FindProperty("skillPointsTotal").intValue = 10;
            so.ApplyModifiedPropertiesWithoutUndo();

            KnightSkillMasteryRuntime.EnsureOn(instance);
            KnightAuraStateRuntime.EnsureOn(instance);

            PrefabUtility.SaveAsPrefabAsset(instance, HumanPlayerPath);
            Object.DestroyImmediate(instance);
        }

        static void UpdateTownStampMarkers()
        {
            var stamp = AssetDatabase.LoadAssetAtPath<DungeonLayoutStamp>(TownPlazaMarkerLayout.StampPath);
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
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = filterMode;
            importer.SaveAndReimport();
        }

        static void ApplySprite(GameObject instance, string spritePath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
                return;

            var renderer = instance.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
                renderer.sprite = sprite;
        }
    }
}
#endif
