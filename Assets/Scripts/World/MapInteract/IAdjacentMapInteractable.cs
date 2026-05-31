using JRogue.Actors;
using UnityEngine;

namespace JRogue.World.MapInteract
{
    public interface IAdjacentMapInteractable
    {
        Vector3Int Cell { get; }
        string ListLabel { get; }
        int SortOrder { get; }
        bool CanInteract(BaseActor actor);
        void OpenInteractUI(BaseActor actor);
    }
}
