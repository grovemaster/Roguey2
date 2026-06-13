using JRogue.Actors;
using JRogue.Combat;
using JRogue.Controller.Enemy;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Party;
using JRogue.Progression.Proficiency;
using JRogue.Stats;
using JRogue.World.Generation;
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
            PartyMemberDeathService.HandleDeath(this);
        }

        protected override void OnBump(BaseActor target)
        {
            Debug.Log($"{gameObject.name} bumped into {target.gameObject.name} at {target.GridPosition}.");
            if (target is EnemyController enemy)
            {
                if (!SafeZonePolicyService.TryAllowHostileAction(out string denyReason))
                {
                    Debug.Log($"{SafeZonePolicyService.LogPrefix} {denyReason}");
                    return;
                }

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
            ItemData weapon = equipment.GetItemFromEquipmentSlot(EquipmentSlot.MainHand);
            int baseDamage = equipment.GetTotalAttack(baseAttack);
            ProficiencyResolvedAction trainAction =
                ProficiencyStrikePayloadBuilder.FromMeleeWeapon(this, weapon, baseDamage);
            int damage = ProficiencyCombatResolver.ComputePhysicalDamage(
                this,
                baseDamage,
                trainAction.WeaponType,
                trainAction.DamageModulesApplied);
            DamageType damageType = trainAction.DamageModulesApplied.Count > 0
                ? trainAction.DamageModulesApplied[0].type
                : DamageType.Slash;
            enemy.TakeDamage(damage, damageType, gameObject);
            Debug.Log($"Player attacked {enemy.name} for {damage} damage!");
            ProduceNoise(meleeNoiseVolume);
            ProficiencyXpDispatcher.Dispatch(this, trainAction);
        }

        void AttackUnarmed(EnemyController enemy)
        {
            ProficiencyResolvedAction trainAction = ProficiencyStrikePayloadBuilder.FromUnarmed(baseAttack);
            int damage = ProficiencyCombatResolver.ComputePhysicalDamage(
                this,
                baseAttack,
                WeaponType.Unarmed,
                trainAction.DamageModulesApplied);
            enemy.TakeDamage(damage, DamageType.Blunt, gameObject);
            Debug.Log($"Player attacked {enemy.name} unarmed for {damage} damage!");
            ProduceNoise(meleeNoiseVolume);
            ProficiencyXpDispatcher.Dispatch(this, trainAction);
        }

        public override void OnHearNoise(BaseActor source, Vector3Int origin, int rawVolume, int effectiveVolume)
        {
            Debug.Log($"[SENSE-HEARING] {name} heard noise of volume {rawVolume} from ({origin.x},{origin.y}). Effective Volume at Player: {effectiveVolume}.");
        }
    }
}
