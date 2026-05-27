using System;
using UnityEngine;

namespace JRogue.Interactables
{
    /// <summary>
    /// Reusable list of interactable cells for a room or level.
    /// Assign on <see cref="InteractableTileBootstrap"/> per scene.
    /// </summary>
    [CreateAssetMenu(
        fileName = "InteractablePlacementSet",
        menuName = "JRogue/Interactables/Interactable Placement Set")]
    public sealed class InteractablePlacementSet : ScriptableObject
    {
        public InteractablePlacement[] placements = Array.Empty<InteractablePlacement>();
    }
}
