using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Manager.Party;
using JRogue.Shop;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Controller.Npc
{
    public sealed class ShopNpcController : NpcController
    {
        [Header("Shop")]
        [SerializeField] ShopNpcDefinition shopDefinition;

        BaseActor _interactionSpeaker;

        public ShopNpcDefinition ShopDefinition => shopDefinition;
        public string ShopNpcId =>
            shopDefinition != null && !string.IsNullOrWhiteSpace(shopDefinition.shopNpcId)
                ? shopDefinition.shopNpcId.Trim()
                : NpcId;

        new void Start()
        {
            if (shopDefinition == null)
            {
                Debug.LogWarning($"[ShopNpc] {DisplayName} has no shop definition.");
                return;
            }

            TownShopStateService.EnsureRunService();
            TownShopStateService.Instance.GetOrCreateSnapshot(shopDefinition);
        }

        public override void BeginDialog(BaseActor speaker)
        {
            if (shopDefinition == null)
            {
                Debug.LogWarning($"[ShopNpc] {DisplayName} has no shop definition.");
                return;
            }

            TownShopStateService.EnsureRunService();
            _interactionSpeaker = speaker;
            ShowGreeting(speaker);
        }

        void CompleteShopInteraction()
        {
            BaseActor speaker = _interactionSpeaker;
            _interactionSpeaker = null;
            if (speaker == null)
                return;

            PartyPlayerActionCompletion.CompleteActiveMemberAction(speaker);
        }

        void ShowGreeting(BaseActor speaker)
        {
            var yes = new DialogChoiceOptionData { label = "Yes" };
            var no = new DialogChoiceOptionData { label = "No" };

            var step = new DialogChoiceStep
            {
                SpeakerName = shopDefinition.displayName,
                PromptText = "Hello. Here to buy and sell?",
                Portrait = Portrait,
                Options = new[] { yes, no },
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, option =>
            {
                NpcDialogBoxUI.EnsureInstance().Close();
                if (option != null && option.label == "Yes")
                    ShopNpcMenuUI.EnsureInstance().Show(this, CompleteShopInteraction);
                else
                    CompleteShopInteraction();
            }, CompleteShopInteraction);
        }
    }
}
