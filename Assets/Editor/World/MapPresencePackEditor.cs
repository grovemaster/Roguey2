#if UNITY_EDITOR
using JRogue.Controller.Enemy;
using JRogue.Traps;
using JRogue.World.MapPresence;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JRogue.Editor.World
{
  public static class MapPresencePackEditor
  {
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string DataRoot = "Assets/Data/World/MapPresence";
    const string EffectsRoot = DataRoot + "/Effects";
    const string ProfilePath = DataRoot + "/Profile_SkeletonPitTest.asset";
    const string EffectPath = EffectsRoot + "/TrapWhileAlive_SkeletonPit.asset";
    const string BearTrapPath = "Assets/Data/Traps/TrapDefinition_Bear.asset";
    const string EnemyPrefabPath = "Assets/Prefabs/Actor/Enemy/Enemy.prefab";

    static readonly Vector3Int PitCell = new Vector3Int(-1, -1, 0);
    static readonly Vector3Int SkeletonSpawnCell = new Vector3Int(-2, -1, 0);

    [MenuItem("JRogue/World/Create Map Presence v0 Assets")]
    public static void CreateV0Assets()
    {
      System.IO.Directory.CreateDirectory(
        System.IO.Path.Combine(UnityEngine.Application.dataPath, "Data/World/MapPresence/Effects"));

      TrapDefinition bear = AssetDatabase.LoadAssetAtPath<TrapDefinition>(BearTrapPath);
      if (bear == null)
      {
        Debug.LogError("[MapPresence] Run JRogue → Traps → Create QA Trap Asset Pack first.");
        return;
      }

      var trapEffect = AssetDatabase.LoadAssetAtPath<TrapWhileAliveMapEffect>(EffectPath);
      if (trapEffect == null)
      {
        trapEffect = ScriptableObject.CreateInstance<TrapWhileAliveMapEffect>();
        AssetDatabase.CreateAsset(trapEffect, EffectPath);
      }

      trapEffect.cell = PitCell;
      trapEffect.trapDefinition = bear;
      trapEffect.logTag = "skeleton_pit";
      EditorUtility.SetDirty(trapEffect);

      var profile = AssetDatabase.LoadAssetAtPath<MonsterMapPresenceProfile>(ProfilePath);
      if (profile == null)
      {
        profile = ScriptableObject.CreateInstance<MonsterMapPresenceProfile>();
        AssetDatabase.CreateAsset(profile, ProfilePath);
      }

      profile.displayName = "Skeleton Pit Test";
      profile.effects = new MonsterMapPresenceEffect[] { trapEffect };
      profile.permanentOnSpawn = false;
      EditorUtility.SetDirty(profile);

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();
      Debug.Log("[MapPresence] Created Profile_SkeletonPitTest + TrapWhileAlive_SkeletonPit.");
    }

    [MenuItem("JRogue/World/Wire Map Presence Service in SampleScene")]
    public static void WireServiceInSampleScene()
    {
      Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
      if (Object.FindAnyObjectByType<MonsterMapPresenceService>() == null)
      {
        GameObject systems = GameObject.Find("GameSystems");
        if (systems == null)
          systems = new GameObject("GameSystems");

        systems.AddComponent<MonsterMapPresenceService>();
      }

      EditorSceneManager.MarkSceneDirty(scene);
      Debug.Log("[MapPresence] Wired MonsterMapPresenceService in SampleScene.");
    }

    [MenuItem("JRogue/World/Place Map-Presence Test Skeleton in SampleScene")]
    public static void PlaceTestSkeletonInSampleScene()
    {
      MonsterMapPresenceProfile profile =
        AssetDatabase.LoadAssetAtPath<MonsterMapPresenceProfile>(ProfilePath);
      if (profile == null)
      {
        Debug.LogError("[MapPresence] Run Create Map Presence v0 Assets first.");
        return;
      }

      GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
      if (prefab == null)
      {
        Debug.LogError($"[MapPresence] Missing prefab at {EnemyPrefabPath}.");
        return;
      }

      Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
      const string objectName = "Enemy_MapPresenceTestSkeleton";
      if (GameObject.Find(objectName) != null)
      {
        Debug.LogWarning($"[MapPresence] {objectName} already in scene.");
        Selection.activeGameObject = GameObject.Find(objectName);
        return;
      }

      Grid grid = Object.FindAnyObjectByType<Grid>();
      Vector3 world = grid != null
        ? grid.GetCellCenterWorld(SkeletonSpawnCell)
        : new Vector3(SkeletonSpawnCell.x + 0.5f, SkeletonSpawnCell.y + 0.5f, 0f);

      GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
      instance.name = objectName;
      instance.transform.position = world;

      MonsterMapPresenceHost host = instance.GetComponent<MonsterMapPresenceHost>();
      if (host == null)
        host = instance.AddComponent<MonsterMapPresenceHost>();

      SerializedObject hostSo = new SerializedObject(host);
      hostSo.FindProperty("profileOverride").objectReferenceValue = profile;
      hostSo.ApplyModifiedPropertiesWithoutUndo();

      EnemyController enemy = instance.GetComponent<EnemyController>();
      if (enemy != null)
      {
        SerializedObject enemySo = new SerializedObject(enemy);
        enemySo.FindProperty("hp").intValue = 3;
        enemySo.ApplyModifiedPropertiesWithoutUndo();
      }

      EditorSceneManager.MarkSceneDirty(scene);
      Selection.activeGameObject = instance;
      Debug.Log(
        $"[MapPresence] Placed {objectName} at {SkeletonSpawnCell} (pit trap at {PitCell} while alive). Save scene (Ctrl+S).");
    }
  }
}
#endif
