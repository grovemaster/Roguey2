using System.Collections.Generic;
using JRogue.Ability.SuddenStrength;
using JRogue.Actors;
using JRogue.Core.Actor;
using JRogue.Core.Targeting;
using JRogue.Manager.Party;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Ability.ArcaneMight
{
    [CreateAssetMenu(fileName = "ArcaneMight_Standard", menuName = "JRogue/Abilities/Arcane Might")]
    public class ArcaneMightAbility : AbilityAction
    {
        public int strengthBonus = 100;
        public int durationTurns = 10;

        public override bool CanExecute(GameObject user) => user != null;

        protected override bool ExecuteCore(GameObject user)
        {
            Debug.Log("[Arcane Might] Requires an ally target.");
            return false;
        }

        protected override bool ExecuteCore(GameObject user, Vector3Int targetTile)
        {
            if (user == null)
                return false;

            List<IBattleTarget> targets = TargetingResolver.GetTargetsOnTile(targetTile);
            BaseActor ally = null;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i]?.Owner == null || targets[i].Owner == user)
                    continue;

                if (targets[i].Owner.TryGetComponent(out BaseActor candidate)
                    && IsPartyMember(candidate))
                {
                    ally = candidate;
                    break;
                }
            }

            if (ally == null)
            {
                Debug.Log("[Arcane Might] No valid party ally on target tile.");
                return false;
            }

            if (!CanApplyToAlly(ally.gameObject))
                return false;

            SuddenStrengthBuffRuntime runtime = ally.gameObject.AddComponent<SuddenStrengthBuffRuntime>();
            runtime.Apply(strengthBonus, durationTurns);
            Debug.Log(
                $"[Arcane Might] Applied +{strengthBonus} STR to {ally.DisplayName} for {durationTurns} player phases.");
            return true;
        }

        static bool CanApplyToAlly(GameObject ally)
        {
            if (ally == null)
                return false;

            CharacterStats stats = ally.GetComponent<CharacterStats>();
            SuddenStrengthBuffRuntime existing = ally.GetComponent<SuddenStrengthBuffRuntime>();
            if (existing == null)
                return true;

            if (stats != null && stats.Strength.HasModifierFromSource(existing))
                return false;

            if (Application.isPlaying)
                Object.Destroy(existing);
            else
                Object.DestroyImmediate(existing);

            return true;
        }

        static bool IsPartyMember(BaseActor actor)
        {
            PartyManager party = PartyManager.Instance;
            return party?.partyMembers != null && party.partyMembers.Contains(actor);
        }
    }
}
