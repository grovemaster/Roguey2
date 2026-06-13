using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Manager.Party;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Racial
{
    public sealed class TieflingForgemasterDialogSession
    {
        readonly BaseActor _speaker;
        readonly INpcTalkTarget _target;
        readonly TieflingForgemasterDefinition _catalog;
        readonly PortraitDefinition _portrait;
        readonly string _displayName;

        public TieflingForgemasterDialogSession(
            BaseActor speaker,
            INpcTalkTarget target,
            TieflingForgemasterDefinition catalog)
        {
            GameStoryFlagService.EnsureInstance();
            _speaker = speaker;
            _target = target;
            _catalog = catalog ?? TieflingImplantForgemasterService.DefaultCatalog;
            _portrait = target.Portrait;
            _displayName = target.Actor != null ? target.Actor.DisplayName : "Tiefling Fleshmetal Forgemaster";
        }

        public void Start()
        {
            if (!TieflingImplantForgemasterLogic.IsSpeakerEligible(_speaker, out TieflingImplantsRuntime runtime, out string rejectLine))
            {
                ShowLine(rejectLine ?? "This forge works fleshmetal for Tieflings only.");
                return;
            }

            IReadOnlyList<TieflingImplantInstallOffer> installOffers =
                TieflingImplantForgemasterService.BuildInstallOffers(runtime, _catalog);
            IReadOnlyList<TieflingImplantRemoveOffer> removeOffers =
                TieflingImplantForgemasterService.BuildRemoveOffers(runtime);

            if ((installOffers == null || installOffers.Count == 0)
                && (removeOffers == null || removeOffers.Count == 0))
            {
                ShowLine("Nothing here for you today.");
                return;
            }

            ShowOffer(runtime, installOffers, removeOffers);
        }

        void ShowOffer(
            TieflingImplantsRuntime runtime,
            IReadOnlyList<TieflingImplantInstallOffer> installOffers,
            IReadOnlyList<TieflingImplantRemoveOffer> removeOffers)
        {
            GameStoryFlagService flags = GameStoryFlagService.Instance;
            var options = new List<DialogChoiceOptionData>();

            for (int i = 0; i < installOffers.Count; i++)
            {
                TieflingImplantInstallOffer offer = installOffers[i];
                CyborgImplantDefinition implant = offer.Implant;
                if (implant == null || string.IsNullOrEmpty(implant.implantId))
                    continue;

                string displayName = TieflingImplantForgemasterLogic.ResolveDisplayName(implant);
                string shortCost = TieflingImplantForgemasterLogic.FormatInstallCostShort(implant.installCost);
                bool enabled = TieflingImplantForgemasterService.IsInstallChoiceEnabled(
                    _speaker,
                    runtime,
                    offer,
                    out _);

                options.Add(new DialogChoiceOptionData
                {
                    label = $"{displayName} ({TieflingImplantForgemasterLogic.FormatSlotDisplay(offer.Slot)}), {shortCost}",
                    payload = TieflingForgemasterIds.InstallPayloadPrefix + implant.implantId,
                    enabled = enabled,
                });
            }

            for (int i = 0; i < removeOffers.Count; i++)
            {
                TieflingImplantRemoveOffer offer = removeOffers[i];
                CyborgImplantRemoveCost removeCost =
                    TieflingImplantForgemasterLogic.ResolveRemoveCost(offer.Installed);
                string shortCost = TieflingImplantForgemasterLogic.FormatRemoveCostShort(removeCost);
                bool enabled = TieflingImplantForgemasterService.IsRemoveChoiceEnabled(_speaker, offer, out _);

                options.Add(new DialogChoiceOptionData
                {
                    label =
                        $"Remove {TieflingImplantForgemasterLogic.FormatSlotDisplay(offer.Slot)}, {shortCost}",
                    payload = TieflingForgemasterIds.RemovePayloadPrefix + offer.Slot,
                    enabled = enabled,
                });
            }

            options.Add(new DialogChoiceOptionData
            {
                label = "Cancel",
                payload = TieflingForgemasterIds.CancelPayload,
                enabled = true,
            });

            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText = TieflingImplantForgemasterLogic.BuildOfferBodyText(
                    runtime,
                    installOffers,
                    removeOffers,
                    flags),
                Portrait = _portrait,
                Options = options,
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, OnChoiceSelected, Complete);
        }

        void OnChoiceSelected(DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null
                || option.payload == TieflingForgemasterIds.CancelPayload
                || string.IsNullOrWhiteSpace(option.payload))
            {
                Complete();
                return;
            }

            string payload = option.payload.Trim();
            if (payload.StartsWith(TieflingForgemasterIds.InstallPayloadPrefix))
            {
                string implantId = payload.Substring(TieflingForgemasterIds.InstallPayloadPrefix.Length);
                if (!TieflingImplantForgemasterService.TryExecuteInstall(_speaker, implantId, _catalog, out _))
                {
                    ShowLine("You no longer have what the forge requires.");
                    return;
                }

                ShowLine("The graft is set.");
                return;
            }

            if (payload.StartsWith(TieflingForgemasterIds.RemovePayloadPrefix))
            {
                string slotText = payload.Substring(TieflingForgemasterIds.RemovePayloadPrefix.Length);
                if (!System.Enum.TryParse(slotText, out ImplantSlot slot)
                    || !TieflingImplantForgemasterService.TryExecuteRemove(_speaker, slot, out _))
                {
                    ShowLine("You no longer have what the forge requires.");
                    return;
                }

                ShowLine("The graft is removed.");
                return;
            }

            Complete();
        }

        void ShowLine(string text)
        {
            var step = new DialogLineStep
            {
                SpeakerName = _displayName,
                ResolvedText = text,
                Portrait = _portrait,
            };

            NpcDialogBoxUI.EnsureInstance().ShowLine(step, FinishLine);
        }

        void FinishLine()
        {
            NpcDialogBoxUI.EnsureInstance().Close();
            Complete();
        }

        void Complete()
        {
            PartyPlayerActionCompletion.CompleteActiveMemberAction(_speaker);
        }
    }
}
