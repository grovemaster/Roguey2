using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "TieflingForgemaster", menuName = "JRogue/Racial/Tiefling Forgemaster")]
    public class TieflingForgemasterDefinition : ScriptableObject
    {
        public string forgemasterId = "tiefling_fleshmetal_forgemaster";
        public List<CyborgImplantDefinition> offeredImplants = new List<CyborgImplantDefinition>();
    }
}
