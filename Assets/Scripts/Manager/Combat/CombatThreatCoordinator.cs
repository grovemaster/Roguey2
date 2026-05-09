using System;
using System.Collections.Generic;
using System.Text;
using JRogue.Actors;
using JRogue.Controller.Enemy;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Manager.Visibility.Algorithm;
using UnityEngine;

namespace JRogue.Manager.Combat
{
    /// <summary>
    /// Central authority for party-level <see cref="CombatTensionState"/>.
    /// <para><b>Deterministic evaluation</b> (runs in gameplay order):</para>
    /// <list type="bullet">
    /// <item>Once after every enemy has finished <see cref="EnemyController.TakeTurn"/> for the wave,
    ///before <see cref="TurnManager"/> restores <see cref="GameState"/> (which is declared alongside <see cref="TurnManager"/> in this project).</item>
    /// <item>Whenever <see cref="TurnManager.OnPlayerActionComplete"/> resolves (party tiles may move).</item>
    /// </list>
    /// Pursuit decay (Alert enemies dropping after K waves without refresh) ticks only on the post-enemy-wave hook.
    /// </summary>
    public sealed class CombatThreatCoordinator : MonoBehaviour
    {
        static CombatThreatCoordinator _instance;

        public static CombatThreatCoordinator Instance => _instance;

        [Tooltip("Enemy Alert pursuit expires after this many completed enemy phases without pursuit refresh.")]
        [SerializeField, Min(1)]
        int pursuitDecayEnemyWaves = 3;

        [Tooltip(
            "Chebyshev distance wall-piercing \"scrying\": any party member this close senses hostile enemies for InCombat LOS.")]
        [SerializeField, Min(0)]
        int remoteSenseChebyshevRadius = 16;

        [Tooltip("Fallback shadow-cast range per party member if no VisibilityManager is present.")]
        [SerializeField, Min(1)]
        int tileSightRangeFallback = 8;

        [Header("Debug")]
        [SerializeField] bool verboseLogging;

        CombatTensionState _tension = CombatTensionState.OutOfCombat;
        readonly List<string> _lastContributors = new List<string>(8);

        public CombatTensionState Tension => _tension;

        public bool IsInCombat => _tension == CombatTensionState.InCombat;

        public IReadOnlyList<string> LastContributorDebugLines => _lastContributors;

        public event Action OnEnterCombat;

        public event Action OnExitCombat;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[CombatThreat] Duplicate '{name}' destroyed.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        void Start() => EvaluateThreat();

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>
        /// Call once per enemy phase, after all enemies have executed their turns.
        /// </summary>
        public void ApplyPursuitDecayAfterEnemyWave()
        {
            EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyController e = enemies[i];
                if (e == null || !IsAliveHostileThreat(e))
                    continue;

                EnemyAiBrain brain = e.GetComponent<EnemyAiBrain>();
                brain?.ApplyPursuitDecayAfterEnemyWave(pursuitDecayEnemyWaves);
            }
        }

        /// <summary>
        /// Recompute party tension from world state.
        /// </summary>
        public void EvaluateThreat()
        {
            MapManager map = MapManager.Instance != null ? MapManager.Instance : FindAnyObjectByType<MapManager>();
            VisibilityManager vis = FindAnyObjectByType<VisibilityManager>();
            int tileRange = vis != null ? vis.viewRange : tileSightRangeFallback;

            PartyManager party = PartyManager.Instance;
            _lastContributors.Clear();

            bool anyThreat = false;
            EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude);

            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || !enemy.gameObject.activeInHierarchy || !IsAliveHostileThreat(enemy))
                    continue;

                EnemyAiBrain brain = enemy.GetComponent<EnemyAiBrain>();
                bool pursuit = brain != null && brain.IsPursuingParty;

                bool tileLos = PartyHasTileShadowLos(party, enemy.GridPosition, tileRange, map);
                bool remote = PartyHasRemoteSense(party, enemy.GridPosition, remoteSenseChebyshevRadius);

                bool sightBucket = tileLos || remote;
                if (!sightBucket && !pursuit)
                    continue;

                anyThreat = true;

                if (verboseLogging)
                {
                    var sb = new StringBuilder();
                    sb.Append(enemy.name);
                    if (pursuit) sb.Append(" [Pursuit]");
                    if (tileLos) sb.Append(" [TileLOS]");
                    if (remote) sb.Append(" [RemoteSense]");
                    _lastContributors.Add(sb.ToString());
                }
            }

            SetTension(anyThreat ? CombatTensionState.InCombat : CombatTensionState.OutOfCombat);
        }

        static bool IsAliveHostileThreat(EnemyController enemy)
        {
            if (enemy.stats != null && enemy.stats.currentHP <= 0)
                return false;

            return true;
        }

        static bool PartyHasTileShadowLos(PartyManager party, Vector3Int enemyCell, int range, MapManager map)
        {
            if (party == null || map == null || range <= 0)
                return false;

            ShadowCaster.IsOpaque opaque = pos => !map.IsWalkable(pos);

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || !member.gameObject.activeInHierarchy)
                    continue;

                Vector3Int origin = new Vector3Int(member.GridPosition.x, member.GridPosition.y, 0);
                Vector3Int target = new Vector3Int(enemyCell.x, enemyCell.y, 0);

                if (ShadowCaster.IsVisible(origin, target, range, opaque))
                    return true;
            }

            return false;
        }

        static bool PartyHasRemoteSense(PartyManager party, Vector3Int enemyCell, int radius)
        {
            if (party == null || radius <= 0)
                return false;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || !member.gameObject.activeInHierarchy)
                    continue;

                int d = Chebyshev(member.GridPosition, enemyCell);
                if (d <= radius)
                    return true;
            }

            return false;
        }

        static int Chebyshev(Vector3Int a, Vector3Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        void SetTension(CombatTensionState next)
        {
            if (_tension == next)
            {
                if (verboseLogging && _lastContributors.Count > 0)
                    LogContributors(next);
                return;
            }

            _tension = next;
            if (next == CombatTensionState.InCombat)
                OnEnterCombat?.Invoke();
            else
                OnExitCombat?.Invoke();

            if (verboseLogging)
                LogContributors(next);
        }

        void LogContributors(CombatTensionState state)
        {
            if (_lastContributors.Count == 0)
                Debug.Log($"[CombatThreat] {state} (no contributing enemies this tick).");
            else
                Debug.Log($"[CombatThreat] {state}: {string.Join("; ", _lastContributors)}");
        }

        #region Optional external hooks (damage / noise / future systems)

        /// <summary>Stub: call from damage pipeline when an enemy successfully harms the party.</summary>
        public void NotifyEnemyStruckParty(EnemyController enemy)
        {
            enemy?.GetComponent<EnemyAiBrain>()?.NotifyExternalPursuitRefresh();
        }

        /// <summary>Stub: call from global noise bus if a noise should hard-refresh pursuit awareness.</summary>
        public void NotifyNoiseMightRefreshPursuit(Vector3Int origin, int volume)
        {
            // Intentionally empty — EnemyAiBrain.NotifyHeard already drives AI; wire volume thresholds here later.
        }

        #endregion
    }
}
