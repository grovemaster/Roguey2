using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(
        fileName = "ElementalSpiritMeditationGate",
        menuName = "JRogue/Racial/Elemental Spirit Meditation Gate")]
    public sealed class ElementalSpiritMeditationGateDefinition : ScriptableObject
    {
        public string gateId = "meditation_shrine";
        public string displayName = "Meditation Shrine";
        [Min(0)] public int spiritXpAward = 10;
        public SpiritImprintUnlockCost cost;
    }
}
