using JRogue.Item.Essence;
using UnityEngine;

namespace JRogue.Manager.Floor
{
    public sealed class FloorEssenceEntry
    {
        public string entryId;
        public Vector3Int tile;
        public EssenceData essenceData;
        public int phasesRemaining;
    }
}
