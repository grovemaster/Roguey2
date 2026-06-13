#if UNITY_EDITOR
using JRogue.Racial;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.Racial
{
    public static class PartyRacialDevSwapMenu
    {
        [MenuItem("JRogue/Dev/Convert Active Party Member To Tiefling")]
        public static void ConvertActiveMemberToTiefling()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PartyRacialDev] Enter Play mode first, or use Ctrl+Shift+T during gameplay.");
                return;
            }

            if (PartyRacialDevSwapService.TryConvertActiveMemberToTiefling(out string reason))
                Debug.Log("[PartyRacialDev] Active party member converted to Tiefling.");
            else
                Debug.LogWarning($"[PartyRacialDev] {reason}");
        }
    }
}
#endif
