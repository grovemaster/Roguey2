using JRogue.Actors;
using JRogue.Controller.Player;
using JRogue.Manager.Map;
using JRogue.Racial;
using Roguey2.Sensing;
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

        /// <summary>For <see cref="EnemyAiBrain"/> pathing and LOS (same assembly).</summary>
        internal MapManager BrainMapManager => mapManager;

        private PlayerController player;
        private EnemyAiBrain brain;

        protected override void Awake()
        {
            base.Awake();
            brain = GetComponent<EnemyAiBrain>();
            if (brain == null)
                brain = gameObject.AddComponent<EnemyAiBrain>();
            brain.Bind(this);
        }

        protected override void Start()
        {
            base.Start();
            player = FindAnyObjectByType<PlayerController>();
        }

        internal void BrainEnsureManagers() => EnsureManagers();

        /// <summary>Cone + shadow LOS sight check used by the AI brain and logging.</summary>
        internal bool ComputePlayerVisible(PlayerController playerController, out ConeVisionZone zone)
        {
            zone = ConeVisionZone.None;
            if (playerController == null || mapManager == null)
                return false;

            return ConeSightUtility.TrySenseTarget(
                this,
                playerController.GridPosition,
                mapManager,
                visionRange,
                primaryConeAngle,
                peripheralRangeMultiplier,
                out zone);
        }

        // Called by the TurnManager during ENEMY_TURN
        public void TakeTurn()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            if (player == null)
                return;

            RacialPassiveHooks.NotifyTurnStart(gameObject);
            essenceManager?.NotifyTurnStart();
            brain.ExecuteTurn(player);
        }

        public override void OnHearNoise(BaseActor source, Vector3Int origin, int rawVolume, int effectiveVolume)
        {
            Debug.Log(
                $"[SENSE-HEARING] {name} heard noise of volume {rawVolume} from ({origin.x},{origin.y}). Effective Volume at Enemy: {effectiveVolume}.");
            brain.NotifyHeard(origin, rawVolume, effectiveVolume);
        }

        internal void BrainAttackPlayer() => AttackPlayer();

        private void AttackPlayer()
        {
            Debug.Log("The Enemy hits you!");
            // Future: player.TakeDamage(attackPower);
            ProduceNoise(meleeNoiseVolume);
        }

        protected override void Die()
        {
            Debug.Log($"{gameObject.name} was defeated!");
            Destroy(gameObject);
        }
    }
}
