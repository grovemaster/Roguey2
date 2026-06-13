using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Ability.Fireball;
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
    public sealed class HumanMageRacialAbilitiesMenuTests
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
            MageSpellDefinition beta = CreateSpell("b", "Beta Bolt");
            MageSpellDefinition alpha = CreateSpell("a", "Alpha Bolt");
            MageSpellDefinition alphaDup = CreateSpell("c", "Alpha Bolt");

            var sorted = HumanMageSpellBodyViewModel.SortSpells(new[] { beta, alpha, alphaDup });

            Assert.AreEqual("a", sorted[0].spellId);
            Assert.AreEqual("c", sorted[1].spellId);
            Assert.AreEqual("b", sorted[2].spellId);
        }

        [Test]
        public void ResolveSelectedSpellId_PrefersRequestedThenPreparedThenKnown()
        {
            MageSpellDefinition prepared = CreateSpell("prep", "Prepared");
            MageSpellDefinition known = CreateSpell("known", "Known");

            var preparedRows = new List<HumanMageSpellRowModel>
            {
                new() { SpellId = prepared.spellId, Spell = prepared },
            };
            var knownRows = new List<HumanMageSpellRowModel>
            {
                new() { SpellId = known.spellId, Spell = known },
            };

            Assert.AreEqual(
                "known",
                HumanMageSpellBodyViewModel.ResolveSelectedSpellId("known", preparedRows, knownRows));
            Assert.AreEqual(
                "prep",
                HumanMageSpellBodyViewModel.ResolveSelectedSpellId(null, preparedRows, knownRows));
            Assert.AreEqual(
                "known",
                HumanMageSpellBodyViewModel.ResolveSelectedSpellId(null, new List<HumanMageSpellRowModel>(), knownRows));
        }

        [Test]
        public void ResolveBannerText_MatchesEditMode()
        {
            Assert.AreEqual(
                HumanMageSpellBodyViewModel.EditModeBannerText,
                HumanMageSpellBodyViewModel.ResolveBannerText(HumanMageSpellLoadoutEditMode.Edit));
            Assert.AreEqual(
                HumanMageSpellBodyViewModel.ViewOnlyDungeonBannerText,
                HumanMageSpellBodyViewModel.ResolveBannerText(HumanMageSpellLoadoutEditMode.ViewOnlyDungeon));
            Assert.AreEqual(
                HumanMageSpellBodyViewModel.ViewOnlyCombatBannerText,
                HumanMageSpellBodyViewModel.ResolveBannerText(HumanMageSpellLoadoutEditMode.ViewOnlyCombat));
        }

        [Test]
        public void Build_PartitionsRowsAndMarksPreparedOnKnownColumn()
        {
            MageSpellDefinition fireball = CreateSpell("mage_spell_fireball", "Fireball", tier: 3, magicPowerCost: 5);
            MageSpellDefinition lightning = CreateSpell("mage_spell_lightning_bolt", "Lightning Bolt", tier: 5, magicPowerCost: 4);
            BaseActor actor = CreateHumanMageActor(
                intelligence: 4,
                fireball,
                lightning,
                equippedIds: new[] { fireball.spellId });

            HumanMageSpellBodyViewModel vm = HumanMageSpellBodyViewModel.Build(actor);

            Assert.AreEqual(1, vm.PreparedRows.Count);
            Assert.AreEqual(2, vm.KnownRows.Count);
            Assert.AreEqual(fireball.spellId, vm.PreparedRows[0].SpellId);
            Assert.IsFalse(vm.PreparedRows[0].ShowPreparedBadge);
            Assert.IsTrue(FindRow(vm.KnownRows, fireball.spellId).ShowPreparedBadge);
            Assert.IsFalse(FindRow(vm.KnownRows, lightning.spellId).ShowPreparedBadge);
            Assert.That(vm.BudgetLine, Does.Contain("Prepared 7/20"));
            Assert.That(vm.BudgetLine, Does.Contain("Free 13"));
        }

        [Test]
        public void Build_ViewOnlyMode_HidesPrepareActionsOnDetail()
        {
            MageSpellDefinition fireball = CreateSpell("mage_spell_fireball", "Fireball", tier: 3, magicPowerCost: 5);
            BaseActor actor = CreateHumanMageActor(
                intelligence: 4,
                fireball,
                equippedIds: System.Array.Empty<string>());

            HumanMageSpellBodyViewModel vm = HumanMageSpellBodyViewModel.Build(actor, fireball.spellId);

            Assert.AreNotEqual(HumanMageSpellLoadoutEditMode.Edit, vm.EditMode);
            Assert.IsFalse(vm.Detail.ShowPrepareButton);
            Assert.IsFalse(vm.Detail.ShowUnprepareButton);
            Assert.IsFalse(vm.Detail.ShowAddToHotbarButton);
            Assert.That(vm.Detail.CostLine, Does.Contain("Prepare cost: 7"));
        }

        [Test]
        public void Build_DefaultSelectionUsesFirstPreparedSpell()
        {
            MageSpellDefinition fireball = CreateSpell("mage_spell_fireball", "Fireball", tier: 3, magicPowerCost: 5);
            MageSpellDefinition lightning = CreateSpell("mage_spell_lightning_bolt", "Lightning Bolt", tier: 5, magicPowerCost: 4);
            BaseActor actor = CreateHumanMageActor(
                intelligence: 4,
                fireball,
                lightning,
                equippedIds: new[] { fireball.spellId, lightning.spellId });

            HumanMageSpellBodyViewModel vm = HumanMageSpellBodyViewModel.Build(actor);

            Assert.AreEqual(fireball.spellId, vm.SelectedSpellId);
            Assert.AreEqual("Fireball", vm.Detail.Title);
        }

        static HumanMageSpellRowModel FindRow(IReadOnlyList<HumanMageSpellRowModel> rows, string spellId)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (string.Equals(rows[i].SpellId, spellId, System.StringComparison.OrdinalIgnoreCase))
                    return rows[i];
            }

            Assert.Fail($"Missing row for {spellId}");
            return null;
        }

        BaseActor CreateHumanMageActor(
            int intelligence,
            MageSpellDefinition spellA,
            MageSpellDefinition spellB = null,
            string[] equippedIds = null)
        {
            var go = new GameObject("HumanMageMenuTest");
            _created.Add(go);

            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Human;
            stats.humanClass = HumanClass.Mage;
            stats.racialSubsystem = RacialSubsystemKind.HumanSpecialization;
            stats.Intelligence = new Stat(intelligence);
            stats.currentMagicPower = stats.MaxMagicPower;

            var runtime = go.AddComponent<HumanMageSpellsRuntime>();
            var spells = spellB != null
                ? new[] { spellA, spellB }
                : new[] { spellA };
            runtime.SetKnownAndEquipped(spells, equippedIds ?? System.Array.Empty<string>());

            return go.AddComponent<BaseActor>();
        }

        MageSpellDefinition CreateSpell(
            string id,
            string displayName,
            int tier = 3,
            int extraEquipCost = 0,
            int magicPowerCost = 1,
            AbilityAction ability = null)
        {
            var spell = ScriptableObject.CreateInstance<MageSpellDefinition>();
            spell.spellId = id;
            spell.displayName = displayName;
            spell.tier = tier;
            spell.extraEquipCost = extraEquipCost;
            spell.magicPowerCost = magicPowerCost;
            spell.ability = ability ?? CreateFireball();
            _assets.Add(spell);
            return spell;
        }

        FireballAbility CreateFireball()
        {
            var ability = ScriptableObject.CreateInstance<FireballAbility>();
            ability.requiresTarget = true;
            _assets.Add(ability);
            return ability;
        }
    }
}
