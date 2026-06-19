using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Manager.Party;
using JRogue.Shop;
using JRogue.UI.Gameplay;
using JRogue.World.Town;
using UnityEngine;

namespace JRogue.Controller.Npc
{
    /// <summary>Shop-wallet innkeeper — collects lodging gold but opens no buy/sell menu.</summary>
    public sealed class InnkeeperNpcController : NpcController
    {
        [Header("Inn")]
        [SerializeField] ShopNpcDefinition shopDefinition;
        [SerializeField] int lodgingCostGold = InnLodgingService.DefaultLodgingCostGold;

        BaseActor _interactionSpeaker;

        public ShopNpcDefinition ShopDefinition => shopDefinition;

        new void Start()
        {
            if (shopDefinition == null)
            {
                Debug.LogWarning($"[Innkeeper] {DisplayName} has no shop definition.");
                return;
            }

            TownShopStateService.EnsureRunService();
            InnLodgingService.EnsureRunService();
            TownShopStateService.Instance.GetOrCreateSnapshot(shopDefinition);
        }

        public override void BeginDialog(BaseActor speaker)
        {
            if (shopDefinition == null)
            {
                Debug.LogWarning($"[Innkeeper] {DisplayName} has no shop definition.");
                return;
            }

            TownShopStateService.EnsureRunService();
            InnLodgingService.EnsureRunService();
            _interactionSpeaker = speaker;

            if (InnLodgingService.HasBedAccess())
                ShowAlreadyPaidDialog();
            else
                ShowPaymentOfferDialog();
        }

        void ShowAlreadyPaidDialog()
        {
            int daysRemaining = InnLodgingService.GetRemainingBedAccessDays();
            string daysText = daysRemaining == 1
                ? "1 day"
                : $"{daysRemaining} days";

            var step = new DialogLineStep
            {
                SpeakerName = DisplayName,
                ResolvedText =
                    $"Your room is already paid — you have {daysText} left to use the beds.\n\n" +
                    "Rest well when you are ready.",
                Portrait = Portrait,
            };

            NpcDialogBoxUI.EnsureInstance().ShowLine(step, CompleteInteraction);
        }

        void ShowPaymentOfferDialog()
        {
            var yes = new DialogChoiceOptionData { label = "Yes" };
            var no = new DialogChoiceOptionData { label = "No" };

            var step = new DialogChoiceStep
            {
                SpeakerName = DisplayName,
                PromptText =
                    $"Welcome. A room costs {lodgingCostGold} gold until the next dungeon opens.\n\n" +
                    "Pay now?",
                Portrait = Portrait,
                Options = new[] { yes, no },
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, option =>
            {
                NpcDialogBoxUI.EnsureInstance().Close();
                if (option != null && option.label == "Yes")
                    TryAcceptPayment();
                else
                    CompleteInteraction();
            }, CompleteInteraction);
        }

        void TryAcceptPayment()
        {
            InnLodgingPaymentResult result = InnLodgingService.TryPayForLodging(
                shopDefinition,
                lodgingCostGold,
                out string message);

            var step = new DialogLineStep
            {
                SpeakerName = DisplayName,
                ResolvedText = message,
                Portrait = Portrait,
            };

            NpcDialogBoxUI.EnsureInstance().ShowLine(step, CompleteInteraction);

            if (result == InnLodgingPaymentResult.Success)
                Debug.Log($"[Innkeeper] {DisplayName} accepted lodging payment.");
        }

        void CompleteInteraction()
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            BaseActor speaker = _interactionSpeaker;
            _interactionSpeaker = null;
            if (speaker == null)
                return;

            PartyPlayerActionCompletion.CompleteActiveMemberAction(speaker);
        }
    }
}
