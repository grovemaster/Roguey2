using System.Collections.Generic;
using JRogue.Racial;
using JRogue.UI.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UI.Racial
{
    public class BarbarianSpiritImprintViewModelTests
    {
        readonly List<Object> _toDestroy = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _toDestroy)
            {
                if (o != null)
                    Object.DestroyImmediate(o);
            }

            _toDestroy.Clear();
        }

        [Test]
        public void ForeclosedSibling_AppearsAsGhost_AfterCommittedChoice()
        {
            SpiritImprintGraph graph = BuildTierOneExclusiveGraph();
            var vm = BarbarianSpiritImprintViewModel.BuildFromPath(
                graph,
                new List<string> { "imprint_root", "tier1_str" });

            Assert.AreEqual(3, vm.Cards.Count);
            Assert.AreEqual(SpiritImprintCardKind.Committed, vm.Cards[0].Kind);
            Assert.AreEqual("imprint_root", vm.Cards[0].NodeId);
            Assert.AreEqual(SpiritImprintCardKind.Committed, vm.Cards[1].Kind);
            Assert.AreEqual("tier1_str", vm.Cards[1].NodeId);
            Assert.AreEqual(SpiritImprintCardKind.ForeclosedGhost, vm.Cards[2].Kind);
            Assert.AreEqual("tier1_dex", vm.Cards[2].NodeId);
            StringAssert.Contains("Not chosen", vm.Cards[2].Subtitle);
            StringAssert.Contains("+1 STR", vm.Cards[2].Subtitle);
        }

        [Test]
        public void DeepUnreachedNodes_AreHidden()
        {
            SpiritImprintGraph graph = BuildDeepBranchGraph();
            var vm = BarbarianSpiritImprintViewModel.BuildFromPath(
                graph,
                new List<string> { "imprint_root", "branch_a" });

            Assert.AreEqual(3, vm.Cards.Count);
            Assert.IsFalse(ContainsNodeId(vm, "branch_a_child"));
            Assert.IsFalse(ContainsNodeId(vm, "branch_b"));
        }

        [Test]
        public void NonExclusiveSibling_IsNotGhost()
        {
            SpiritImprintGraph graph = BuildNonExclusiveSiblingGraph();
            var vm = BarbarianSpiritImprintViewModel.BuildFromPath(
                graph,
                new List<string> { "imprint_root", "tier1_str" });

            Assert.AreEqual(2, vm.Cards.Count);
            Assert.IsFalse(ContainsNodeId(vm, "side_from_root"));
        }

        static bool ContainsNodeId(BarbarianSpiritImprintViewModel vm, string nodeId)
        {
            foreach (SpiritImprintCardViewModel card in vm.Cards)
            {
                if (card.NodeId == nodeId)
                    return true;
            }

            return false;
        }

        SpiritImprintGraph BuildTierOneExclusiveGraph()
        {
            var graph = ScriptableObject.CreateInstance<SpiritImprintGraph>();
            _toDestroy.Add(graph);
            graph.rootNodeId = "imprint_root";
            graph.nodes = new List<SpiritImprintNodeData>
            {
                new SpiritImprintNodeData
                {
                    nodeId = "imprint_root",
                    displayName = "Root",
                    parentNodeId = string.Empty
                },
                new SpiritImprintNodeData
                {
                    nodeId = "tier1_str",
                    displayName = "+1 STR",
                    parentNodeId = "imprint_root",
                    siblingExclusivityGroup = 1
                },
                new SpiritImprintNodeData
                {
                    nodeId = "tier1_dex",
                    displayName = "First Mark — Dexterity",
                    parentNodeId = "imprint_root",
                    siblingExclusivityGroup = 1
                }
            };
            return graph;
        }

        SpiritImprintGraph BuildDeepBranchGraph()
        {
            var graph = ScriptableObject.CreateInstance<SpiritImprintGraph>();
            _toDestroy.Add(graph);
            graph.rootNodeId = "imprint_root";
            graph.nodes = new List<SpiritImprintNodeData>
            {
                new SpiritImprintNodeData
                {
                    nodeId = "imprint_root",
                    displayName = "Root",
                    parentNodeId = string.Empty
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
                },
                new SpiritImprintNodeData
                {
                    nodeId = "branch_a_child",
                    displayName = "Deep",
                    parentNodeId = "branch_a"
                }
            };
            return graph;
        }

        SpiritImprintGraph BuildNonExclusiveSiblingGraph()
        {
            var graph = BuildTierOneExclusiveGraph();
            graph.nodes.Add(new SpiritImprintNodeData
            {
                nodeId = "side_from_root",
                displayName = "Side",
                parentNodeId = "imprint_root",
                siblingExclusivityGroup = 0
            });
            return graph;
        }
    }
}
