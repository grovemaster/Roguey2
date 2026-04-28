using UnityEngine;
using JRogue.Core.Actor;

namespace JRogue.Tests.Mocks
{
    /// <summary>
    /// Mock implementation of IBattleTarget for testing purposes.
    /// Tracks all interactions and state changes.
    /// </summary>
    public class MockBattleTarget : IBattleTarget
    {
        private GameObject _owner;
        private Vector3Int _gridPosition;

        public GameObject Owner => _owner;
        public Vector3Int GridPosition => _gridPosition;

        public int DamageTakenCount { get; private set; } = 0;
        public int LastDamageAmount { get; private set; } = 0;
        public GameObject LastDamageSource { get; private set; }

        public int PositionChangeCount { get; private set; } = 0;
        public Vector3Int LastPositionChange { get; private set; }

        public MockBattleTarget(string namePrefix = "MockTarget", Vector3Int? startingPosition = null)
        {
            _owner = new GameObject($"{namePrefix}_{GetHashCode()}");
            _gridPosition = startingPosition ?? Vector3Int.zero;
        }

        public void TakeDamage(int amount, GameObject source)
        {
            DamageTakenCount++;
            LastDamageAmount = amount;
            LastDamageSource = source;
        }

        public void ApplyPositionChange(Vector3Int newPosition)
        {
            PositionChangeCount++;
            LastPositionChange = newPosition;
            _gridPosition = newPosition;
        }

        public void Cleanup()
        {
            if (_owner != null)
            {
                Object.DestroyImmediate(_owner);
            }
        }
    }
}
