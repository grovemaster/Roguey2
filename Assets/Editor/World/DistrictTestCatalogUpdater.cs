#if UNITY_EDITOR
using JRogue.World.Generation;
using JRogue.World.Town;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    public static class DistrictTestCatalogUpdater
    {
        public static void UpdateCatalog(params DungeonFloorDefinition[] floors)
        {
            if (floors == null || floors.Length == 0)
                return;

            var catalog = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinitionCatalog>(
                TownDistrictTestPaths.DistrictTestCatalog);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DungeonFloorDefinitionCatalog>();
                AssetDatabase.CreateAsset(catalog, TownDistrictTestPaths.DistrictTestCatalog);
            }

            var so = new SerializedObject(catalog);
            SerializedProperty catalogFloors = so.FindProperty("floors");
            catalogFloors.arraySize = floors.Length;
            for (int i = 0; i < floors.Length; i++)
                catalogFloors.GetArrayElementAtIndex(i).objectReferenceValue = floors[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }
    }
}
#endif
