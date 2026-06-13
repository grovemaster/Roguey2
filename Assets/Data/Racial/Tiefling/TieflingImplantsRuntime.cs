using System.Collections.Generic;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Tiefling cyborg implants: preset slot map, install/replace/remove with per-slot modifier sources.
    /// </summary>
    [DefaultExecutionOrder(52)]
    public class TieflingImplantsRuntime : MonoBehaviour
    {
        [SerializeField] List<ImplantSlotPreset> presetImplants = new List<ImplantSlotPreset>();
        [SerializeField] bool requireTieflingImplantSubsystem = true;

        readonly Dictionary<ImplantSlot, CyborgImplantDefinition> _installedBySlot =
            new Dictionary<ImplantSlot, CyborgImplantDefinition>();

        readonly Dictionary<ImplantSlot, TieflingImplantSlotModifierSource> _sources =
            new Dictionary<ImplantSlot, TieflingImplantSlotModifierSource>();

        CharacterStats _stats;

        public IReadOnlyDictionary<ImplantSlot, CyborgImplantDefinition> InstalledSnapshot => _installedBySlot;

        void Awake() => _stats = GetComponent<CharacterStats>();

        void Start() => TryApplyPresetFromSerialized();

        void OnDestroy()
        {
            foreach (ImplantSlot slot in new List<ImplantSlot>(_installedBySlot.Keys))
                RemoveImplantInternal(slot);
        }

        public void TryApplyPresetFromSerialized()
        {
            if (!ValidateTieflingActor(out _))
                return;

            foreach (ImplantSlot slot in new List<ImplantSlot>(_installedBySlot.Keys))
                RemoveImplantInternal(slot);

            if (presetImplants == null)
                return;

            foreach (ImplantSlotPreset preset in presetImplants)
            {
                if (preset?.implant == null)
                    continue;
                TryInstallImplant(preset.slot, preset.implant, out _);
            }
        }

        public bool TryInstallImplant(ImplantSlot slot, CyborgImplantDefinition implant, out string failureReason) =>
            TryInstallOrReplace(slot, implant, replaceExisting: false, out failureReason);

        public bool TryReplaceImplant(ImplantSlot slot, CyborgImplantDefinition implant, out string failureReason) =>
            TryInstallOrReplace(slot, implant, replaceExisting: true, out failureReason);

        public bool TryRemoveImplant(ImplantSlot slot)
        {
            if (!_installedBySlot.ContainsKey(slot))
                return false;
            RemoveImplantInternal(slot);
            return true;
        }

        public bool TryGetInstalled(ImplantSlot slot, out CyborgImplantDefinition implant) =>
            _installedBySlot.TryGetValue(slot, out implant);

        public bool HasImplantId(string implantId)
        {
            if (string.IsNullOrEmpty(implantId))
                return false;

            foreach (CyborgImplantDefinition installed in _installedBySlot.Values)
            {
                if (installed != null && installed.implantId == implantId)
                    return true;
            }

            return false;
        }

        public void ClearAllImplants()
        {
            foreach (ImplantSlot slot in new List<ImplantSlot>(_installedBySlot.Keys))
                RemoveImplantInternal(slot);
        }

        public void RefreshPassives()
        {
            foreach (CyborgImplantDefinition implant in _installedBySlot.Values)
                RefreshPassivesForImplant(implant);
        }

        public void NotifyPassivesTurnStart()
        {
            foreach (CyborgImplantDefinition implant in _installedBySlot.Values)
                NotifyPassivesTurnStartForImplant(implant);
        }

        bool TryInstallOrReplace(
            ImplantSlot slot,
            CyborgImplantDefinition implant,
            bool replaceExisting,
            out string failureReason)
        {
            failureReason = null;

            if (!ValidateTieflingActor(out failureReason))
                return false;

            if (implant == null)
            {
                failureReason = "Implant is null.";
                return false;
            }

            if (string.IsNullOrEmpty(implant.implantId))
            {
                failureReason = "Implant has no implantId.";
                return false;
            }

            if (!implant.TryGetTargetSlot(out ImplantSlot targetSlot, out failureReason))
                return false;

            if (slot != targetSlot)
            {
                failureReason = $"Implant '{implant.implantId}' belongs in slot {targetSlot}, not {slot}.";
                return false;
            }

            bool occupied = _installedBySlot.ContainsKey(slot);
            if (occupied && !replaceExisting)
            {
                failureReason = $"Slot {slot} already has an implant.";
                return false;
            }

            if (occupied)
                RemoveImplantInternal(slot);

            ApplyImplant(slot, implant);
            _installedBySlot[slot] = implant;
            return true;
        }

        void ApplyImplant(ImplantSlot slot, CyborgImplantDefinition implant)
        {
            if (!_sources.TryGetValue(slot, out TieflingImplantSlotModifierSource src))
            {
                src = new TieflingImplantSlotModifierSource(slot);
                _sources[slot] = src;
            }

            RacialProgressionPayloadApplicator.Apply(gameObject, _stats, src, implant);
        }

        void RemoveImplantInternal(ImplantSlot slot)
        {
            if (!_installedBySlot.TryGetValue(slot, out CyborgImplantDefinition implant))
                return;

            if (_sources.TryGetValue(slot, out TieflingImplantSlotModifierSource src))
                RacialProgressionPayloadApplicator.Remove(gameObject, _stats, src, implant);

            _stats.UnregisterBodyEquipmentContribution(BodyContributionKey(slot));
            _installedBySlot.Remove(slot);
        }

        static string BodyContributionKey(ImplantSlot slot) => $"TieflingImplant:{slot}";

        void RefreshPassivesForImplant(CyborgImplantDefinition implant) =>
            RacialProgressionPayloadApplicator.RefreshPassives(gameObject, implant);

        void NotifyPassivesTurnStartForImplant(CyborgImplantDefinition implant) =>
            RacialProgressionPayloadApplicator.NotifyPassivesTurnStart(gameObject, implant);

        bool ValidateTieflingActor(out string failureReason)
        {
            failureReason = null;
            if (_stats == null)
            {
                failureReason = "No CharacterStats.";
                return false;
            }

            if (_stats.race != Race.Tiefling)
            {
                failureReason = "Not a Tiefling.";
                return false;
            }

            if (requireTieflingImplantSubsystem &&
                _stats.racialSubsystem != RacialSubsystemKind.TieflingImplants)
            {
                failureReason = "Racial subsystem is not TieflingImplants.";
                return false;
            }

            return true;
        }
    }
}
