using UnityEngine;

namespace JRogue.World.Altar
{
    public abstract class AltarSlotAcceptFilter : ScriptableObject
    {
        public abstract bool AcceptsManaStone(int tier, string sourceSpeciesId);
    }
}
