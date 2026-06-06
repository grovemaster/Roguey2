#if UNITY_EDITOR
using System.IO;
using JRogue.Controller.Npc;
using JRogue.Controller.Player;
using JRogue.Dialog;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.World.Generation;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace JRogue.Editor.World
{
    public static class NpcDialogPackCreator
    {
        const string HumanPlayerPrefabPath = "Assets/Prefabs/Actor/Race/HumanPlayer.prefab";
        const string HumanNpcPrefabPath = "Assets/Prefabs/Actor/Npc/HumanNpc.prefab";
        const string StampPath = "Assets/Resources/Town/Stamp_TownPlaza_20x20.asset";
        const string ResourcesNpcFolder = "Assets/Resources/Town/Npc";
        const string ResourcesDialogFolder = "Assets/Resources/Dialog";
        const string ResourcesProfilesFolder = "Assets/Resources/Dialog/Profiles";
        const string ResourcesPortraitsFolder = "Assets/Resources/Dialog/Portraits";

        [MenuItem("JRogue/Town/Create NPC Dialog Pack")]
        public static void CreateNpcDialogPack()
        {
            EnsureFolders();
            ConfigureSpriteImports();
            AssetDatabase.Refresh();

            PartyRacePortraitCatalog catalog = CreatePartyRacePortraitCatalog();
            PortraitDefinition miraPortrait = CreatePortrait("Portrait_Mira", "Assets/Art/Portraits/NPC/Portrait_Mira.png");
            PortraitDefinition lucPortrait = CreatePortrait("Portrait_Luc", "Assets/Art/Portraits/NPC/Portrait_Luc.png");
            PortraitDefinition eddaPortrait = CreatePortrait("Portrait_Edda", "Assets/Art/Portraits/NPC/Portrait_Edda.png");

            NpcDialogProfile miraProfile = CreateMiraProfile();
            NpcDialogProfile lucProfile = CreateLucProfile();
            NpcDialogProfile eddaProfile = CreateEddaProfile();

            GameObject humanNpcBase = CreateHumanNpcBasePrefab();
            CreateTownNpcPrefab("TownNpc_Mira", "Mira", TownNpcIds.Npc1,
                "Assets/Art/NPC/Sprites/NPC_Mira.png", miraPortrait, miraProfile, humanNpcBase);
            CreateTownNpcPrefab("TownNpc_Luc", "Luc", TownNpcIds.Npc2,
                "Assets/Art/NPC/Sprites/NPC_Luc.png", lucPortrait, lucProfile, humanNpcBase);
            CreateTownNpcPrefab("TownNpc_Edda", "Edda", TownNpcIds.Npc3,
                "Assets/Art/NPC/Sprites/NPC_Edda.png", eddaPortrait, eddaProfile, humanNpcBase);

            UpdateTownStampMarkers();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NpcDialog] Created NPC dialog pack. Run TownTest and press Play.");
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory(ResourcesNpcFolder);
            Directory.CreateDirectory(ResourcesDialogFolder);
            Directory.CreateDirectory(ResourcesProfilesFolder);
            Directory.CreateDirectory(ResourcesPortraitsFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(HumanNpcPrefabPath)!);
        }

        static void ConfigureSpriteImports()
        {
            ConfigureTexture("Assets/Art/NPC/Sprites/NPC_Mira.png", 32, FilterMode.Point);
            ConfigureTexture("Assets/Art/NPC/Sprites/NPC_Luc.png", 32, FilterMode.Point);
            ConfigureTexture("Assets/Art/NPC/Sprites/NPC_Edda.png", 32, FilterMode.Point);
            ConfigureTexture("Assets/Art/Portraits/NPC/Portrait_Mira.png", 128, FilterMode.Point);
            ConfigureTexture("Assets/Art/Portraits/NPC/Portrait_Luc.png", 128, FilterMode.Point);
            ConfigureTexture("Assets/Art/Portraits/NPC/Portrait_Edda.png", 128, FilterMode.Point);
            ConfigureTexture("Assets/Art/Portraits/Party/Race/Portrait_Human.png", 128, FilterMode.Point);
            ConfigureTexture("Assets/Art/Portraits/Party/Race/Portrait_Barbarian.png", 128, FilterMode.Point);
            ConfigureTexture("Assets/Art/Portraits/Party/Race/Portrait_Elf.png", 128, FilterMode.Point);
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

        static PartyRacePortraitCatalog CreatePartyRacePortraitCatalog()
        {
            string path = $"{ResourcesDialogFolder}/PartyRacePortraitCatalog.asset";
            var catalog = LoadOrCreate<PartyRacePortraitCatalog>(path);
            catalog.fallbackPortrait = CreatePortrait("Portrait_Fallback", "Assets/Art/Portraits/Party/Race/Portrait_Human.png");
            catalog.racePortraits = new[]
            {
                new PartyRacePortraitCatalog.RacePortraitEntry
                {
                    race = Race.Human,
                    portrait = CreatePortrait("Portrait_Race_Human", "Assets/Art/Portraits/Party/Race/Portrait_Human.png"),
                },
                new PartyRacePortraitCatalog.RacePortraitEntry
                {
                    race = Race.Barbarian,
                    portrait = CreatePortrait("Portrait_Race_Barbarian", "Assets/Art/Portraits/Party/Race/Portrait_Barbarian.png"),
                },
                new PartyRacePortraitCatalog.RacePortraitEntry
                {
                    race = Race.Elf,
                    portrait = CreatePortrait("Portrait_Race_Elf", "Assets/Art/Portraits/Party/Race/Portrait_Elf.png"),
                },
            };
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        static PortraitDefinition CreatePortrait(string assetName, string texturePath)
        {
            string path = $"{ResourcesPortraitsFolder}/{assetName}.asset";
            var portrait = LoadOrCreate<PortraitDefinition>(path);
            portrait.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            EditorUtility.SetDirty(portrait);
            return portrait;
        }

        static NpcDialogProfile CreateMiraProfile()
        {
            var profile = LoadOrCreate<NpcDialogProfile>($"{ResourcesProfilesFolder}/NpcDialog_Mira.asset");
            profile.npcId = TownNpcIds.Npc1;
            profile.completionFlagId = TownNpcStoryFlags.TalkedNpc1;
            profile.incrementTalkCountOnStart = true;
            profile.rootNodeIndex = 0;
            profile.nodes = new[]
            {
                new DialogNodeData
                {
                    kind = DialogNodeKind.Conditional,
                    conditionKind = DialogConditionKind.NpcTalkCount,
                    npcIdForTalkCount = TownNpcIds.Npc1,
                    talkCountMin = 0,
                    talkCountMax = 0,
                    trueNodeIndex = 1,
                    falseNodeIndex = 2,
                },
                LineNode("My name is {npcName}. Hello, {partyName}."),
                LineNode("Hello again. My name is {npcName}."),
            };
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static NpcDialogProfile CreateLucProfile()
        {
            var profile = LoadOrCreate<NpcDialogProfile>($"{ResourcesProfilesFolder}/NpcDialog_Luc.asset");
            profile.npcId = TownNpcIds.Npc2;
            profile.completionFlagId = TownNpcStoryFlags.TalkedNpc2;
            profile.incrementTalkCountOnStart = false;
            profile.rootNodeIndex = 0;
            profile.nodes = new[]
            {
                new DialogNodeData
                {
                    kind = DialogNodeKind.Choice,
                    line = new DialogLineData { textTemplate = "Do you prefer Hello or Bonjour?" },
                    choices = new[]
                    {
                        new DialogChoiceOptionData { label = "Hello", responseNodeIndex = 1 },
                        new DialogChoiceOptionData { label = "Bonjour", responseNodeIndex = 2 },
                    },
                },
                LineNode("Then hello to you."),
                LineNode("Then bonjour to you sir."),
            };
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static NpcDialogProfile CreateEddaProfile()
        {
            var profile = LoadOrCreate<NpcDialogProfile>($"{ResourcesProfilesFolder}/NpcDialog_Edda.asset");
            profile.npcId = TownNpcIds.Npc3;
            profile.completionFlagId = string.Empty;
            profile.incrementTalkCountOnStart = false;
            profile.rootNodeIndex = 0;
            profile.nodes = new[]
            {
                new DialogNodeData
                {
                    kind = DialogNodeKind.Conditional,
                    conditionKind = DialogConditionKind.AnyNpcTalked,
                    anyTalkedNpcIds = new[] { TownNpcStoryFlags.TalkedNpc1, TownNpcStoryFlags.TalkedNpc2 },
                    trueNodeIndex = 1,
                    falseNodeIndex = 2,
                },
                LineNode("Greetings."),
                LineNode("Hello World."),
            };
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static DialogNodeData LineNode(string text) =>
            new DialogNodeData
            {
                kind = DialogNodeKind.Line,
                line = new DialogLineData { textTemplate = text },
            };

        static GameObject CreateHumanNpcBasePrefab()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(HumanPlayerPrefabPath);
            if (source == null)
                throw new FileNotFoundException($"Missing {HumanPlayerPrefabPath}");

            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            instance.name = "HumanNpc";
            instance.tag = "Untagged";

            Object.DestroyImmediate(instance.GetComponent<PlayerController>(), true);
            instance.AddComponent<NpcController>();

            DestroyIfPresent<InventoryManager>(instance);
            DestroyIfPresent<InventoryCollector>(instance);
            DestroyIfPresent<EquipmentManager>(instance);
            DestroyIfPresent<RacialLoadoutApplier>(instance);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, HumanNpcPrefabPath);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        static void CreateTownNpcPrefab(
            string prefabName,
            string displayName,
            string npcId,
            string spritePath,
            PortraitDefinition portrait,
            NpcDialogProfile profile,
            GameObject humanNpcBase)
        {
            string path = $"{ResourcesNpcFolder}/{prefabName}.prefab";
            GameObject instance = PrefabUtility.InstantiatePrefab(humanNpcBase) as GameObject;
            instance.name = prefabName;

            NpcController npc = instance.GetComponent<NpcController>();
            SerializedObject npcSo = new SerializedObject(npc);
            npcSo.FindProperty("npcId").stringValue = npcId;
            npcSo.FindProperty("dialogProfile").objectReferenceValue = profile;
            npcSo.FindProperty("portrait").objectReferenceValue = portrait;
            npcSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject actorSo = new SerializedObject(npc);
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
            {
                Debug.LogWarning($"[NpcDialog] Missing stamp at {StampPath}.");
                return;
            }

            stamp.SetMarker(StampMarkerIds.TownNpc1, new Vector3Int(4, 8, 0));
            stamp.SetMarker(StampMarkerIds.TownNpc2, new Vector3Int(6, 8, 0));
            stamp.SetMarker(StampMarkerIds.TownNpc3, new Vector3Int(8, 8, 0));
            EditorUtility.SetDirty(stamp);
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

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }

    [InitializeOnLoad]
    static class NpcDialogPackAutoCreator
    {
        static NpcDialogPackAutoCreator()
        {
            EditorApplication.delayCall += TryCreatePack;
        }

        static void TryCreatePack()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!NeedsProfileRebuild())
                return;

            NpcDialogPackCreator.CreateNpcDialogPack();
        }

        static bool NeedsProfileRebuild()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Resources/Town/Npc/TownNpc_Mira.prefab") == null)
                return true;

            var profile = AssetDatabase.LoadAssetAtPath<NpcDialogProfile>(
                "Assets/Resources/Dialog/Profiles/NpcDialog_Mira.asset");
            if (profile == null)
                return true;

            return profile.nodes == null
                   || profile.nodes.Length == 0
                   || profile.rootNodeIndex < 0;
        }
    }
}
#endif
