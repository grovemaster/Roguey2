using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.Quest;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Quest
{
    [TestFixture]
    public sealed class DragonianElderQuestLogicTests
    {
        readonly List<Object> _assets = new List<Object>();
        readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _assets)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }

            _assets.Clear();

            foreach (GameObject go in _objects)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _objects.Clear();
        }

        [Test]
        public void TryResolveNextOffer_ReturnsFirstChainQuestForEligibleDragonian()
        {
            DragonianElderDefinition elder = CreateElder("quest_a", "quest_b");
            QuestDefinition questA = CreateQuest("quest_a", QuestOwnership.PerPartyMember, spellId: "spell_a");
            RegisterQuests(questA);

            BaseActor dragonian = CreateDragonian("DragonA");
            var instances = new Dictionary<string, QuestInstance>();
            var questService = CreateQuestService(instances, questA);

            bool ok = DragonianElderQuestLogic.TryResolveNextOffer(
                elder,
                dragonian,
                questService,
                out QuestDefinition next,
                out string deny);

            Assert.IsTrue(ok, deny);
            Assert.AreEqual("quest_a", next.ResolvedQuestId);
        }

        [Test]
        public void TryResolveNextOffer_BlocksUntilPriorQuestCompletedForSameMember()
        {
            DragonianElderDefinition elder = CreateElder("quest_a", "quest_b");
            QuestDefinition questA = CreateQuest("quest_a", QuestOwnership.PerPartyMember, spellId: "spell_a");
            QuestDefinition questB = CreateQuest("quest_b", QuestOwnership.PerPartyMember, minLevel: 1, spellId: "spell_b");
            RegisterQuests(questA, questB);

            BaseActor dragonian = CreateDragonian("DragonA");
            var instances = new Dictionary<string, QuestInstance>();
            var questService = CreateQuestService(instances, questA, questB);

            bool ok = DragonianElderQuestLogic.TryResolveNextOffer(
                elder,
                dragonian,
                questService,
                out _,
                out string deny);

            Assert.IsFalse(ok);
            Assert.That(deny, Does.Contain("prior lesson"));
        }

        [Test]
        public void PerMemberInstances_AllowParallelProgressForTwoDragonians()
        {
            QuestDefinition quest = CreateQuest("quest_shared_lesson", QuestOwnership.PerPartyMember, spellId: "spell_a");
            RegisterQuests(quest);

            BaseActor dragonA = CreateDragonian("DragonA");
            BaseActor dragonB = CreateDragonian("DragonB");
            var instances = new Dictionary<string, QuestInstance>();
            QuestService service = CreateQuestService(instances, quest);

            Assert.IsTrue(service.TryAcceptForMember(quest.ResolvedQuestId, dragonA, out string denyA), denyA);
            Assert.IsTrue(service.TryAcceptForMember(quest.ResolvedQuestId, dragonB, out string denyB), denyB);

            string keyA = QuestInstanceKey.StorageKey(quest.ResolvedQuestId, "DragonA");
            string keyB = QuestInstanceKey.StorageKey(quest.ResolvedQuestId, "DragonB");
            Assert.IsTrue(instances.ContainsKey(keyA));
            Assert.IsTrue(instances.ContainsKey(keyB));
            Assert.AreNotEqual(instances[keyA].ownerPartyMemberId, instances[keyB].ownerPartyMemberId);
        }

        QuestService CreateQuestService(Dictionary<string, QuestInstance> instances, params QuestDefinition[] definitions)
        {
            var go = new GameObject("QuestService");
            _objects.Add(go);
            QuestService service = go.AddComponent<QuestService>();

            foreach (QuestDefinition definition in definitions)
                service.RegisterDefinition(definition);

            typeof(QuestService)
                .GetField("_instances", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(service, instances);

            return service;
        }

        void RegisterQuests(params QuestDefinition[] definitions)
        {
            foreach (QuestDefinition definition in definitions)
            {
                if (definition != null)
                    _assets.Add(definition);
            }
        }

        DragonianElderDefinition CreateElder(params string[] chainQuestIds)
        {
            var elder = ScriptableObject.CreateInstance<DragonianElderDefinition>();
            elder.elderId = "elder_test";
            elder.displayName = "Test Elder";
            elder.npcId = "elder_test_npc";
            elder.chainQuestIds = chainQuestIds;
            _assets.Add(elder);
            return elder;
        }

        QuestDefinition CreateQuest(
            string questId,
            QuestOwnership ownership,
            int minLevel = 1,
            string spellId = "spell_test")
        {
            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            quest.questId = questId;
            quest.displayTitle = questId;
            quest.ownership = ownership;
            quest.requiredRace = Race.Dragonian;
            quest.requiredMinLevel = minLevel;
            quest.learnDragonianSpellId = spellId;
            quest.giverNpcId = "elder_test_npc";
            quest.objectives = new[]
            {
                new QuestObjectiveDefinition
                {
                    objectiveId = "kill",
                    kind = QuestObjectiveKind.KillSpecies,
                    speciesId = "skeleton",
                    killCount = 1,
                },
            };
            return quest;
        }

        BaseActor CreateDragonian(string memberId)
        {
            var go = new GameObject(memberId);
            _objects.Add(go);

            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Dragonian;
            stats.racialSubsystem = RacialSubsystemKind.DragonianSpells;
            stats.level = 5;

            go.AddComponent<DragonianSpellsRuntime>();
            PartyMemberId marker = go.AddComponent<PartyMemberId>();
            marker.ConfigureMemberId(memberId);

            TestPartyActor actor = go.AddComponent<TestPartyActor>();
            return actor;
        }

        sealed class TestPartyActor : BaseActor
        {
            protected override void Die()
            {
            }
        }
    }
}
