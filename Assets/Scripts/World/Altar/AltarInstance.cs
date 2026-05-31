using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Altar
{
    public sealed class AltarInstance
    {
        public readonly Vector3Int Cell;
        public readonly AltarDefinition Definition;
        public readonly List<AltarSlotState> Slots;
        readonly HashSet<string> _firedRuleIds = new HashSet<string>();

        public AltarInstance(Vector3Int cell, AltarDefinition definition)
        {
            Cell = cell;
            Definition = definition;
            Slots = new List<AltarSlotState>();

            if (definition?.slots != null)
            {
                for (int i = 0; i < definition.slots.Length; i++)
                {
                    AltarSlotDefinition slotDef = definition.slots[i];
                    string id = slotDef != null ? slotDef.slotId : $"slot_{i}";
                    Slots.Add(new AltarSlotState(id));
                }
            }
        }

        public bool IsRuleFired(string ruleId) =>
            !string.IsNullOrEmpty(ruleId) && _firedRuleIds.Contains(ruleId);

        /// <summary>True after any completion rule has fired (altar spent for this run).</summary>
        public bool IsDepleted => _firedRuleIds.Count > 0;

        public void MarkRuleFired(string ruleId)
        {
            if (!string.IsNullOrEmpty(ruleId))
                _firedRuleIds.Add(ruleId);
        }

        public void ClearOfferings()
        {
            for (int i = 0; i < Slots.Count; i++)
                Slots[i].Offering = default;
        }

        public AltarSlotState FindSlotById(string slotId)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].SlotId == slotId)
                    return Slots[i];
            }

            return null;
        }

        public int FindSlotIndexById(string slotId)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].SlotId == slotId)
                    return i;
            }

            return -1;
        }
    }
}
