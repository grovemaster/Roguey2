using UnityEngine;

namespace JRogue.World.Altar
{
    public abstract class AltarCompletionEffect : ScriptableObject
    {
        /// <summary>Optional pre-flight before offerings are consumed.</summary>
        public virtual bool CanExecute(AltarInstance instance, out string denyReason)
        {
            denyReason = null;
            return true;
        }

        public abstract void Execute(AltarInstance instance);
    }
}
