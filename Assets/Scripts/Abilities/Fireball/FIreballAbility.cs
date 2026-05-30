using System.Collections.Generic;
using JRogue.Core.Actor;
using JRogue.Core.Targeting;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Ability.Fireball
{
    [CreateAssetMenu(fileName = "NewFireball", menuName = "JRogue/Abilities/Fireball")]
    public class FireballAbility : AbilityAction
    {
        public int fireDamage = 15;
        public bool canHurtCaster = true;

        public override bool CanExecute(GameObject user)
        {
            // Standard Soul Power check (Milestone 13 logic)
            return true;
        }

        protected override bool ExecuteCore(GameObject user)
        {
            Debug.Log("Do not cast fireball with this method");
            return false; // Needs target!
        }

        protected override bool ExecuteCore(GameObject user, Vector3Int targetTile)
        {
            Debug.Log($"Casting Fireball at {targetTile}!");

            if (!user.TryGetComponent(out JRogue.Actors.BaseActor caster))
                return false;

            var ctx = new SplashZoneContext(caster.GridPosition, targetTile, caster.currentFacing);
            IReadOnlyList<Vector3Int> cells = SplashZoneResolver.GetEffectCells(ResolveSplashZone(), ctx);
            List<IBattleTarget> targets = TargetingResolver.GetTargetsInCells(cells);

            // Loop through and apply damage
            foreach (var target in targets)
            {
                // Self-damage check
                if (target.Owner == user && !canHurtCaster)
                {
                    continue;
                }

                // Apply damage via the interface
                // Note: We cast back to BaseActor if we want to use specific DamageTypes
                if (target is JRogue.Actors.BaseActor actor)
                {
                    actor.TakeDamage(fireDamage, DamageType.Fire, user);
                }
                else
                {
                    // Fallback for non-actor targets (destructibles, etc)
                    target.TakeDamage(fireDamage, user);
                }
            }

            // 3. Trigger Visual FX (to be implemented in a later milestone)
            return true;
        }
    }
}