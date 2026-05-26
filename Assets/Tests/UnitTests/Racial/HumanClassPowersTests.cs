using JRogue.Ability;
using JRogue.Ability.Fireball;
using JRogue.Ability.Teleport;
using JRogue.Manager.Essence;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Racial
{
    [TestFixture]
    public sealed class HumanClassPowersTests
    {
        readonly System.Collections.Generic.List<GameObject> _created = new System.Collections.Generic.List<GameObject>();
        readonly System.Collections.Generic.List<Object> _assets = new System.Collections.Generic.List<Object>();

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
        public void Commit_BlocksSecondClassChange()
        {
            GameObject actor = CreateHumanActor();
            Assert.IsTrue(HumanClassCommitment.TryCommit(actor, HumanClass.Knight, out string error), error);
            Assert.IsFalse(HumanClassCommitment.TryCommit(actor, HumanClass.Priest, out error));
            Assert.That(error, Does.Contain("permanent"));
        }

        [Test]
        public void Mage_HasZeroEssenceSlots_AndCannotEquip()
        {
            GameObject actor = CreateHumanActor();
            var essence = actor.GetComponent<EssenceSlotManager>();
            var essenceData = ScriptableObject.CreateInstance<JRogue.Item.Essence.EssenceData>();
            _assets.Add(essenceData);

            HumanClassCommitment.TryCommit(actor, HumanClass.Mage, out _);

            Assert.AreEqual(0, essence.totalSlots);
            Assert.IsFalse(essence.EquipEssence(essenceData, 0));
            Assert.AreEqual(0, actor.GetComponent<CharacterStats>().MaxSoulPower);
        }

        [Test]
        public void Mage_EquipBudget_FireballAndTeleport_LeavesNineRemaining()
        {
            MageSpellDefinition fireball = CreateMageSpell("mage_spell_fireball", 3, CreateFireball());
            MageSpellDefinition teleport = CreateMageSpell("mage_spell_teleport", 6, CreateTeleport());

            GameObject actor = CreateHumanActor();
            var mage = actor.AddComponent<HumanMageSpellsRuntime>();
            mage.SetKnownAndEquipped(
                new[] { fireball, teleport },
                new[] { "mage_spell_fireball", "mage_spell_teleport" });

            var stats = actor.GetComponent<CharacterStats>();
            stats.levelMagicPowerBonus = 0;
            HumanClassCommitment.TryCommit(actor, HumanClass.Mage, out _);

            stats.Intelligence = new Stat(4);
            Assert.AreEqual(20, stats.MaxMagicPower);
            Assert.AreEqual(9, mage.RemainingEquipCapacity);
        }

        [Test]
        public void Mage_CannotEquipTier1Spell_WhenBudgetInsufficient()
        {
            MageSpellDefinition fireball = CreateMageSpell("mage_spell_fireball", 3, CreateFireball());
            MageSpellDefinition teleport = CreateMageSpell("mage_spell_teleport", 6, CreateTeleport());
            MageSpellDefinition tier1 = CreateMageSpell("mage_spell_tier1", 1, CreateFireball());

            GameObject actor = CreateHumanActor();
            var mage = actor.AddComponent<HumanMageSpellsRuntime>();
            mage.SetKnownAndEquipped(
                new[] { fireball, teleport, tier1 },
                new[] { "mage_spell_fireball", "mage_spell_teleport" });

            var stats = actor.GetComponent<CharacterStats>();
            stats.Intelligence = new Stat(4);
            HumanClassCommitment.TryCommit(actor, HumanClass.Mage, out _);

            Assert.IsFalse(mage.TryEquip("mage_spell_tier1", out string reason));
            Assert.That(reason, Does.Contain("capacity"));
        }

        [Test]
        public void Knight_PassiveMight_AddsStrengthPerRank()
        {
            HumanClassSkillTreeDefinition tree = CreateKnightSampleTree();
            GameObject actor = CreateHumanActor();
            var runtime = actor.AddComponent<HumanClassSkillTreeRuntime>();
            runtime.SetSkillTreeAndRanks(
                tree,
                10,
                new[] { new HumanSkillNodeRankEntry { nodeId = "knight_passive_might", rank = 1 } });

            HumanClassCommitment.TryCommit(actor, HumanClass.Knight, out _);

            int baseStr = actor.GetComponent<CharacterStats>().Strength.GetValue();
            Assert.AreEqual(12, baseStr);
        }

        [Test]
        public void None_UsesDefaultEssenceSlots()
        {
            GameObject actor = CreateHumanActor();
            var essence = actor.GetComponent<EssenceSlotManager>();
            essence.ApplyMaxSlotsFromClass();
            Assert.AreEqual(3, essence.totalSlots);
            Assert.Greater(actor.GetComponent<CharacterStats>().MaxSoulPower, 0);
        }

        GameObject CreateHumanActor()
        {
            var go = new GameObject("HumanTest");
            _created.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Human;
            stats.humanClass = HumanClass.None;
            stats.Strength = new Stat(10);
            stats.Dexterity = new Stat(10);
            stats.Intelligence = new Stat(10);
            stats.Wisdom = new Stat(10);
            go.AddComponent<EssenceSlotManager>();
            return go;
        }

        HumanClassSkillTreeDefinition CreateKnightSampleTree()
        {
            var tree = ScriptableObject.CreateInstance<HumanClassSkillTreeDefinition>();
            tree.humanClass = HumanClass.Knight;
            tree.nodes = new System.Collections.Generic.List<HumanClassSkillTreeNodeData>
            {
                new HumanClassSkillTreeNodeData
                {
                    nodeId = "knight_passive_might",
                    maxRanks = 5,
                    requiredCharacterLevel = 1,
                    perRankStatModifiers = new System.Collections.Generic.List<HumanPerRankStatModifier>
                    {
                        new HumanPerRankStatModifier { attribute = StatType.Strength, valuePerRank = 2 }
                    }
                },
                new HumanClassSkillTreeNodeData
                {
                    nodeId = "knight_passive_finesse",
                    maxRanks = 5,
                    requiredCharacterLevel = 1,
                    requiredParentNodeId = "knight_passive_might",
                    requiredParentMinRank = 1,
                    perRankStatModifiers = new System.Collections.Generic.List<HumanPerRankStatModifier>
                    {
                        new HumanPerRankStatModifier { attribute = StatType.Dexterity, valuePerRank = 2 }
                    }
                }
            };
            _assets.Add(tree);
            return tree;
        }

        MageSpellDefinition CreateMageSpell(string id, int tier, AbilityAction ability)
        {
            var spell = ScriptableObject.CreateInstance<MageSpellDefinition>();
            spell.spellId = id;
            spell.tier = tier;
            spell.ability = ability;
            spell.magicPowerCost = 1;
            _assets.Add(spell);
            return spell;
        }

        FireballAbility CreateFireball()
        {
            var a = ScriptableObject.CreateInstance<FireballAbility>();
            a.requiresTarget = true;
            _assets.Add(a);
            return a;
        }

        TeleportAbility CreateTeleport()
        {
            var a = ScriptableObject.CreateInstance<TeleportAbility>();
            a.requiresTarget = true;
            _assets.Add(a);
            return a;
        }
    }
}
