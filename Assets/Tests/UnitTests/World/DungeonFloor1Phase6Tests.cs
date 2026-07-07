using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.Status;
using JRogue.World.Generation;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class DungeonFloor1Phase6Tests
    {
        [TearDown]
        public void TearDown()
        {
            RunPartyPersistence.ResetForTests();
        }

        [Test]
        public void ProductionFloorAsset_UsesFourCycleOverride()
        {
            var floor = Resources.Load<DungeonFloorDefinition>("Dungeon/Floor_prod_dungeon_floor_01");
            Assert.IsNotNull(floor, "Missing Floor_prod_dungeon_floor_01 in Resources/Dungeon.");
            Assert.AreEqual(4, floor.BaseDayNightCycles);
            Assert.AreEqual("dungeon_floor_01", floor.FloorId);
        }

        [Test]
        public void ProductionCatalog_ReferencesProductionFloorNotTestFork()
        {
            var catalog = Resources.Load<DungeonFloorDefinitionCatalog>("Dungeon/DungeonProdFloor1Catalog");
            Assert.IsNotNull(catalog);
            Assert.IsNotNull(catalog.Floors);
            Assert.GreaterOrEqual(catalog.Floors.Length, 1);

            DungeonFloorDefinition floor1 = catalog.Floors[0];
            Assert.IsNotNull(floor1);
            Assert.AreEqual("Floor_prod_dungeon_floor_01", floor1.name);
        }

        [Test]
        public void ProductionFloor1_HasNoEdgePortalPlacementRules()
        {
            var floor = Resources.Load<DungeonFloorDefinition>("Dungeon/Floor_prod_dungeon_floor_01");
            Assert.IsNotNull(floor);
            Assert.AreEqual(0, floor.PortalPlacementRules.Count);
        }

        [Test]
        public void ProductionFloor2_IsTenByTwentyWithSouthReturnPortal()
        {
            var floor = Resources.Load<DungeonFloorDefinition>("Dungeon/Floor_prod_dungeon_floor_02");
            if (floor == null)
            {
                Assert.Inconclusive("Run JRogue → Dungeon → Create Floor 2 Production Pack to generate assets.");
                return;
            }

            Assert.AreEqual("dungeon_floor_02", floor.FloorId);
            Assert.AreEqual(FloorLayoutMode.PreBakedStamp, floor.LayoutMode);
            Assert.IsNotNull(floor.LayoutStamp);
            Assert.AreEqual(10, floor.LayoutStamp.Width);
            Assert.AreEqual(20, floor.LayoutStamp.Height);
            Assert.AreEqual(0, floor.EnemyPopulation.Count);

            Assert.IsTrue(floor.TryGetArrivalBinding(
                DungeonFloorTransitionIds.Floor01ToFloor02,
                out PortalArrivalBinding arrival));
            Assert.AreEqual(new Vector3Int(5, 1, 0), arrival.arrivalAnchor);
        }

        [Test]
        public void MultiFloorVisit_PreservesRunSeedAndDeepestFloor()
        {
            var go = new GameObject("DungeonRunState_Test");
            try
            {
                DungeonRunState run = go.AddComponent<DungeonRunState>();
                run.BeginRun(424242);

                run.SetActiveFloor("dungeon_floor_01");
                Assert.AreEqual(424242, run.RunSeed);
                Assert.AreEqual(1, run.DeepestFloorNumberReached);

                run.SetActiveFloor("dungeon_floor_02");
                Assert.AreEqual(2, run.DeepestFloorNumberReached);

                run.SetActiveFloor("dungeon_floor_01");
                Assert.AreEqual(424242, run.RunSeed);
                Assert.AreEqual(2, run.DeepestFloorNumberReached);
                Assert.AreEqual("dungeon_floor_01", run.ActiveFloorId);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RunPartyPersistence_RoundTripEntryAndExitFlags()
        {
            RunPartyPersistence.SetReturnTownSceneName("DimensionSquareTest");
            RunPartyPersistence.MarkEnteringDungeonFromTown();

            Assert.IsTrue(RunPartyPersistence.ConsumeEnteringDungeonFromTown());
            Assert.AreEqual("DimensionSquareTest", RunPartyPersistence.ReturnTownSceneName);

            RunPartyPersistence.MarkAwaitingTownArrival();
            RunPartyPersistence.MarkForcedDungeonExpiryPending();

            Assert.IsTrue(RunPartyPersistence.ConsumeAwaitingTownArrival());
            Assert.IsTrue(RunPartyPersistence.ConsumeForcedDungeonExpiryPending());
        }

        [Test]
        public void FeatureSnapshot_ClearRemovesCapturedEntries()
        {
            var snapshot = new DungeonFloorFeatureSnapshot();
            snapshot.traps.Add(new TrapSnapshotEntry
            {
                hostCell = new Vector3Int(3, 4, 0),
                hasTriggered = true,
            });
            snapshot.interactables.Add(new InteractableSnapshotEntry
            {
                cell = new Vector3Int(1, 1, 0),
                isOn = true,
            });

            snapshot.Clear();

            Assert.AreEqual(0, snapshot.traps.Count);
            Assert.AreEqual(0, snapshot.interactables.Count);
            Assert.AreEqual(0, snapshot.hazards.Count);
            Assert.AreEqual(0, snapshot.floorItems.Count);
        }

        [Test]
        public void ApplySurvivorRules_RefreshesHpAndClearsStatuses()
        {
            var partyGo = new GameObject("Party");
            var manager = partyGo.AddComponent<PartyManager>();
            manager.partyMembers = new System.Collections.Generic.List<BaseActor>();

            var memberGo = new GameObject("Member");
            memberGo.transform.SetParent(partyGo.transform, false);

            var stats = memberGo.AddComponent<CharacterStats>();
            stats.Constitution = new Stat(10);
            stats.currentHP = 12;
            stats.humanClass = HumanClass.Knight;
            stats.Intelligence = new Stat(10);
            stats.Wisdom = new Stat(10);
            stats.currentSoulPower = 1;
            stats.levelSoulPowerBonus = 0;
            memberGo.AddComponent<StatusEffectController>();
            var actor = memberGo.AddComponent<TestPartyActor>();
            manager.partyMembers.Add(actor);

            try
            {
                DungeonExitService.ApplySurvivorRules();

                Assert.AreEqual(stats.MaxHP, stats.currentHP);
                Assert.AreEqual(stats.MaxSoulPower, stats.currentSoulPower);
            }
            finally
            {
                Object.DestroyImmediate(partyGo);
            }
        }

        [Test]
        public void ResolveHighestFloorReached_UsesDeepestVisitNotActiveFloor()
        {
            var go = new GameObject("DungeonRunState_Test");
            try
            {
                DungeonRunState run = go.AddComponent<DungeonRunState>();
                run.BeginRun(1);
                run.SetActiveFloor("dungeon_floor_02");
                run.SetActiveFloor("dungeon_floor_01");

                Assert.AreEqual(2, run.DeepestFloorNumberReached);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        sealed class TestPartyActor : BaseActor
        {
            protected override void Die() { }
        }
    }
}
