using System;
using UnityEngine;

namespace JRogue.Interactables
{
    public sealed class InteractableTileBootstrap : MonoBehaviour
    {
        [SerializeField] InteractablePlacementSet placementSet;
        [SerializeField] InteractablePlacement[] placements = Array.Empty<InteractablePlacement>();

        void Start()
        {
            if (InteractableTileService.Instance == null)
                return;

            InteractablePlacement[] source = placementSet != null && placementSet.placements != null
                && placementSet.placements.Length > 0
                ? placementSet.placements
                : placements;

            for (int i = 0; i < source.Length; i++)
            {
                InteractablePlacement p = source[i];
                if (p.definition != null)
                    InteractableTileService.Instance.Register(p.cell, p.definition);
            }
        }
    }
}
