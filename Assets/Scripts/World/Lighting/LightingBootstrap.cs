using System;
using UnityEngine;

namespace JRogue.World.Lighting
{
    /// <summary>Applies a placement set (and optional inline entries) when the floor loads.</summary>
    [DefaultExecutionOrder(0)]
    public sealed class LightingBootstrap : MonoBehaviour
    {
        [SerializeField] LightingPlacementSet placementSet;
        [SerializeField] LightingPlacementEntry[] placements = Array.Empty<LightingPlacementEntry>();

        [SerializeField] AmbientRegionSetup[] ambientRegions = Array.Empty<AmbientRegionSetup>();

        LightingService _service;

        void Awake()
        {
            _service = ResolveService();
            if (_service == null)
                return;

            RegisterAmbientRegions();
            RegisterPlacements();
        }

        void RegisterAmbientRegions()
        {
            if (_service == null)
                return;

            for (int i = 0; i < ambientRegions.Length; i++)
            {
                AmbientRegionSetup setup = ambientRegions[i];
                AmbientRegion region = _service.GetOrCreateAmbientRegion(setup.regionId);
                region.CurrentAmbientLight = LightLevel.Clamp(setup.currentAmbientLight);
                region.CycleLengthTurns = setup.cycleLengthTurns;
                region.Phases = setup.phases ?? Array.Empty<AmbientPhaseScheduleEntry>();
                region.PhaseIndex = 0;
                region.TurnsUntilNextPhase = setup.phases != null && setup.phases.Length > 0
                    ? setup.phases[0].durationTurns
                    : 0;
            }
        }

        void RegisterPlacements()
        {
            if (_service == null)
                return;

            LightingPlacementEntry[] source = placementSet != null && placementSet.placements != null
                && placementSet.placements.Length > 0
                ? placementSet.placements
                : placements;

            for (int i = 0; i < source.Length; i++)
                _service.RegisterPlacement(source[i]);
        }

        LightingService ResolveService()
        {
            if (LightingService.Instance != null)
                return LightingService.Instance;

            LightingService onSelf = GetComponent<LightingService>();
            if (onSelf != null)
                return onSelf;

            LightingService existing = FindAnyObjectByType<LightingService>();
            if (existing != null)
                return existing;

            var svcGo = new GameObject("LightingService");
            return svcGo.AddComponent<LightingService>();
        }
    }

    [Serializable]
    public struct AmbientRegionSetup
    {
        public int regionId;

        [Range(LightLevel.Min, LightLevel.Max)]
        public int currentAmbientLight;

        public int cycleLengthTurns;
        public AmbientPhaseScheduleEntry[] phases;
    }
}
