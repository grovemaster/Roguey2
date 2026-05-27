using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Interactables;
using JRogue.Manager.Map;
using JRogue.Manager.Progression;
using JRogue.Manager.Turn;
using JRogue.Stats;
using JRogue.Tests.UnitTests.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Tests.UnitTests.Combat
{
    [TestFixture]
    public sealed class InteractableTileTests
    {
        readonly List<GameObject> _created = new List<GameObject>();
        readonly List<Object> _assets = new List<Object>();

        [SetUp]
        public void SetUp() => InputTestSceneBuilder.ResetSingletonManagersForTests();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _created.Clear();

            foreach (Object asset in _assets)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }

            _assets.Clear();

            ClearInteractableServiceInstance();
            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [Test]
        public void BlocksOccupancy_PreventsWalkingOntoLever()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            InteractableTileService service = CreateService();
            InteractableLeverContent.LeverSet levers = RegisterLeverSet(service);
            Vector3Int leverCell = new Vector3Int(2, 0, 0);
            service.Register(leverCell, levers.Second);

            BaseActor actor = CreateActor(new Vector3Int(1, 0, 0));
            Assert.IsFalse(actor.TryMove(Vector3Int.right));
            Assert.AreEqual(new Vector3Int(1, 0, 0), actor.GridPosition);
            Assert.IsTrue(service.TryGetInstance(leverCell, out InteractableTileInstance instance));
            Assert.IsFalse(instance.IsOn);
        }

        [Test]
        public void ActivatedLever_StillBlocksOccupancy()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            InteractableTileService service = CreateService();
            InteractableLeverContent.LeverSet levers = RegisterLeverSet(service);
            Vector3Int leverCell = new Vector3Int(2, 0, 0);
            service.Register(leverCell, levers.First);
            service.ActivateById(InteractableTileId.LeverSwitchFirst, InteractableActivationSource.Scripted);

            BaseActor actor = CreateActor(new Vector3Int(1, 0, 0));
            Assert.IsFalse(actor.TryMove(Vector3Int.right));
            Assert.AreEqual(new Vector3Int(1, 0, 0), actor.GridPosition);
        }

        [Test]
        public void Bump_ActivatesFirstLever_AndSpendsTurn()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            TurnManager turn = TurnManager.Instance;
            turn.currentState = GameState.PLAYER_TURN;

            InteractableTileService service = CreateService();
            InteractableLeverContent.LeverSet levers = RegisterLeverSet(service);
            Vector3Int leverCell = new Vector3Int(1, 0, 0);
            service.Register(leverCell, levers.First);

            BaseActor actor = CreateActor(Vector3Int.zero);
            actor.gameObject.tag = "Player";

            Assert.IsTrue(actor.TryMove(Vector3Int.right));
            Assert.AreEqual(Vector3Int.zero, actor.GridPosition);
            Assert.IsTrue(service.TryGetInstance(leverCell, out InteractableTileInstance instance));
            Assert.IsTrue(instance.IsOn);
            Assert.IsFalse(turn.CanActorTakeAction(actor.gameObject));
        }

        [Test]
        public void DiagonalBump_ActivatesFirstLever_AndSpendsTurn()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            TurnManager turn = TurnManager.Instance;
            turn.currentState = GameState.PLAYER_TURN;

            InteractableTileService service = CreateService();
            InteractableLeverContent.LeverSet levers = RegisterLeverSet(service);
            Vector3Int leverCell = new Vector3Int(1, 1, 0);
            service.Register(leverCell, levers.First);

            BaseActor actor = CreateActor(Vector3Int.zero);
            actor.gameObject.tag = "Player";

            Assert.IsTrue(actor.TryMove(new Vector3Int(1, 1, 0)));
            Assert.AreEqual(Vector3Int.zero, actor.GridPosition);
            Assert.IsTrue(service.TryGetInstance(leverCell, out InteractableTileInstance instance));
            Assert.IsTrue(instance.IsOn);
            Assert.IsFalse(turn.CanActorTakeAction(actor.gameObject));
        }

        [Test]
        public void SecondLever_BlockedUntilFirstOn_ThenChainsThird()
        {
            InteractableTileService service = CreateService();
            InteractableLeverContent.LeverSet levers = RegisterLeverSet(service);

            Vector3Int firstCell = new Vector3Int(1, 0, 0);
            Vector3Int secondCell = new Vector3Int(2, 0, 0);
            service.Register(firstCell, levers.First);
            service.Register(secondCell, levers.Second);
            service.Register(new Vector3Int(3, 0, 0), levers.Third);

            BaseActor actor = CreateActor(Vector3Int.zero);

            Assert.AreEqual(
                InteractableBumpResult.PreconditionFailed,
                service.TryBumpActivate(secondCell, actor));

            service.TryBumpActivate(firstCell, actor);
            Assert.AreEqual(InteractableBumpResult.Activated, service.TryBumpActivate(secondCell, actor));
            Assert.IsTrue(
                service.TryGetInstanceById(InteractableTileId.LeverSwitchThird, out InteractableTileInstance third));
            Assert.IsTrue(third.IsOn);
        }

        [Test]
        public void ThirdLever_PlayerBumpFails_ScriptActivates()
        {
            InteractableTileService service = CreateService();
            InteractableLeverContent.LeverSet levers = RegisterLeverSet(service);

            Vector3Int thirdCell = new Vector3Int(3, 0, 0);
            service.Register(thirdCell, levers.Third);
            BaseActor actor = CreateActor(new Vector3Int(2, 0, 0));

            Assert.AreEqual(InteractableBumpResult.Failed, service.TryBumpActivate(thirdCell, actor));
            Assert.AreEqual(
                InteractableBumpResult.Activated,
                service.ActivateById(InteractableTileId.LeverSwitchThird, InteractableActivationSource.Scripted));
        }

        [Test]
        public void FourthLever_GrantsPartyExperience_WhenThirdOn()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            var party = InputTestSceneBuilder.CreatePartyWithTestActors(2, _created);
            var xp = party.GetComponent<PartyExperienceService>();
            if (xp == null)
                xp = party.gameObject.AddComponent<PartyExperienceService>();

            InteractableTileService service = CreateService();
            InteractableLeverContent.LeverSet levers = RegisterLeverSet(service);

            service.Register(new Vector3Int(3, 0, 0), levers.Third);
            service.Register(new Vector3Int(4, 0, 0), levers.Fourth);
            service.ActivateById(InteractableTileId.LeverSwitchThird, InteractableActivationSource.Scripted);

            int before = party.partyMembers[0].stats.experience;
            BaseActor actor = CreateActor(new Vector3Int(3, 0, 0));
            Assert.AreEqual(
                InteractableBumpResult.Activated,
                service.TryBumpActivate(new Vector3Int(4, 0, 0), actor));
            Assert.Greater(party.partyMembers[0].stats.experience, before);
        }

        [Test]
        public void AlreadyOnBump_DoesNotSpendTurn()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            TurnManager turn = TurnManager.Instance;
            turn.currentState = GameState.PLAYER_TURN;

            InteractableTileService service = CreateService();
            InteractableLeverContent.LeverSet levers = RegisterLeverSet(service);

            Vector3Int leverCell = new Vector3Int(1, 0, 0);
            service.Register(leverCell, levers.First);
            service.ActivateById(InteractableTileId.LeverSwitchFirst, InteractableActivationSource.Scripted);

            BaseActor actor = CreateActor(Vector3Int.zero);
            actor.gameObject.tag = "Player";

            Assert.IsFalse(actor.TryMove(Vector3Int.right));
            Assert.IsTrue(turn.CanActorTakeAction(actor.gameObject));
        }

        InteractableLeverContent.LeverSet RegisterLeverSet(InteractableTileService service)
        {
            InteractableLeverContent.LeverSet levers = InteractableLeverContent.CreateRuntimeDefinitions();
            _assets.Add(levers.First);
            _assets.Add(levers.Second);
            _assets.Add(levers.Third);
            _assets.Add(levers.Fourth);
            return levers;
        }

        InteractableTileService CreateService()
        {
            var go = new GameObject("InteractableTileService_Test");
            _created.Add(go);
            var overlayGo = new GameObject("InteractableOverlay_Test");
            _created.Add(overlayGo);
            Tilemap overlay = overlayGo.AddComponent<Tilemap>();
            InteractableTileService service = go.AddComponent<InteractableTileService>();
            service.SetOverlayMap(overlay);
            return service;
        }

        BaseActor CreateActor(Vector3Int gridPos)
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);

            GameObject go = new GameObject("Actor");
            _created.Add(go);
            go.AddComponent<GridMover>();
            go.AddComponent<HealthComponent>();
            var stats = go.AddComponent<CharacterStats>();
            stats.Strength = new Stat(10);
            stats.currentHP = stats.MaxHP;

            var actor = go.AddComponent<InputTestSceneBuilder.TestPartyActor>();
            actor.SetGridPosition(gridPos);
            InputTestSceneBuilder.SetPrivateField(actor, "mapManager", MapManager.Instance);
            InputTestSceneBuilder.SetPrivateField(actor, "turnManager", TurnManager.Instance);

            TurnManager.Instance.currentState = GameState.PLAYER_TURN;
            return actor;
        }

        static void ClearInteractableServiceInstance()
        {
            var prop = typeof(InteractableTileService).GetProperty(
                nameof(InteractableTileService.Instance),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            prop?.SetValue(null, null);
        }
    }
}
