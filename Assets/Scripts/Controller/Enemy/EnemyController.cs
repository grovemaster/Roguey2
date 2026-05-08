using JRogue.Actors;
using JRogue.Controller.Player;
using JRogue.Manager.Grid;
using JRogue.Pathfinding;
using UnityEngine;

namespace JRogue.Controller.Enemy
{
    public class EnemyController : BaseActor
    {
        public int hp = 3;
        public int attackPower = 1;
        [Header("Acoustics")]
        [SerializeField, Min(0)] private int meleeNoiseVolume = 5;
        [Header("Sight")]
        [SerializeField, Min(1)] private int visionRange = 8;
        [SerializeField, Range(0f, 180f)] private float primaryConeAngle = 135f;
        [SerializeField, Range(0.1f, 1f)] private float peripheralRangeMultiplier = 0.5f;
        public int VisionRange => visionRange;
        public float PrimaryConeAngle => primaryConeAngle;
        public float PeripheralRangeMultiplier => peripheralRangeMultiplier;

        private PlayerController player;
        private bool playerWasVisibleLastTurn;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        new void Start()
        {
            base.Start();
            player = FindAnyObjectByType<PlayerController>();
            // gridPosition = Vector3Int.FloorToInt(transform.position);
            // SyncPosition();
        }

        // This will be called by the TurnManager
        public void TakeTurn()
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;

            bool detectedThisTurn = DetectAndLogPlayerIfNew();

            Vector3Int playerPos = player.GetGridPosition();

            // 8-way adjacency (Chebyshev): matches diagonal movement and melee range.
            Vector3Int diff = playerPos - gridPosition;
            int cheb = Mathf.Max(Mathf.Abs(diff.x), Mathf.Abs(diff.y));
            if (cheb <= 1)
            {
                AttackPlayer();
                return;
            }

            if (mapManager != null
                && GridManager.Instance != null
                && GridAStarPathfinder.TryGetFirstStepTowards(
                    gridPosition,
                    playerPos,
                    gameObject,
                    mapManager,
                    GridManager.Instance,
                    out Vector3Int firstStep))
            {
                Vector3Int step = firstStep - gridPosition;
                bool moved = TryMove(step);
                if (moved && !detectedThisTurn)
                {
                    DetectAndLogPlayerIfNew();
                }
                return;
            }

            Vector3Int direction = GetFallbackCardinalStep(playerPos);
            bool fallbackMoved = TryMove(direction);
            if (fallbackMoved && !detectedThisTurn)
            {
                DetectAndLogPlayerIfNew();
            }
        }

        private Vector3Int GetFallbackCardinalStep(Vector3Int target)
        {
            Vector3Int diff = target - gridPosition;
            if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
                return new Vector3Int(diff.x > 0 ? 1 : -1, 0, 0);
            return new Vector3Int(0, diff.y > 0 ? 1 : -1, 0);
        }

        private void AttackPlayer()
        {
            Debug.Log("The Enemy hits you!");
            // Future: player.TakeDamage(attackPower);
            ProduceNoise(meleeNoiseVolume);
        }

        public override void OnHearNoise(BaseActor source, Vector3Int origin, int rawVolume, int effectiveVolume)
        {
            Debug.Log($"[SENSE-HEARING] {name} heard noise of volume {rawVolume} from ({origin.x},{origin.y}). Effective Volume at Enemy: {effectiveVolume}.");
        }

        private bool DetectAndLogPlayerIfNew()
        {
            bool playerVisibleNow = Roguey2.Sensing.ConeSightUtility.TrySenseTarget(
                this,
                player.GridPosition,
                mapManager,
                visionRange,
                primaryConeAngle,
                peripheralRangeMultiplier,
                out Roguey2.Sensing.ConeVisionZone zone);

            bool newlyDetected = playerVisibleNow && !playerWasVisibleLastTurn;
            if (newlyDetected)
            {
                Vector3Int p = player.GridPosition;
                Debug.Log($"[SENSE-SIGHT] {name} detected {player.name} at ({p.x},{p.y}) (Zone: {zone}).");
            }

            playerWasVisibleLastTurn = playerVisibleNow;
            return newlyDetected;
        }

        // public void TakeDamage(int damage)
        // {
        //     hp -= damage;
        //     Debug.Log($"Enemy hit! HP left: {hp}");
        //     if (hp <= 0) Die();
        // }

        // public void TakeDamage(int rawDamage, DamageType type)
        // {
        //     CharacterStats stats = GetComponent<CharacterStats>();
        //     int resistanceValue = stats.GetResistance(type);

        //     // Calculation: Damage - Resistance. 
        //     // Positive resistance reduces damage. Negative resistance (Vulnerability) increases it.
        //     int damageAfterResistance = Mathf.Max(1, rawDamage - resistanceValue);

        //     // Apply Armor Class only for Physical damage
        //     if (type == DamageType.Blunt || type == DamageType.Slash || type == DamageType.Pierce)
        //     {
        //         damageAfterResistance = Mathf.Max(1, damageAfterResistance - (stats.ArmorClass / 5));
        //     }

        //     stats.currentHP -= damageAfterResistance;
        //     Debug.Log($"{gameObject.name} hit! Raw: {rawDamage} | After Res({resistanceValue}): {rawDamage - resistanceValue} | Final after AC: {damageAfterResistance}");
        //     Debug.Log($"{gameObject.name} took {damageAfterResistance} {type} damage. HP: {stats.currentHP}");

        //     if (stats.currentHP <= 0) Die();
        // }

        protected override void Die()
        {
            Debug.Log($"{gameObject.name} was defeated!");
            Destroy(gameObject);
        }

        // private void SyncPosition() =>
        //     transform.position = new Vector3(gridPosition.x + 0.5f, gridPosition.y + 0.5f, 0);
    }
}