using UnityEngine;

namespace JRogue.Manager.Inventory
{
    /// <summary>Tagged debug logs for inventory menu → targeting flows (scrolls, wands, etc.).</summary>
    public static class InventoryTargetedUseLog
    {
        public static void Log(string tag, string message)
        {
            if (string.IsNullOrEmpty(tag))
                Debug.Log(message);
            else
                Debug.Log($"[{tag}] {message}");
        }

        public static void LogWarning(string tag, string message)
        {
            if (string.IsNullOrEmpty(tag))
                Debug.LogWarning(message);
            else
                Debug.LogWarning($"[{tag}] {message}");
        }
    }
}
