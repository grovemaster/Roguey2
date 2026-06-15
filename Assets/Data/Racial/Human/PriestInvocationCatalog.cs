using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "PriestInvocationCatalog", menuName = "JRogue/Racial/Priest Invocation Catalog")]
    public sealed class PriestInvocationCatalog : ScriptableObject
    {
        public List<PriestInvocationDefinition> invocations = new();
    }
}
