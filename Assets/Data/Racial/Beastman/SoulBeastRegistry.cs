using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "SoulBeastRegistry", menuName = "JRogue/Racial/Soul Beast Registry")]
    public sealed class SoulBeastRegistry : ScriptableObject
    {
        public List<SoulBeastDefinition> beasts = new List<SoulBeastDefinition>();

        public IReadOnlyList<SoulBeastDefinition> Beasts => beasts;

        public bool TryGetById(string soulBeastId, out SoulBeastDefinition beast)
        {
            beast = null;
            if (string.IsNullOrEmpty(soulBeastId) || beasts == null)
                return false;

            foreach (SoulBeastDefinition candidate in beasts)
            {
                if (candidate != null && candidate.soulBeastId == soulBeastId)
                {
                    beast = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
