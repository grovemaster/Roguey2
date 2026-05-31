using UnityEngine;

namespace JRogue.World.Altar
{
    public abstract class AltarCompletionEffect : ScriptableObject
    {
        public abstract void Execute(AltarInstance instance);
    }
}
