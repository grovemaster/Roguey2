using UnityEngine;

namespace JRogue.World.Lighting
{
    [CreateAssetMenu(
        menuName = "JRogue/Lighting/Light Emitter Definition",
        fileName = "LightEmitter_")]
    public sealed class LightEmitterDefinition : ScriptableObject
    {
        const int MinLight = 0;
        const int MaxLight = 10;
        const int DefaultTorchEmission = 6;

        [SerializeField]
        [Range(MinLight, MaxLight)]
        int baseEmissionMax = DefaultTorchEmission;

        [SerializeField]
        [Min(0)]
        int falloffRadius = 8;

        [SerializeField]
        [Min(0)]
        int falloffPerTile = 1;

        [SerializeField]
        bool blocksLos;

        public int BaseEmissionMax => baseEmissionMax;
        public int FalloffRadius => falloffRadius;
        public int FalloffPerTile => falloffPerTile;
        public bool BlocksLos => blocksLos;
    }
}
