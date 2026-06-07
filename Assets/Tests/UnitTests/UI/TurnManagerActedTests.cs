using JRogue.Manager.Turn;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.UI
{
    public sealed class TurnManagerActedTests
    {
        [Test]
        public void HasActedThisTurn_ReturnsFalseUntilMarked()
        {
            var go = new GameObject("Actor");
            var turnManagerGo = new GameObject("TurnManager");
            TurnManager turnManager = turnManagerGo.AddComponent<TurnManager>();
            TurnManager.Instance = turnManager;
            turnManager.currentState = GameState.PLAYER_TURN;

            Assert.IsFalse(turnManager.HasActedThisTurn(go));

            turnManager.OnPlayerActionComplete(go);

            Assert.IsTrue(turnManager.HasActedThisTurn(go));
            Assert.IsFalse(turnManager.CanActorTakeAction(go));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(turnManagerGo);
            TurnManager.Instance = null;
        }
    }
}
