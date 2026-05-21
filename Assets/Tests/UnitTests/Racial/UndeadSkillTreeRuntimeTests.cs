using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Controller.Enemy;
using JRogue.Item;
using JRogue.Item.Essence;
using JRogue.Manager.Inventory;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.Racial
{
    public class UndeadSkillTreeRuntimeTests
    {
        readonly List<Object> _destroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _destroy)
            {
                if (o != null)
                    Object.DestroyImmediate(o);
            }

            _destroy.Clear();
        }

        [Test]
        public void Preset_AppliesCalcifiedHideConstitution()
        {
            UndeadSkillTreeDefinition tree = BuildSampleTree();
            GameObject go = CreateUndead(tree, new UndeadSkillNodeRankEntry { nodeId = "calcified_hide", rank = 1 });
            go.GetComponent<UndeadSkillTreeRuntime>().TryApplyFromSerializedState();

            CharacterStats stats = go.GetComponent<CharacterStats>();
            Assert.AreEqual(11, stats.Constitution.GetValue());
        }

        [Test]
        public void Baseline_RadiantWeakness_And_PoisonResistance()
        {
            GameObject go = CreateUndead(BuildSampleTree());
            var loadout = ScriptableObject.CreateInstance<RacialLoadoutDefinition>();
            _destroy.Add(loadout);
            loadout.requiredRace = Race.Undead;
            loadout.resistanceModifiers = new List<DamageResistanceModifier>
            {
                new DamageResistanceModifier { type = DamageType.Radiant, value = -50 },
                new DamageResistanceModifier { type = DamageType.Poison, value = 999 }
            };
            go.GetComponent<RacialLoadoutApplier>().SetLoadout(loadout);

            CharacterStats stats = go.GetComponent<CharacterStats>();
            Assert.AreEqual(-50, stats.GetResistance(DamageType.Radiant));
            Assert.AreEqual(999, stats.GetResistance(DamageType.Poison));
        }

        [Test]
        public void SpendPoint_IncreasesRank_AndAppliesPayload()
        {
            UndeadSkillTreeDefinition tree = BuildSampleTree();
            GameObject go = CreateUndead(tree);
            UndeadSkillTreeRuntime runtime = go.GetComponent<UndeadSkillTreeRuntime>();
            runtime.TryApplyFromSerializedState();

            Assert.IsTrue(runtime.TrySpendPoint("calcified_hide", out _));
            Assert.AreEqual(1, runtime.RanksSnapshot["calcified_hide"]);
            Assert.AreEqual(11, go.GetComponent<CharacterStats>().Constitution.GetValue());
        }

        [Test]
        public void RefundRank_RemovesPayload_AndReturnsPoint()
        {
            UndeadSkillTreeDefinition tree = BuildSampleTree();
            GameObject go = CreateUndead(tree, new UndeadSkillNodeRankEntry { nodeId = "calcified_hide", rank = 1 });
            UndeadSkillTreeRuntime runtime = go.GetComponent<UndeadSkillTreeRuntime>();
            runtime.TryApplyFromSerializedState();
            CharacterStats stats = go.GetComponent<CharacterStats>();
            Assert.AreEqual(11, stats.Constitution.GetValue());

            Assert.IsTrue(runtime.TryRefundRank("calcified_hide", out _));
            Assert.IsFalse(runtime.RanksSnapshot.ContainsKey("calcified_hide"));
            Assert.AreEqual(10, stats.Constitution.GetValue());
            Assert.AreEqual(9, runtime.UnspentPoints);
        }

        [Test]
        public void Exclusivity_SecondNodeInGroup_Blocked()
        {
            UndeadSkillTreeDefinition tree = BuildSampleTree();
            GameObject go = CreateUndead(tree);
            UndeadSkillTreeRuntime runtime = go.GetComponent<UndeadSkillTreeRuntime>();
            runtime.TryApplyFromSerializedState();

            Assert.IsTrue(runtime.TrySpendPoint("lichs_bargain", out _));
            Assert.IsFalse(runtime.TrySpendPoint("death_knell", out string reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void ClusterGate_CoreLocked_UntilTwoPointsSpent()
        {
            UndeadSkillTreeDefinition tree = BuildSampleTree();
            GameObject go = CreateUndead(tree);
            UndeadSkillTreeRuntime runtime = go.GetComponent<UndeadSkillTreeRuntime>();
            runtime.TryApplyFromSerializedState();

            Assert.IsFalse(runtime.TrySpendPoint("embrace_the_dark", out string reason));
            Assert.That(reason, Does.Contain("locked"));

            Assert.IsTrue(runtime.TrySpendPoint("grave_touch", out _));
            Assert.IsTrue(runtime.TrySpendPoint("calcified_hide", out _));
            Assert.IsTrue(runtime.TrySpendPoint("embrace_the_dark", out _));
        }

        [Test]
        public void NonUndead_RuntimeDoesNotApply()
        {
            UndeadSkillTreeDefinition tree = BuildSampleTree();
            GameObject go = new GameObject("Human");
            _destroy.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Human;
            var runtime = go.AddComponent<UndeadSkillTreeRuntime>();
            runtime.SetSkillTreeAndRanks(tree, 10,
                new List<UndeadSkillNodeRankEntry> { new UndeadSkillNodeRankEntry { nodeId = "calcified_hide", rank = 1 } });
            runtime.TryApplyFromSerializedState();

            Assert.AreEqual(10, stats.Constitution.GetValue());
        }

        [Test]
        public void Undead_CannotUsePotion_InInventoryUsability()
        {
            GameObject owner = CreateUndead(BuildSampleTree());
            owner.AddComponent<EnemyController>();
            ItemData potion = ScriptableObject.CreateInstance<ItemData>();
            _destroy.Add(potion);
            potion.category = ItemCategory.Potion;
            var instance = new ItemInstance(potion);
            BaseActor actor = owner.GetComponent<BaseActor>();

            var row = new InventoryViewModel.Row(
                'a',
                instance,
                actor,
                actor.DisplayName,
                isEquipped: false,
                equippedSlot: null,
                carriedListIndex: 0,
                stackedWeight: 1f);

            Assert.IsFalse(InventoryUsability.AppearsUsableNow(row, inCombat: false));
            Assert.IsFalse(InventoryConsumePolicy.CanConsume(row, out string reason));
            Assert.AreEqual(InventoryConsumePolicy.UndeadPotionBanMessage, reason);
        }

        GameObject CreateUndead(UndeadSkillTreeDefinition tree, params UndeadSkillNodeRankEntry[] presets)
        {
            GameObject go = new GameObject("UndeadTest");
            _destroy.Add(tree);
            _destroy.Add(go);
            CharacterStats stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Undead;
            stats.racialSubsystem = RacialSubsystemKind.UndeadSkillTree;
            go.AddComponent<RacialLoadoutApplier>();
            UndeadSkillTreeRuntime runtime = go.AddComponent<UndeadSkillTreeRuntime>();
            runtime.SetSkillTreeAndRanks(tree, 10, new List<UndeadSkillNodeRankEntry>(presets));
            return go;
        }

        static UndeadSkillTreeDefinition BuildSampleTree()
        {
            var tree = ScriptableObject.CreateInstance<UndeadSkillTreeDefinition>();
            tree.clusters = new List<UndeadSkillTreeClusterData>
            {
                new UndeadSkillTreeClusterData { clusterId = "basic", pointsRequiredToUnlock = 0 },
                new UndeadSkillTreeClusterData { clusterId = "core", pointsRequiredToUnlock = 2 },
                new UndeadSkillTreeClusterData { clusterId = "class", pointsRequiredToUnlock = 6 }
            };
            tree.nodes = new List<UndeadSkillTreeNodeData>
            {
                new UndeadSkillTreeNodeData
                {
                    nodeId = "grave_touch",
                    clusterId = "basic",
                    nodeKind = UndeadSkillNodeKind.Skill,
                    maxRanks = 5
                },
                new UndeadSkillTreeNodeData
                {
                    nodeId = "calcified_hide",
                    clusterId = "basic",
                    nodeKind = UndeadSkillNodeKind.Passive,
                    maxRanks = 3,
                    statModifiers = new List<AttributeModifier>
                    {
                        new AttributeModifier { attribute = StatType.Constitution, value = 1 }
                    }
                },
                new UndeadSkillTreeNodeData
                {
                    nodeId = "embrace_the_dark",
                    clusterId = "core",
                    nodeKind = UndeadSkillNodeKind.Passive,
                    maxRanks = 1,
                    resistanceModifiers = new List<DamageResistanceModifier>
                    {
                        new DamageResistanceModifier { type = DamageType.Necrotic, value = 10 }
                    }
                },
                new UndeadSkillTreeNodeData
                {
                    nodeId = "lichs_bargain",
                    clusterId = "basic",
                    nodeKind = UndeadSkillNodeKind.Skill,
                    maxRanks = 1,
                    mutualExclusivityGroup = 1,
                    statModifiers = new List<AttributeModifier>
                    {
                        new AttributeModifier { attribute = StatType.Intelligence, value = 1 }
                    }
                },
                new UndeadSkillTreeNodeData
                {
                    nodeId = "death_knell",
                    clusterId = "basic",
                    nodeKind = UndeadSkillNodeKind.Skill,
                    maxRanks = 1,
                    mutualExclusivityGroup = 1,
                    statModifiers = new List<AttributeModifier>
                    {
                        new AttributeModifier { attribute = StatType.Strength, value = 1 }
                    }
                }
            };
            return tree;
        }
    }
}
