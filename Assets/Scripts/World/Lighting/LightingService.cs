using System.Collections.Generic;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Lighting
{
    /// <summary>
    /// Singleton lighting registry: emitters, receivers, ambient regions, received-light recompute.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class LightingService : MonoBehaviour
    {
        public static LightingService Instance { get; private set; }

        [SerializeField]
        [Tooltip("Ambient region id applied to floor receivers without an explicit region.")]
        int defaultFloorAmbientRegionId;

        [SerializeField]
        [Range(LightLevel.Min, LightLevel.Max)]
        int defaultFloorAmbientLight = LightLevel.FullDaylightAmbient;

        [SerializeField]
        bool verboseReceiveLogs;

        readonly Dictionary<Vector3Int, LightCellData> _cells = new Dictionary<Vector3Int, LightCellData>();
        readonly Dictionary<int, AmbientRegion> _ambientRegions = new Dictionary<int, AmbientRegion>();
        readonly List<PendingRegistration> _pending = new List<PendingRegistration>();

        bool _registryFinalized;

        struct PendingRegistration
        {
            public Vector3Int Cell;
            public LightCellData Data;
            public bool Overwrite;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureDefaultAmbientRegion();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            FinalizeRegistry();
        }

        public bool VerboseReceiveLogs
        {
            get => verboseReceiveLogs;
            set => verboseReceiveLogs = value;
        }

        public int DefaultFloorAmbientRegionId => defaultFloorAmbientRegionId;

        public int GetReceivedLight(Vector3Int cell)
        {
            EnsureRegistryFinalized();
            cell = Flatten(cell);
            if (_cells.TryGetValue(cell, out LightCellData data) && data.IsReceiver)
                return data.ReceivedLight;

            return GetAmbientAtRegion(defaultFloorAmbientRegionId);
        }

        public int GetEmitLight(Vector3Int cell)
        {
            EnsureRegistryFinalized();
            cell = Flatten(cell);
            return _cells.TryGetValue(cell, out LightCellData data) && data.IsEmitter
                ? data.EmitLight
                : 0;
        }

        public bool TryGetCellData(Vector3Int cell, out LightCellData data)
        {
            EnsureRegistryFinalized();
            return _cells.TryGetValue(Flatten(cell), out data);
        }

        public AmbientRegion GetOrCreateAmbientRegion(int regionId)
        {
            if (_ambientRegions.TryGetValue(regionId, out AmbientRegion region))
                return region;

            region = new AmbientRegion
            {
                Id = regionId,
                CurrentAmbientLight = regionId == defaultFloorAmbientRegionId
                    ? defaultFloorAmbientLight
                    : LightLevel.PitchDark
            };
            _ambientRegions[regionId] = region;
            return region;
        }

        public void SetAmbientLight(int regionId, int level, string reason = null)
        {
            AmbientRegion region = GetOrCreateAmbientRegion(regionId);
            int clamped = LightLevel.Clamp(level);
            if (region.CurrentAmbientLight == clamped)
                return;

            region.CurrentAmbientLight = clamped;
            if (verboseReceiveLogs)
            {
                Debug.Log(
                    $"[Lighting:Receive] Ambient region {regionId} → {clamped}"
                    + (string.IsNullOrEmpty(reason) ? "." : $" ({reason})."));
            }

            RecomputeReceiversInRegion(regionId);
        }

        /// <summary>Registers or overwrites cell data before <see cref="FinalizeRegistry"/>.</summary>
        public void RegisterPending(Vector3Int cell, LightCellData data, bool overwrite = true)
        {
            cell = Flatten(cell);
            _pending.Add(new PendingRegistration { Cell = cell, Data = data, Overwrite = overwrite });
        }

        public void RegisterPlacement(LightingPlacementEntry entry)
        {
            if (entry.isEmitter && entry.emitterDefinition == null)
            {
                Debug.LogWarning($"[Lighting] Emitter placement at {entry.cell} missing definition.");
                return;
            }

            LightCellData data;
            if (entry.isEmitter)
            {
                data = LightCellData.Emitter(
                    entry.emitterDefinition,
                    entry.initialEmission,
                    entry.ambientRegionId,
                    entry.isReceiver);
            }
            else
            {
                data = new LightCellData
                {
                    IsReceiver = entry.isReceiver,
                    AmbientRegionId = entry.ambientRegionId,
                    ReceivedLight = GetAmbientAtRegion(entry.ambientRegionId)
                };
            }

            RegisterPending(entry.cell, data);
        }

        public void EnableEmitter(
            Vector3Int cell,
            LightEmitterDefinition definition,
            int initialEmission = -1,
            string reason = null)
        {
            if (definition == null)
                return;

            EnsureRegistryFinalized();
            cell = Flatten(cell);

            int emission = initialEmission < 0
                ? definition.BaseEmissionMax
                : LightLevel.ClampEmission(initialEmission, definition);

            LightCellData data;
            if (_cells.TryGetValue(cell, out LightCellData existing))
            {
                data = existing;
                data.IsEmitter = true;
                data.EmitterDefinition = definition;
                data.EmitLight = emission;
                data.BlocksLos = definition.BlocksLos;
                if (!data.IsReceiver)
                    data.IsReceiver = true;
            }
            else
            {
                data = LightCellData.Emitter(definition, emission);
            }

            _cells[cell] = data;
            LogEmit(cell, data.EmitLight, reason ?? "enable");
            RecomputeAroundEmitter(cell);
        }

        public void SetEmission(Vector3Int cell, int level, string reason = null)
        {
            EnsureRegistryFinalized();
            cell = Flatten(cell);

            if (!_cells.TryGetValue(cell, out LightCellData data) || !data.IsEmitter)
            {
                Debug.LogWarning($"[Lighting:Emit] No emitter at {cell}; ignored.");
                return;
            }

            int clamped = LightLevel.ClampEmission(level, data.EmitterDefinition);
            if (data.EmitLight == clamped)
                return;

            data.EmitLight = clamped;
            _cells[cell] = data;
            LogEmit(cell, clamped, reason ?? "set");
            RecomputeAroundEmitter(cell);
        }

        public void RecomputeAll()
        {
            EnsureRegistryFinalized();
            // RecomputeReceivers writes updated values back into _cells.
            // Snapshot keys first to avoid mutating while enumerating dictionary keys.
            RecomputeReceivers(new List<Vector3Int>(_cells.Keys));
        }

        /// <summary>Called when party moves or a turn boundary refreshes vision (Phase B/C consumers).</summary>
        public void OnPartyVisionActivity()
        {
            if (!_registryFinalized)
                return;

            if (verboseReceiveLogs)
                Debug.Log("[Lighting:Receive] Party vision activity — full recompute.");

            RecomputeAll();
        }

        public void FinalizeRegistry()
        {
            if (_registryFinalized)
                return;

            BuildFloorReceivers();
            ApplyPendingRegistrations();
            _registryFinalized = true;
            RecomputeAll();

            if (verboseReceiveLogs)
                Debug.Log($"[Lighting:Receive] Registry finalized ({_cells.Count} cells).");
        }

        void EnsureRegistryFinalized()
        {
            if (!_registryFinalized)
                FinalizeRegistry();
        }

        void EnsureDefaultAmbientRegion()
        {
            AmbientRegion region = GetOrCreateAmbientRegion(defaultFloorAmbientRegionId);
            region.CurrentAmbientLight = defaultFloorAmbientLight;
        }

        void BuildFloorReceivers()
        {
            MapManager map = MapManager.Instance;
            if (map == null || map.FloorMap == null)
                return;

            Tilemap floor = map.FloorMap;
            BoundsInt bounds = floor.cellBounds;
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                if (!floor.HasTile(pos))
                    continue;

                Vector3Int cell = Flatten(pos);
                if (_cells.ContainsKey(cell))
                    continue;

                _cells[cell] = LightCellData.Receiver(
                    defaultFloorAmbientRegionId,
                    defaultFloorAmbientLight);
            }
        }

        void ApplyPendingRegistrations()
        {
            for (int i = 0; i < _pending.Count; i++)
            {
                PendingRegistration pending = _pending[i];
                if (!pending.Overwrite && _cells.ContainsKey(pending.Cell))
                    continue;

                _cells[pending.Cell] = pending.Data;
            }

            _pending.Clear();
        }

        void RecomputeAroundEmitter(Vector3Int emitterCell)
        {
            if (!_cells.TryGetValue(emitterCell, out LightCellData emitterData) || !emitterData.IsEmitter)
            {
                RecomputeAll();
                return;
            }

            int radius = emitterData.EmitterDefinition != null
                ? emitterData.EmitterDefinition.FalloffRadius
                : LightLevel.Max;

            var affected = new List<Vector3Int>();
            foreach (Vector3Int cell in _cells.Keys)
            {
                if (!_cells[cell].IsReceiver)
                    continue;

                if (ManhattanDistance(emitterCell, cell) <= radius)
                    affected.Add(cell);
            }

            RecomputeReceivers(affected);
        }

        void RecomputeReceiversInRegion(int regionId)
        {
            var affected = new List<Vector3Int>();
            foreach (KeyValuePair<Vector3Int, LightCellData> entry in _cells)
            {
                if (entry.Value.IsReceiver && entry.Value.AmbientRegionId == regionId)
                    affected.Add(entry.Key);
            }

            RecomputeReceivers(affected);
        }

        void RecomputeReceivers(IEnumerable<Vector3Int> receiverCells)
        {
            foreach (Vector3Int cell in receiverCells)
            {
                if (!_cells.TryGetValue(cell, out LightCellData data) || !data.IsReceiver)
                    continue;

                data.ReceivedLight = ComputeReceivedLightAt(cell);
                _cells[cell] = data;
            }
        }

        int ComputeReceivedLightAt(Vector3Int cell)
        {
            int fromEmitters = 0;
            foreach (KeyValuePair<Vector3Int, LightCellData> entry in _cells)
            {
                LightCellData source = entry.Value;
                if (!source.IsEmitter || source.EmitLight <= 0)
                    continue;

                int radius = source.EmitterDefinition != null
                    ? source.EmitterDefinition.FalloffRadius
                    : 0;
                int falloffPerTile = source.EmitterDefinition != null
                    ? source.EmitterDefinition.FalloffPerTile
                    : 0;

                int distance = ManhattanDistance(entry.Key, cell);
                if (distance > radius)
                    continue;

                int contrib = Mathf.Max(0, source.EmitLight - falloffPerTile * distance);
                fromEmitters += contrib;
            }

            int ambient = GetAmbientForCell(cell);
            return LightLevel.Clamp(fromEmitters + ambient);
        }

        int GetAmbientForCell(Vector3Int cell)
        {
            if (_cells.TryGetValue(cell, out LightCellData data))
                return GetAmbientAtRegion(data.AmbientRegionId);

            return GetAmbientAtRegion(defaultFloorAmbientRegionId);
        }

        int GetAmbientAtRegion(int regionId)
        {
            AmbientRegion region = GetOrCreateAmbientRegion(regionId);
            return LightLevel.Clamp(region.CurrentAmbientLight);
        }

        static int ManhattanDistance(Vector3Int a, Vector3Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        static Vector3Int Flatten(Vector3Int cell) => new Vector3Int(cell.x, cell.y, 0);

        static void LogEmit(Vector3Int cell, int level, string reason) =>
            Debug.Log($"[Lighting:Emit] {cell} → {level} (reason: {reason})");
    }
}
