using System.Collections.Generic;
using JRogue.Item.Essence;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.Racial
{
    public class SpiritImprintRuntimeTests
    {
        List<Object> _toDestroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _toDestroy)
            {
                if (o != null)
                    Object.DestroyImmediate(o);
            }

            _toDestroy.Clear();
        }

        [Test]
        public void Rank0_RootOnly_NoStatModifier()
        {
            var graph = BuildSampleGraph();
            var go = new GameObject("BarbTest");
            _toDestroy.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Barbarian;
            stats.racialSubsystem = RacialSubsystemKind.SpiritImprintBarbarian;
            var imprint = go.AddComponent<SpiritImprintRuntime>();
            imprint.SetGraphAndChosenPath(graph, new List<string> { "imprint_root" });
            imprint.TryApplyFromSerializedState();

            Assert.AreEqual(0, imprint.ImprintRank);
            Assert.AreEqual(10, stats.Strength.GetValue());
        }

        [Test]
        public void Rank1_PresetPath_AppliesStrengthModifier()
        {
            var graph = BuildSampleGraph();
            var go = new GameObject("BarbTest2");
            _toDestroy.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Barbarian;
            stats.racialSubsystem = RacialSubsystemKind.SpiritImprintBarbarian;
            var imprint = go.AddComponent<SpiritImprintRuntime>();
            imprint.SetGraphAndChosenPath(graph, new List<string> { "imprint_root", "tier1_str" });
            imprint.TryApplyFromSerializedState();

            Assert.AreEqual(1, imprint.ImprintRank);
            Assert.AreEqual(11, stats.Strength.GetValue());
        }

        [Test]
        public void InvalidPath_FallsBackToRootOnly()
        {
            var graph = BuildSampleGraphWithSideBranch();
            var go = new GameObject("BarbTest3");
            _toDestroy.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Barbarian;
            stats.racialSubsystem = RacialSubsystemKind.SpiritImprintBarbarian;
            var imprint = go.AddComponent<SpiritImprintRuntime>();
            imprint.SetGraphAndChosenPath(graph,
                new List<string> { "imprint_root", "tier1_str", "side_from_root" });
            imprint.TryApplyFromSerializedState();

            Assert.AreEqual(0, imprint.ImprintRank);
            Assert.AreEqual(10, stats.Strength.GetValue());
            Assert.AreEqual("imprint_root", imprint.ChosenPathNodeIds[0]);
        }

        [Test]
        public void ValidatePath_RejectsWhenChildDoesNotExtendTail()
        {
            var graph = BuildExclusiveBranchGraph();
            var badPath = new List<string> { "imprint_root", "branch_a", "branch_b" };
            var normalized = graph.ValidateAndNormalizePath(badPath, out var err);
            Assert.IsNull(normalized);
            Assert.IsNotNull(err);
            StringAssert.Contains("not a child", err);
        }

        [Test]
        public void ExclusiveBranch_EitherSingleChildPathValid()
        {
            var graph = BuildExclusiveBranchGraph();
            var aPath = graph.ValidateAndNormalizePath(new List<string> { "imprint_root", "branch_a" }, out var e1);
            var bPath = graph.ValidateAndNormalizePath(new List<string> { "imprint_root", "branch_b" }, out var e2);
            Assert.IsNull(e1);
            Assert.IsNull(e2);
            Assert.NotNull(aPath);
            Assert.NotNull(bPath);
        }

        [Test]
        public void Graph_ValidateAndNormalize_EmptyPathBecomesRoot()
        {
            var graph = BuildSampleGraph();
            var path = graph.ValidateAndNormalizePath(null, out var err);
            Assert.IsNull(err);
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual("imprint_root", path[0]);
        }

        SpiritImprintGraph BuildSampleGraph()
        {
            var g = ScriptableObject.CreateInstance<SpiritImprintGraph>();
            _toDestroy.Add(g);
            g.rootNodeId = "imprint_root";
            g.nodes = new List<SpiritImprintNodeData>
            {
                new SpiritImprintNodeData
                {
                    nodeId = "imprint_root",
                    displayName = "Root",
                    parentNodeId = "",
                    statModifiers = new List<AttributeModifier>(),
                    resistanceModifiers = new List<DamageResistanceModifier>(),
                    passiveEffects = new List<PassiveEffect>(),
                    activeAbilities = new List<JRogue.Ability.AbilityAction>()
                },
                new SpiritImprintNodeData
                {
                    nodeId = "tier1_str",
                    displayName = "+1 STR",
                    parentNodeId = "imprint_root",
                    statModifiers = new List<AttributeModifier>
                    {
                        new AttributeModifier { attribute = StatType.Strength, value = 1 }
                    },
                    resistanceModifiers = new List<DamageResistanceModifier>(),
                    passiveEffects = new List<PassiveEffect>(),
                    activeAbilities = new List<JRogue.Ability.AbilityAction>()
                }
            };
            return g;
        }

        SpiritImprintGraph BuildSampleGraphWithSideBranch()
        {
            var g = BuildSampleGraph();
            g.nodes.Add(new SpiritImprintNodeData
            {
                nodeId = "side_from_root",
                displayName = "Side",
                parentNodeId = "imprint_root",
                statModifiers = new List<AttributeModifier>()
            });
            return g;
        }

        SpiritImprintGraph BuildExclusiveBranchGraph()
        {
            var g = ScriptableObject.CreateInstance<SpiritImprintGraph>();
            _toDestroy.Add(g);
            g.rootNodeId = "imprint_root";
            g.nodes = new List<SpiritImprintNodeData>
            {
                new SpiritImprintNodeData
                {
                    nodeId = "imprint_root",
                    displayName = "Root",
                    parentNodeId = ""
                },
                new SpiritImprintNodeData
                {
                    nodeId = "branch_a",
                    displayName = "A",
                    parentNodeId = "imprint_root",
                    siblingExclusivityGroup = 1
                },
                new SpiritImprintNodeData
                {
                    nodeId = "branch_b",
                    displayName = "B",
                    parentNodeId = "imprint_root",
                    siblingExclusivityGroup = 1
                }
            };
            return g;
        }
    }
}
