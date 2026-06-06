using JRogue.Ability;
using JRogue.Ability.HelmetOfLight;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.World.Lighting;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class LightSourceItemRulesTests
    {
        [Test]
        public void PassiveTorch_EmitsWhenEquippedInAccessorySlot()
        {
            var torch = ScriptableObject.CreateInstance<LightSourceItemData>();
            torch.emitsWhenEquipped = true;
            torch.startsLit = true;

            var instance = new ItemInstance(torch, 1);

            Assert.IsTrue(LightSourceItemRules.ShouldEmitCarriedLight(
                instance, EquipmentSlot.Accessory_MainHand, isEquipped: true));
            Assert.IsFalse(LightSourceItemRules.ShouldEmitCarriedLight(
                instance, EquipmentSlot.Accessory_MainHand, isEquipped: false));
            Object.DestroyImmediate(torch);
        }

        [Test]
        public void HelmetRadiance_LightTicksThenCooldown()
        {
            var radiance = ScriptableObject.CreateInstance<HelmetOfLightRadianceAbility>();
            radiance.cooldownTurns = 3;
            radiance.lightDurationTurns = 5;

            var helmet = ScriptableObject.CreateInstance<LightSourceItemData>();
            helmet.activeAbilities = new System.Collections.Generic.List<AbilityAction> { radiance };

            var instance = new ItemInstance(helmet, 1);
            LightSourceItemRules.BeginHelmetRadiance(instance, radiance, 5);

            Assert.IsTrue(LightSourceItemRules.ShouldEmitCarriedLight(
                instance, EquipmentSlot.Head, isEquipped: true));
            Assert.IsFalse(LightSourceItemRules.ShouldEmitCarriedLight(
                instance, EquipmentSlot.Head, isEquipped: false));

            for (int i = 0; i < 5; i++)
                LightSourceItemRules.TickInstanceForTests(instance);

            Assert.AreEqual(0, instance.HelmetLightTurnsRemaining);
            Assert.AreEqual(3, instance.HelmetCooldownTurnsRemaining);
            Assert.IsFalse(LightSourceItemRules.CanActivateTimedLight(instance, radiance));

            for (int i = 0; i < 3; i++)
                LightSourceItemRules.TickInstanceForTests(instance);

            Assert.AreEqual(0, instance.HelmetCooldownTurnsRemaining);
            Assert.IsTrue(LightSourceItemRules.CanActivateTimedLight(instance, radiance));

            Object.DestroyImmediate(radiance);
            Object.DestroyImmediate(helmet);
        }

        [Test]
        public void AbilityCooldownService_TracksGenericCooldown()
        {
            AbilityCooldownService.ResetForTests();
            var ability = ScriptableObject.CreateInstance<HelmetOfLightRadianceAbility>();
            ability.cooldownTurns = 2;
            var instance = new ItemInstance(ScriptableObject.CreateInstance<LightSourceItemData>(), 1);

            AbilityCooldownService.StartCooldown(instance, ability);
            Assert.IsTrue(AbilityCooldownService.IsOnCooldown(instance, ability));

            AbilityCooldownService.TickInstanceCooldowns(instance);
            Assert.IsTrue(AbilityCooldownService.IsOnCooldown(instance, ability));

            AbilityCooldownService.TickInstanceCooldowns(instance);
            Assert.IsFalse(AbilityCooldownService.IsOnCooldown(instance, ability));

            Object.DestroyImmediate(ability);
        }
    }

    [TestFixture]
    public sealed class CarriedEmitterLightingTests
    {
        LightEmitterDefinition _torch;
        GameObject _serviceGo;

        [SetUp]
        public void SetUp()
        {
            _torch = Resources.Load<LightEmitterDefinition>("Lighting/Torch");
            _serviceGo = new GameObject("LightingServiceCarriedTest");
            var service = _serviceGo.AddComponent<LightingService>();
            service.SetAmbientLight(service.DefaultFloorAmbientRegionId, LightLevel.PitchDark, "test");
            service.RegisterPending(new Vector3Int(0, 0, 0), LightCellData.Receiver(0, LightLevel.PitchDark), overwrite: true);
            service.FinalizeRegistry();
        }

        [TearDown]
        public void TearDown()
        {
            if (_serviceGo != null)
                Object.DestroyImmediate(_serviceGo);
        }

        [Test]
        public void CarriedEmitter_ContributesToReceivedLight()
        {
            LightingService service = LightingService.Instance;
            var desired = new System.Collections.Generic.Dictionary<string, LightingService.CarriedEmitterEntry>
            {
                ["carried:test"] = new LightingService.CarriedEmitterEntry(
                    new Vector3Int(0, 0, 0),
                    _torch,
                    LightLevel.TorchEmission)
            };

            service.SyncCarriedEmitters(desired);

            Assert.AreEqual(LightLevel.TorchEmission, service.GetReceivedLight(new Vector3Int(0, 0, 0)));
        }

        [Test]
        public void CarriedEmitter_RemovedWhenUnequipped()
        {
            LightingService service = LightingService.Instance;
            service.SyncCarriedEmitters(new System.Collections.Generic.Dictionary<string, LightingService.CarriedEmitterEntry>
            {
                ["carried:test"] = new LightingService.CarriedEmitterEntry(
                    new Vector3Int(0, 0, 0),
                    _torch,
                    LightLevel.TorchEmission)
            });

            service.SyncCarriedEmitters(new System.Collections.Generic.Dictionary<string, LightingService.CarriedEmitterEntry>());
            Assert.AreEqual(LightLevel.PitchDark, service.GetReceivedLight(new Vector3Int(0, 0, 0)));
        }
    }
}
