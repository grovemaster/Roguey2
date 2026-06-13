using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Ability.Fireball;
using JRogue.Ability.SuddenStrength;
using JRogue.Actors;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UI.Racial
{
    [TestFixture]
    public sealed class DragonianRacialAbilitiesMenuTests
    {
        readonly List<GameObject> _created = new();
        readonly List<Object> _assets = new();

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
        public void SortSpells_OrdersByDisplayNameThenSpellId()
        {
            DragonianSpellDefinition beta = CreateSpell("b", "Beta Form");
            DragonianSpellDefinition alpha = CreateSpell("a", "Alpha Form");
            DragonianSpellDefinition alphaDup = CreateSpell("c", "Alpha Form");

            var sorted = DragonianSpellBodyViewModel.SortSpells(new[] { beta, alpha, alphaDup });

            Assert.AreEqual("a", sorted[0].spellId);
            Assert.AreEqual("c", sorted[1].spellId);
            Assert.AreEqual("b", sorted[2].spellId);
        }

        [Test]
        public void ResolveSelectedSpellId_PrefersRequestedThenMemorizedThenKnown()
        {
            DragonianSpellDefinition memorized = CreateSpell("mem", "Memorized");
            DragonianSpellDefinition known = CreateSpell("known", "Known");

            var memorizedRows = new List<DragonianSpellRowModel>
            {
                new() { SpellId = memorized.spellId, Spell = memorized },
            };
            var knownRows = new List<DragonianSpellRowModel>
            {
                new() { SpellId = known.spellId, Spell = known },
            };

            Assert.AreEqual(
                "known",
                DragonianSpellBodyViewModel.ResolveSelectedSpellId("known", memorizedRows, knownRows));
            Assert.AreEqual(
                "mem",
                DragonianSpellBodyViewModel.ResolveSelectedSpellId(null, memorizedRows, knownRows));
            Assert.AreEqual(
                "known",
                DragonianSpellBodyViewModel.ResolveSelectedSpellId(null, new List<DragonianSpellRowModel>(), knownRows));
        }

        [Test]
        public void ResolveBannerText_MatchesEditMode()
        {
            Assert.AreEqual(
                DragonianSpellBodyViewModel.EditModeBannerText,
                DragonianSpellBodyViewModel.ResolveBannerText(DragonianSpellLoadoutEditMode.Edit));
            Assert.AreEqual(
                DragonianSpellBodyViewModel.ViewOnlyDungeonBannerText,
                DragonianSpellBodyViewModel.ResolveBannerText(DragonianSpellLoadoutEditMode.ViewOnlyDungeon));
            Assert.AreEqual(
                DragonianSpellBodyViewModel.ViewOnlyCombatBannerText,
                DragonianSpellBodyViewModel.ResolveBannerText(DragonianSpellLoadoutEditMode.ViewOnlyCombat));
        }

        [Test]
        public void Build_PartitionsRowsAndMarksEquippedOnKnownColumn()
        {
            DragonianSpellDefinition surge = CreateSpell("dragonian_spell_sudden_strength", "Draconic Surge", 3, 1);
            DragonianSpellDefinition flame = CreateSpell("dragonian_spell_fireball", "Dragon Flame", 7, 5);
            BaseActor actor = CreateDragonianActor(maxSoulPowerBudget: 100, surge, flame, memorizedIds: new[] { surge.spellId });

            DragonianSpellBodyViewModel vm = DragonianSpellBodyViewModel.Build(actor);

            Assert.AreEqual(1, vm.MemorizedRows.Count);
            Assert.AreEqual(2, vm.KnownRows.Count);
            Assert.AreEqual(surge.spellId, vm.MemorizedRows[0].SpellId);
            Assert.IsFalse(vm.MemorizedRows[0].ShowEquippedBadge);
            Assert.IsTrue(FindRow(vm.KnownRows, surge.spellId).ShowEquippedBadge);
            Assert.IsFalse(FindRow(vm.KnownRows, flame.spellId).ShowEquippedBadge);
            Assert.That(vm.BudgetLine, Does.Contain("Equipped 3/100"));
            Assert.That(vm.BudgetLine, Does.Contain("Free 97"));
        }

        [Test]
        public void Build_ViewOnlyMode_HidesEquipActionsOnDetail()
        {
            DragonianSpellDefinition flame = CreateSpell("dragonian_spell_fireball", "Dragon Flame", 7, 5);
            BaseActor actor = CreateDragonianActor(
                maxSoulPowerBudget: 100,
                spellA: flame,
                memorizedIds: System.Array.Empty<string>());

            DragonianSpellBodyViewModel vm = DragonianSpellBodyViewModel.Build(actor, flame.spellId);

            Assert.AreNotEqual(DragonianSpellLoadoutEditMode.Edit, vm.EditMode);
            Assert.IsFalse(vm.Detail.ShowEquipButton);
            Assert.IsFalse(vm.Detail.ShowUnequipButton);
            Assert.That(vm.Detail.CostLine, Does.Contain("Memorize cost: 7"));
        }

        [Test]
        public void Build_DefaultSelectionUsesFirstMemorizedSpell()
        {
            DragonianSpellDefinition surge = CreateSpell("dragonian_spell_sudden_strength", "Draconic Surge", 3, 1);
            DragonianSpellDefinition flame = CreateSpell("dragonian_spell_fireball", "Dragon Flame", 7, 5);
            BaseActor actor = CreateDragonianActor(
                maxSoulPowerBudget: 100,
                surge,
                flame,
                memorizedIds: new[] { surge.spellId, flame.spellId });

            DragonianSpellBodyViewModel vm = DragonianSpellBodyViewModel.Build(actor);

            Assert.AreEqual(surge.spellId, vm.SelectedSpellId);
            Assert.AreEqual("Draconic Surge", vm.Detail.Title);
        }

        static DragonianSpellRowModel FindRow(IReadOnlyList<DragonianSpellRowModel> rows, string spellId)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (string.Equals(rows[i].SpellId, spellId, System.StringComparison.OrdinalIgnoreCase))
                    return rows[i];
            }

            Assert.Fail($"Missing row for {spellId}");
            return null;
        }

        BaseActor CreateDragonianActor(
            int maxSoulPowerBudget,
            DragonianSpellDefinition spellA,
            DragonianSpellDefinition spellB = null,
            string[] memorizedIds = null)
        {
            var go = new GameObject("DragonianMenuTest");
            _created.Add(go);

            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Dragonian;
            stats.humanClass = HumanClass.None;
            stats.racialSubsystem = RacialSubsystemKind.DragonianSpells;
            stats.Intelligence = new Stat(maxSoulPowerBudget / 10);
            stats.Wisdom = new Stat(maxSoulPowerBudget / 10);
            stats.currentSoulPower = maxSoulPowerBudget;

            var runtime = go.AddComponent<DragonianSpellsRuntime>();
            var spells = spellB != null
                ? new[] { spellA, spellB }
                : new[] { spellA };
            runtime.SetKnownAndMemorized(spells, memorizedIds ?? System.Array.Empty<string>());

            return go.AddComponent<BaseActor>();
        }

        DragonianSpellDefinition CreateSpell(
            string id,
            string displayName,
            int memorizeCost = 3,
            int castCost = 1,
            AbilityAction ability = null)
        {
            var spell = ScriptableObject.CreateInstance<DragonianSpellDefinition>();
            spell.spellId = id;
            spell.displayName = displayName;
            spell.memorizeCost = memorizeCost;
            spell.soulPowerCastCost = castCost;
            spell.ability = ability ?? CreateSuddenStrength();
            _assets.Add(spell);
            return spell;
        }

        SuddenStrengthAbility CreateSuddenStrength()
        {
            var ability = ScriptableObject.CreateInstance<SuddenStrengthAbility>();
            ability.requiresTarget = false;
            ability.soulPowerCost = 0;
            _assets.Add(ability);
            return ability;
        }
    }
}
