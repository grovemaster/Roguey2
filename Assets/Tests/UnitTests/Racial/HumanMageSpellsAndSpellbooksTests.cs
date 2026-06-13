using JRogue.Ability.Fireball;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Item.Essence;
using JRogue.Manager.Essence;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using JRogue.Quest;
using JRogue.Racial;
using JRogue.Shop;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Hotbar;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Racial
{
    [TestFixture]
    public sealed class HumanMageSpellsAndSpellbooksTests
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

            MageSpellCatalogService.ResetCacheForTests();
            typeof(PartyCurrencyLedger)
                .GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, null);
        }

        [Test]
        public void TryLearnSpell_AddsKnownSpell_IdempotentOnRepeat()
        {
            MageSpellDefinition fireball = CreateMageSpell("mage_spell_fireball", 3, 0, 5, CreateFireball());
            SeedCatalog(fireball);

            GameObject actor = CreateHumanMageActor();
            var runtime = actor.GetComponent<HumanMageSpellsRuntime>();

            Assert.IsTrue(runtime.TryLearnSpell(fireball.spellId, out string reason1), reason1);
            Assert.IsTrue(runtime.HasLearned(fireball.spellId));
            Assert.IsTrue(runtime.TryLearnSpell(fireball.spellId, out string reason2), reason2);
            Assert.AreEqual(1, runtime.KnownSpells.Count);
        }

        [Test]
        public void TryLearnSpell_RejectsNonMageActor()
        {
            MageSpellDefinition fireball = CreateMageSpell("mage_spell_fireball", 3, 0, 5, CreateFireball());
            SeedCatalog(fireball);

            GameObject actor = CreateHumanActor(HumanClass.None);
            actor.AddComponent<HumanMageSpellsRuntime>();

            Assert.IsFalse(actor.GetComponent<HumanMageSpellsRuntime>().TryLearnSpell(fireball.spellId, out _));
        }

        [Test]
        public void EquipBudget_AllThreeSampleSpells_LeavesFourRemaining()
        {
            MageSpellDefinition fireball = CreateMageSpell("mage_spell_fireball", 3, 0, 5, CreateFireball());
            MageSpellDefinition lightning = CreateMageSpell("mage_spell_lightning_bolt", 5, 1, 4, CreateFireball());
            MageSpellDefinition arcaneMight = CreateMageSpell("mage_spell_arcane_might", 7, 0, 2, CreateFireball());

            GameObject actor = CreateHumanMageActor(intelligence: 4);
            var runtime = actor.GetComponent<HumanMageSpellsRuntime>();
            runtime.SetKnownAndEquipped(
                new[] { fireball, lightning, arcaneMight },
                new[]
                {
                    fireball.spellId,
                    lightning.spellId,
                    arcaneMight.spellId,
                });

            Assert.AreEqual(20, actor.GetComponent<CharacterStats>().MaxMagicPower);
            Assert.AreEqual(4, runtime.RemainingEquipCapacity);
        }

        [Test]
        public void CanBeginMageTraining_RejectsEquippedEssence()
        {
            GameObject actor = CreateHumanActor(HumanClass.None);
            EssenceData essence = ScriptableObject.CreateInstance<EssenceData>();
            _assets.Add(essence);
            actor.GetComponent<EssenceSlotManager>().EquipEssence(essence, 0);

            Assert.IsFalse(HumanMageClassCommitService.CanBeginMageTraining(
                actor.GetComponent<BaseActor>(),
                out string reason));
            Assert.That(reason, Does.Contain("essences"));
        }

        [Test]
        public void CanBeginMageTraining_AllowsUnconsumedEssenceCategoryInInventory()
        {
            GameObject actor = CreateHumanActor(HumanClass.None);
            var essenceItem = ScriptableObject.CreateInstance<ItemData>();
            essenceItem.category = ItemCategory.Essence;
            _assets.Add(essenceItem);

            var inventory = actor.AddComponent<InventoryManager>();
            inventory.AddItem(new ItemInstance(essenceItem, 1));

            Assert.IsTrue(HumanMageClassCommitService.CanBeginMageTraining(
                actor.GetComponent<BaseActor>(),
                out _));
        }

        [Test]
        public void TryCompleteMageApprenticeship_AddsMageSpellsRuntimeWhenMissing()
        {
            GameObject actor = CreateHumanActor(HumanClass.None);
            Assert.IsNull(actor.GetComponent<HumanMageSpellsRuntime>());

            var ledgerGo = new GameObject("Ledger");
            _created.Add(ledgerGo);
            ledgerGo.AddComponent<PartyCurrencyLedger>();
            typeof(PartyCurrencyLedger)
                .GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, ledgerGo.GetComponent<PartyCurrencyLedger>());
            ShopGoldUtility.AddPartyGold(10);

            Assert.IsTrue(HumanMageClassCommitService.TryCompleteMageApprenticeship(
                actor.GetComponent<BaseActor>(),
                out string reason),
                reason);

            Assert.IsNotNull(actor.GetComponent<HumanMageSpellsRuntime>());
        }

        [Test]
        public void TryCompleteMageApprenticeship_CommitsMageAndDeductsGold()
        {
            GameObject actor = CreateHumanActor(HumanClass.None);
            actor.AddComponent<HumanMageSpellsRuntime>();

            var ledgerGo = new GameObject("Ledger");
            _created.Add(ledgerGo);
            var ledger = ledgerGo.AddComponent<PartyCurrencyLedger>();
            typeof(PartyCurrencyLedger)
                .GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, ledger);
            ShopGoldUtility.AddPartyGold(10);

            Assert.IsTrue(HumanMageClassCommitService.TryCompleteMageApprenticeship(
                actor.GetComponent<BaseActor>(),
                out string reason),
                reason);

            CharacterStats stats = actor.GetComponent<CharacterStats>();
            Assert.AreEqual(HumanClass.Mage, stats.humanClass);
            Assert.AreEqual(5, ShopGoldUtility.GetPartyGoldTotal());
            Assert.AreEqual(0, actor.GetComponent<EssenceSlotManager>().totalSlots);
            Assert.AreEqual(stats.MaxMagicPower, stats.currentMagicPower);
        }

        [Test]
        public void HotbarSync_AssignsEquippedSpellsToEmptyMainSlots()
        {
            MageSpellDefinition fireball = CreateMageSpell("mage_spell_fireball", 3, 0, 5, CreateFireball());
            MageSpellDefinition arcaneMight = CreateMageSpell("mage_spell_arcane_might", 7, 0, 2, CreateFireball());

            GameObject actor = CreateHumanMageActor(intelligence: 4);
            var runtime = actor.GetComponent<HumanMageSpellsRuntime>();
            runtime.SetKnownAndEquipped(
                new[] { fireball, arcaneMight },
                new[] { fireball.spellId, arcaneMight.spellId });

            BaseActor baseActor = actor.GetComponent<BaseActor>();
            HumanMageHotbarSync.TryAssignEquippedSpellsToEmptyMainSlots(baseActor);

            HotbarLayout layout = baseActor.GetComponent<HotbarLayout>();
            Assert.IsNotNull(layout);
            Assert.AreEqual(HotbarEntryKind.HumanMageSpell, layout.GetSlot(0).Kind);
            Assert.AreEqual(0, layout.GetSlot(0).abilityIndex);
            Assert.AreEqual(HotbarEntryKind.HumanMageSpell, layout.GetSlot(1).Kind);
            Assert.AreEqual(1, layout.GetSlot(1).abilityIndex);
        }

        [Test]
        public void SpellbookRead_LearnsSpellAndConsumesBook()
        {
            MageSpellDefinition fireball = CreateMageSpell("mage_spell_fireball", 3, 0, 5, CreateFireball());
            SeedCatalog(fireball);

            MageSpellbookDefinition bookDef = ScriptableObject.CreateInstance<MageSpellbookDefinition>();
            bookDef.spellIds = new System.Collections.Generic.List<string> { fireball.spellId };
            _assets.Add(bookDef);

            SpellbookItemData bookItem = ScriptableObject.CreateInstance<SpellbookItemData>();
            bookItem.spellbook = bookDef;
            _assets.Add(bookItem);

            GameObject actor = CreateHumanMageActor();
            var runtime = actor.GetComponent<HumanMageSpellsRuntime>();
            var inventory = actor.AddComponent<InventoryManager>();
            ItemInstance instance = new ItemInstance(bookItem, 1);
            inventory.AddItem(instance);

            var row = CreateSpellbookRow(actor.GetComponent<BaseActor>(), inventory, bookItem, instance);

            InventoryUseResult result = MageSpellbookReadService.TryRead(row);
            Assert.AreEqual(InventoryUseOutcome.ConsumedImmediately, result.Outcome);
            Assert.IsTrue(runtime.HasLearned(fireball.spellId));
            Assert.AreEqual(0, inventory.CarriedItems.Count);
        }

        [Test]
        public void SpellbookRead_FailsWhenAllKnown()
        {
            MageSpellDefinition fireball = CreateMageSpell("mage_spell_fireball", 3, 0, 5, CreateFireball());
            SeedCatalog(fireball);

            MageSpellbookDefinition bookDef = ScriptableObject.CreateInstance<MageSpellbookDefinition>();
            bookDef.spellIds = new System.Collections.Generic.List<string> { fireball.spellId };
            _assets.Add(bookDef);

            SpellbookItemData bookItem = ScriptableObject.CreateInstance<SpellbookItemData>();
            bookItem.spellbook = bookDef;
            _assets.Add(bookItem);

            GameObject actor = CreateHumanMageActor();
            actor.GetComponent<HumanMageSpellsRuntime>().TryLearnSpell(fireball.spellId, out _);
            var inventory = actor.AddComponent<InventoryManager>();
            ItemInstance instance = new ItemInstance(bookItem, 1);
            inventory.AddItem(instance);

            var row = CreateSpellbookRow(actor.GetComponent<BaseActor>(), inventory, bookItem, instance);

            InventoryUseResult result = MageSpellbookReadService.TryRead(row);
            Assert.AreEqual(InventoryUseOutcome.Failed, result.Outcome);
            Assert.That(result.FailureReason, Does.Contain("already know"));
            Assert.AreEqual(1, inventory.CarriedItems.Count);
        }

        [Test]
        public void SpellbookRead_FailsForNonMage()
        {
            MageSpellDefinition fireball = CreateMageSpell("mage_spell_fireball", 3, 0, 5, CreateFireball());
            SeedCatalog(fireball);

            MageSpellbookDefinition bookDef = ScriptableObject.CreateInstance<MageSpellbookDefinition>();
            bookDef.spellIds = new System.Collections.Generic.List<string> { fireball.spellId };
            _assets.Add(bookDef);

            SpellbookItemData bookItem = ScriptableObject.CreateInstance<SpellbookItemData>();
            bookItem.spellbook = bookDef;
            _assets.Add(bookItem);

            GameObject actor = CreateHumanActor(HumanClass.Knight);
            actor.AddComponent<HumanMageSpellsRuntime>();
            var inventory = actor.AddComponent<InventoryManager>();
            ItemInstance instance = new ItemInstance(bookItem, 1);
            inventory.AddItem(instance);

            var row = CreateSpellbookRow(actor.GetComponent<BaseActor>(), inventory, bookItem, instance);

            InventoryUseResult result = MageSpellbookReadService.TryRead(row);
            Assert.AreEqual(InventoryUseOutcome.Failed, result.Outcome);
            Assert.AreEqual(1, inventory.CarriedItems.Count);
        }

        JRogue.UI.Inventory.InventoryViewModel.Row CreateSpellbookRow(
            BaseActor owner,
            InventoryManager inventory,
            SpellbookItemData bookItem,
            ItemInstance instance)
        {
            int index = 0;
            for (int i = 0; i < inventory.CarriedItems.Count; i++)
            {
                if (inventory.CarriedItems[i] == instance)
                {
                    index = i;
                    break;
                }
            }

            return new JRogue.UI.Inventory.InventoryViewModel.Row(
                'b',
                instance,
                owner,
                owner.DisplayName,
                isEquipped: false,
                equippedSlot: null,
                carriedListIndex: index,
                stackedWeight: bookItem.weight);
        }

        void SeedCatalog(MageSpellDefinition spell)
        {
            MageSpellCatalog catalog = ScriptableObject.CreateInstance<MageSpellCatalog>();
            catalog.spells = new System.Collections.Generic.List<MageSpellDefinition> { spell };
            _assets.Add(catalog);
            typeof(MageSpellCatalogService)
                .GetField("_cached", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, catalog);
            typeof(MageSpellCatalogService)
                .GetField("_spellLookup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, null);
        }

        GameObject CreateHumanActor(HumanClass humanClass)
        {
            var go = new GameObject("HumanTest");
            _created.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Human;
            stats.humanClass = humanClass;
            stats.Intelligence = new Stat(4);
            stats.Strength = new Stat(10);
            go.AddComponent<EssenceSlotManager>();
            go.AddComponent<JRogue.Actors.BaseActor>();
            return go;
        }

        GameObject CreateHumanMageActor(int intelligence = 4)
        {
            GameObject actor = CreateHumanActor(HumanClass.Mage);
            actor.GetComponent<CharacterStats>().Intelligence = new Stat(intelligence);
            actor.AddComponent<HumanMageSpellsRuntime>();
            return actor;
        }

        MageSpellDefinition CreateMageSpell(
            string id,
            int tier,
            int extraEquipCost,
            int magicPowerCost,
            JRogue.Ability.AbilityAction ability)
        {
            var spell = ScriptableObject.CreateInstance<MageSpellDefinition>();
            spell.spellId = id;
            spell.tier = tier;
            spell.extraEquipCost = extraEquipCost;
            spell.magicPowerCost = magicPowerCost;
            spell.ability = ability;
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
