using JRogue.Manager.Party;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JRogue.Racial
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [DefaultExecutionOrder(500)]
    public sealed class PartyRacialDevTools : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindAnyObjectByType<PartyRacialDevTools>(FindObjectsInactive.Include) != null)
                return;

            var go = new GameObject(nameof(PartyRacialDevTools));
            DontDestroyOnLoad(go);
            go.AddComponent<PartyRacialDevTools>();
        }

        void Update()
        {
            if (Keyboard.current == null)
                return;

            Keyboard kb = Keyboard.current;
            if (!kb.leftCtrlKey.isPressed || !kb.leftShiftKey.isPressed || !kb.tKey.wasPressedThisFrame)
                return;

            if (PartyRacialDevSwapService.TryConvertActiveMemberToTiefling(out string reason))
                return;

            Debug.LogWarning($"[PartyRacialDev] {reason}");
        }
    }
#endif
}
