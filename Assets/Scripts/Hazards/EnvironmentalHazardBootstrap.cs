using System;
using UnityEngine;

namespace JRogue.Hazards
{
    /// <summary>Registers sample hazards for QA (SampleScene).</summary>
    public sealed class EnvironmentalHazardBootstrap : MonoBehaviour
    {
        [Serializable]
        public struct Placement
        {
            public Vector3Int cell;
            public EnvironmentalHazardDefinition definition;
        }

        [SerializeField] Placement[] placements = Array.Empty<Placement>();

        void Start()
        {
            if (HazardService.Instance == null)
                return;

            for (int i = 0; i < placements.Length; i++)
            {
                Placement p = placements[i];
                if (p.definition != null)
                    HazardService.Instance.Register(p.cell, p.definition);
            }
        }
    }
}
