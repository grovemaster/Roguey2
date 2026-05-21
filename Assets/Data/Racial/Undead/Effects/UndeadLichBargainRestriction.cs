using JRogue.Item.Essence;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "LichBargainRestriction", menuName = "JRogue/Racial/Undead/Lich's Bargain Restriction")]
    public class UndeadLichBargainRestriction : RacialRestrictionDefinition
    {
        public int charismaPenalty = -1;

        public override void OnApply(GameObject target)
        {
            UndeadRacialEffectTracker.GetOrCreate(target)?.RegisterRestriction(this);
            CharacterStats stats = target.GetComponent<CharacterStats>();
            Stat cha = stats?.GetStatByType(StatType.Charisma);
            cha?.AddModifier(charismaPenalty, this);
        }

        public override void OnRemove(GameObject target)
        {
            if (target != null && target.TryGetComponent(out UndeadRacialEffectTracker tracker))
                tracker.UnregisterRestriction(this);

            CharacterStats stats = target != null ? target.GetComponent<CharacterStats>() : null;
            Stat cha = stats?.GetStatByType(StatType.Charisma);
            cha?.RemoveModifiersFromSource(this);
        }
    }
}
