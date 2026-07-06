#if UNITY_EDITOR
using System.IO;
using JRogue.Item.Essence;
using JRogue.Party.Recruitment;
using JRogue.World.Generation;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.Party
{
  public static class PartyRecruitCatalogCreator
  {
    const string CatalogPath = "Assets/Resources/Party/PartyRecruitCatalog.asset";

    const string GoblinEssencePath = "Assets/Resources/Item/Essence/Production/GoblinEssence.asset";
    const string GhoulEssencePath = "Assets/Resources/Item/Essence/Production/GhoulEssence.asset";
    const string DireWolfEssencePath = "Assets/Resources/Item/Essence/Production/DireWolfEssence.asset";

    [MenuItem("JRogue/Party/Create Recruit Catalog")]
    public static void CreateOrUpdateCatalog()
    {
      Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath)!);

      PartyRecruitCatalog catalog = AssetDatabase.LoadAssetAtPath<PartyRecruitCatalog>(CatalogPath);
      if (catalog == null)
      {
        catalog = ScriptableObject.CreateInstance<PartyRecruitCatalog>();
        AssetDatabase.CreateAsset(catalog, CatalogPath);
      }

      EssenceData goblin = LoadEssence(GoblinEssencePath);
      EssenceData ghoul = LoadEssence(GhoulEssencePath);
      EssenceData direWolf = LoadEssence(DireWolfEssencePath);
      EssenceData[] rankEightEssences = { goblin, ghoul, direWolf };

      SerializedObject serializedCatalog = new SerializedObject(catalog);
      SerializedProperty entries = serializedCatalog.FindProperty("entries");
      entries.arraySize = 5;

      SetEntry(entries, 0, "guild_recruit_09_human", "Human Adventurer",
        PartyCompositionPresets.HumanPrefabPath, 9, null);
      SetEntry(entries, 1, "guild_recruit_09_elf", "Elf Adventurer",
        PartyCompositionPresets.ElfPrefabPath, 9, null);
      SetEntry(entries, 2, "guild_recruit_09_barbarian", "Barbarian Adventurer",
        PartyCompositionPresets.BarbarianPrefabPath, 9, null);
      SetEntry(entries, 3, "guild_recruit_08_human", "Human Adventurer",
        PartyCompositionPresets.HumanPrefabPath, 8, rankEightEssences);
      SetEntry(entries, 4, "guild_recruit_08_elf", "Elf Adventurer",
        PartyCompositionPresets.ElfPrefabPath, 8, rankEightEssences);

      serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
      EditorUtility.SetDirty(catalog);
      AssetDatabase.SaveAssets();
      Debug.Log($"[PartyRecruit] Updated catalog at {CatalogPath}.");
    }

    static void SetEntry(
      SerializedProperty entries,
      int index,
      string recruitId,
      string displayName,
      string prefabPath,
      int guildRank,
      EssenceData[] essences)
    {
      SerializedProperty entry = entries.GetArrayElementAtIndex(index);
      entry.FindPropertyRelative("recruitId").stringValue = recruitId;
      entry.FindPropertyRelative("displayName").stringValue = displayName;
      entry.FindPropertyRelative("actorPrefab").objectReferenceValue =
        AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
      entry.FindPropertyRelative("guildRank").intValue = guildRank;

      SerializedProperty essenceArray = entry.FindPropertyRelative("essences");
      if (essences == null || essences.Length == 0)
      {
        essenceArray.arraySize = 0;
        return;
      }

      essenceArray.arraySize = essences.Length;
      for (int i = 0; i < essences.Length; i++)
        essenceArray.GetArrayElementAtIndex(i).objectReferenceValue = essences[i];
    }

    static EssenceData LoadEssence(string path) =>
      AssetDatabase.LoadAssetAtPath<EssenceData>(path);
  }
}
#endif
