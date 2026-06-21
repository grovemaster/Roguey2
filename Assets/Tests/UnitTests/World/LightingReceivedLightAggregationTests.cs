using JRogue.World.Lighting;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class LightingReceivedLightAggregationTests
    {
        LightEmitterDefinition _torch;
        GameObject _serviceGo;

        [SetUp]
        public void SetUp()
        {
            _torch = Resources.Load<LightEmitterDefinition>("Lighting/Torch");
            Assert.IsNotNull(_torch, "Missing Resources/Lighting/Torch.asset");

            _serviceGo = new GameObject("LightingServiceTest");
            var service = _serviceGo.AddComponent<LightingService>();
            service.SetAmbientLight(service.DefaultFloorAmbientRegionId, LightLevel.PitchDark, "test-setup");
        }

        [TearDown]
        public void TearDown()
        {
            if (_serviceGo != null)
                Object.DestroyImmediate(_serviceGo);
        }

        [Test]
        public void FinalizeRegistry_AppliesPendingAfterEarlyFinalize()
        {
            LightingService service = LightingService.Instance;
            service.ResetForActiveFloor();
            service.FinalizeRegistry();

            service.RegisterPending(
                new Vector3Int(0, 0, 0),
                LightCellData.Emitter(_torch, LightLevel.TorchEmission),
                overwrite: true);

            service.FinalizeRegistry();

            Assert.AreEqual(LightLevel.TorchEmission, service.GetEmitLight(new Vector3Int(0, 0, 0)));
        }

        [Test]
        public void OverlappingEmitters_SumContributions_CappedAtMax()
        {
            LightingService service = LightingService.Instance;
            Vector3Int receiver = new Vector3Int(0, 0, 0);
            Vector3Int neighbor = new Vector3Int(1, 0, 0);

            service.RegisterPending(receiver, LightCellData.Emitter(_torch, LightLevel.TorchEmission), overwrite: true);
            service.RegisterPending(neighbor, LightCellData.Emitter(_torch, LightLevel.TorchEmission), overwrite: true);
            service.FinalizeRegistry();

            int received = service.GetReceivedLight(receiver);
            Assert.AreEqual(LightLevel.Max, received);
        }

        [Test]
        public void SingleEmitter_ReceivedLight_MatchesFalloff()
        {
            LightingService service = LightingService.Instance;
            Vector3Int emitter = new Vector3Int(0, 0, 0);
            Vector3Int receiver = new Vector3Int(2, 0, 0);

            service.RegisterPending(emitter, LightCellData.Emitter(_torch, LightLevel.TorchEmission), overwrite: true);
            service.RegisterPending(receiver, LightCellData.Receiver(0, LightLevel.PitchDark), overwrite: true);
            service.FinalizeRegistry();

            int received = service.GetReceivedLight(receiver);
            Assert.AreEqual(4, received);
        }

        [Test]
        public void TwoEmitters_OnReceiver_SumsHigherThanSingle()
        {
            LightingService service = LightingService.Instance;
            Vector3Int left = new Vector3Int(0, 0, 0);
            Vector3Int right = new Vector3Int(4, 0, 0);
            Vector3Int receiver = new Vector3Int(2, 0, 0);

            service.RegisterPending(left, LightCellData.Emitter(_torch, LightLevel.TorchEmission), overwrite: true);
            service.RegisterPending(right, LightCellData.Emitter(_torch, LightLevel.TorchEmission), overwrite: true);
            service.RegisterPending(receiver, LightCellData.Receiver(0, LightLevel.PitchDark), overwrite: true);
            service.FinalizeRegistry();

            int received = service.GetReceivedLight(receiver);
            Assert.AreEqual(8, received);
        }
        [Test]
        public void CrossZoneTileEmitters_DoNotContributeToOtherZoneReceivers()
        {
            LightingService service = LightingService.Instance;
            Vector3Int luminescentEmitter = new Vector3Int(0, 0, 0);
            Vector3Int darkReceiver = new Vector3Int(1, 0, 0);

            service.RegisterPending(
                luminescentEmitter,
                LightCellData.Emitter(_torch, LightLevel.TorchEmission, zoneId: "luminescent_cavern"),
                overwrite: true);
            service.RegisterPending(
                darkReceiver,
                LightCellData.Receiver(0, LightLevel.PitchDark, "northern_dark"),
                overwrite: true);
            service.FinalizeRegistry();

            Assert.AreEqual(LightLevel.PitchDark, service.GetReceivedLight(darkReceiver));
        }

        [Test]
        public void ZonedTileEmitters_DoNotContributeToUntaggedReceivers()
        {
            LightingService service = LightingService.Instance;
            Vector3Int luminescentEmitter = new Vector3Int(0, 0, 0);
            Vector3Int untaggedReceiver = new Vector3Int(1, 0, 0);

            service.RegisterPending(
                luminescentEmitter,
                LightCellData.Emitter(_torch, LightLevel.TorchEmission, zoneId: "luminescent_cavern"),
                overwrite: true);
            service.RegisterPending(
                untaggedReceiver,
                LightCellData.Receiver(0, LightLevel.PitchDark),
                overwrite: true);
            service.FinalizeRegistry();

            Assert.AreEqual(LightLevel.PitchDark, service.GetReceivedLight(untaggedReceiver));
        }

        [Test]
        public void SameZoneTileEmitters_StillContribute()
        {
            LightingService service = LightingService.Instance;
            Vector3Int emitter = new Vector3Int(0, 0, 0);
            Vector3Int receiver = new Vector3Int(1, 0, 0);

            service.RegisterPending(
                emitter,
                LightCellData.Emitter(_torch, LightLevel.TorchEmission, zoneId: "northern_dark"),
                overwrite: true);
            service.RegisterPending(
                receiver,
                LightCellData.Receiver(0, LightLevel.PitchDark, "northern_dark"),
                overwrite: true);
            service.FinalizeRegistry();

            Assert.Greater(service.GetReceivedLight(receiver), LightLevel.PitchDark);
        }

        [Test]
        public void UnregisteredCell_GetReceivedLight_ReturnsPitchDarkNotDefaultAmbient()
        {
            LightingService service = LightingService.Instance;
            service.SetAmbientLight(service.DefaultFloorAmbientRegionId, LightLevel.FullDaylightAmbient, "test");
            service.FinalizeRegistry();

            Assert.AreEqual(
                LightLevel.PitchDark,
                service.GetReceivedLight(new Vector3Int(99, 99, 0)));
        }
    }
}
