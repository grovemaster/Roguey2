using JRogue.Ability;
using JRogue.Ability.Fireball;
using JRogue.Ability.SuddenStrength;
using JRogue.Actors;
using JRogue.Manager.Essence;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Hotbar;
using JRogue.World.Generation;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Racial
{
    [TestFixture]
    public sealed class DragonianSpellsRuntimeTests
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
        public void TryLearnSpell_AddsKnownSpell_IdempotentOnRepeat()
        {
            DragonianSpellDefinition surge = CreateSpell("dragonian_spell_sudden_strength", 3, 1, CreateSuddenStrength());
            DragonianSpellCatalog catalog = ScriptableObject.CreateInstance<DragonianSpellCatalog>();
            catalog.spells = new System.Collections.Generic.List<DragonianSpellDefinition> { surge };
            _assets.Add(catalog);
            typeof(DragonianSpellCatalogService)
                .GetField("_cached", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, catalog);
            typeof(DragonianSpellCatalogService)
                .GetField("_spellLookup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, null);

            GameObject actor = CreateDragonianActor(maxSoulPowerBudget: 100);
            var runtime = actor.GetComponent<DragonianSpellsRuntime>();

            Assert.IsTrue(runtime.TryLearnSpell(surge.spellId, out string reason1), reason1);
            Assert.IsTrue(runtime.HasLearned(surge.spellId));
            Assert.IsTrue(runtime.TryLearnSpell(surge.spellId, out string reason2), reason2);
            Assert.AreEqual(1, runtime.KnownSpells.Count);

            DragonianSpellCatalogService.ResetCacheForTests();
        }

        [Test]
        public void MemorizeBudget_BothSampleSpells_FitsWithinMaxSoulPower()
        {
            DragonianSpellDefinition surge = CreateSpell("dragonian_spell_sudden_strength", 3, 1, CreateSuddenStrength());
            DragonianSpellDefinition flame = CreateSpell("dragonian_spell_fireball", 7, 5, CreateFireball());

            GameObject actor = CreateDragonianActor(maxSoulPowerBudget: 100);
            var runtime = actor.GetComponent<DragonianSpellsRuntime>();
            runtime.SetKnownAndMemorized(new[] { surge, flame }, System.Array.Empty<string>());

            Assert.IsTrue(runtime.TryMemorize("dragonian_spell_sudden_strength", out string reason1), reason1);
            Assert.IsTrue(runtime.TryMemorize("dragonian_spell_fireball", out string reason2), reason2);
            Assert.AreEqual(90, runtime.RemainingMemoryCapacity);
        }

        [Test]
        public void MemorizeBudget_RejectsSpellWhenCapacityInsufficient()
        {
            DragonianSpellDefinition surge = CreateSpell("dragonian_spell_sudden_strength", 3, 1, CreateSuddenStrength());
            DragonianSpellDefinition flame = CreateSpell("dragonian_spell_fireball", 7, 5, CreateFireball());
            DragonianSpellDefinition huge = CreateSpell("dragonian_spell_huge", 91, 1, CreateSuddenStrength());

            GameObject actor = CreateDragonianActor(maxSoulPowerBudget: 100);
            var runtime = actor.GetComponent<DragonianSpellsRuntime>();
            runtime.SetKnownAndMemorized(new[] { surge, flame, huge }, new[] { surge.spellId, flame.spellId });

            Assert.IsFalse(runtime.TryMemorize("dragonian_spell_huge", out string reason));
            Assert.That(reason, Does.Contain("capacity"));
        }

        [Test]
        public void Unmemorize_FreesCapacity_ForReMemorize()
        {
            DragonianSpellDefinition surge = CreateSpell("dragonian_spell_sudden_strength", 3, 1, CreateSuddenStrength());
            DragonianSpellDefinition flame = CreateSpell("dragonian_spell_fireball", 7, 5, CreateFireball());

            GameObject actor = CreateDragonianActor(maxSoulPowerBudget: 100);
            var runtime = actor.GetComponent<DragonianSpellsRuntime>();
            runtime.SetKnownAndMemorized(new[] { surge, flame }, new[] { surge.spellId, flame.spellId });

            Assert.IsTrue(runtime.TryUnmemorize(flame.spellId));
            Assert.AreEqual(93, runtime.RemainingMemoryCapacity);
            Assert.IsTrue(runtime.TryMemorize(flame.spellId, out string reason), reason);
        }

        [Test]
        public void Cast_DraconicSurge_DeductsSoulPowerCastCost()
        {
            DragonianSpellDefinition surge = CreateSpell("dragonian_spell_sudden_strength", 3, 1, CreateSuddenStrength());

            GameObject actor = CreateDragonianActor(maxSoulPowerBudget: 100);
            var runtime = actor.GetComponent<DragonianSpellsRuntime>();
            var stats = actor.GetComponent<CharacterStats>();
            runtime.SetKnownAndMemorized(new[] { surge }, new[] { surge.spellId });
            stats.currentSoulPower = 1;

            Assert.IsTrue(runtime.TryExecuteMemorized(0, actor));
            Assert.AreEqual(0, stats.currentSoulPower);
            Assert.IsFalse(runtime.TryExecuteMemorized(0, actor));
        }

        [Test]
        public void Cast_DragonFlame_DeductsFiveSoulPowerOnce()
        {
            DragonianSpellDefinition flame = CreateSpell("dragonian_spell_fireball", 7, 5, CreateSuddenStrength());

            GameObject actor = CreateDragonianActor(maxSoulPowerBudget: 100);
            var runtime = actor.GetComponent<DragonianSpellsRuntime>();
            var stats = actor.GetComponent<CharacterStats>();
            runtime.SetKnownAndMemorized(new[] { flame }, new[] { flame.spellId });
            stats.currentSoulPower = 5;

            Assert.IsTrue(runtime.TryExecuteMemorized(0, actor));
            Assert.AreEqual(0, stats.currentSoulPower);
        }

        [Test]
        public void NonMemorizedKnownSpell_IsNotInHotbarPool()
        {
            DragonianSpellDefinition surge = CreateSpell("dragonian_spell_sudden_strength", 3, 1, CreateSuddenStrength());
            DragonianSpellDefinition flame = CreateSpell("dragonian_spell_fireball", 7, 5, CreateFireball());

            GameObject actor = CreateDragonianActor(maxSoulPowerBudget: 100);
            actor.GetComponent<DragonianSpellsRuntime>().SetKnownAndMemorized(
                new[] { surge, flame },
                new[] { surge.spellId });

            var pool = HotbarAssignabilityService.BuildPool(actor.GetComponent<BaseActor>());
            Assert.AreEqual(1, pool.FindAll(entry => entry.entry.Kind == HotbarEntryKind.DragonianSpell).Count);
        }

        [Test]
        public void LoadoutService_BlocksMemorizeOutsideSafeZone()
        {
            DragonianSpellDefinition surge = CreateSpell("dragonian_spell_sudden_strength", 3, 1, CreateSuddenStrength());
            GameObject actor = CreateDragonianActor(maxSoulPowerBudget: 100);
            actor.GetComponent<DragonianSpellsRuntime>().SetKnownAndMemorized(new[] { surge }, System.Array.Empty<string>());

            Assert.IsFalse(DragonianSpellLoadoutService.TryMemorize(
                actor.GetComponent<BaseActor>(),
                surge.spellId,
                out string reason));
            Assert.AreEqual(SafeZonePolicyService.MemorizeLoadoutDenyMessage, reason);
        }

        [Test]
        public void HotbarResolver_RejectsDragonianSpellForHumanMage()
        {
            DragonianSpellDefinition surge = CreateSpell("dragonian_spell_sudden_strength", 3, 1, CreateSuddenStrength());
            GameObject actor = CreateHumanMageActor();
            actor.AddComponent<DragonianSpellsRuntime>().SetKnownAndMemorized(new[] { surge }, new[] { surge.spellId });

            var resolved = HotbarResolver.Resolve(
                actor.GetComponent<BaseActor>(),
                new HotbarEntry { Kind = HotbarEntryKind.DragonianSpell, abilityIndex = 0 });

            Assert.IsFalse(resolved.IsValid);
            Assert.IsTrue(resolved.IsStale);
        }

        GameObject CreateDragonianActor(int maxSoulPowerBudget)
        {
            var go = new GameObject("DragonianTest");
            _created.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Dragonian;
            stats.humanClass = HumanClass.None;
            stats.racialSubsystem = RacialSubsystemKind.DragonianSpells;
            stats.Intelligence = new Stat(maxSoulPowerBudget / 10);
            stats.Wisdom = new Stat(maxSoulPowerBudget / 10);
            stats.currentSoulPower = maxSoulPowerBudget;
            go.AddComponent<EssenceSlotManager>();
            go.AddComponent<BaseActor>();
            go.AddComponent<DragonianSpellsRuntime>();
            return go;
        }

        GameObject CreateHumanMageActor()
        {
            var go = new GameObject("HumanMageTest");
            _created.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Human;
            stats.humanClass = HumanClass.Mage;
            stats.racialSubsystem = RacialSubsystemKind.HumanSpecialization;
            go.AddComponent<EssenceSlotManager>();
            go.AddComponent<BaseActor>();
            return go;
        }

        DragonianSpellDefinition CreateSpell(string id, int memorizeCost, int castCost, AbilityAction ability)
        {
            var spell = ScriptableObject.CreateInstance<DragonianSpellDefinition>();
            spell.spellId = id;
            spell.displayName = id;
            spell.memorizeCost = memorizeCost;
            spell.soulPowerCastCost = castCost;
            spell.ability = ability;
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

        FireballAbility CreateFireball()
        {
            var ability = ScriptableObject.CreateInstance<FireballAbility>();
            ability.requiresTarget = true;
            ability.soulPowerCost = 0;
            _assets.Add(ability);
            return ability;
        }
    }
}
