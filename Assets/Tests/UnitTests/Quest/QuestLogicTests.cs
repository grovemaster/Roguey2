using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.Quest;
using JRogue.Stats;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Quest
{
    [TestFixture]
    public sealed class QuestLogicTests
    {
        sealed class TestPartyActor : BaseActor
        {
            protected override void Die()
            {
            }
        }

        readonly List<Object> _assets = new List<Object>();
        readonly List<GameObject> _objects = new List<GameObject>();
        GameStoryFlagService _flags;

        [SetUp]
        public void SetUp()
        {
            var flagGo = new GameObject("Flags");
            _objects.Add(flagGo);
            _flags = flagGo.AddComponent<GameStoryFlagService>();
            _flags.ClearAll();
        }

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
        public void TryOffer_BlockedWhenPrerequisiteFlagMissing()
        {
            QuestDefinition quest = CreateQuest("quest_test_flag");
            quest.acceptPrerequisites = new[]
            {
                new QuestPrerequisite
                {
                    kind = QuestPrerequisiteKind.StoryFlag,
                    flagId = "portal_opened",
                    expectedFlagValue = true,
                },
            };

            var instances = new Dictionary<string, QuestInstance>();
            bool ok = QuestLogic.EvaluatePrerequisites(quest, instances, _flags, null, out string deny);
            Assert.IsFalse(ok);
            Assert.That(deny, Does.Contain("portal_opened"));
        }

        [Test]
        public void KillObjective_IncrementsForMatchingSpecies()
        {
            QuestDefinition quest = CreateQuest("quest_skeleton_proof");
            quest.objectives = new[]
            {
                new QuestObjectiveDefinition
                {
                    objectiveId = "kill_skeletons",
                    kind = QuestObjectiveKind.KillSpecies,
                    speciesId = "skeleton",
                    killCount = 3,
                },
            };

            var instance = new QuestInstance
            {
                questId = quest.ResolvedQuestId,
                progress = QuestLogic.CreateInitialProgress(quest),
            };

            QuestLogic.NotifyEnemyKilled(quest, instance, "skeleton", null, null);
            Assert.AreEqual(1, instance.progress[0].current);
            Assert.IsFalse(instance.progress[0].completed);

            QuestLogic.NotifyEnemyKilled(quest, instance, "skeleton", null, null);
            QuestLogic.NotifyEnemyKilled(quest, instance, "skeleton", null, null);
            Assert.IsTrue(instance.progress[0].completed);
        }

        [Test]
        public void EquipObjective_RequiresMatchingPartyMemberId()
        {
            QuestDefinition quest = CreateQuest("quest_barbarian_blade");
            ItemData blade = CreateItem("Giants_Blade");
            quest.objectives = new[]
            {
                new QuestObjectiveDefinition
                {
                    objectiveId = "equip_blade",
                    kind = QuestObjectiveKind.EquipItem,
                    item = blade,
                    equipSlot = EquipmentSlot.MainHand,
                    actorRequirement = new QuestActorRequirement
                    {
                        kind = QuestActorRequirementKind.PartyMemberId,
                        partyMemberId = "BarbarianWarrior",
                    },
                },
            };

            var instance = new QuestInstance
            {
                questId = quest.ResolvedQuestId,
                progress = QuestLogic.CreateInitialProgress(quest),
            };

            TestPartyActor wrongActor = CreateActor("HumanWarrior");
            var item = new ItemInstance(blade, 1);
            QuestLogic.NotifyItemEquipped(quest, instance, wrongActor, item, EquipmentSlot.MainHand, null);
            Assert.IsFalse(instance.progress[0].completed);

            TestPartyActor barbarian = CreateActor("BarbarianWarrior");
            QuestLogic.NotifyItemEquipped(quest, instance, barbarian, item, EquipmentSlot.MainHand, null);
            Assert.IsTrue(instance.progress[0].completed);
        }

        [Test]
        public void TurnIn_RemovesDeliverItems()
        {
            ItemData crate = CreateItem("Delivery Crate");
            crate.category = ItemCategory.QuestItem;

            QuestDefinition quest = CreateQuest("quest_greta_fetch");
            quest.giverNpcId = "shop_greta";
            quest.objectives = new[]
            {
                new QuestObjectiveDefinition
                {
                    objectiveId = "deliver_crate",
                    kind = QuestObjectiveKind.DeliverItem,
                    item = crate,
                    itemQuantity = 1,
                },
            };

            TestPartyActor member = CreateActor("Member");
            member.gameObject.AddComponent<InventoryManager>();
            InventoryManager inventory = member.GetComponent<InventoryManager>();
            inventory.AddItem(new ItemInstance(crate, 1));

            bool removed = QuestLogic.TryRemoveDeliverItems(
                quest,
                new List<BaseActor> { member },
                out string deny);
            Assert.IsTrue(removed, deny);
            Assert.AreEqual(
                0,
                QuestLogic.CountItemInParty(
                    new List<BaseActor> { member },
                    crate,
                    QuestActorRequirementKind.None,
                    default));
        }

        TestPartyActor CreateActor(string memberId)
        {
            var go = new GameObject(memberId);
            _objects.Add(go);
            go.AddComponent<CharacterStats>();
            TestPartyActor actor = go.AddComponent<TestPartyActor>();
            PartyMemberId marker = go.AddComponent<PartyMemberId>();
            marker.ConfigureMemberId(memberId);
            return actor;
        }

        QuestDefinition CreateQuest(string questId)
        {
            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            quest.questId = questId;
            quest.displayTitle = questId;
            _assets.Add(quest);
            return quest;
        }

        ItemData CreateItem(string name)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.itemName = name;
            item.weight = 1f;
            _assets.Add(item);
            return item;
        }
    }
}
