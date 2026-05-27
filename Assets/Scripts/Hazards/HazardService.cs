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

        readonly Dictionary<Vector3Int, EnvironmentalHazardDefinition> _hazards =
            new Dictionary<Vector3Int, EnvironmentalHazardDefinition>();

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
            _hazards.TryGetValue(cell, out EnvironmentalHazardDefinition def) ? def : null;

        public bool HasHazardAt(Vector3Int cell) => _hazards.ContainsKey(cell);

        public void Register(Vector3Int cell, EnvironmentalHazardDefinition definition)
        {
            if (definition == null)
                return;

            _hazards[cell] = definition;
            PaintOverlay(cell, definition);
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
                    Register(pos, hazardTile.hazardDefinition);
            }
        }

        public bool CanEnter(Vector3Int cell, BaseActor actor)
        {
            EnvironmentalHazardDefinition def = GetHazardAt(cell);
            if (def == null)
                return true;

            if (def.kind == EnvironmentalHazardKind.Persistent)
                return true;

            if (HazardPassageEvaluator.CanEnter(def, actor))
                return true;

            LogPassageBlocked(def, actor);
            return false;
        }

        public bool RequiresEnterConfirm(Vector3Int cell)
        {
            EnvironmentalHazardDefinition def = GetHazardAt(cell);
            return def != null && def.kind == EnvironmentalHazardKind.Persistent;
        }

        public bool IsEnemyAvoidCell(Vector3Int cell)
        {
            EnvironmentalHazardDefinition def = GetHazardAt(cell);
            return def != null && def.avoidForEnemyPathing;
        }

        public void OnActorEntered(Vector3Int cell, BaseActor actor)
        {
            EnvironmentalHazardDefinition def = GetHazardAt(cell);
            if (def == null || def.kind != EnvironmentalHazardKind.Persistent)
                return;

            ApplyPersistentDamage(actor, def, "enter");
        }

        public void OnActorWaitOnCell(BaseActor actor)
        {
            if (actor == null)
                return;

            EnvironmentalHazardDefinition def = GetHazardAt(actor.GridPosition);
            if (def == null || def.kind != EnvironmentalHazardKind.Persistent)
                return;

            ApplyPersistentDamage(actor, def, "wait");
        }

        public void TickOccupancyOnPlayerPhaseStart()
        {
            PartyManager party = PartyManager.Instance;
            if (party == null)
                return;

            foreach (BaseActor member in party.partyMembers)
            {
                if (member == null)
                    continue;

                EnvironmentalHazardDefinition def = GetHazardAt(member.GridPosition);
                if (def == null || def.kind != EnvironmentalHazardKind.Persistent)
                    continue;

                ApplyPersistentDamage(member, def, "phase-start");
            }
        }

        public void TickOccupancyOnEnemyTurnStart(BaseActor enemy)
        {
            if (enemy == null)
                return;

            EnvironmentalHazardDefinition def = GetHazardAt(enemy.GridPosition);
            if (def == null || def.kind != EnvironmentalHazardKind.Persistent)
                return;

            ApplyPersistentDamage(enemy, def, "enemy-phase-start");
        }

        public void NotifyActorMovedOntoCell(BaseActor actor)
        {
            if (actor == null)
                return;

            OnActorEntered(actor.GridPosition, actor);
        }

        void ApplyPersistentDamage(BaseActor actor, EnvironmentalHazardDefinition def, string trigger)
        {
            if (def.persistentDamagePerTrigger <= 0)
                return;

            HealthComponent health = actor.GetComponent<HealthComponent>();
            if (health == null)
                return;

            health.TakeDamage(def.persistentDamagePerTrigger, def.persistentDamageType, gameObject);
            Debug.Log(
                $"[Hazard] {def.displayName} ({trigger}) dealt {def.persistentDamagePerTrigger} " +
                $"{def.persistentDamageType} to {actor.DisplayName} at {actor.GridPosition}.");
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
        }

        static void LogPassageBlocked(EnvironmentalHazardDefinition def, BaseActor actor)
        {
            int str = actor.stats != null ? actor.stats.Strength.GetValue() : 0;
            Debug.Log(
                $"[Hazard] {actor.DisplayName} cannot enter {def.displayName} (STR {str} < {def.requiredStrength}).");
        }
    }
}
