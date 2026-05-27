using System;
using UnityEngine;

namespace JRogue.Traps
{
    public sealed class TrapBootstrap : MonoBehaviour
    {
        [SerializeField] TrapPlacementSet placementSet;
        [SerializeField] TrapPlacementEntry[] placements = Array.Empty<TrapPlacementEntry>();

        void Start()
        {
            if (TrapService.Instance == null)
                return;

            TrapPlacementEntry[] source = placementSet != null && placementSet.placements != null
                && placementSet.placements.Length > 0
                ? placementSet.placements
                : placements;

            for (int i = 0; i < source.Length; i++)
            {
                TrapPlacementEntry entry = source[i];
                if (entry.definition != null)
                    TrapService.Instance.Register(entry.cell, entry.definition);
            }
        }
    }
}
