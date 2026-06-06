using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Core.Actor;
using JRogue.Core.Targeting;
using JRogue.Manager.Party;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Ability.ThrowingKnife
{
    [CreateAssetMenu(fileName = "NewThrowingKnife", menuName = "JRogue/Abilities/Throwing Knife")]
    public class ThrowingKnifeAbility : AbilityAction
    {
        public int pierceDamage = 10;
        public DamageType damageType = DamageType.Pierce;
        public bool canHurtAllies;
        public bool canHurtCaster;

        public override bool WouldHarm(IBattleTarget target, GameObject caster)
        {
            if (pierceDamage <= 0 || target is not BaseActor actor)
                return false;

            PartyManager party = PartyManager.Instance;
            if (!canHurtAllies
                && party != null
                && party.partyMembers.Contains(actor))
                return false;

            return true;
        }

        public override bool CanExecute(GameObject user) => true;

        protected override bool ExecuteCore(GameObject user)
        {
            Debug.Log("Do not throw knife with this method");
            return false;
        }

        protected override bool ExecuteCore(GameObject user, Vector3Int targetTile)
        {
            List<IBattleTarget> targets = TargetingResolver.GetTargetsOnTile(targetTile);
            if (targets.Count == 0)
                return false;

            PartyManager party = PartyManager.Instance;
            bool hitAny = false;

            foreach (IBattleTarget target in targets)
            {
                if (!IsValidTarget(user, target, party, canHurtCaster, canHurtAllies))
                    continue;

                hitAny = true;
                if (target is BaseActor actor)
                    actor.TakeDamage(pierceDamage, damageType, user);
                else
                    target.TakeDamage(pierceDamage, user);
            }

            return hitAny;
        }

        static bool IsValidTarget(
            GameObject user,
            IBattleTarget target,
            PartyManager party,
            bool hurtCaster,
            bool hurtAllies)
        {
            if (target?.Owner == null)
                return false;

            if (target.Owner == user && !hurtCaster)
                return false;

            if (!hurtAllies
                && party != null
                && target.Owner.TryGetComponent(out BaseActor actor)
                && party.partyMembers.Contains(actor))
                return false;

            return true;
        }
    }
}
