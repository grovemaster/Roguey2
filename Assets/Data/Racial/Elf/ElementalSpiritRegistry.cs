using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "ElementalSpiritRegistry", menuName = "JRogue/Racial/Elemental Spirit Registry")]
    public sealed class ElementalSpiritRegistry : ScriptableObject
    {
        public List<ElementalSpiritDefinition> spirits = new List<ElementalSpiritDefinition>();

        public IReadOnlyList<ElementalSpiritDefinition> Spirits => spirits;

        public bool TryPickRandom(out ElementalSpiritDefinition spirit)
        {
            spirit = null;
            if (spirits == null || spirits.Count == 0)
                return false;

            var eligible = new List<ElementalSpiritDefinition>();
            for (int i = 0; i < spirits.Count; i++)
            {
                ElementalSpiritDefinition candidate = spirits[i];
                if (candidate != null && !string.IsNullOrEmpty(candidate.spiritId))
                    eligible.Add(candidate);
            }

            if (eligible.Count == 0)
                return false;

            int index = Random.Range(0, eligible.Count);
            spirit = eligible[index];
            return spirit != null;
        }
    }
}
