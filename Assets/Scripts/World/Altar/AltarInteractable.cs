using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.UI.Gameplay;
using JRogue.World.MapInteract;
using UnityEngine;

namespace JRogue.World.Altar
{
    public sealed class AltarInteractable : IAdjacentMapInteractable
    {
        public AltarInstance Instance { get; }

        public Vector3Int Cell => Instance.Cell;
        public string ListLabel => Instance.Definition != null
            ? Instance.Definition.displayName
            : "Altar";

        public int SortOrder => Instance.Definition != null
            ? Instance.Definition.pickerSortOrder
            : 0;

        public AltarInteractable(AltarInstance instance) => Instance = instance;

        public bool CanInteract(BaseActor actor) => actor != null && Instance != null;

        public void OpenInteractUI(BaseActor actor)
        {
            if (Instance.IsDepleted)
            {
                AltarUsedModalUI.EnsureInstance().Show(Instance);
                return;
            }

            AltarOfferingModalUI.EnsureInstance().Show(
                actor,
                Instance,
                consumedTurn =>
                {
                    if (consumedTurn)
                        PartyPlayerActionCompletion.CompleteActiveMemberAction(actor);
                });
        }
    }
}
