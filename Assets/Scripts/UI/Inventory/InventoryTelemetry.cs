using System.Collections.Generic;
using UnityEngine;

namespace JRogue.UI.Inventory
{
    /// <summary>Lightweight action frequency + time-since-open logging for tuning inventory UX (Phase 3).</summary>
    public static class InventoryTelemetry
    {
        public static bool Enabled = true;

        static float _openedAtUnscaled = -1f;
        static readonly Dictionary<string, int> TotalCounts = new Dictionary<string, int>();

        public static void NotifyInventoryOpened()
        {
            if (!Enabled)
                return;
            _openedAtUnscaled = Time.unscaledTime;
        }

        public static void NotifyInventoryClosed()
        {
            _openedAtUnscaled = -1f;
        }

        public static void RecordAction(string actionId)
        {
            if (!Enabled || string.IsNullOrEmpty(actionId))
                return;

            TotalCounts.TryGetValue(actionId, out int n);
            n++;
            TotalCounts[actionId] = n;

            float sinceOpen = _openedAtUnscaled >= 0f ? Time.unscaledTime - _openedAtUnscaled : 0f;
            Debug.Log($"[InvTelemetry] action={actionId} total={n} tSinceOpen={sinceOpen:0.###}s");
        }
    }
}
