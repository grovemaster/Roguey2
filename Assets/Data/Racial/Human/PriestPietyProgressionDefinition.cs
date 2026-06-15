using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "PriestPietyProgression", menuName = "JRogue/Racial/Priest Piety Progression")]
    public sealed class PriestPietyProgressionDefinition : ScriptableObject
    {
        [Min(1)] public int maxPiety = 100;
        [Min(1)] public int startingPietyOnCommit = 10;
        public List<PriestPietyBandData> bands = new();
    }
}
