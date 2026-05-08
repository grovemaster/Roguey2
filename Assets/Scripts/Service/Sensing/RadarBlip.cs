using JRogue.Core.Actor;
using UnityEngine;

namespace JRogue.Service.Sensing
{
    /// <summary>
    /// A vague "ping" returned by radar pulses: a tile and a creature category.
    /// Intentionally carries no <c>BaseActor</c> reference so callers can't
    /// extract identity, HP, or any other gameplay data from a radar contact.
    /// </summary>
    public readonly struct RadarBlip
    {
        public readonly Vector3Int Position;
        public readonly EssenceType Type;

        public RadarBlip(Vector3Int position, EssenceType type)
        {
            Position = position;
            Type = type;
        }
    }
}
