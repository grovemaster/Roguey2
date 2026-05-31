using UnityEngine;

namespace JRogue.World.Generation
{
    public static class DungeonGenerationLog
    {
        const string Tag = "[DungeonGen]";

        public static void Info(string message) => Debug.Log($"{Tag} {message}");

        public static void Warn(string message) => Debug.LogWarning($"{Tag} {message}");

        public static void Error(string message) => Debug.LogError($"{Tag} {message}");

        public static void SceneObject(string name, bool present, string detail = null)
        {
            if (present)
                Info($"[Scene] OK  GameObject '{name}'{FormatDetail(detail)}");
            else
                Error($"[Scene] MISSING GameObject '{name}'{FormatDetail(detail)}");
        }

        public static void SceneComponent<T>(string ownerName, bool present, string detail = null) where T : Component
        {
            string typeName = typeof(T).Name;
            if (present)
                Info($"[Scene] OK  {typeName} on '{ownerName}'{FormatDetail(detail)}");
            else
                Error($"[Scene] MISSING {typeName} on '{ownerName}'{FormatDetail(detail)}");
        }

        public static void Phase(string phaseName, string message) => Info($"[Phase:{phaseName}] {message}");

        static string FormatDetail(string detail) =>
            string.IsNullOrEmpty(detail) ? string.Empty : $" — {detail}";
    }
}
