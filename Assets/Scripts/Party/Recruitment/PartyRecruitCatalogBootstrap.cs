using JRogue.Item.Essence;
using JRogue.World.Generation;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JRogue.Party.Recruitment
{
  static class PartyRecruitCatalogBootstrap
  {
    const string GoblinEssenceResourcePath = "Item/Essence/Production/GoblinEssence";
    const string GhoulEssenceResourcePath = "Item/Essence/Production/GhoulEssence";
    const string DireWolfEssenceResourcePath = "Item/Essence/Production/DireWolfEssence";

    static PartyRecruitCatalog _runtimeInstance;

    public static PartyRecruitCatalog RuntimeInstance
    {
      get
      {
        if (_runtimeInstance == null)
          _runtimeInstance = BuildRuntimeCatalog();
        return _runtimeInstance;
      }
    }

    static PartyRecruitCatalog BuildRuntimeCatalog()
    {
      var catalog = ScriptableObject.CreateInstance<PartyRecruitCatalog>();
      EssenceData[] rankEightEssences =
      {
        LoadEssence(GoblinEssenceResourcePath, "Assets/Resources/Item/Essence/Production/GoblinEssence.asset"),
        LoadEssence(GhoulEssenceResourcePath, "Assets/Resources/Item/Essence/Production/GhoulEssence.asset"),
        LoadEssence(DireWolfEssenceResourcePath, "Assets/Resources/Item/Essence/Production/DireWolfEssence.asset"),
      };

      catalog.ConfigureEntriesForTests(new[]
      {
        CreateEntry(
          "guild_recruit_09_human",
          "Human Adventurer",
          PartyCompositionPresets.HumanPrefabPath,
          9,
          null),
        CreateEntry(
          "guild_recruit_09_elf",
          "Elf Adventurer",
          PartyCompositionPresets.ElfPrefabPath,
          9,
          null),
        CreateEntry(
          "guild_recruit_09_barbarian",
          "Barbarian Adventurer",
          PartyCompositionPresets.BarbarianPrefabPath,
          9,
          null),
        CreateEntry(
          "guild_recruit_08_human",
          "Human Adventurer",
          PartyCompositionPresets.HumanPrefabPath,
          8,
          rankEightEssences),
        CreateEntry(
          "guild_recruit_08_elf",
          "Elf Adventurer",
          PartyCompositionPresets.ElfPrefabPath,
          8,
          rankEightEssences),
      });

      return catalog;
    }

    static PartyRecruitDefinition CreateEntry(
      string recruitId,
      string displayName,
      string editorPrefabPath,
      int guildRank,
      EssenceData[] essences) =>
      new()
      {
        recruitId = recruitId,
        displayName = displayName,
        actorPrefab = LoadPrefab(editorPrefabPath),
        guildRank = guildRank,
        essences = essences ?? System.Array.Empty<EssenceData>(),
      };

    static GameObject LoadPrefab(string editorPath)
    {
#if UNITY_EDITOR
      GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(editorPath);
      if (prefab != null)
        return prefab;
#endif
      Debug.LogWarning($"[PartyRecruit] Missing prefab at {editorPath}. Create PartyRecruitCatalog via JRogue/Party/Create Recruit Catalog.");
      return null;
    }

    static EssenceData LoadEssence(string resourcePath, string editorPath)
    {
      EssenceData essence = Resources.Load<EssenceData>(resourcePath);
#if UNITY_EDITOR
      if (essence == null)
        essence = AssetDatabase.LoadAssetAtPath<EssenceData>(editorPath);
#endif
      return essence;
    }
  }
}
