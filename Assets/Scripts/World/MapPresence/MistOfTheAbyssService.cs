using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.UI.Gameplay;
using JRogue.UI.Hotbar;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.World.MapPresence
{
    /// <summary>
    /// Floor-scoped Mist of the Abyss registration, essence-active suppress queries,
    /// combat-log notifications, and visual show/hide.
    /// </summary>
    public static class MistOfTheAbyssService
    {
        public const string LogPrefix = "[MistOfTheAbyss]";
        public const string DenyMessage = "Mist of the Abyss suppresses essence powers.";

        public const string FirstApplyMessage =
            "Mist of the Abyss settles over Floor 1. Essence powers are suppressed.";

        public const string ReEntryMessage =
            "Mist of the Abyss still blankets Floor 1. Essence powers are suppressed.";

        public const string LiftedMessage =
            "Mist of the Abyss has lifted. Essence powers can be used again.";

        static readonly Dictionary<string, int> ActiveMistByFloor =
            new Dictionary<string, int>(System.StringComparer.Ordinal);

        /// <summary>Floor id for which the party already received an apply/re-entry combat log this visit.</summary>
        static string _notifiedFloorId;
        static bool _visualsVisible;

        public static void ResetForNewRun()
        {
            ActiveMistByFloor.Clear();
            _notifiedFloorId = null;
            SetVisualsVisible(false);
            Debug.Log($"{LogPrefix} Reset for new run.");
        }

        public static void RegisterMist(string hostFloorId)
        {
            if (string.IsNullOrEmpty(hostFloorId))
                return;

            ActiveMistByFloor.TryGetValue(hostFloorId, out int count);
            ActiveMistByFloor[hostFloorId] = count + 1;
            Debug.Log($"{LogPrefix} Register mist on '{hostFloorId}' (hosts={count + 1}).");

            string active = ResolveActiveFloorId();
            if (string.Equals(active, hostFloorId, System.StringComparison.Ordinal))
                NotifyPartyOnMistFloor(hostFloorId, isFirstApply: true);
        }

        public static void UnregisterMist(string hostFloorId)
        {
            if (string.IsNullOrEmpty(hostFloorId))
                return;

            if (!ActiveMistByFloor.TryGetValue(hostFloorId, out int count))
                return;

            count--;
            if (count <= 0)
                ActiveMistByFloor.Remove(hostFloorId);
            else
                ActiveMistByFloor[hostFloorId] = count;

            Debug.Log($"{LogPrefix} Unregister mist on '{hostFloorId}' (hosts={Mathf.Max(0, count)}).");

            string active = ResolveActiveFloorId();
            if (!IsMistActiveOnFloor(active))
            {
                if (_visualsVisible)
                {
                    GameLogService.ActiveSession.Append(LiftedMessage);
                    SetVisualsVisible(false);
                }

                if (_notifiedFloorId == hostFloorId || _notifiedFloorId == active)
                    _notifiedFloorId = null;
            }
        }

        public static bool IsMistActiveOnFloor(string floorId)
        {
            if (string.IsNullOrEmpty(floorId))
                return false;

            return ActiveMistByFloor.TryGetValue(floorId, out int count) && count > 0;
        }

        public static bool IsSuppressedForActor(BaseActor actor)
        {
            if (actor == null)
                return false;

            string floorId = ResolveActiveFloorId();
            if (!IsMistActiveOnFloor(floorId))
                return false;

            PartyManager party = PartyManager.Instance;
            if (party?.partyMembers == null)
                return false;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                if (party.partyMembers[i] == actor)
                    return true;
            }

            return false;
        }

        public static bool TryAllowEssenceActive(BaseActor actor, out string denyReason, bool logDeny = true)
        {
            denyReason = null;
            if (!IsSuppressedForActor(actor))
                return true;

            denyReason = DenyMessage;
            if (logDeny)
                GameLogService.ActiveSession.Append(DenyMessage);
            else
                Debug.Log($"{LogPrefix} Essence active suppressed for '{actor.DisplayName}'.");

            return false;
        }

        public static bool TryAllowEssenceActive(GameObject actorGo, out string denyReason, bool logDeny = true)
        {
            BaseActor actor = actorGo != null ? actorGo.GetComponent<BaseActor>() : null;
            return TryAllowEssenceActive(actor, out denyReason, logDeny);
        }

        /// <summary>Called when the dungeon run changes the active floor.</summary>
        public static void OnActiveFloorChanged(string floorId)
        {
            if (IsMistActiveOnFloor(floorId))
            {
                NotifyPartyOnMistFloor(floorId, isFirstApply: false);
                return;
            }

            if (_visualsVisible)
                SetVisualsVisible(false);
            _notifiedFloorId = null;
        }

        static void NotifyPartyOnMistFloor(string floorId, bool isFirstApply)
        {
            SetVisualsVisible(true);

            if (_notifiedFloorId == floorId)
                return;

            _notifiedFloorId = floorId;
            string message = isFirstApply ? FirstApplyMessage : ReEntryMessage;
            GameLogService.ActiveSession.Append(message);
            Debug.Log($"{LogPrefix} Notified party on '{floorId}': {message}");
        }

        static string ResolveActiveFloorId()
        {
            if (DungeonRunState.Instance != null && !string.IsNullOrEmpty(DungeonRunState.Instance.ActiveFloorId))
                return DungeonRunState.Instance.ActiveFloorId;

            return DungeonFloorInstanceManager.Instance?.GetActiveFloorInstance()?.FloorId;
        }

        static void SetVisualsVisible(bool visible)
        {
            bool changed = _visualsVisible != visible;
            _visualsVisible = visible;
            // Avoid forcing UI bootstrap when only clearing state (unit tests / headless).
            if (!visible && MistOfTheAbyssVisualUI.Instance == null)
            {
                if (changed)
                    AbilityHotbarUI.Instance?.RefreshAll();
                return;
            }

            MistOfTheAbyssVisualUI.EnsureInstance().SetVisible(visible);
            if (changed)
                AbilityHotbarUI.Instance?.RefreshAll();
        }
    }
}
