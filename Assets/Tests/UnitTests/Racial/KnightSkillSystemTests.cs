using System.Collections.Generic;
using JRogue.Ability.Knight;
using JRogue.Actors;
using JRogue.Racial;
using JRogue.Racial.Knight;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Racial
{
    [TestFixture]
    public sealed class KnightSkillSystemTests
    {
        readonly List<GameObject> _created = new List<GameObject>();
        readonly List<Object> _assets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
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
        }

        [Test]
        public void TrySpendPoint_IncrementsPassiveRank()
        {
            GameObject actor = CreateCommittedKnight(out HumanClassSkillTreeRuntime tree);
            Assert.IsTrue(tree.TrySpendPoint("knight_passive_might", out string error), error);
            Assert.AreEqual(1, tree.GetRank("knight_passive_might"));
            Assert.AreEqual(9, tree.UnspentPoints);
        }

        [Test]
        public void StanceExclusivity_ActivatingSecondStanceDeactivatesFirst()
        {
            GameObject actor = CreateCommittedKnight(out HumanClassSkillTreeRuntime tree);
            tree.SetSkillTreeAndRanks(
                tree.SkillTree,
                10,
                new[]
                {
                    new HumanSkillNodeRankEntry { nodeId = "knight_aura_valor", rank = 1 },
                    new HumanSkillNodeRankEntry { nodeId = "knight_aura_bulwark", rank = 1 },
                });
            tree.TryApplyFromSerializedState();

            KnightAuraToggleAbility valor = CreateToggle("knight_aura_valor");
            KnightAuraToggleAbility bulwark = CreateToggle("knight_aura_bulwark");
            KnightAuraStateRuntime auraState = actor.GetComponent<KnightAuraStateRuntime>();

            Assert.IsTrue(valor.Execute(actor));
            Assert.IsTrue(auraState.IsActive("knight_aura_valor"));

            Assert.IsTrue(bulwark.Execute(actor));
            Assert.IsFalse(auraState.IsActive("knight_aura_valor"));
            Assert.IsTrue(auraState.IsActive("knight_aura_bulwark"));
        }

        [Test]
        public void Dispatch_ActivationAwardsMasteryPxp()
        {
            GameObject actor = CreateCommittedKnight(out HumanClassSkillTreeRuntime tree);
            tree.SetSkillTreeAndRanks(
                tree.SkillTree,
                10,
                new[] { new HumanSkillNodeRankEntry { nodeId = "knight_aura_valor", rank = 1 } });
            tree.TryApplyFromSerializedState();

            KnightAuraToggleAbility valor = CreateToggle("knight_aura_valor");
            KnightSkillMasteryRuntime mastery = actor.GetComponent<KnightSkillMasteryRuntime>();

            KnightSkillProficiencyDispatcher.Dispatch(
                actor.GetComponent<BaseActor>(),
                "knight_aura_valor",
                KnightSkillProficiencyEventKind.Activation,
                valor);

            Assert.Greater(mastery.GetMasteryPxp("knight_aura_valor"), 0);
        }

        [Test]
        public void Dispatch_RankPxpAutoIncrementsActiveRank()
        {
            GameObject actor = CreateCommittedKnight(out HumanClassSkillTreeRuntime tree);
            tree.SetSkillTreeAndRanks(
                tree.SkillTree,
                10,
                new[] { new HumanSkillNodeRankEntry { nodeId = "knight_aura_valor", rank = 1 } });
            tree.TryApplyFromSerializedState();

            KnightAuraToggleAbility valor = CreateToggle("knight_aura_valor");
            KnightSkillMasteryRuntime mastery = actor.GetComponent<KnightSkillMasteryRuntime>();

            int threshold = KnightSkillProgressionRules.GetXpToNextRank(1);
            mastery.SetRankPxp("knight_aura_valor", threshold - 1);

            KnightSkillProficiencyDispatcher.Dispatch(
                actor.GetComponent<BaseActor>(),
                "knight_aura_valor",
                KnightSkillProficiencyEventKind.Activation,
                valor);

            Assert.AreEqual(2, tree.GetRank("knight_aura_valor"));
        }

        [Test]
        public void Dispatch_PassiveDoesNotGainRankPxp()
        {
            GameObject actor = CreateCommittedKnight(out HumanClassSkillTreeRuntime tree);
            tree.SetSkillTreeAndRanks(
                tree.SkillTree,
                10,
                new[] { new HumanSkillNodeRankEntry { nodeId = "knight_passive_might", rank = 1 } });
            tree.TryApplyFromSerializedState();

            KnightSkillMasteryRuntime mastery = actor.GetComponent<KnightSkillMasteryRuntime>();
            mastery.SetRankPxp("knight_passive_might", 9999);

            KnightSkillProficiencyDispatcher.Dispatch(
                actor.GetComponent<BaseActor>(),
                "knight_passive_might",
                KnightSkillProficiencyEventKind.Activation,
                null);

            Assert.AreEqual(1, tree.GetRank("knight_passive_might"));
            Assert.AreEqual(9999, mastery.GetRankPxp("knight_passive_might"));
        }

        [Test]
        public void PulseAbility_AwardsMasteryThroughExecutionService()
        {
            GameObject actor = CreateCommittedKnight(out HumanClassSkillTreeRuntime tree);
            tree.SetSkillTreeAndRanks(
                tree.SkillTree,
                10,
                new[] { new HumanSkillNodeRankEntry { nodeId = "knight_pulse_rally", rank = 1 } });
            tree.TryApplyFromSerializedState();

            KnightAuraPulseAbility pulse = CreatePulse("knight_pulse_rally");
            KnightSkillMasteryRuntime mastery = actor.GetComponent<KnightSkillMasteryRuntime>();

            Assert.IsTrue(HumanKnightSkillExecutionService.TryExecute(
                actor.GetComponent<BaseActor>(),
                pulse,
                null,
                out _));

            Assert.Greater(mastery.GetMasteryPxp("knight_pulse_rally"), 0);
        }

        GameObject CreateCommittedKnight(out HumanClassSkillTreeRuntime treeRuntime)
        {
            GameObject actor = new GameObject("KnightTest");
            _created.Add(actor);
            actor.AddComponent<BaseActor>();
            var stats = actor.AddComponent<CharacterStats>();
            stats.race = Race.Human;
            stats.humanClass = HumanClass.None;
            stats.level = 10;

            treeRuntime = actor.AddComponent<HumanClassSkillTreeRuntime>();
            treeRuntime.SetSkillTreeAndRanks(CreateSampleTree(), 10, new List<HumanSkillNodeRankEntry>());

            HumanClassCommitment.TryCommit(actor, HumanClass.Knight, out _);
            return actor;
        }

        HumanClassSkillTreeDefinition CreateSampleTree()
        {
            var tree = ScriptableObject.CreateInstance<HumanClassSkillTreeDefinition>();
            tree.humanClass = HumanClass.Knight;

            var might = new HumanClassSkillTreeNodeData
            {
                nodeId = "knight_passive_might",
                maxRanks = 5,
                requiredCharacterLevel = 1,
                tags = new List<KnightSkillTag> { KnightSkillTag.Passive },
                perRankStatModifiers = new List<HumanPerRankStatModifier>
                {
                    new HumanPerRankStatModifier { attribute = StatType.Strength, valuePerRank = 2 },
                },
            };

            var valorToggle = CreateToggle("knight_aura_valor");
            var valor = new HumanClassSkillTreeNodeData
            {
                nodeId = "knight_aura_valor",
                maxRanks = 5,
                requiredCharacterLevel = 3,
                requiredParentNodeId = "knight_passive_might",
                requiredParentMinRank = 1,
                tags = new List<KnightSkillTag>
                {
                    KnightSkillTag.Aura,
                    KnightSkillTag.AuraStance,
                    KnightSkillTag.AuraToggle,
                },
                activeAbilities = new List<Ability.AbilityAction> { valorToggle },
            };

            var bulwarkToggle = CreateToggle("knight_aura_bulwark");
            var bulwark = new HumanClassSkillTreeNodeData
            {
                nodeId = "knight_aura_bulwark",
                maxRanks = 5,
                requiredCharacterLevel = 5,
                requiredParentNodeId = "knight_passive_might",
                requiredParentMinRank = 2,
                tags = new List<KnightSkillTag>
                {
                    KnightSkillTag.Aura,
                    KnightSkillTag.AuraStance,
                    KnightSkillTag.AuraToggle,
                },
                activeAbilities = new List<Ability.AbilityAction> { bulwarkToggle },
            };

            var pulseAbility = CreatePulse("knight_pulse_rally");
            var rally = new HumanClassSkillTreeNodeData
            {
                nodeId = "knight_pulse_rally",
                maxRanks = 3,
                requiredCharacterLevel = 4,
                tags = new List<KnightSkillTag> { KnightSkillTag.Aura, KnightSkillTag.AuraPulse },
                activeAbilities = new List<Ability.AbilityAction> { pulseAbility },
            };

            tree.nodes = new List<HumanClassSkillTreeNodeData> { might, valor, bulwark, rally };
            _assets.Add(tree);
            return tree;
        }

        KnightAuraToggleAbility CreateToggle(string nodeId)
        {
            var ability = ScriptableObject.CreateInstance<KnightAuraToggleAbility>();
            ability.knightSkillId = nodeId;
            _assets.Add(ability);
            return ability;
        }

        KnightAuraPulseAbility CreatePulse(string nodeId)
        {
            var ability = ScriptableObject.CreateInstance<KnightAuraPulseAbility>();
            ability.knightSkillId = nodeId;
            _assets.Add(ability);
            return ability;
        }
    }
}
