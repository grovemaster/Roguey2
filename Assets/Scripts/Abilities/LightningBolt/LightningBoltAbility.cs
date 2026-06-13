using System.Collections.Generic;
using JRogue.Core.Actor;
using JRogue.Core.Targeting;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Ability.LightningBolt
{
    [CreateAssetMenu(fileName = "LightningBolt_Standard", menuName = "JRogue/Abilities/Lightning Bolt")]
    public class LightningBoltAbility : AbilityAction
    {
        public int lightningDamage = 12;

        public override bool WouldHarm(IBattleTarget target, GameObject caster) =>
            lightningDamage > 0 && target is JRogue.Actors.BaseActor;

        public override bool CanExecute(GameObject user) => true;

        protected override bool ExecuteCore(GameObject user)
        {
            Debug.Log("[Lightning Bolt] Requires a target tile.");
            return false;
        }

        protected override bool ExecuteCore(GameObject user, Vector3Int targetTile)
        {
            if (!user.TryGetComponent(out JRogue.Actors.BaseActor caster))
                return false;

            List<IBattleTarget> targets = TargetingResolver.GetTargetsOnTile(targetTile);
            if (targets.Count == 0)
                return true;

            for (int i = 0; i < targets.Count; i++)
            {
                IBattleTarget target = targets[i];
                if (target is JRogue.Actors.BaseActor actor)
                    actor.TakeDamage(lightningDamage, DamageType.Lightning, user);
                else
                    target.TakeDamage(lightningDamage, user);
            }

            return true;
        }
    }
}
