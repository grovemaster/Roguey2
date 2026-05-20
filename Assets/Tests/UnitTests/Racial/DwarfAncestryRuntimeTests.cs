using System.Collections.Generic;
using JRogue.Item.Essence;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.Racial
{
    public class DwarfAncestryRuntimeTests
    {
        readonly List<Object> _toDestroy = new List<Object>();

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
        public void CommonAbilities_ApplyStatModifiersInAssignedSlots()
        {
            DwarfCommonAbilityDefinition stonecunning = CreateCommonAbility("stonecunning", StatType.Wisdom, 1);
            DwarfCommonAbilityDefinition resilience = CreateCommonAbility("resilience", StatType.Constitution, 1);

            GameObject go = CreateDwarfActor(out CharacterStats stats);
            _toDestroy.Add(stonecunning);
            _toDestroy.Add(resilience);

            DwarfCommonAbilitiesRuntime common = go.AddComponent<DwarfCommonAbilitiesRuntime>();
            common.SetPresetCommonAbilities(new List<DwarfCommonSlotPreset>
            {
                new DwarfCommonSlotPreset { slotIndex = 0, ability = stonecunning },
                new DwarfCommonSlotPreset { slotIndex = 1, ability = resilience }
            });
            common.TryApplyPresetFromSerialized();

            Assert.AreEqual(11, stats.Wisdom.GetValue());
            Assert.AreEqual(11, stats.Constitution.GetValue());
        }

        [Test]
        public void AncestorPath_Rank2_AppliesStrengthAndConstitutionFromPath()
        {
            SpiritImprintGraph graph = BuildThreeNodeAncestorGraph();
            _toDestroy.Add(graph);
            AncestorDefinition patron = ScriptableObject.CreateInstance<AncestorDefinition>();
            patron.ancestorId = "test_patron";
            patron.abilityTree = graph;
            _toDestroy.Add(patron);

            GameObject go = CreateDwarfActor(out CharacterStats stats);
            DwarfAncestorPathRuntime ancestor = go.AddComponent<DwarfAncestorPathRuntime>();
            ancestor.SetPatronAndPath(patron,
                new List<string> { "ancestor_root", "forge_blessing", "stone_endurance" });
            ancestor.TryApplyFromSerializedState();

            Assert.AreEqual(2, ancestor.AncestorRank);
            Assert.AreEqual(11, stats.Strength.GetValue());
            Assert.AreEqual(11, stats.Constitution.GetValue());
        }

        [Test]
        public void AncestorPath_NoPatron_AppliesNothing()
        {
            GameObject go = CreateDwarfActor(out CharacterStats stats);
            DwarfAncestorPathRuntime ancestor = go.AddComponent<DwarfAncestorPathRuntime>();
            ancestor.TryApplyFromSerializedState();

            Assert.AreEqual(0, ancestor.AncestorRank);
            Assert.AreEqual(10, stats.Strength.GetValue());
        }

        [Test]
        public void CommonAndAncestor_StackModifiers()
        {
            DwarfCommonAbilityDefinition wisdom = CreateCommonAbility("w", StatType.Wisdom, 1);
            _toDestroy.Add(wisdom);
            SpiritImprintGraph graph = BuildThreeNodeAncestorGraph();
            _toDestroy.Add(graph);
            AncestorDefinition patron = ScriptableObject.CreateInstance<AncestorDefinition>();
            patron.abilityTree = graph;
            _toDestroy.Add(patron);

            GameObject go = CreateDwarfActor(out CharacterStats stats);
            DwarfCommonAbilitiesRuntime common = go.AddComponent<DwarfCommonAbilitiesRuntime>();
            common.SetPresetCommonAbilities(new List<DwarfCommonSlotPreset>
            {
                new DwarfCommonSlotPreset { slotIndex = 0, ability = wisdom }
            });
            common.TryApplyPresetFromSerialized();

            DwarfAncestorPathRuntime ancestor = go.AddComponent<DwarfAncestorPathRuntime>();
            ancestor.SetPatronAndPath(patron,
                new List<string> { "ancestor_root", "forge_blessing" });
            ancestor.TryApplyFromSerializedState();

            Assert.AreEqual(11, stats.Wisdom.GetValue());
            Assert.AreEqual(11, stats.Strength.GetValue());
        }

        GameObject CreateDwarfActor(out CharacterStats stats)
        {
            GameObject go = new GameObject("DwarfTest");
            _toDestroy.Add(go);
            stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Dwarf;
            stats.racialSubsystem = RacialSubsystemKind.DwarfAncestry;
            return go;
        }

        static DwarfCommonAbilityDefinition CreateCommonAbility(string id, StatType stat, int value)
        {
            var def = ScriptableObject.CreateInstance<DwarfCommonAbilityDefinition>();
            def.abilityId = id;
            def.statModifiers = new List<AttributeModifier>
            {
                new AttributeModifier { attribute = stat, value = value }
            };
            return def;
        }

        static SpiritImprintGraph BuildThreeNodeAncestorGraph()
        {
            var graph = ScriptableObject.CreateInstance<SpiritImprintGraph>();
            graph.rootNodeId = "ancestor_root";
            graph.nodes = new List<SpiritImprintNodeData>
            {
                new SpiritImprintNodeData
                {
                    nodeId = "ancestor_root",
                    displayName = "Root",
                    parentNodeId = string.Empty
                },
                new SpiritImprintNodeData
                {
                    nodeId = "forge_blessing",
                    displayName = "Forge",
                    parentNodeId = "ancestor_root",
                    statModifiers = new List<AttributeModifier>
                    {
                        new AttributeModifier { attribute = StatType.Strength, value = 1 }
                    }
                },
                new SpiritImprintNodeData
                {
                    nodeId = "stone_endurance",
                    displayName = "Stone",
                    parentNodeId = "forge_blessing",
                    statModifiers = new List<AttributeModifier>
                    {
                        new AttributeModifier { attribute = StatType.Constitution, value = 1 }
                    }
                }
            };
            return graph;
        }
    }
}
