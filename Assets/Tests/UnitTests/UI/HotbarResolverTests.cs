using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Ability.SuddenStrength;
using JRogue.Actors;
using JRogue.Input;
using JRogue.Item;
using JRogue.Item.Essence;
using JRogue.Manager.Essence;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Hotbar;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UI
{
    [TestFixture]
    public sealed class HotbarResolverTests
    {
        readonly List<Object> _cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _cleanup)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }

            _cleanup.Clear();
        }

        [Test]
        public void Resolve_EssenceActive_ReturnsAbilityAndSource()
        {
            BaseActor actor = CreateActorWithEssence(out EssenceData essence, out AbilityAction ability);

            HotbarResolvedAction resolved = HotbarResolver.Resolve(
                actor,
                new HotbarEntry
                {
                    Kind = HotbarEntryKind.EssenceActive,
                    essenceSlotIndex = 0,
                    abilityIndex = 0,
                });

            Assert.IsTrue(resolved.IsValid);
            Assert.IsFalse(resolved.IsStale);
            Assert.AreSame(ability, resolved.Ability);
            Assert.AreEqual(PlayerAbilitySource.Essence, resolved.Source);
            Assert.AreEqual(0, resolved.SlotIndex);
            Assert.AreEqual(0, resolved.AbilityIndex);
            Assert.AreSame(essence, actor.GetComponent<EssenceSlotManager>().GetEssenceInSlot(0));
        }

        [Test]
        public void Resolve_MissingInventoryItem_IsStale()
        {
            BaseActor actor = CreateBareActor();

            HotbarResolvedAction resolved = HotbarResolver.Resolve(
                actor,
                new HotbarEntry
                {
                    Kind = HotbarEntryKind.InventoryUse,
                    itemInstanceId = "missing-item",
                });

            Assert.IsFalse(resolved.IsValid);
            Assert.IsTrue(resolved.IsStale);
        }

        [Test]
        public void Resolve_SpiritImprintNodeOnPath_ReturnsRacialActive()
        {
            BaseActor actor = CreateBareActor();
            var stats = actor.GetComponent<CharacterStats>();
            stats.race = Race.Barbarian;
            stats.racialSubsystem = RacialSubsystemKind.SpiritImprintBarbarian;

            var ability = ScriptableObject.CreateInstance<SuddenStrengthAbility>();
            ability.requiresTarget = false;
            _cleanup.Add(ability);

            SpiritImprintGraph graph = ScriptableObject.CreateInstance<SpiritImprintGraph>();
            graph.rootNodeId = "root";
            graph.nodes = new List<SpiritImprintNodeData>
            {
                new SpiritImprintNodeData
                {
                    nodeId = "root",
                    parentNodeId = string.Empty,
                    activeAbilities = new List<AbilityAction>(),
                },
                new SpiritImprintNodeData
                {
                    nodeId = "tier1",
                    parentNodeId = "root",
                    activeAbilities = new List<AbilityAction> { ability },
                },
            };
            _cleanup.Add(graph);

            var imprint = actor.gameObject.AddComponent<SpiritImprintRuntime>();
            imprint.SetGraphAndChosenPath(graph, new List<string> { "root", "tier1" });

            HotbarResolvedAction resolved = HotbarResolver.Resolve(
                actor,
                new HotbarEntry
                {
                    Kind = HotbarEntryKind.RacialActive,
                    racialBindingKey = HotbarResolver.BuildSpiritImprintBindingKey("tier1", 0),
                });

            Assert.IsTrue(resolved.IsValid);
            Assert.AreEqual(PlayerAbilitySource.RacialActive, resolved.Source);
            Assert.AreSame(ability, resolved.Ability);
            Assert.AreEqual("SpiritImprint:tier1:0", resolved.RacialBindingKey);
        }

        BaseActor CreateBareActor()
        {
            var go = new GameObject("HotbarActor");
            _cleanup.Add(go);
            go.AddComponent<CharacterStats>();
            go.AddComponent<EssenceSlotManager>();
            return go.AddComponent<BaseActor>();
        }

        BaseActor CreateActorWithEssence(out EssenceData essence, out AbilityAction ability)
        {
            BaseActor actor = CreateBareActor();

            ability = ScriptableObject.CreateInstance<SuddenStrengthAbility>();
            ability.requiresTarget = false;
            _cleanup.Add(ability);

            essence = ScriptableObject.CreateInstance<EssenceData>();
            essence.essenceName = "Test Essence";
            essence.statModifiers = new List<AttributeModifier>();
            essence.resistanceModifiers = new List<DamageResistanceModifier>();
            essence.complexPassives = new List<PassiveEffect>();
            essence.activeAbilities = new List<AbilityAction> { ability };
            _cleanup.Add(essence);

            actor.GetComponent<EssenceSlotManager>().EquipEssence(essence, 0);
            return actor;
        }
    }
}
