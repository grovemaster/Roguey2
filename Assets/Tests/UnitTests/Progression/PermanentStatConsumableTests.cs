using System.Collections.Generic;
using JRogue.Ability.Progression;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Character;
using JRogue.UI.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Progression
{
    public sealed class PermanentStatConsumableTests
    {
        readonly List<Object> _destroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _destroy.Count; i++)
            {
                if (_destroy[i] != null)
                    Object.DestroyImmediate(_destroy[i]);
            }

            _destroy.Clear();
        }

        [Test]
        public void ApplyAttribute_OnlyAffectsConsumer_AndStacks()
        {
            GameObject a = CreateActor("A");
            GameObject b = CreateActor("B");
            CharacterStats statsA = a.GetComponent<CharacterStats>();
            CharacterStats statsB = b.GetComponent<CharacterStats>();
            int baseA = statsA.Strength.GetValue();
            int baseB = statsB.Strength.GetValue();

            PermanentStatBoostAbility ability = CreateStrengthAbility(1);
            Assert.IsTrue(ability.Execute(a));
            Assert.AreEqual(baseA + 1, statsA.Strength.GetValue());
            Assert.AreEqual(baseB, statsB.Strength.GetValue());

            Assert.IsTrue(ability.Execute(a));
            Assert.AreEqual(baseA + 2, statsA.Strength.GetValue());
            Assert.AreEqual(2, PermanentStatBoostRuntime.Ensure(a).GetAttributeTotal(StatType.Strength));
        }

        [Test]
        public void ApplyResistance_Poison_Stacks()
        {
            GameObject actor = CreateActor("PoisonTest");
            CharacterStats stats = actor.GetComponent<CharacterStats>();
            int before = stats.GetResistance(DamageType.Poison);

            PermanentStatBoostAbility ability = CreatePoisonAbility(1);
            Assert.IsTrue(ability.Execute(actor));
            Assert.AreEqual(before + 1, stats.GetResistance(DamageType.Poison));
            Assert.IsTrue(ability.Execute(actor));
            Assert.AreEqual(before + 2, stats.GetResistance(DamageType.Poison));
        }

        [Test]
        public void PermanentBoost_SurvivesEquipmentUnequipSimulation()
        {
            GameObject actor = CreateActor("GearTest");
            CharacterStats stats = actor.GetComponent<CharacterStats>();
            int baseStr = stats.Strength.GetValue();

            Assert.IsTrue(CreateStrengthAbility(1).Execute(actor));
            object gearSource = new object();
            stats.Strength.AddModifier(5, gearSource, ModifierSourceLayer.Equipment);
            Assert.AreEqual(baseStr + 1 + 5, stats.Strength.GetValue());

            stats.Strength.RemoveModifiersFromSource(gearSource);
            Assert.AreEqual(baseStr + 1, stats.Strength.GetValue());
        }

        [Test]
        public void Undead_CannotConsumePill_ViaConsumePolicy()
        {
            GameObject owner = CreateActor("Undead");
            owner.GetComponent<CharacterStats>().race = Race.Undead;
            ItemData pill = ScriptableObject.CreateInstance<ItemData>();
            _destroy.Add(pill);
            pill.category = ItemCategory.Potion;
            pill.itemName = "Pill of Strength";
            var instance = new ItemInstance(pill);
            BaseActor actor = owner.GetComponent<BaseActor>();

            var row = new InventoryViewModel.Row(
                'a',
                instance,
                actor,
                actor.DisplayName,
                isEquipped: false,
                equippedSlot: null,
                carriedListIndex: 0,
                stackedWeight: 1f);

            Assert.IsFalse(InventoryConsumePolicy.CanConsume(row, out string reason));
            Assert.AreEqual(InventoryConsumePolicy.UndeadPotionBanMessage, reason);
        }

        [Test]
        public void CharacterSheetModel_IncludesPermanentLines()
        {
            GameObject actorGo = CreateActor("Sheet");
            Assert.IsTrue(CreateStrengthAbility(1).Execute(actorGo));
            Assert.IsTrue(CreatePoisonAbility(1).Execute(actorGo));

            CharacterEquipmentSheetModel model =
                CharacterEquipmentViewModel.Build(actorGo.GetComponent<BaseActor>());
            Assert.AreEqual(2, model.PermanentLines.Count);
            Assert.That(model.PermanentLines[0], Does.Contain("Strength"));
            Assert.That(model.PermanentLines[1], Does.Contain("Poison"));
        }

        [Test]
        public void InventoryInspect_ShowsPermanentWording()
        {
            PermanentStatBoostAbility ability = CreateStrengthAbility(1);
            ItemData pill = ScriptableObject.CreateInstance<ItemData>();
            _destroy.Add(pill);
            pill.itemName = "Pill of Strength";
            pill.category = ItemCategory.Potion;
            pill.activeAbilities = new List<JRogue.Ability.AbilityAction> { ability };

            string body = InventoryDetailFormatter.FormatInspectBody(pill);
            Assert.That(body, Does.Contain("Permanent"));
            Assert.That(body, Does.Contain("Strength"));
        }

        GameObject CreateActor(string name)
        {
            var go = new GameObject(name);
            _destroy.Add(go);
            go.AddComponent<CharacterStats>();
            BaseActor actor = go.AddComponent<BaseActor>();
            return go;
        }

        PermanentStatBoostAbility CreateStrengthAbility(int amount)
        {
            PermanentStatBoostAbility ability = ScriptableObject.CreateInstance<PermanentStatBoostAbility>();
            _destroy.Add(ability);
            ability.abilityName = "Pill of Strength";
            ability.boostKind = PermanentStatBoostKind.Attribute;
            ability.attribute = StatType.Strength;
            ability.amount = amount;
            ability.requiresTarget = false;
            return ability;
        }

        PermanentStatBoostAbility CreatePoisonAbility(int amount)
        {
            PermanentStatBoostAbility ability = ScriptableObject.CreateInstance<PermanentStatBoostAbility>();
            _destroy.Add(ability);
            ability.abilityName = "Pill of Poison Resistance";
            ability.boostKind = PermanentStatBoostKind.Resistance;
            ability.resistance = DamageType.Poison;
            ability.amount = amount;
            ability.requiresTarget = false;
            return ability;
        }
    }
}
