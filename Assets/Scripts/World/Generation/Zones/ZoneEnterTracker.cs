using System;
using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.Quest;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public sealed class ZoneEnterTracker : MonoBehaviour
    {
        public static ZoneEnterTracker Instance { get; private set; }

        public event Action<string, Vector3Int> ZoneEntered;

        string _currentZoneId;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void LateUpdate()
        {
            PartyManager party = PartyManager.Instance;
            if (party == null)
                return;

            BaseActor leader = party.GetActiveMember();
            if (leader == null)
                return;

            TryUpdateZone(leader.GridPosition);
        }

        public void ResetTracking() => _currentZoneId = null;

        public void TryUpdateZone(Vector3Int cell)
        {
            if (!TryResolveZoneId(cell, out string zoneId))
                zoneId = null;

            if (string.IsNullOrEmpty(zoneId)
                || zoneId == ZoneIds.Empty
                || zoneId == ZoneIds.Rock)
            {
                _currentZoneId = zoneId;
                return;
            }

            if (zoneId == _currentZoneId)
                return;

            _currentZoneId = zoneId;
            ZoneEntered?.Invoke(zoneId, cell);
            QuestService.Instance?.NotifyZoneEntered(zoneId);
        }

        static bool TryResolveZoneId(Vector3Int cell, out string zoneId)
        {
            zoneId = null;
            DungeonFloorInstanceManager floors = DungeonFloorInstanceManager.Instance;
            if (floors == null)
                return false;

            DungeonFloorInstance active = floors.GetActiveFloorInstance();
            return active != null && active.TryGetZoneId(cell, out zoneId);
        }
    }
}
