using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.Racial
{
    public static class TieflingImplantForgemasterService
    {
        const string DefaultCatalogResourcesPath = "Racial/Tiefling/DefaultFleshmetalForgemaster";

        static TieflingForgemasterDefinition _defaultCatalog;

        public static TieflingForgemasterDefinition DefaultCatalog
        {
            get
            {
                if (_defaultCatalog == null)
                    _defaultCatalog = LoadDefaultCatalog();

                return _defaultCatalog;
            }
        }

        static TieflingForgemasterDefinition LoadDefaultCatalog()
        {
            TieflingForgemasterDefinition catalog =
                Resources.Load<TieflingForgemasterDefinition>(DefaultCatalogResourcesPath);
#if UNITY_EDITOR
            if (catalog == null)
            {
                catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<TieflingForgemasterDefinition>(
                    "Assets/Resources/Racial/Tiefling/DefaultFleshmetalForgemaster.asset");
            }
#endif
            return catalog;
        }

        public static void SetDefaultCatalogForTests(TieflingForgemasterDefinition catalog) =>
            _defaultCatalog = catalog;

        public static void ResetDefaultCatalogForTests() =>
            _defaultCatalog = null;

        public static bool IsSpeakerEligible(BaseActor speaker, out TieflingImplantsRuntime runtime, out string rejectLine) =>
            TieflingImplantForgemasterLogic.IsSpeakerEligible(speaker, out runtime, out rejectLine);

        public static IReadOnlyList<TieflingImplantInstallOffer> BuildInstallOffers(
            TieflingImplantsRuntime runtime,
            TieflingForgemasterDefinition catalog = null) =>
            TieflingImplantForgemasterLogic.BuildInstallOffers(runtime, catalog ?? DefaultCatalog);

        public static IReadOnlyList<TieflingImplantRemoveOffer> BuildRemoveOffers(TieflingImplantsRuntime runtime) =>
            TieflingImplantForgemasterLogic.BuildRemoveOffers(runtime);

        public static bool IsInstallChoiceEnabled(
            BaseActor speaker,
            TieflingImplantsRuntime runtime,
            TieflingImplantInstallOffer offer,
            out string disableReason)
        {
            GameStoryFlagService.EnsureInstance();
            return TieflingImplantForgemasterLogic.IsInstallChoiceEnabled(
                speaker,
                runtime,
                offer,
                GameStoryFlagService.Instance,
                out disableReason);
        }

        public static bool IsRemoveChoiceEnabled(
            BaseActor speaker,
            TieflingImplantRemoveOffer offer,
            out string disableReason)
        {
            GameStoryFlagService.EnsureInstance();
            return TieflingImplantForgemasterLogic.IsRemoveChoiceEnabled(
                speaker,
                offer,
                GameStoryFlagService.Instance,
                out disableReason);
        }

        public static bool TryExecuteInstall(
            BaseActor speaker,
            string implantId,
            TieflingForgemasterDefinition catalog,
            out string failureReason)
        {
            GameStoryFlagService.EnsureInstance();
            PartyManager party = PartyManager.Instance;
            IReadOnlyList<BaseActor> members = party != null ? party.partyMembers : null;
            if (!TieflingImplantForgemasterLogic.IsSpeakerEligible(speaker, out TieflingImplantsRuntime runtime, out failureReason))
                return false;

            return TieflingImplantForgemasterLogic.TryExecuteInstall(
                speaker,
                runtime,
                implantId,
                catalog ?? DefaultCatalog,
                members,
                GameStoryFlagService.Instance,
                out failureReason);
        }

        public static bool TryExecuteInstall(BaseActor speaker, string implantId, out string failureReason) =>
            TryExecuteInstall(speaker, implantId, DefaultCatalog, out failureReason);

        public static bool TryExecuteRemove(BaseActor speaker, ImplantSlot slot, out string failureReason)
        {
            GameStoryFlagService.EnsureInstance();
            PartyManager party = PartyManager.Instance;
            IReadOnlyList<BaseActor> members = party != null ? party.partyMembers : null;
            if (!TieflingImplantForgemasterLogic.IsSpeakerEligible(speaker, out TieflingImplantsRuntime runtime, out failureReason))
                return false;

            return TieflingImplantForgemasterLogic.TryExecuteRemove(
                speaker,
                runtime,
                slot,
                members,
                GameStoryFlagService.Instance,
                out failureReason);
        }
    }
}
