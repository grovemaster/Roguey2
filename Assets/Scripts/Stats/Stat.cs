using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

namespace JRogue.Stats
{
    [System.Serializable]
    public class Stat
    {
        [SerializeField] private int baseValue;
        // Changed from List<int> to List<StatModifier>
        [ShowInInspector] private List<StatModifier> modifiers = new List<StatModifier>();

        public Stat(int value) => baseValue = value;

        public int GetValue()
        {
            // int finalValue = baseValue;
            // // Sum up all active modifiers (from gear, essences, etc.)
            // modifiers.ForEach(x => finalValue += x);
            // return finalValue;

            // Sums the base plus all temporary buffs/debuffs
            // return baseValue + modifiers.Sum();
            int finalValue = baseValue;
            modifiers.ForEach(m => finalValue += m.Value);
            return finalValue;
        }

        public void AddModifier(int value, object source)
        {
            modifiers.Add(new StatModifier(value, source));
        }

        public void RemoveModifiersFromSource(object source)
        {
            // Removes all modifiers associated with that specific asset/passive
            modifiers.RemoveAll(m => m.Source == source);
        }

        public bool HasModifierFromSource(object source)
        {
            return modifiers.Exists(m => m.Source == source);
        }
    }
}