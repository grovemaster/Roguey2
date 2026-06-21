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
        readonly List<Vector3Int> _emitterCells = new List<Vector3Int>();
        readonly Dictionary<string, CarriedEmitterEntry> _carriedEmitters = new Dictionary<string, CarriedEmitterEntry>();

        bool _registryFinalized;
        int _playerPhaseTurnCount;

        public readonly struct CarriedEmitterEntry
        {
            public CarriedEmitterEntry(Vector3Int cell, LightEmitterDefinition definition, int emitLight)
            {
                Cell = cell;
                Definition = definition;
                EmitLight = emitLight;
            }

            public Vector3Int Cell { get; }
            public LightEmitterDefinition Definition { get; }
            public int EmitLight { get; }
        }

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

        /// <summary>
        /// Advances any configured ambient cycles at each player-phase boundary.
        /// </summary>
        public void OnPlayerPhaseBoundary()
        {
            EnsureRegistryFinalized();
            _playerPhaseTurnCount++;
            TickAmbientCycles();
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
            if (data.IsEmitter && !_emitterCells.Contains(cell))
                _emitterCells.Add(cell);
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

        /// <summary>
        /// Legacy hook for party/vision activity. Tile emitters are static; carried emitters
        /// recompute locally via <see cref="SyncCarriedEmitters"/>.
        /// </summary>
        public void OnPartyVisionActivity()
        {
        }

        /// <summary>Registers or moves a party-carried virtual emitter (does not overwrite map cells).</summary>
        public void SetCarriedEmitter(
            string emitterId,
            Vector3Int cell,
            LightEmitterDefinition definition,
            int initialEmission = -1,
            string reason = null)
        {
            if (string.IsNullOrEmpty(emitterId) || definition == null)
                return;

            EnsureRegistryFinalized();
            cell = Flatten(cell);

            int emission = initialEmission < 0
                ? definition.BaseEmissionMax
                : LightLevel.ClampEmission(initialEmission, definition);

            _carriedEmitters[emitterId] = new CarriedEmitterEntry(cell, definition, emission);
            Debug.Log(
                $"[Lighting:Carried] Set {emitterId} at {cell} emission={emission}"
                + (string.IsNullOrEmpty(reason) ? "." : $" ({reason})."));

            RecomputeReceiversNear(new[] { cell }, definition.FalloffRadius);
        }

        public void RemoveCarriedEmitter(string emitterId, string reason = null)
        {
            if (string.IsNullOrEmpty(emitterId))
                return;

            if (!_carriedEmitters.TryGetValue(emitterId, out CarriedEmitterEntry removed))
                return;

            _carriedEmitters.Remove(emitterId);

            Debug.Log(
                $"[Lighting:Carried] Removed {emitterId}"
                + (string.IsNullOrEmpty(reason) ? "." : $" ({reason})."));

            if (_registryFinalized && removed.Definition != null)
                RecomputeReceiversNear(new[] { removed.Cell }, removed.Definition.FalloffRadius);
        }

        public void ClearCarriedEmitters()
        {
            if (_carriedEmitters.Count == 0)
                return;

            _carriedEmitters.Clear();
            if (_registryFinalized)
                RecomputeAll();
        }

        public int CarriedEmitterCount => _carriedEmitters.Count;

        /// <summary>Replaces the carried-emitter set with <paramref name="desired"/> (removes stale ids).</summary>
        public void SyncCarriedEmitters(System.Collections.Generic.Dictionary<string, CarriedEmitterEntry> desired)
        {
            EnsureRegistryFinalized();
            desired ??= new System.Collections.Generic.Dictionary<string, CarriedEmitterEntry>();

            var recomputeCenters = new System.Collections.Generic.List<Vector3Int>();
            int maxRadius = 0;

            var stale = new System.Collections.Generic.List<string>();
            foreach (string existingId in _carriedEmitters.Keys)
            {
                if (!desired.ContainsKey(existingId))
                    stale.Add(existingId);
            }

            for (int i = 0; i < stale.Count; i++)
            {
                if (!_carriedEmitters.TryGetValue(stale[i], out CarriedEmitterEntry removed))
                    continue;

                recomputeCenters.Add(removed.Cell);
                if (removed.Definition != null)
                    maxRadius = Mathf.Max(maxRadius, removed.Definition.FalloffRadius);

                _carriedEmitters.Remove(stale[i]);
            }

            foreach (System.Collections.Generic.KeyValuePair<string, CarriedEmitterEntry> pair in desired)
            {
                CarriedEmitterEntry entry = pair.Value;
                if (entry.Definition == null)
                    continue;

                maxRadius = Mathf.Max(maxRadius, entry.Definition.FalloffRadius);

                if (_carriedEmitters.TryGetValue(pair.Key, out CarriedEmitterEntry existing)
                    && existing.Cell == entry.Cell
                    && existing.Definition == entry.Definition
                    && existing.EmitLight == entry.EmitLight)
                {
                    continue;
                }

                if (_carriedEmitters.TryGetValue(pair.Key, out existing))
                    recomputeCenters.Add(existing.Cell);

                recomputeCenters.Add(entry.Cell);
                _carriedEmitters[pair.Key] = entry;
            }

            if (recomputeCenters.Count > 0 && maxRadius > 0)
                RecomputeReceiversNear(recomputeCenters, maxRadius);
        }

        public void FinalizeRegistry()
        {
            bool firstTime = !_registryFinalized;
            if (firstTime)
            {
                BuildFloorReceivers();
                _registryFinalized = true;
            }

            bool hadPending = _pending.Count > 0;
            if (hadPending)
                ApplyPendingRegistrations();

            if (firstTime || hadPending)
            {
                RebuildEmitterCellIndex();
                RecomputeAll();
            }

            if (verboseReceiveLogs && (firstTime || hadPending))
                Debug.Log($"[Lighting:Receive] Registry finalized ({_cells.Count} cells).");
        }

        /// <summary>Clears cell registry when switching active floor tilemaps.</summary>
        public void ResetForActiveFloor()
        {
            _cells.Clear();
            _pending.Clear();
            _emitterCells.Clear();
            _carriedEmitters.Clear();
            _registryFinalized = false;
            EnsureDefaultAmbientRegion();
        }

        /// <summary>
        /// Adds floor receivers for cells painted after <see cref="FinalizeRegistry"/> (e.g. procedural dungeon generate).
        /// </summary>
        public void SyncFloorReceiversFromMap()
        {
            BuildFloorReceivers();
            if (_registryFinalized)
                RecomputeAll();
        }

        /// <summary>Small town interior rooms — full ambient so fog/light gates do not hide floor tiles.</summary>
        public void ApplyFullInteriorDaylight()
        {
            EnsureDefaultAmbientRegion();
            AmbientRegion region = GetOrCreateAmbientRegion(defaultFloorAmbientRegionId);
            region.CurrentAmbientLight = LightLevel.FullDaylightAmbient;
            EnsureRegistryFinalized();
            RecomputeAll();
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

        void TickAmbientCycles()
        {
            if (_ambientRegions.Count == 0)
                return;

            foreach (KeyValuePair<int, AmbientRegion> pair in _ambientRegions)
            {
                AmbientRegion region = pair.Value;
                if (region == null || region.Phases == null || region.Phases.Length == 0)
                    continue;

                if (region.CycleLengthTurns <= 0)
                {
                    int sum = 0;
                    for (int i = 0; i < region.Phases.Length; i++)
                        sum += Mathf.Max(1, region.Phases[i].durationTurns);
                    region.CycleLengthTurns = sum;
                }

                if (region.TurnsUntilNextPhase <= 0)
                {
                    region.PhaseIndex = Mathf.Clamp(region.PhaseIndex, 0, region.Phases.Length - 1);
                    region.TurnsUntilNextPhase = Mathf.Max(1, region.Phases[region.PhaseIndex].durationTurns);
                }

                region.TurnsUntilNextPhase--;
                if (region.TurnsUntilNextPhase > 0)
                    continue;

                region.PhaseIndex = (region.PhaseIndex + 1) % region.Phases.Length;
                AmbientPhaseScheduleEntry phase = region.Phases[region.PhaseIndex];
                region.TurnsUntilNextPhase = Mathf.Max(1, phase.durationTurns);

                int nextAmbient = LightLevel.Clamp(phase.ambientLight);
                if (region.CurrentAmbientLight == nextAmbient)
                    continue;

                region.CurrentAmbientLight = nextAmbient;
                Debug.Log($"[Lighting:Cycle] Region {region.Id} -> ambient {nextAmbient} (turn {_playerPhaseTurnCount})");
                RecomputeReceiversInRegion(region.Id);
            }
        }

        void BuildFloorReceivers()
        {
            MapManager map = MapManager.Instance;
            if (map == null)
                return;

            if (map.FloorMap != null)
                BuildReceiversFromTilemap(map.FloorMap);

            if (map.WallMap != null)
                BuildReceiversFromTilemap(map.WallMap);
        }

        void BuildReceiversFromTilemap(Tilemap tilemap)
        {
            BoundsInt bounds = tilemap.cellBounds;
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(pos))
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
            RebuildEmitterCellIndex();
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

            RecomputeReceiversNear(new[] { emitterCell }, radius);
        }

        void RecomputeReceiversNear(IReadOnlyList<Vector3Int> centers, int radius)
        {
            if (centers == null || centers.Count == 0 || radius <= 0)
                return;

            var affected = new HashSet<Vector3Int>();
            for (int c = 0; c < centers.Count; c++)
            {
                Vector3Int center = Flatten(centers[c]);
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Mathf.Abs(dx) + Mathf.Abs(dy) > radius)
                            continue;

                        Vector3Int cell = new Vector3Int(center.x + dx, center.y + dy, 0);
                        if (_cells.TryGetValue(cell, out LightCellData data) && data.IsReceiver)
                            affected.Add(cell);
                    }
                }
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
            int fromEmitters = SumEmitterContribution(cell);
            fromEmitters += SumCarriedEmitterContribution(cell);
            int ambient = GetAmbientForCell(cell);
            return LightLevel.Clamp(fromEmitters + ambient);
        }

        int SumCarriedEmitterContribution(Vector3Int cell)
        {
            if (_carriedEmitters.Count == 0)
                return 0;

            int total = 0;
            foreach (KeyValuePair<string, CarriedEmitterEntry> pair in _carriedEmitters)
            {
                CarriedEmitterEntry carried = pair.Value;
                if (carried.Definition == null || carried.EmitLight <= 0)
                    continue;

                int radius = carried.Definition.FalloffRadius;
                int falloffPerTile = carried.Definition.FalloffPerTile;
                int distance = ManhattanDistance(carried.Cell, cell);
                if (distance > radius)
                    continue;

                total += Mathf.Max(0, carried.EmitLight - falloffPerTile * distance);
            }

            return total;
        }

        int SumEmitterContribution(Vector3Int cell)
        {
            int fromEmitters = 0;
            for (int i = 0; i < _emitterCells.Count; i++)
            {
                Vector3Int emitterCell = _emitterCells[i];
                if (!_cells.TryGetValue(emitterCell, out LightCellData source)
                    || !source.IsEmitter
                    || source.EmitLight <= 0)
                {
                    continue;
                }

                int radius = source.EmitterDefinition != null
                    ? source.EmitterDefinition.FalloffRadius
                    : 0;
                int falloffPerTile = source.EmitterDefinition != null
                    ? source.EmitterDefinition.FalloffPerTile
                    : 0;

                int distance = ManhattanDistance(emitterCell, cell);
                if (distance > radius)
                    continue;

                int contrib = Mathf.Max(0, source.EmitLight - falloffPerTile * distance);
                fromEmitters += contrib;
            }

            return fromEmitters;
        }

        void RebuildEmitterCellIndex()
        {
            _emitterCells.Clear();
            foreach (KeyValuePair<Vector3Int, LightCellData> entry in _cells)
            {
                if (entry.Value.IsEmitter)
                    _emitterCells.Add(entry.Key);
            }
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
