using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Stats;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Hazards
{
    public sealed class HazardService : MonoBehaviour
    {
        public static HazardService Instance { get; private set; }

        [SerializeField] Tilemap hazardOverlayMap;

        readonly Dictionary<Vector3Int, HazardCellState> _hazards =
            new Dictionary<Vector3Int, HazardCellState>();

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (hazardOverlayMap == null && MapManager.Instance != null)
                hazardOverlayMap = MapManager.Instance.HazardOverlayMap;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetOverlayMap(Tilemap overlay) => hazardOverlayMap = overlay;

        public EnvironmentalHazardDefinition GetHazardAt(Vector3Int cell) =>
            TryGetState(cell, out HazardCellState state) ? state.Definition : null;

        public bool HasHazardAt(Vector3Int cell) => _hazards.ContainsKey(cell);

        public bool IsHiddenToPlayer(Vector3Int cell) =>
            TryGetState(cell, out HazardCellState state) && state.IsHiddenToPlayer;

        public bool IsRevealedToPlayer(Vector3Int cell) =>
            !TryGetState(cell, out HazardCellState state) || state.IsRevealed;

        public void Register(Vector3Int cell, EnvironmentalHazardDefinition definition, bool startHidden = false)
        {
            if (definition == null)
                return;

            var state = new HazardCellState(definition, startHidden);
            _hazards[cell] = state;
            RefreshOverlayVisual(cell, state);
        }

        public void RegisterFromOverlayTilemap(Tilemap overlay)
        {
            if (overlay == null)
                return;

            BoundsInt bounds = overlay.cellBounds;
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                TileBase tile = overlay.GetTile(pos);
                if (tile is EnvironmentalHazardTile hazardTile && hazardTile.hazardDefinition != null)
                    Register(pos, hazardTile.hazardDefinition, hazardTile.startHidden);
            }
        }

        public bool CanEnter(Vector3Int cell, BaseActor actor)
        {
            if (!TryGetState(cell, out HazardCellState state) || state.Definition == null)
                return true;

            // Occupants may stand on a revealed passage hazard they no longer qualify to enter.
            if (actor != null && actor.GridPosition == cell)
                return true;

            EnvironmentalHazardDefinition def = state.Definition;

            if (state.IsHiddenToPlayer)
                return true;

            if (def.kind == EnvironmentalHazardKind.Persistent)
                return true;

            if (HazardPassageEvaluator.MeetsPassageCondition(def, actor))
                return true;

            LogPassageBlocked(def, actor);
            return false;
        }

        public bool CanExit(Vector3Int cell, BaseActor actor)
        {
            EnvironmentalHazardDefinition def = GetHazardAt(cell);
            return HazardExitEvaluator.CanExit(def, actor);
        }

        public bool RequiresEnterConfirm(Vector3Int cell)
        {
            if (!TryGetState(cell, out HazardCellState state) || state.Definition == null)
                return false;

            if (state.IsHiddenToPlayer)
                return false;

            return state.Definition.kind == EnvironmentalHazardKind.Persistent;
        }

        /// <summary>
        /// Revealed hazards that pathfinding should treat as undesirable (enemies and party followers).
        /// </summary>
        public bool IsPathingAvoidCell(Vector3Int cell)
        {
            EnvironmentalHazardDefinition def = GetHazardAt(cell);
            if (def == null || !def.avoidForEnemyPathing)
                return false;

            return IsRevealedToPlayer(cell);
        }

        public bool IsEnemyAvoidCell(Vector3Int cell) => IsPathingAvoidCell(cell);

        public void OnActorEntered(Vector3Int cell, BaseActor actor)
        {
            if (!TryGetState(cell, out HazardCellState state) || state.Definition == null)
                return;

            EnvironmentalHazardDefinition def = state.Definition;
            bool wasHidden = state.IsHiddenToPlayer;

            if (wasHidden && def.hiddenDetection.revealOnEnter)
                Reveal(cell, state, "enter");

            ApplyOccupancyEffects(actor, def, "enter", skipIfJustEnteredHiddenPassage: wasHidden);
        }

        public void OnActorWaitOnCell(BaseActor actor)
        {
            if (actor == null)
                return;

            EnvironmentalHazardDefinition def = GetHazardAt(actor.GridPosition);
            if (def == null)
                return;

            ApplyOccupancyEffects(actor, def, "wait");
        }

        public void TickOccupancyOnPlayerPhaseStart()
        {
            RefreshHiddenHazardDetection();

            PartyManager party = PartyManager.Instance;
            if (party == null)
                return;

            foreach (BaseActor member in party.partyMembers)
            {
                if (member == null)
                    continue;

                EnvironmentalHazardDefinition def = GetHazardAt(member.GridPosition);
                if (def == null)
                    continue;

                ApplyOccupancyEffects(member, def, "phase-start");
            }
        }

        public void TickOccupancyOnEnemyTurnStart(BaseActor enemy)
        {
            if (enemy == null)
                return;

            EnvironmentalHazardDefinition def = GetHazardAt(enemy.GridPosition);
            if (def == null)
                return;

            ApplyOccupancyEffects(enemy, def, "enemy-phase-start");
        }

        public void NotifyActorMovedOntoCell(BaseActor actor)
        {
            if (actor == null)
                return;

            RefreshHiddenHazardDetection();
            OnActorEntered(actor.GridPosition, actor);
        }

        public void RefreshHiddenHazardDetection()
        {
            MapManager map = MapManager.Instance;
            if (map == null || _hazards.Count == 0)
                return;

            foreach (KeyValuePair<Vector3Int, HazardCellState> entry in _hazards)
            {
                HazardCellState state = entry.Value;
                if (!state.IsHiddenToPlayer)
                    continue;

                EnvironmentalHazardDefinition def = state.Definition;
                if (def == null)
                    continue;

                if (!HazardDetectionEvaluator.CanAnyPartyMemberDetect(
                        entry.Key,
                        def.hiddenDetection,
                        map))
                {
                    continue;
                }

                Reveal(entry.Key, state, "detection");
            }
        }

        void ApplyOccupancyEffects(
            BaseActor actor,
            EnvironmentalHazardDefinition def,
            string trigger,
            bool skipIfJustEnteredHiddenPassage = false)
        {
            if (def == null || actor == null)
                return;

            Vector3Int cell = actor.GridPosition;
            if (!IsRevealedToPlayer(cell))
                return;

            if (def.kind == EnvironmentalHazardKind.Persistent)
            {
                ApplyDamage(
                    actor,
                    def.persistentDamagePerTrigger,
                    def.persistentDamageType,
                    def.displayName,
                    trigger);
                return;
            }

            if (def.kind != EnvironmentalHazardKind.Passage)
                return;

            if (skipIfJustEnteredHiddenPassage)
                return;

            if (HazardPassageEvaluator.MeetsPassageCondition(def, actor))
                return;

            ApplyDamage(
                actor,
                def.failedPassageOccupancyDamagePerTurn,
                def.failedPassageOccupancyDamageType,
                def.displayName,
                $"{trigger}-failed-passage");
        }

        void Reveal(Vector3Int cell, HazardCellState state, string reason)
        {
            state.Reveal();
            RefreshOverlayVisual(cell, state);
            Debug.Log(
                $"[Hazard] {state.Definition.displayName} at {cell} revealed ({reason}).");
        }

        void ApplyDamage(BaseActor actor, int amount, DamageType type, string hazardName, string trigger)
        {
            if (amount <= 0)
                return;

            HealthComponent health = actor.GetComponent<HealthComponent>();
            if (health == null)
                return;

            health.TakeDamage(amount, type, gameObject);
            Debug.Log(
                $"[Hazard] {hazardName} ({trigger}) dealt {amount} {type} to {actor.DisplayName} at {actor.GridPosition}.");
        }

        void RefreshOverlayVisual(Vector3Int cell, HazardCellState state)
        {
            if (hazardOverlayMap == null || state == null)
                return;

            if (!state.IsRevealed)
            {
                hazardOverlayMap.SetTile(cell, null);
                return;
            }

            PaintOverlay(cell, state.Definition);
        }

        void PaintOverlay(Vector3Int cell, EnvironmentalHazardDefinition definition)
        {
            if (hazardOverlayMap == null)
                return;

            if (definition.overlayTile != null)
            {
                hazardOverlayMap.SetTile(cell, definition.overlayTile);
                return;
            }

            if (definition.overlaySprite != null)
            {
                var runtimeTile = ScriptableObject.CreateInstance<Tile>();
                runtimeTile.sprite = definition.overlaySprite;
                hazardOverlayMap.SetTile(cell, runtimeTile);
            }
        }

        void Start()
        {
            if (hazardOverlayMap == null && MapManager.Instance != null)
                hazardOverlayMap = MapManager.Instance.HazardOverlayMap;

            if (hazardOverlayMap != null && _hazards.Count == 0)
                RegisterFromOverlayTilemap(hazardOverlayMap);

            RefreshHiddenHazardDetection();
        }

        bool TryGetState(Vector3Int cell, out HazardCellState state) =>
            _hazards.TryGetValue(cell, out state);

        static void LogPassageBlocked(EnvironmentalHazardDefinition def, BaseActor actor)
        {
            int str = actor.stats != null ? actor.stats.Strength.GetValue() : 0;
            Debug.Log(
                $"[Hazard] {actor.DisplayName} cannot enter {def.displayName} (STR {str} < {def.requiredStrength}).");
        }
    }
}
