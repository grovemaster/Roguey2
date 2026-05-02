using System;
using System.Collections.Generic;
using System.Linq;
using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.Tests.UnitTests.MockMonoBehavior;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JRogue.Tests.UnitTests.Manager.Party
{
    [TestFixture]
    public class PartyManagerTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject createdObject in _createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObject);
                }
            }

            _createdObjects.Clear();
            PartyManager.Instance = null;
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void SnapHistoryToCurrentPositions_OneToSixMembers_HistoryMatchesMembers(int partySize)
        {
            PartyManager partyManager = CreatePartyManagerWithMembers(partySize);

            partyManager.SnapHistoryToCurrentPositions();

            Assert.AreEqual(partySize, partyManager.positionHistory.Count);
            for (int i = 0; i < partySize; i++)
            {
                Assert.AreEqual(partyManager.partyMembers[i].GridPosition, partyManager.positionHistory[i]);
            }

            AssertAllPositionsUnique(partyManager.partyMembers.Select(m => m.GridPosition).ToList());
            AssertAllPositionsUnique(partyManager.positionHistory);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void RecordNewLeaderPosition_OneToSixMembers_ShiftsHistoryForward(int partySize)
        {
            PartyManager partyManager = CreatePartyManagerWithMembers(partySize);
            partyManager.SnapHistoryToCurrentPositions();
            List<Vector3Int> originalHistory = new List<Vector3Int>(partyManager.positionHistory);
            Vector3Int newLeaderPos = new Vector3Int(-100, partySize, 0); // guaranteed unique in this setup

            partyManager.RecordNewLeaderPosition(newLeaderPos);

            Assert.AreEqual(partySize, partyManager.positionHistory.Count);
            Assert.AreEqual(newLeaderPos, partyManager.positionHistory[0]);
            for (int i = 1; i < partySize; i++)
            {
                Assert.AreEqual(originalHistory[i - 1], partyManager.positionHistory[i]);
            }

            AssertAllPositionsUnique(partyManager.positionHistory);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void RecordNewLeaderPosition_OneToSixMembers_SameLeaderPosition_DoesNotMutateHistory(int partySize)
        {
            PartyManager partyManager = CreatePartyManagerWithMembers(partySize);
            partyManager.SnapHistoryToCurrentPositions();
            List<Vector3Int> before = new List<Vector3Int>(partyManager.positionHistory);

            partyManager.RecordNewLeaderPosition(before[0]);

            CollectionAssert.AreEqual(before, partyManager.positionHistory);
            AssertAllPositionsUnique(partyManager.positionHistory);
        }

        [Test]
        public void RecordNewLeaderPosition_ShortHistory_UsesFallbackAndKeepsCount()
        {
            PartyManager partyManager = CreatePartyManagerWithMembers(4);
            partyManager.positionHistory = new List<Vector3Int> { new Vector3Int(0, 0, 0) };
            Vector3Int newLeaderPos = new Vector3Int(50, 50, 0);

            partyManager.RecordNewLeaderPosition(newLeaderPos);

            Assert.AreEqual(4, partyManager.positionHistory.Count);
            Assert.AreEqual(newLeaderPos, partyManager.positionHistory[0]);
            Assert.AreEqual(new Vector3Int(0, 0, 0), partyManager.positionHistory[1]);
            Assert.AreEqual(partyManager.partyMembers[1].GridPosition, partyManager.positionHistory[2]);
            Assert.AreEqual(partyManager.partyMembers[2].GridPosition, partyManager.positionHistory[3]);
            AssertAllPositionsUnique(partyManager.positionHistory);
        }

        [Test]
        public void RecordNewLeaderPosition_LargeGap_LogsSanityFail()
        {
            PartyManager partyManager = CreatePartyManagerWithMembers(4);
            partyManager.positionHistory = new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(100, 100, 0),
                new Vector3Int(200, 200, 0),
                new Vector3Int(300, 300, 0)
            };

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[SANITY-FAIL\].*"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[SANITY-FAIL\].*"));

            partyManager.RecordNewLeaderPosition(new Vector3Int(1, 0, 0));

            Assert.AreEqual(4, partyManager.positionHistory.Count);
            Assert.AreEqual(new Vector3Int(1, 0, 0), partyManager.positionHistory[0]);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void GetActiveMember_OneToSixMembers_ReturnsLeaderAndSwapUpdates(int partySize)
        {
            PartyManager partyManager = CreatePartyManagerWithMembers(partySize);

            BaseActor initial = partyManager.GetActiveMember();
            Assert.IsNotNull(initial);
            Assert.AreEqual(partyManager.partyMembers[0], initial);

            partyManager.SwapActiveMember(partySize - 1);

            Assert.AreEqual(partyManager.partyMembers[partySize - 1], partyManager.GetActiveMember());
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void CycleActiveMember_OneToSixMembers_CyclesToExpectedMember(int partySize)
        {
            PartyManager partyManager = CreatePartyManagerWithMembers(partySize);

            BaseActor before = partyManager.GetActiveMember();
            partyManager.CycleActiveMember();
            BaseActor after = partyManager.GetActiveMember();

            Assert.IsNotNull(before);
            Assert.IsNotNull(after);
            if (partySize == 1)
            {
                Assert.AreEqual(before, after);
            }
            else
            {
                Assert.AreEqual(partyManager.partyMembers[1], after);
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void SwapActiveMember_OneToSixMembers_ClampsOutOfRangeIndices(int partySize)
        {
            PartyManager partyManager = CreatePartyManagerWithMembers(partySize);

            partyManager.SwapActiveMember(-99);
            Assert.AreEqual(partyManager.partyMembers[0], partyManager.GetActiveMember());

            partyManager.SwapActiveMember(999);
            Assert.AreEqual(partyManager.partyMembers[partySize - 1], partyManager.GetActiveMember());
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void UpdatePositionHistory_OneToSixMembers_InsertAndTrimBehavior(int partySize)
        {
            PartyManager partyManager = CreatePartyManagerWithMembers(partySize);
            partyManager.SnapHistoryToCurrentPositions();
            Vector3Int firstLeaderPos = partyManager.positionHistory[0];
            Vector3Int movedPos = new Vector3Int(-200, partySize, 0);

            partyManager.UpdatePositionHistory(movedPos);
            Assert.AreEqual(partySize, partyManager.positionHistory.Count);
            Assert.AreEqual(movedPos, partyManager.positionHistory[0]);

            // Calling with same leader position should not insert duplicates.
            partyManager.UpdatePositionHistory(movedPos);
            Assert.AreEqual(partySize, partyManager.positionHistory.Count);
            Assert.AreEqual(movedPos, partyManager.positionHistory[0]);

            // Ensure we did actually move from original.
            Assert.AreNotEqual(firstLeaderPos, partyManager.positionHistory[0]);
        }

        [Test]
        public void PublicMethods_EmptyParty_SafeBehavior()
        {
            PartyManager partyManager = CreatePartyManagerWithMembers(0);
            partyManager.positionHistory = new List<Vector3Int> { new Vector3Int(1, 1, 0) };

            Assert.IsNull(partyManager.GetActiveMember());

            Assert.DoesNotThrow(() => partyManager.SwapActiveMember(2));
            Assert.DoesNotThrow(() => partyManager.SnapHistoryToCurrentPositions());
            Assert.DoesNotThrow(() => partyManager.UpdatePositionHistory(new Vector3Int(5, 5, 0)));

            // Current implementation uses modulo with party count and throws on empty lists.
            Assert.Throws<DivideByZeroException>(() => partyManager.CycleActiveMember());

            Assert.AreEqual(0, partyManager.positionHistory.Count);
        }

        private PartyManager CreatePartyManagerWithMembers(int count)
        {
            GameObject managerObject = new GameObject("PartyManager_Test");
            _createdObjects.Add(managerObject);
            PartyManager partyManager = managerObject.AddComponent<PartyManager>();
            partyManager.partyMembers = new List<BaseActor>();
            List<IActorSeed> actorSeeds = CreateActorSeeds(count);

            for (int i = 0; i < count; i++)
            {
                GameObject actorObject = new GameObject($"PartyActor_{i}");
                _createdObjects.Add(actorObject);

                actorObject.AddComponent<TestQuietEssenceSlotManager>();
                TestPartyActor actor = actorObject.AddComponent<TestPartyActor>();

                actor.SetGridPosition(actorSeeds[i].GridPosition);
                Assert.AreEqual(actorSeeds[i].GridPosition, actor.GridPosition);
                partyManager.partyMembers.Add(actor);
            }

            Assert.AreEqual(count, partyManager.partyMembers.Count);
            AssertAllPositionsUnique(partyManager.partyMembers.Select(member => member.GridPosition).ToList());
            return partyManager;
        }

        private static List<IActorSeed> CreateActorSeeds(int count)
        {
            var seeds = new List<IActorSeed>(count);
            for (int i = 0; i < count; i++)
            {
                IActorSeed seed = Substitute.For<IActorSeed>();
                // Keep members unique but adjacent so normal shift tests do not trigger SANITY-FAIL logs.
                seed.GridPosition.Returns(new Vector3Int(i, 0, 0));
                seeds.Add(seed);
            }

            return seeds;
        }

        private static void AssertAllPositionsUnique(IReadOnlyList<Vector3Int> positions)
        {
            int distinctCount = positions.Distinct().Count();
            Assert.AreEqual(positions.Count, distinctCount, "Expected every party position to be unique.");
        }

        private class TestPartyActor : BaseActor
        {
            protected override void Die()
            {
                // Not needed for unit tests.
            }
        }

        public interface IActorSeed
        {
            Vector3Int GridPosition { get; }
        }
    }
}
