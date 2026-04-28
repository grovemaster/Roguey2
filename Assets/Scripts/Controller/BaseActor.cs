using UnityEngine;
using JRogue.Stats;
using JRogue.Manager.Map;
using JRogue.Manager.Essence;
using JRogue.Core.Actor;
using JRogue.Manager.Grid;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;

namespace JRogue.Actors
{
    // 'abstract' means this is a template for other classes
    // These attributes force Unity to add the components if they are missing
    [RequireComponent(typeof(CharacterStats))]
    [RequireComponent(typeof(EssenceSlotManager))]
    public abstract class BaseActor : MonoBehaviour, IBattleTarget
    {
        [Header("References")]
        public CharacterStats stats;

        protected MapManager mapManager;
        protected EssenceSlotManager essenceManager;
        protected Vector3Int gridPosition;

        public Vector3Int GridPosition => gridPosition;

        public GameObject Owner => this.gameObject; // Simple: the actor is its own owner

        protected virtual void Awake()
        {
            stats = GetComponent<CharacterStats>();
            essenceManager = GetComponent<EssenceSlotManager>();
        }

        protected virtual void Start()
        {
            // Every actor needs to know about the map to move
            mapManager = FindAnyObjectByType<MapManager>();
            gridPosition = Vector3Int.FloorToInt(transform.position);
            // Register with the Spatial Hash on start
            GridManager.Instance?.RegisterActor(gridPosition, this);
            SyncPosition();
        }

        private void OnDestroy()
        {
            // Clean up the Spatial Hash when an actor is removed
            GridManager.Instance?.UnregisterActor(gridPosition);
        }

        public void TakeDamage(int amount, GameObject source)
        {
            // Defaulting to Blunt for generic calls, or you could add a default type to the interface
            TakeDamage(amount, DamageType.Blunt);
        }

        // Shared Logic: Taking Damage
        public virtual void TakeDamage(int rawDamage, DamageType type)
        {
            int resistanceValue = stats.GetResistance(type);
            int damageAfterResistance = Mathf.Max(1, rawDamage - resistanceValue);

            // Factor in AC for physical types
            if (type == DamageType.Blunt || type == DamageType.Slash || type == DamageType.Pierce)
            {
                damageAfterResistance = Mathf.Max(1, damageAfterResistance - (stats.ArmorClass / 5));
            }

            stats.currentHP -= damageAfterResistance;

            // Check if this HP change triggered any passive thresholds (like Heroic Spirit)
            essenceManager.RefreshConditionalPassives();

            Debug.Log($"{gameObject.name} took {damageAfterResistance} {type} damage. " +
                      $"HP: {stats.currentHP}/{stats.MaxHP}");

            if (stats.currentHP <= 0)
            {
                Die();
            }
        }

        public bool TryMove(Vector3Int direction)
        {
            Vector3Int targetPos = gridPosition + direction;

            // 1. Check Map Collision
            if (!mapManager.IsWalkable(targetPos)) return false;

            // 2. Check for Actors
            IBattleTarget target = GridManager.Instance.GetActorAt(targetPos);

            if (target != null && target.Owner != this.gameObject)
            {
                if (target is BaseActor targetActor)
                {
                    OnBump(targetActor);

                    // SWAP CHECK: If the target is a party member
                    if (PartyManager.Instance.partyMembers.Contains(targetActor))
                    {
                        // Proceed to ApplyPositionChange for the swap
                    }
                    else
                    {
                        // RECONCILED: It's an enemy. The bump is the action.
                        // We MUST notify the TurnManager that this actor is done 
                        // before returning true, otherwise the turn never ends.
                        if (TurnManager.Instance != null)
                        {
                            TurnManager.Instance.OnPlayerActionComplete(this.gameObject);
                        }
                        return true;
                    }
                }
            }

            // 3. Validation: Check if we are actually allowed to land on the grid
            IBattleTarget occupant = GridManager.Instance.GetActorAt(targetPos);
            if (occupant != null && occupant.Owner != this.gameObject)
            {
                return false;
            }

            // 4. Perform actual move
            ApplyPositionChange(targetPos);
            return true;
        }

        public void ApplyPositionChange(Vector3Int newPosition)
        {
            Vector3Int oldPosition = gridPosition;

            // Interface Implementation: Perform the grid registration
            // GridManager.RegisterActor will log a [GRID-CONFLICT] if this fails.
            GridManager.Instance.RegisterActor(newPosition, this);

            // CRITICAL: Double check that the registration actually worked 
            // by asking the grid who is currently in that tile.
            if (GridManager.Instance.GetActorAt(newPosition) != (IBattleTarget)this)
            {
                // If the GridManager rejected us, we STOP here.
                // We do not update our gridPosition, and we do NOT call SyncPosition().
                return;
            }

            // Success: Update internal state
            gridPosition = newPosition;

            // Only remove from the grid if WE are the ones at the old spot.
            if (GridManager.Instance.GetActorAt(oldPosition) == (IBattleTarget)this)
            {
                GridManager.Instance.UnregisterActor(oldPosition);
            }

            // Move the physical transform
            SyncPosition();
            Debug.Log($"{gameObject.name} moved from {oldPosition} to {newPosition}");
        }

        public Vector3Int GetSmartStepTowards(Vector3Int target)
        {
            Vector3Int diff = target - gridPosition;
            Vector3Int step = Vector3Int.zero;

            // Simple Manhattan-style step selection
            if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
                step.x = (int)Mathf.Sign(diff.x);
            else if (diff.y != 0)
                step.y = (int)Mathf.Sign(diff.y);

            Vector3Int potentialPos = gridPosition + step;

            // Validation: if the direct step is blocked, we'd ideally trigger pathfinding here.
            // For now, return the intended step for InputHandler to validate.
            return potentialPos;
        }

        // Logic for what happens when walking into someone
        protected virtual void OnBump(BaseActor target)
        {
            // Default behavior: Combat
            // We will expand this for Milestones 12-14 (Essences/Skills)
            Debug.Log($"{gameObject.name} bumped into {target.gameObject.name} and initiates combat!");
        }

        private BaseActor FindActorAt(Vector3Int pos)
        {
            // We check all Actors. In the future, Milestone 15 will optimize this list.
            foreach (var actor in FindObjectsByType<BaseActor>())
            {
                if (actor.GetGridPosition() == pos) return actor;
            }
            return null;
        }

        protected void SyncPosition() =>
        transform.position = new Vector3(gridPosition.x + 0.5f, gridPosition.y + 0.5f, 0);

        // 'abstract' forces the Player and Enemy to define their own death logic
        protected abstract void Die();

        public Vector3Int GetGridPosition() => gridPosition;

        public void SetGridPosition(Vector3Int newPos) => gridPosition = newPos;
    }
}