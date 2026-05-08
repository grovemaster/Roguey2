using JRogue.Ability;
using JRogue.Core.Actor;
using UnityEngine;

namespace JRogue.Tests.Mocks
{
    /// <summary>
    /// Minimal <see cref="AbilityAction"/> for input/processor tests: always succeeds,
    /// supports targeted execution via <see cref="AbilityAction.Execute(UnityEngine.GameObject, Vector3Int)"/>.
    /// </summary>
    public sealed class DummyTargetAbility : AbilityAction
    {
        public override bool CanExecute(GameObject user) => true;

        protected override bool ExecuteCore(GameObject user) => true;

        protected override bool ExecuteCore(GameObject user, Vector3Int targetTile) => true;
    }
}
