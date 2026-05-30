using JRogue.Actors;
using JRogue.Combat;
using JRogue.Controller.Enemy;
using JRogue.Manager.Equipment;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Controller.Player
{
    public class PlayerController : BaseActor
    {
        private EquipmentManager equipment;
        public int baseAttack = 1;

        [Header("Acoustics")]
        [SerializeField, Min(0)] private int meleeNoiseVolume = 5;

        protected override void Awake()
        {
            base.Awake();
            equipment = GetComponent<EquipmentManager>();
        }

        protected override void Die()
        {
            Debug.Log("Game Over! The Player has fallen.");
            // Future: trigger Game Over UI
        }

        protected override void OnBump(BaseActor target)
        {
            Debug.Log($"{gameObject.name} bumped into {target.gameObject.name} at {target.GridPosition}.");
            if (target is EnemyController enemy)
            {
                if (BowRangedCombatService.HasBowEquipped(this))
                {
                    equipment?.TryEnsureDefaultAmmoEquipped();
                    if (BowRangedCombatService.TryExecuteBowShot(this, enemy.GridPosition, 1))
                        return;

                    BowRangedCombatService.LogBumpUnarmed();
                    AttackUnarmed(enemy);
                    return;
                }

                AttackEnemy(enemy);
            }
        }

        void AttackEnemy(EnemyController enemy)
        {
            int damage = equipment.GetTotalAttack(baseAttack);
            enemy.TakeDamage(damage, DamageType.Slash, gameObject);
            Debug.Log($"Player attacked {enemy.name} for {damage} damage!");
            ProduceNoise(meleeNoiseVolume);
        }

        void AttackUnarmed(EnemyController enemy)
        {
            int damage = Mathf.Max(1, baseAttack);
            enemy.TakeDamage(damage, DamageType.Blunt, gameObject);
            Debug.Log($"Player attacked {enemy.name} unarmed for {damage} damage!");
            ProduceNoise(meleeNoiseVolume);
        }

        public override void OnHearNoise(BaseActor source, Vector3Int origin, int rawVolume, int effectiveVolume)
        {
            Debug.Log($"[SENSE-HEARING] {name} heard noise of volume {rawVolume} from ({origin.x},{origin.y}). Effective Volume at Player: {effectiveVolume}.");
        }
    }
}
