using UnityEngine;

namespace JRogue.Core.Actor
{
    public interface IBattleTarget
    {
        GameObject Owner { get; }
        Vector3Int GridPosition { get; }

        void TakeDamage(int amount, GameObject source);
        void ApplyPositionChange(Vector3Int newPosition);
        // We can add ApplyStatusEffect, Pushback, etc., later
    }
}
