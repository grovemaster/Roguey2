using System;
using UnityEngine;

namespace JRogue.World.Rift
{
    /// <summary>
    /// Session-lifetime (O-B) counters for rift portal cooldowns. Resets when Play Mode stops.
    /// </summary>
    public sealed class RiftSessionMeta : MonoBehaviour
    {
        public static RiftSessionMeta Instance { get; private set; }

        int _dungeonRunIndex;
        readonly System.Collections.Generic.Dictionary<string, int> _lastPortalOpenedRunByFloor =
            new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
        readonly System.Collections.Generic.Dictionary<string, int> _lastRiftEnteredRunByFloor =
            new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Per-run: host floor already opened a player portal or the party entered a rift.</summary>
        readonly System.Collections.Generic.HashSet<string> _portalConsumedThisRun =
            new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        public int DungeonRunIndex => _dungeonRunIndex;

        public static RiftSessionMeta EnsureInstance()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject(nameof(RiftSessionMeta));
            DontDestroyOnLoad(go);
            return go.AddComponent<RiftSessionMeta>();
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void OnDungeonRunBegun()
        {
            _dungeonRunIndex++;
            _portalConsumedThisRun.Clear();
            Debug.Log($"[Rift] Dungeon run index → {_dungeonRunIndex}");
        }

        public bool WasPortalConsumedThisRun(string hostFloorId) =>
            !string.IsNullOrEmpty(hostFloorId) && _portalConsumedThisRun.Contains(hostFloorId);

        /// <summary>Player-triggered open: starts cooldown and consumes the floor's portal slot for this run.</summary>
        public void MarkPlayerPortalOpened(string hostFloorId)
        {
            if (string.IsNullOrEmpty(hostFloorId))
                return;

            _portalConsumedThisRun.Add(hostFloorId);
            _lastPortalOpenedRunByFloor[hostFloorId] = _dungeonRunIndex;
        }

        /// <summary>Wandering open: starts cooldown but allows 30/20 respawn until entry.</summary>
        public void MarkWanderingPortalOpened(string hostFloorId)
        {
            if (string.IsNullOrEmpty(hostFloorId))
                return;

            _lastPortalOpenedRunByFloor[hostFloorId] = _dungeonRunIndex;
        }

        public void MarkRiftEntered(string hostFloorId)
        {
            if (string.IsNullOrEmpty(hostFloorId))
                return;

            _lastRiftEnteredRunByFloor[hostFloorId] = _dungeonRunIndex;
            _portalConsumedThisRun.Add(hostFloorId);
        }

        public int GetLastPortalOpenedRun(string hostFloorId) =>
            TryGet(_lastPortalOpenedRunByFloor, hostFloorId);

        public int GetLastRiftEnteredRun(string hostFloorId) =>
            TryGet(_lastRiftEnteredRunByFloor, hostFloorId);

        static int TryGet(System.Collections.Generic.Dictionary<string, int> map, string key)
        {
            if (string.IsNullOrEmpty(key))
                return 0;
            return map.TryGetValue(key, out int v) ? v : 0;
        }
    }
}
