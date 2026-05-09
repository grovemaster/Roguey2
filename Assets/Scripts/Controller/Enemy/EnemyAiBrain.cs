using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Controller.Player;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Visibility.Algorithm;
using JRogue.Pathfinding;
using Roguey2.Sensing;
using UnityEngine;

namespace JRogue.Controller.Enemy
{
    public enum EnemyAiState
    {
        Idle,
        Suspicious,
        Searching,
        Alert
    }

    /// <summary>
    /// Enemy turn-taking state machine: Idle / Suspicious / Searching / Alert, driven by sight and hearing.
    /// </summary>
    public class EnemyAiBrain : MonoBehaviour
    {
        private const int SearchTurnsBeforeIdle = 2;

        [Header("Alert / coordination")]
        [SerializeField, Min(0)] private int shoutChebyshevRadius = 8;

        [Header("Idle patrol (grid cells; empty = stand guard)")]
        [SerializeField] private List<Vector3Int> patrolWaypoints = new List<Vector3Int>();

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = true;

        private EnemyController _owner;
        private EnemyAiState _state = EnemyAiState.Idle;
        private bool _hasLastHeard;
        private Vector3Int _lastHeardPosition;
        private int _searchTurnsRemaining;
        private int _patrolIndex;
        private bool _playerVisibleLatch;

        public EnemyAiState State => _state;
        public Vector3Int LastHeardPosition => _lastHeardPosition;
        public bool HasLastHeard => _hasLastHeard;

        public void Bind(EnemyController owner)
        {
            _owner = owner;
        }

        public void ExecuteTurn(PlayerController player)
        {
            if (_owner == null || player == null)
                return;

            _owner.BrainEnsureManagers();

            if (TryPromoteSightToAlert(player, "turn-start"))
                return;

            switch (_state)
            {
                case EnemyAiState.Idle:
                    RunIdleTurn(player);
                    break;
                case EnemyAiState.Suspicious:
                    RunSuspiciousTurn(player);
                    break;
                case EnemyAiState.Searching:
                    RunSearchingTurn(player);
                    break;
                case EnemyAiState.Alert:
                    RunAlertTurn(player);
                    break;
            }
        }

        /// <summary>
        /// Noise heard by this enemy: Suspicious, store last heard (unless already in combat Alert).
        /// </summary>
        public void NotifyHeard(Vector3Int origin, int rawVolume, int effectiveVolume)
        {
            if (_owner == null)
                return;

            if (_state == EnemyAiState.Alert)
            {
                if (verboseLogging)
                    Debug.Log($"[AI-BRAIN] {_owner.name}: Ignoring new noise while Alert (raw={rawVolume}, eff={effectiveVolume} at {origin.x},{origin.y}).");
                return;
            }

            EnemyAiState prev = _state;
            _lastHeardPosition = new Vector3Int(origin.x, origin.y, _owner.GridPosition.z);
            _hasLastHeard = true;
            _state = EnemyAiState.Suspicious;
            _searchTurnsRemaining = 0;

            Debug.Log(
                $"[AI-STATE] {_owner.name}: {prev} → Suspicious (heard at {_lastHeardPosition.x},{_lastHeardPosition.y}, eff={effectiveVolume}).");
        }

        /// <summary>
        /// Ally entered Alert and shouted: nearby enemies investigate toward the given grid cell.
        /// </summary>
        public void NotifyAllyShout(EnemyController shouter, Vector3Int approximateThreatCell)
        {
            if (_owner == null || shouter == null || shouter == _owner)
                return;

            int d = Chebyshev(_owner.GridPosition, shouter.GridPosition);
            if (d > shoutChebyshevRadius)
            {
                if (verboseLogging)
                    Debug.Log($"[AI-SHOUT] {_owner.name}: Ally {shouter.name} shouted but out of range (dist={d}, max={shoutChebyshevRadius}).");
                return;
            }

            if (_state == EnemyAiState.Alert)
            {
                if (verboseLogging)
                    Debug.Log($"[AI-SHOUT] {_owner.name}: Already Alert; reinforcing call from {shouter.name}.");
                return;
            }

            EnemyAiState prev = _state;
            _lastHeardPosition = new Vector3Int(approximateThreatCell.x, approximateThreatCell.y, _owner.GridPosition.z);
            _hasLastHeard = true;
            _state = EnemyAiState.Suspicious;
            _searchTurnsRemaining = 0;

            Debug.Log(
                $"[AI-SHOUT] {_owner.name}: Heard ally {shouter.name} → Suspicious, investigating ({_lastHeardPosition.x},{_lastHeardPosition.y}) (was {prev}).");
        }

