using System.Collections;
using JRogue.World.Lighting;
using JRogue.World.LotF;
using UnityEngine;

namespace JRogue.World.Generation
{
    public sealed class DungeonTimeService : MonoBehaviour
    {
        public static DungeonTimeService Instance { get; private set; }

        const string LogPrefix = "[DungeonTime]";

        [SerializeField] bool showDebugOverlay = true;

        readonly DungeonTimeRunState _state = new DungeonTimeRunState();

        DungeonFloorDefinition[] _floorChain = System.Array.Empty<DungeonFloorDefinition>();
        bool _dungeonRunActive;
        Coroutine _forcedExitCoroutine;

        public bool DungeonRunActive => _dungeonRunActive;
        public int ElapsedCycles => _state.ElapsedCycles;
        public int MaximumCycles => _state.MaximumCycles;
        public DungeonTimePhase CurrentPhase => _state.CurrentPhase;
        public int PhasePlayerTurnsElapsed => _state.PhasePlayerTurnsElapsed;

        public int ResolveActiveFloorChainIndex()
        {
            if (!_dungeonRunActive || _floorChain == null || string.IsNullOrEmpty(_state.ActiveTimeFloorId))
                return 0;

            for (int i = 0; i < _floorChain.Length; i++)
            {
                DungeonFloorDefinition def = _floorChain[i];
                if (def != null && def.FloorId == _state.ActiveTimeFloorId)
                    return i + 1;
            }

            return 1;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void BeginDungeonRun(DungeonFloorDefinition startFloor, DungeonFloorDefinition[] floorChain)
        {
            _floorChain = floorChain ?? System.Array.Empty<DungeonFloorDefinition>();
            _dungeonRunActive = startFloor != null && startFloor.ParticipatesInDungeonTime;

            if (!_dungeonRunActive)
            {
                Debug.Log($"{LogPrefix} Run started without dungeon time (floor '{startFloor?.FloorId ?? "null"}').");
                return;
            }

            int baseCycles = startFloor.BaseDayNightCycles;
            _state.ResetForNewRun(startFloor.FloorId, baseCycles);
            _state.AppliedAdditionalBudgetFloors.Add(startFloor.FloorId);

            Debug.Log(
                $"{LogPrefix} Clock started — max {baseCycles} cycle(s), " +
                $"day={startFloor.PlayerTurnsPerDay} night={startFloor.PlayerTurnsPerNight} turns per phase.");
            SyncLightingPhase();
        }

        public void EndDungeonRun()
        {
            _dungeonRunActive = false;
            Debug.Log($"{LogPrefix} Clock stopped.");
        }

        public void ScheduleForcedExitCoroutine()
        {
            if (_forcedExitCoroutine != null)
                StopCoroutine(_forcedExitCoroutine);

            _forcedExitCoroutine = StartCoroutine(ForcedExitNextFrame());
        }

        IEnumerator ForcedExitNextFrame()
        {
            yield return null;
            _forcedExitCoroutine = null;
            DungeonExitService.ExecuteForcedExitAfterFrame();
        }

        public void ScheduleDungeonEntryCoroutine()
        {
            if (_forcedExitCoroutine != null)
                StopCoroutine(_forcedExitCoroutine);

            _forcedExitCoroutine = StartCoroutine(DungeonEntryNextFrame());
        }

        IEnumerator DungeonEntryNextFrame()
        {
            yield return null;
            _forcedExitCoroutine = null;
            DungeonEntryService.ExecuteDungeonEntryAfterFrame();
        }

        public void OnFloorActivated(DungeonFloorDefinition floor, bool isFirstVisit)
        {
            if (!_dungeonRunActive || floor == null || !floor.ParticipatesInDungeonTime)
                return;

            _state.ActiveTimeFloorId = floor.FloorId;

            bool isFirstInChain = IsFirstFloorInChain(floor);
            int previousMax = _state.MaximumCycles;
            DungeonTimeLogic.ApplyFirstVisitBudget(_state, floor, isFirstInChain, isFirstVisit);

            if (!isFirstInChain && isFirstVisit && _state.MaximumCycles > previousMax)
            {
                Debug.Log(
                    $"{LogPrefix} Deadline extended visiting '{floor.FloorId}': " +
                    $"{previousMax} → {_state.MaximumCycles} cycles (+{floor.AdditionalDayNightCycles}).");
            }

            if (isFirstVisit)
            {
                Debug.Log(
                    $"{LogPrefix} Active floor '{floor.FloorId}' — day={floor.PlayerTurnsPerDay}, night={floor.PlayerTurnsPerNight} turns/phase.");
            }
        }

        /// <summary>
        /// Advances the calendar after a completed player phase. Returns true if forced town exit started.
        /// </summary>
        public bool TryTickAfterPlayerPhase()
        {
            if (!_dungeonRunActive)
                return false;

            DungeonFloorDefinition activeFloor = ResolveActiveFloorDefinition();
            DungeonTimeTickResult result = DungeonTimeLogic.AdvancePlayerTurn(_state, activeFloor);

            int limit = DungeonTimeLogic.GetPhaseTurnLimit(activeFloor, _state.CurrentPhase);
            if (!result.PhaseAdvanced)
            {
                LogRemaining(result, activeFloor, limit);
                return false;
            }

            if (result.PhaseAdvanced)
            {
                int displayLimit = DungeonFloorTimeLimitLogic.ResolveDisplayCycleLimit(
                    activeFloor,
                    _state.MaximumCycles);
                Debug.Log(
                    $"{LogPrefix} Phase→{_state.CurrentPhase} (cycle {_state.ElapsedCycles}/{displayLimit}, " +
                    $"floor {activeFloor?.FloorId ?? _state.ActiveTimeFloorId}).");
                SyncLightingPhase();
            }

            if (result.CycleCompleted)
            {
                WarnIfNearDeadline();
                TryApplyMonsterSpawnScheduleForNewDay();
            }

            if (result.TimeExpired)
            {
                int floorLimit = DungeonFloorTimeLimitLogic.ResolveDisplayCycleLimit(
                    activeFloor,
                    _state.MaximumCycles);
                Debug.Log(
                    $"{LogPrefix} Time expired on '{activeFloor?.FloorId ?? _state.ActiveTimeFloorId}' " +
                    $"after {_state.ElapsedCycles} cycle(s) (floor limit {floorLimit}).");

                if (JRogue.World.Rift.RiftService.IsInsideRift)
                {
                    JRogue.World.Rift.RiftService.NotifyDungeonTimeExpiredWhilePossiblyInRift();
                    return false;
                }

                DungeonExitService.RequestForcedExitToTown();
                return true;
            }

            return false;
        }

        void LogRemaining(DungeonTimeTickResult result, DungeonFloorDefinition activeFloor, int limit)
        {
            int remaining = limit - _state.PhasePlayerTurnsElapsed;
            if (remaining == 1)
            {
                int displayLimit = DungeonFloorTimeLimitLogic.ResolveDisplayCycleLimit(
                    activeFloor,
                    _state.MaximumCycles);
                Debug.Log(
                    $"{LogPrefix} {_state.CurrentPhase} on '{activeFloor?.FloorId}': 1 player phase until phase change " +
                    $"(cycle {_state.ElapsedCycles}/{displayLimit}).");
            }
        }

        void WarnIfNearDeadline()
        {
            DungeonFloorDefinition activeFloor = ResolveActiveFloorDefinition();
            int limit = DungeonFloorTimeLimitLogic.ResolveDisplayCycleLimit(
                activeFloor,
                _state.MaximumCycles);
            int remaining = limit - _state.ElapsedCycles;
            if (remaining == 2)
                Debug.Log($"{LogPrefix} Warning: 2 day–night cycles remaining.");
            else if (remaining == 1)
                Debug.Log($"{LogPrefix} Warning: 1 day–night cycle remaining.");
        }

        void TryApplyMonsterSpawnScheduleForNewDay()
        {
            if (!_dungeonRunActive || _state.CurrentPhase != DungeonTimePhase.Day)
                return;

            int dungeonDay = _state.ElapsedCycles + 1;
            int runSeed = DungeonRunState.Instance != null ? DungeonRunState.Instance.RunSeed : 0;
            MonsterSpawn.MonsterSpawnScheduleService.ApplyForActiveFloorOnDayStarted(dungeonDay, runSeed);
            LordOfTheFloorService.EvaluateOnDayStarted(dungeonDay, runSeed);
        }

        void SyncLightingPhase()
        {
            LightingService lighting = LightingService.Instance;
            if (lighting == null)
                return;

            int ambient = _state.CurrentPhase == DungeonTimePhase.Day
                ? LightLevel.FullDaylightAmbient
                : 3;

            lighting.SetAmbientLight(
                lighting.DefaultFloorAmbientRegionId,
                ambient,
                $"dungeon time {_state.CurrentPhase}");
        }

        DungeonFloorDefinition ResolveActiveFloorDefinition()
        {
            if (_floorChain == null || string.IsNullOrEmpty(_state.ActiveTimeFloorId))
                return null;

            for (int i = 0; i < _floorChain.Length; i++)
            {
                DungeonFloorDefinition def = _floorChain[i];
                if (def != null && def.FloorId == _state.ActiveTimeFloorId)
                    return def;
            }

            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            if (manager == null)
                return null;

            return manager.TryFindDefinition(_state.ActiveTimeFloorId);
        }

        bool IsFirstFloorInChain(DungeonFloorDefinition floor)
        {
            if (_floorChain == null || _floorChain.Length == 0 || floor == null)
                return true;

            DungeonFloorDefinition first = _floorChain[0];
            return first != null && first.FloorId == floor.FloorId;
        }

        void OnGUI()
        {
            if (!showDebugOverlay || !_dungeonRunActive)
                return;

            int dayLimit = 1;
            int nightLimit = 1;
            DungeonFloorDefinition active = ResolveActiveFloorDefinition();
            if (active != null)
            {
                dayLimit = active.PlayerTurnsPerDay;
                nightLimit = active.PlayerTurnsPerNight;
            }

            int phaseLimit = _state.CurrentPhase == DungeonTimePhase.Day ? dayLimit : nightLimit;
            int cycleLimit = DungeonFloorTimeLimitLogic.ResolveDisplayCycleLimit(active, _state.MaximumCycles);
            string text =
                $"Dungeon time: {_state.CurrentPhase}  " +
                $"phase {_state.PhasePlayerTurnsElapsed}/{phaseLimit}  " +
                $"cycle {_state.ElapsedCycles}/{cycleLimit}";

            var rect = new Rect(12f, 52f, 520f, 24f);
            GUI.Label(rect, text);
        }
    }
}
