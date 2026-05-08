using JRogue.Actors.Components;
using JRogue.Core.Actor;
using JRogue.Manager.Essence;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Service.Sensing;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Actors
{
    public enum FacingDirection
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    // Forces Unity to add the components if they are missing
    [RequireComponent(typeof(CharacterStats))]
    [RequireComponent(typeof(EssenceSlotManager))]
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(GridMover))]
    public abstract class BaseActor : MonoBehaviour, IBattleTarget, INoiseProducer
    {
        [Header("References")]
        public CharacterStats stats;
        public FacingDirection currentFacing = FacingDirection.North;

        [Header("Identity")]
        [SerializeField, Tooltip("Categorical 'kind' of this actor for detection filters (radar etc.). Set a single bit per actor.")]
        private EssenceType essenceType = EssenceType.Life;
        public EssenceType EssenceType => essenceType;

        protected MapManager mapManager;
        protected EssenceSlotManager essenceManager;
        protected HealthComponent health;
        protected GridMover mover;

        // Cached singleton refs. Populated lazily via EnsureManagers() because
        // singleton .Instance may not be set yet during Awake order, and unit
        // tests sometimes skip Start. Once non-null, no more lookups happen.
        protected GridManager gridManager;
        protected TurnManager turnManager;
        protected PartyManager partyManager;

        public Vector3Int GridPosition => mover != null ? mover.GridPosition : Vector3Int.FloorToInt(transform.position);

        // The actor IS its own owner for IBattleTarget purposes
        public GameObject Owner => this.gameObject;

        protected virtual void Awake()
        {
            stats = GetComponent<CharacterStats>();
            essenceManager = GetComponent<EssenceSlotManager>();
            health = GetComponent<HealthComponent>();
            mover = GetComponent<GridMover>();

            // BaseActor is the only place that knows how this kind of actor dies,
            // so it owns the Died subscription. Subclasses still implement Die().
            health.Died += HandleDied;
        }

        protected virtual void Start()
        {
            mapManager = MapManager.Instance != null
                ? MapManager.Instance
                : FindAnyObjectByType<MapManager>();
            EnsureManagers();
        }

        protected void EnsureManagers()
        {
            if (gridManager == null) gridManager = GridManager.Instance;
            if (turnManager == null) turnManager = TurnManager.Instance;
            if (partyManager == null) partyManager = PartyManager.Instance;
        }

        protected virtual void OnDestroy()
        {
            if (health != null) health.Died -= HandleDied;
        }

        private void HandleDied() => Die();

        public void TakeDamage(int amount, GameObject source)
        {
            // Defaulting to Blunt for generic IBattleTarget calls
            health.TakeDamage(amount, DamageType.Blunt);
        }

        public virtual void TakeDamage(int rawDamage, DamageType type)
        {
            health.TakeDamage(rawDamage, type);
        }

        public bool TryMove(Vector3Int direction)
        {
            EnsureManagers();

            if (direction != Vector3Int.zero)
            {
                UpdateFacingFromDirection(direction);
            }

            // Source-of-truth check: don't let the player act twice in one turn
            if (gameObject.CompareTag("Player")
                && turnManager != null
                && !turnManager.CanActorTakeAction(this.gameObject))
            {
                Debug.Log($"{gameObject.name} has already moved this turn. Skipping move.");
                return false;
            }

            Vector3Int targetPos = GridPosition + direction;

            // 1. Check Map Collision
            if (!mapManager.IsWalkable(targetPos)) return false;

            // 2. Check for Actors at the target cell
            IBattleTarget target = gridManager != null ? gridManager.GetActorAt(targetPos) : null;

            if (target != null && target.Owner != this.gameObject)
            {
                if (target is BaseActor targetActor)
                {
                    OnBump(targetActor);

                    // Swap allowed when target is a party member; otherwise the
                    // bump itself replaces the move (combat consumes the action).
                    bool isPartyMember = partyManager != null
                        && partyManager.partyMembers.Contains(targetActor);
                    if (!isPartyMember)
                    {
                        // Combat ended the turn — notify the TurnManager so
                        // the player turn doesn't hang.
                        turnManager?.OnPlayerActionComplete(this.gameObject);
                        return true;
                    }
                }
            }

            // 3. Defensive re-check: another actor may have appeared
            IBattleTarget occupant = gridManager != null ? gridManager.GetActorAt(targetPos) : null;
            if (occupant != null && occupant.Owner != this.gameObject)
            {
                return false;
            }

            // 4. Perform the actual move via GridMover
            return mover.ApplyPositionChange(targetPos);
        }

        public void ApplyPositionChange(Vector3Int newPosition)
        {
            mover.ApplyPositionChange(newPosition);
        }

        public virtual void ProduceNoise(int volume)
        {
            if (volume <= 0) return;
            AcousticsService.Broadcast(this, volume);
        }

        /// <summary>
        /// Invoked when this actor perceives noise from elsewhere. Default: no-op.
        /// Override on types that should react (player, enemies, future allies with hearing).
        /// </summary>
        public virtual void OnHearNoise(BaseActor source, Vector3Int origin, int rawVolume, int effectiveVolume)
        {
        }

        public Vector3Int GetSmartStepTowards(Vector3Int target)
        {
            Vector3Int diff = target - GridPosition;
            Vector3Int step = Vector3Int.zero;

            // Manhattan-style step selection: prefer the larger axis
            if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
                step.x = (int)Mathf.Sign(diff.x);
            else if (diff.y != 0)
                step.y = (int)Mathf.Sign(diff.y);

            return GridPosition + step;
        }

        // Logic for what happens when walking into someone. Subclasses override
        // to add combat (PlayerController), reactions, etc.
        protected virtual void OnBump(BaseActor target)
        {
            Debug.Log($"{gameObject.name} bumped into {target.gameObject.name} and initiates combat!");
        }

        public void SyncPosition() => mover.SyncPosition();

        // Forces Player and Enemy to define their own death logic
        protected abstract void Die();

        public Vector3Int GetGridPosition() => GridPosition;

        public void SetGridPosition(Vector3Int newPos) => mover.SetGridPosition(newPos);

        public Vector2 GetFacingVector()
        {
            switch (currentFacing)
            {
                case FacingDirection.North: return Vector2.up;
                case FacingDirection.NorthEast: return new Vector2(1f, 1f).normalized;
                case FacingDirection.East: return Vector2.right;
                case FacingDirection.SouthEast: return new Vector2(1f, -1f).normalized;
                case FacingDirection.South: return Vector2.down;
                case FacingDirection.SouthWest: return new Vector2(-1f, -1f).normalized;
                case FacingDirection.West: return Vector2.left;
                case FacingDirection.NorthWest: return new Vector2(-1f, 1f).normalized;
                default: return Vector2.up;
            }
        }

        protected void UpdateFacingFromDirection(Vector3Int direction)
        {
            int dx = Mathf.Clamp(direction.x, -1, 1);
            int dy = Mathf.Clamp(direction.y, -1, 1);

            if (dx == 0 && dy > 0) currentFacing = FacingDirection.North;
            else if (dx > 0 && dy > 0) currentFacing = FacingDirection.NorthEast;
            else if (dx > 0 && dy == 0) currentFacing = FacingDirection.East;
            else if (dx > 0 && dy < 0) currentFacing = FacingDirection.SouthEast;
            else if (dx == 0 && dy < 0) currentFacing = FacingDirection.South;
            else if (dx < 0 && dy < 0) currentFacing = FacingDirection.SouthWest;
            else if (dx < 0 && dy == 0) currentFacing = FacingDirection.West;
            else if (dx < 0 && dy > 0) currentFacing = FacingDirection.NorthWest;
        }
    }
}