        public static void PerformTheShout(EnemyController shouter, PlayerController player, int radius)
        {
            if (shouter == null || player == null)
                return;

            Vector3Int p = player.GridPosition;
            Debug.Log($"[AI-SHOUT] {shouter.name}: THE SHOUT at player cell ({p.x},{p.y}), radius={radius}.");

            var allies = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude);
            for (int i = 0; i < allies.Length; i++)
            {
                EnemyController ally = allies[i];
                if (ally == null || ally == shouter || !ally.gameObject.activeInHierarchy)
                    continue;

                int d = Chebyshev(ally.GridPosition, shouter.GridPosition);
                if (d > radius)
                    continue;

                EnemyAiBrain brain = ally.GetComponent<EnemyAiBrain>();
                if (brain != null)
                    brain.NotifyAllyShout(shouter, p);
            }
        }

        private static int Chebyshev(Vector3Int a, Vector3Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private void RunIdleTurn(PlayerController player)
        {
            if (patrolWaypoints == null || patrolWaypoints.Count == 0)
            {
                if (verboseLogging)
                    Debug.Log($"[AI-BRAIN] {_owner.name}: Idle guard (no patrol waypoints).");
                return;
            }

            Vector3Int goal = patrolWaypoints[_patrolIndex % patrolWaypoints.Count];
            if (_owner.GridPosition == goal)
            {
                _patrolIndex = (_patrolIndex + 1) % patrolWaypoints.Count;
                goal = patrolWaypoints[_patrolIndex % patrolWaypoints.Count];
                if (verboseLogging)
                    Debug.Log($"[AI-BRAIN] {_owner.name}: Patrol reached waypoint, next index {_patrolIndex}.");
            }

            TryStepTowards(goal, "idle-patrol");
        }

        private void RunSuspiciousTurn(PlayerController player)
        {
            if (!_hasLastHeard)
            {
                if (verboseLogging)
                    Debug.LogWarning($"[AI-BRAIN] {_owner.name}: Suspicious but no LastHeard — reverting to Idle.");
                TransitionToIdle("suspicious-without-last-heard");
                return;
            }

            if (CanSeeLastHeardTile())
            {
                EnemyAiState prev = _state;
                _state = EnemyAiState.Searching;
                _searchTurnsRemaining = SearchTurnsBeforeIdle;
                Debug.Log(
                    $"[AI-STATE] {_owner.name}: {prev} → Searching (LOS to last heard {_lastHeardPosition.x},{_lastHeardPosition.y}, no player in cone); will wait {_searchTurnsRemaining} turn(s) then Idle.");
                return;
            }

            TryStepTowards(_lastHeardPosition, "suspicious-investigate");
        }

        private void RunSearchingTurn(PlayerController player)
        {
            if (verboseLogging)
                Debug.Log($"[AI-BRAIN] {_owner.name}: Searching… turns left before Idle: {_searchTurnsRemaining}.");

            if (_searchTurnsRemaining > 0)
                _searchTurnsRemaining--;

            if (_searchTurnsRemaining <= 0)
                TransitionToIdle("search complete");
        }

        private void RunAlertTurn(PlayerController player)
        {
            Vector3Int playerPos = player.GridPosition;
            Vector3Int diff = playerPos - _owner.GridPosition;
            int cheb = Mathf.Max(Mathf.Abs(diff.x), Mathf.Abs(diff.y));
            if (cheb <= 1)
            {
                _owner.BrainAttackPlayer();
                return;
            }

            if (_owner.BrainMapManager != null
                && GridManager.Instance != null
                && GridAStarPathfinder.TryGetFirstStepTowards(
                    _owner.GridPosition,
                    playerPos,
                    _owner.gameObject,
                    _owner.BrainMapManager,
                    GridManager.Instance,
                    out Vector3Int firstStep))
            {
                Vector3Int step = firstStep - _owner.GridPosition;
                if (_owner.TryMove(step) && verboseLogging)
                    Debug.Log($"[AI-BRAIN] {_owner.name}: Alert chase step toward player.");
                return;
            }

            Vector3Int fallback = GetFallbackCardinalStep(playerPos);
            if (_owner.TryMove(fallback) && verboseLogging)
                Debug.Log($"[AI-BRAIN] {_owner.name}: Alert fallback step.");
        }

        private bool TryPromoteSightToAlert(PlayerController player, string context)
        {
            bool visible = _owner.ComputePlayerVisible(player, out ConeVisionZone zone);
            bool newly = visible && !_playerVisibleLatch;
            if (newly)
            {
                Vector3Int p = player.GridPosition;
                Debug.Log($"[SENSE-SIGHT] {_owner.name} detected {player.name} at ({p.x},{p.y}) (Zone: {zone}) [{context}].");
            }

            _playerVisibleLatch = visible;

            if (_state == EnemyAiState.Alert)
                return false;

            if (!visible)
                return false;

            EnterAlert(player, $"sight ({context}, zone={zone})");
            return true;
        }

        private void EnterAlert(PlayerController player, string reason)
        {
            EnemyAiState prev = _state;
            _state = EnemyAiState.Alert;
            _searchTurnsRemaining = 0;
            Debug.Log($"[AI-STATE] {_owner.name}: {prev} → Alert ({reason}).");

            if (prev != EnemyAiState.Alert)
                PerformTheShout(_owner, player, shoutChebyshevRadius);
        }

        private void TransitionToIdle(string reason)
        {
            EnemyAiState prev = _state;
            _state = EnemyAiState.Idle;
            _hasLastHeard = false;
            _searchTurnsRemaining = 0;
            Debug.Log($"[AI-STATE] {_owner.name}: {prev} → Idle ({reason}).");
        }

        private bool CanSeeLastHeardTile()
        {
            if (!_hasLastHeard || _owner.BrainMapManager == null)
                return false;

            ShadowCaster.IsOpaque isOpaque = pos => !_owner.BrainMapManager.IsWalkable(pos);
            return ShadowCaster.IsVisible(
                _owner.GridPosition,
                _lastHeardPosition,
                _owner.VisionRange,
                isOpaque);
        }

        private void TryStepTowards(Vector3Int goal, string context)
        {
            if (_owner.BrainMapManager == null || GridManager.Instance == null)
            {
                if (verboseLogging)
                    Debug.LogWarning($"[AI-BRAIN] {_owner.name}: Cannot path ({context}); map or grid missing.");
                return;
            }

            if (GridAStarPathfinder.TryGetFirstStepTowards(
                    _owner.GridPosition,
                    goal,
                    _owner.gameObject,
                    _owner.BrainMapManager,
                    GridManager.Instance,
                    out Vector3Int firstStep))
            {
                Vector3Int step = firstStep - _owner.GridPosition;
                if (_owner.TryMove(step) && verboseLogging)
                    Debug.Log($"[AI-BRAIN] {_owner.name}: Move ({context}) toward ({goal.x},{goal.y}).");
                return;
            }

            Vector3Int fallback = GetFallbackCardinalStep(goal);
            if (_owner.TryMove(fallback) && verboseLogging)
                Debug.Log($"[AI-BRAIN] {_owner.name}: Fallback move ({context}).");
        }

        private static Vector3Int GetFallbackCardinalStep(Vector3Int fromSelf, Vector3Int target)
        {
            Vector3Int diff = target - fromSelf;
            if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
                return new Vector3Int(diff.x > 0 ? 1 : -1, 0, 0);
            return new Vector3Int(0, diff.y > 0 ? 1 : -1, 0);
        }

        private Vector3Int GetFallbackCardinalStep(Vector3Int target)
        {
            return GetFallbackCardinalStep(_owner.GridPosition, target);
        }
    }
}
