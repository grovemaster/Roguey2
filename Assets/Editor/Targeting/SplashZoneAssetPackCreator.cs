#if UNITY_EDITOR
using JRogue.Core.Targeting;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.Targeting
{
    public static class SplashZoneAssetPackCreator
    {
        const string Root = "Assets/Data/Targeting";
        const string FireballAbilityPath = "Assets/Resources/Item/Ability/Fireball_Standard.asset";

        const string MenuPath = "JRogue/Targeting/Create Fireball Splash Zone Assets";

        [MenuItem(MenuPath, false, 0)]
        public static void CreateFireballSplashAssets()
        {
            EnsureFolder(Root);

            var fireballZone = LoadOrCreate<SplashZoneDefinition>($"{Root}/SplashZone_Fireball_Disk2.asset");
            fireballZone.shapeKind = SplashZoneShapeKind.DiskChebyshev;
            fireballZone.radius = 2;
            fireballZone.includePrimaryInEffect = true;
            fireballZone.distanceMetric = SplashZoneDistanceMetric.Chebyshev;
            EditorUtility.SetDirty(fireballZone);

            var noneZone = LoadOrCreate<SplashZoneDefinition>($"{Root}/SplashZone_None.asset");
            noneZone.shapeKind = SplashZoneShapeKind.None;
            EditorUtility.SetDirty(noneZone);

            var fireballAbility = AssetDatabase.LoadAssetAtPath<JRogue.Ability.Fireball.FireballAbility>(FireballAbilityPath);
            if (fireballAbility != null)
            {
                fireballAbility.splashZone = fireballZone;
                EditorUtility.SetDirty(fireballAbility);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SplashZone] Created SplashZone_Fireball_Disk2, SplashZone_None, wired Fireball_Standard.");
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
