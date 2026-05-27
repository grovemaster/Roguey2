using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Hazards;
using JRogue.Input;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Service.Formation;
using JRogue.Stats;
using JRogue.Tests.UnitTests.Input;
using JRogue.UI.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

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
        public void HiddenPoisonGas_StartsWithoutOverlay()
        {
            HazardService hazards = CreateHazardServiceWithOverlay();
            Vector3Int gas = new Vector3Int(5, 0, 0);
            hazards.Register(gas, CreatePoisonGas(), startHidden: true);

            Assert.IsTrue(hazards.IsHiddenToPlayer(gas));
            Assert.IsFalse(hazards.RequiresEnterConfirm(gas));
        }

        [Test]
        public void HiddenPoisonGas_RevealedOnEnter()
        {
            HazardService hazards = CreateHazardServiceWithOverlay();
            Vector3Int gas = new Vector3Int(1, 0, 0);
            hazards.Register(gas, CreatePoisonGas(), startHidden: true);

            BaseActor actor = CreateActor(strength: 10, gridPos: Vector3Int.zero);
            Assert.IsTrue(actor.TryMove(Vector3Int.right));

            Assert.IsFalse(hazards.IsHiddenToPlayer(gas));
        }

        [Test]
        public void HiddenPoisonGas_RevealedBySight100WithinRange()
        {
            HazardService hazards = CreateHazardServiceWithOverlay();
            Vector3Int gas = new Vector3Int(4, 0, 0);
            hazards.Register(gas, CreatePoisonGas(), startHidden: true);

            PartyManager party = CreatePartyManager();
            BaseActor scout = CreateActor(strength: 10, gridPos: Vector3Int.zero);
            scout.stats.sight = new Stat(100);
            party.partyMembers.Add(scout);

            hazards.RefreshHiddenHazardDetection();

            Assert.IsFalse(hazards.IsHiddenToPlayer(gas));
        }

        [Test]
        public void HiddenPoisonGas_NotRevealedByLowSight()
        {
            HazardService hazards = CreateHazardServiceWithOverlay();
            Vector3Int gas = new Vector3Int(4, 0, 0);
            hazards.Register(gas, CreatePoisonGas(), startHidden: true);

            PartyManager party = CreatePartyManager();
            BaseActor scout = CreateActor(strength: 10, gridPos: Vector3Int.zero);
            scout.stats.sight = new Stat(8);
            party.partyMembers.Add(scout);

            hazards.RefreshHiddenHazardDetection();

            Assert.IsTrue(hazards.IsHiddenToPlayer(gas));
        }

        [Test]
        public void FormationRush_IsValidMove_RejectsTileReservedByAnotherFollower()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            PartyManager party = InputTestSceneBuilder.CreatePartyWithTestActors(2, _created);
            BaseActor first = party.partyMembers[0];
            BaseActor second = party.partyMembers[1];
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);

            Vector3Int reserved = new Vector3Int(2, -2, 0);
            first.SetGridPosition(new Vector3Int(1, -2, 0));
            second.SetGridPosition(new Vector3Int(0, -2, 0));

            var planned = new Dictionary<BaseActor, Vector3Int> { { first, reserved } };

            Assert.IsFalse(
                FormationRushService.IsValidMove(
                    MapManager.Instance,
                    GridManager.Instance,
                    reserved,
                    planned,
                    follower: second));
        }

        [Test]
        public void FormationRush_IsValidMove_RejectsRevealedAvoidableHazard()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            PartyManager party = InputTestSceneBuilder.CreatePartyWithTestActors(1, _created);
            BaseActor follower = party.partyMembers[0];
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);

            Vector3Int gas = new Vector3Int(2, -3, 0);
            Vector3Int safe = new Vector3Int(1, -3, 0);
            HazardService hazards = HazardService.Instance;
            EnvironmentalHazardDefinition poison = CreatePoisonGas();
            poison.avoidForEnemyPathing = true;
            hazards.Register(gas, poison);
            follower.SetGridPosition(new Vector3Int(0, -3, 0));

            Assert.IsFalse(
                FormationRushService.IsValidMove(
                    MapManager.Instance,
                    GridManager.Instance,
                    gas,
                    new Dictionary<BaseActor, Vector3Int>(),
                    follower: follower));

            Assert.IsTrue(
                FormationRushService.IsValidMove(
                    MapManager.Instance,
                    GridManager.Instance,
                    safe,
                    new Dictionary<BaseActor, Vector3Int>(),
                    follower: follower));
        }

        [Test]
        public void HiddenLava_LeaderFormationMove_ValidatesDestinationOnly()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            PartyManager party = InputTestSceneBuilder.CreatePartyWithTestActors(1, _created);
            BaseActor leader = party.partyMembers[0];
            leader.stats.Strength = new Stat(11);
            InputTestSceneBuilder.SetPrivateField(party, "isFormationActive", true);
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);

            HazardService hazards = HazardService.Instance;
            Vector3Int revealedLava = new Vector3Int(1, 1, 0);
            Vector3Int hiddenLava = new Vector3Int(2, 1, 0);
            hazards.Register(revealedLava, CreateLava(), startHidden: false);
            hazards.Register(hiddenLava, CreateLava(), startHidden: true);

            leader.SetGridPosition(new Vector3Int(1, 2, 0));

            Assert.IsFalse(
                FormationRushService.IsValidMove(
                    MapManager.Instance,
                    GridManager.Instance,
                    hiddenLava,
                    new Dictionary<BaseActor, Vector3Int>(),
                    allowAllies: false,
                    follower: leader));

            Assert.IsTrue(
                FormationRushService.IsValidMove(
                    MapManager.Instance,
                    GridManager.Instance,
                    hiddenLava,
                    new Dictionary<BaseActor, Vector3Int>(),
                    allowAllies: true,
                    follower: leader));
        }

        [Test]
        public void RevealedLava_OccupantCanLeaveWhileFailingStrength()
        {
            HazardService hazards = CreateHazardService();
            Vector3Int lava = new Vector3Int(0, 0, 0);
            hazards.Register(lava, CreateLava(), startHidden: false);

            Vector3Int otherLava = new Vector3Int(2, 0, 0);
            hazards.Register(otherLava, CreateLava(), startHidden: false);

            BaseActor weak = CreateActor(strength: 10, gridPos: lava);
            Assert.IsTrue(hazards.CanEnter(lava, weak));
            Assert.IsFalse(hazards.CanEnter(otherLava, weak));
            Assert.IsTrue(weak.TryMove(Vector3Int.right));
            Assert.AreEqual(new Vector3Int(1, 0, 0), weak.GridPosition);
        }

        [Test]
        public void HiddenLava_WeakCanEnterWhileHidden()
        {
            HazardService hazards = CreateHazardService();
            Vector3Int lava = new Vector3Int(1, 0, 0);
            hazards.Register(lava, CreateLava(), startHidden: true);

            BaseActor weak = CreateActor(strength: 10, gridPos: Vector3Int.zero);
            Assert.IsTrue(weak.TryMove(Vector3Int.right));
            Assert.AreEqual(lava, weak.GridPosition);
        }

        [Test]
        public void HiddenLava_RevealedBlocksWeakEntry()
        {
            HazardService hazards = CreateHazardService();
            Vector3Int lava = new Vector3Int(1, 0, 0);
            hazards.Register(lava, CreateLava(), startHidden: true);

            PartyManager party = CreatePartyManager();
            BaseActor scout = CreateActor(strength: 10, gridPos: new Vector3Int(5, 0, 0));
            scout.stats.sight = new Stat(100);
            party.partyMembers.Add(scout);

            hazards.RefreshHiddenHazardDetection();
            Assert.IsFalse(hazards.IsHiddenToPlayer(lava));

            BaseActor weak = CreateActor(strength: 10, gridPos: Vector3Int.zero);
            Assert.IsFalse(weak.TryMove(Vector3Int.right));
            Assert.AreEqual(Vector3Int.zero, weak.GridPosition);
        }

        [Test]
        public void HiddenLava_RevealedDamagesWeakOccupantEachPhase()
        {
            HazardService hazards = CreateHazardService();
            Vector3Int lava = new Vector3Int(0, 0, 0);
            hazards.Register(lava, CreateLava(), startHidden: true);

            BaseActor weak = CreateActor(strength: 10, gridPos: Vector3Int.right);
            Assert.IsTrue(weak.TryMove(Vector3Int.left));

            PartyManager party = CreatePartyManager();
            party.partyMembers.Add(weak);
            BaseActor scout = CreateActor(strength: 10, gridPos: new Vector3Int(5, 0, 0));
            scout.stats.sight = new Stat(100);
            party.partyMembers.Add(scout);

            int hpBefore = weak.stats.currentHP;
            hazards.RefreshHiddenHazardDetection();
            hazards.TickOccupancyOnPlayerPhaseStart();
            Assert.Less(weak.stats.currentHP, hpBefore);
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

        HazardService CreateHazardServiceWithOverlay()
        {
            HazardService hazards = CreateHazardService();
            var overlayGo = new GameObject("HazardOverlay_Test");
            _created.Add(overlayGo);
            Tilemap overlay = overlayGo.AddComponent<Tilemap>();
            hazards.SetOverlayMap(overlay);
            return hazards;
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
            def.failedPassageOccupancyDamagePerTurn = 1;
            def.failedPassageOccupancyDamageType = DamageType.Fire;
            _assets.Add(def);
            return def;
        }

        EnvironmentalHazardDefinition CreatePoisonGas()
        {
            var def = ScriptableObject.CreateInstance<EnvironmentalHazardDefinition>();
            def.hazardId = EnvironmentalHazardId.PoisonGas;
            def.displayName = "Poison Gas";
            def.kind = EnvironmentalHazardKind.Persistent;
            def.avoidForEnemyPathing = true;
            def.persistentDamagePerTrigger = 1;
            def.persistentDamageType = DamageType.Poison;
            def.hiddenDetection.method = HazardDetectionMethod.PartyStatInRange;
            def.hiddenDetection.minimumValue = 100;
            def.hiddenDetection.statType = StatType.Sight;
            def.hiddenDetection.requireLineOfSight = true;
            def.hiddenDetection.useStatValueAsRange = true;
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
