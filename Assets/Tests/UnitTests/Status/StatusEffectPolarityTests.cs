using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Manager.Party;
using JRogue.Stats;
using JRogue.Status;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Status
{
    [TestFixture]
    public sealed class StatusEffectPolarityTests
    {
        GameObject _actorGo;

        [TearDown]
        public void TearDown()
        {
            if (_actorGo != null)
                Object.DestroyImmediate(_actorGo);

            if (PartyManager.Instance != null)
                Object.DestroyImmediate(PartyManager.Instance.gameObject);
        }

        [Test]
        public void DefaultPolarity_Poisoned_Drained_Slowed_AreNegative()
        {
            Assert.AreEqual(StatusPolarity.Negative, StatusEffectPolarityRules.GetDefaultPolarity(StatusEffectId.Poisoned));
            Assert.AreEqual(StatusPolarity.Negative, StatusEffectPolarityRules.GetDefaultPolarity(StatusEffectId.Drained));
            Assert.AreEqual(StatusPolarity.Negative, StatusEffectPolarityRules.GetDefaultPolarity(StatusEffectId.Slowed));
        }

        [Test]
        public void DefaultPolarity_Might_Hasted_ArePositive()
        {
            Assert.AreEqual(StatusPolarity.Positive, StatusEffectPolarityRules.GetDefaultPolarity(StatusEffectId.Might));
            Assert.AreEqual(StatusPolarity.Positive, StatusEffectPolarityRules.GetDefaultPolarity(StatusEffectId.Hasted));
            Assert.AreEqual(StatusPolarity.Negative, StatusEffectPolarityRules.GetDefaultPolarity(StatusEffectId.Stunned));
        }

        [Test]
        public void HasNegativeStatus_UsesPolarity_NotStatusId()
        {
            StatusEffectController controller = CreateActorWithStatusController();
            var slowed = ScriptableObject.CreateInstance<StatusEffectDefinition>();
            slowed.statusId = StatusEffectId.Slowed;
            slowed.polarity = StatusPolarity.Negative;
            slowed.displayName = "Slowed";
            slowed.maxDurationTurns = 3;

            StatusEffectService.TryApply(controller, slowed);

            Assert.IsTrue(controller.HasNegativeStatus());
            Assert.IsFalse(controller.HasStatus(StatusEffectId.Poisoned));
        }

        [Test]
        public void HasNegativeStatus_FalseForPositiveOnly()
        {
            StatusEffectController controller = CreateActorWithStatusController();
            var hasted = ScriptableObject.CreateInstance<StatusEffectDefinition>();
            hasted.statusId = StatusEffectId.Hasted;
            hasted.polarity = StatusPolarity.Positive;
            hasted.displayName = "Hasted";
            hasted.maxDurationTurns = 3;

            StatusEffectService.TryApply(controller, hasted);

            Assert.IsFalse(controller.HasNegativeStatus());
            Assert.IsTrue(controller.HasPositiveStatus());
        }

        StatusEffectController CreateActorWithStatusController()
        {
            _actorGo = new GameObject("StatusActor");
            _actorGo.AddComponent<CharacterStats>();
            _actorGo.AddComponent<HealthComponent>();
            return _actorGo.AddComponent<StatusEffectController>();
        }
    }
}
