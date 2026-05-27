using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Hazards;
using JRogue.Input;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Stats;
using JRogue.Tests.UnitTests.Input;
using JRogue.UI.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JRogue.Tests.UnitTests.Combat
{
    [TestFixture]
    public sealed class EnvironmentalHazardTests
    {
        readonly List<GameObject> _created = new List<GameObject>();
        readonly List<Object> _assets = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
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

            if (HazardConfirmDialogUI.EnsureInstance() != null)
                Object.DestroyImmediate(HazardConfirmDialogUI.EnsureInstance().gameObject);

            if (AutoPickupConfirmDialogUI.EnsureInstance() != null)
                Object.DestroyImmediate(AutoPickupConfirmDialogUI.EnsureInstance().gameObject);

            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [Test]
        public void Lava_BlocksWeakStrength()
        {
            HazardService hazards = CreateHazardService();
            EnvironmentalHazardDefinition lava = CreateLava();
            Vector3Int lavaCell = new Vector3Int(2, 0, 0);
            hazards.Register(lavaCell, lava);

            BaseActor weak = CreateActor(strength: 10, gridPos: Vector3Int.zero);
            Assert.IsFalse(weak.TryMove(Vector3Int.right));
            Assert.AreEqual(Vector3Int.zero, weak.GridPosition);
        }

        [Test]
        public void Lava_AllowsStrength50()
        {
            HazardService hazards = CreateHazardService();
            hazards.Register(new Vector3Int(1, 0, 0), CreateLava());

            BaseActor strong = CreateActor(strength: 50, gridPos: Vector3Int.zero);
            Assert.IsTrue(strong.TryMove(Vector3Int.right));
            Assert.AreEqual(new Vector3Int(1, 0, 0), strong.GridPosition);
        }

        [Test]
        public void PoisonGas_ConfirmCancel_DoesNotMove()
        {
            SetupPartyProcessor(out PlayerCommandProcessor processor, out BaseActor leader);
            Vector3Int gas = leader.GridPosition + Vector3Int.right;
            HazardService.Instance.Register(gas, CreatePoisonGas());

            Vector3Int start = leader.GridPosition;
            Assert.IsTrue(processor.TryApply(PlayerCommand.MoveGrid(Vector3Int.right)));
            Assert.AreEqual(start, leader.GridPosition);
            Assert.IsTrue(HazardConfirmDialogUI.BlocksGameplay);
        }

        [Test]
        public void PoisonGas_Enter_AppliesDamage()
        {
            SetupPartyProcessor(out PlayerCommandProcessor processor, out BaseActor leader);
            Vector3Int gas = leader.GridPosition + Vector3Int.right;
            HazardService.Instance.Register(gas, CreatePoisonGas());

            int hpBefore = leader.stats.currentHP;
            Assert.IsTrue(processor.TryApply(PlayerCommand.MoveGrid(Vector3Int.right)));
            CommitHazardYes();
            Assert.AreEqual(gas, leader.GridPosition);
            Assert.Less(leader.stats.currentHP, hpBefore);
        }

        [Test]
        public void PoisonGas_Wait_AppliesDamage()
        {
            HazardService hazards = CreateHazardService();
            Vector3Int gas = new Vector3Int(0, 0, 0);
            hazards.Register(gas, CreatePoisonGas());

            BaseActor actor = CreateActor(strength: 10, gridPos: gas);
            int hpBefore = actor.stats.currentHP;
            hazards.OnActorWaitOnCell(actor);
            Assert.Less(actor.stats.currentHP, hpBefore);
        }

        [Test]
        public void PoisonGas_PlayerPhaseStart_AppliesDamageWhenStanding()
        {
            HazardService hazards = CreateHazardService();
            Vector3Int gas = new Vector3Int(0, 0, 0);
            hazards.Register(gas, CreatePoisonGas());

            PartyManager partyGo = CreatePartyManager();
            BaseActor actor = CreateActor(strength: 10, gridPos: gas);
            partyGo.partyMembers.Add(actor);

            int hpBefore = actor.stats.currentHP;
            hazards.TickOccupancyOnPlayerPhaseStart();
            Assert.Less(actor.stats.currentHP, hpBefore);
        }

        PartyManager CreatePartyManager()
        {
            var go = new GameObject("PartyManager_Test");
            _created.Add(go);
            var party = go.AddComponent<PartyManager>();
            party.partyMembers = new List<BaseActor>();
            return party;
        }

        HazardService CreateHazardService()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            var go = new GameObject("HazardService_Test");
            _created.Add(go);
            return go.AddComponent<HazardService>();
        }

        void SetupPartyProcessor(out PlayerCommandProcessor processor, out BaseActor leader)
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);

            PartyManager party = InputTestSceneBuilder.CreatePartyWithTestActors(1, _created);
            leader = party.partyMembers[0];
            leader.stats.Strength = new Stat(10);
            InputTestSceneBuilder.SetPrivateField(party, "isFormationActive", false);
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);

            processor = new PlayerCommandProcessor();
            TurnManager.Instance.currentState = GameState.PLAYER_TURN;
        }

        BaseActor CreateActor(int strength, Vector3Int gridPos)
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);

            GameObject go = new GameObject("Actor");
            _created.Add(go);
            go.AddComponent<GridMover>();
            go.AddComponent<HealthComponent>();
            var stats = go.AddComponent<CharacterStats>();
            stats.Strength = new Stat(strength);
            stats.currentHP = stats.MaxHP;

            var actor = go.AddComponent<InputTestSceneBuilder.TestPartyActor>();
            actor.SetGridPosition(gridPos);
            InputTestSceneBuilder.SetPrivateField(actor, "mapManager", MapManager.Instance);

            TurnManager.Instance.currentState = GameState.PLAYER_TURN;
            return actor;
        }

        EnvironmentalHazardDefinition CreateLava()
        {
            var def = ScriptableObject.CreateInstance<EnvironmentalHazardDefinition>();
            def.hazardId = EnvironmentalHazardId.Lava;
            def.displayName = "Lava";
            def.kind = EnvironmentalHazardKind.Passage;
            def.passageCondition = PassageCondition.MinimumStrength;
            def.requiredStrength = 50;
            _assets.Add(def);
            return def;
        }

        EnvironmentalHazardDefinition CreatePoisonGas()
        {
            var def = ScriptableObject.CreateInstance<EnvironmentalHazardDefinition>();
            def.hazardId = EnvironmentalHazardId.PoisonGas;
            def.displayName = "Poison Gas";
            def.kind = EnvironmentalHazardKind.Persistent;
            def.persistentDamagePerTrigger = 1;
            def.persistentDamageType = DamageType.Poison;
            _assets.Add(def);
            return def;
        }

        static void CommitHazardYes()
        {
            var commit = typeof(HazardConfirmDialogUI).GetMethod(
                "CommitYes",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            commit.Invoke(HazardConfirmDialogUI.EnsureInstance(), null);
        }

    }
}
