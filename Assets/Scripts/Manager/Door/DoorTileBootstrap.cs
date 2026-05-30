using System;
using JRogue.Data.Door;
using UnityEngine;

namespace JRogue.Manager.Door
{
    public sealed class DoorTileBootstrap : MonoBehaviour
    {
        [SerializeField] DoorPlacementSet placementSet;
        [SerializeField] DoorPlacement[] placements = Array.Empty<DoorPlacement>();

        void Start()
        {
            if (DoorService.Instance == null)
                return;

            DoorPlacement[] source = placementSet != null && placementSet.placements != null
                && placementSet.placements.Length > 0
                ? placementSet.placements
                : placements;

            for (int i = 0; i < source.Length; i++)
                DoorService.Instance.Register(source[i]);
        }
    }
}
