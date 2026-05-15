#if UNITY_EDITOR || DEVELOPMENT_BUILD
using JRogue.Ability;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JRogue.Racial
{
    /// <summary>
    /// Dev-only: runs one imprint <see cref="AbilityAction"/> from the deepest chosen node for manual validation (N4.4).
    /// Not compiled into release player builds without DEVELOPMENT_BUILD.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class SpiritImprintDevActiveInvoker : MonoBehaviour
    {
        [SerializeField] SpiritImprintRuntime imprint;

        [Tooltip("When enabled, press F9 (new Input System) to fire the selected active.")]
        [SerializeField]
        bool listenForF9 = true;

        [SerializeField] [Tooltip("Index into the deepest node's activeAbilities list.")]
        int activeIndex;

        void Update()
        {
            if (imprint == null || imprint.Graph == null) return;
            if (!listenForF9) return;
            if (Keyboard.current == null || !Keyboard.current.f9Key.wasPressedThisFrame) return;
            if (!TryGetAbility(out var ability, out var reason))
            {
                Debug.LogWarning($"[SpiritImprint][DevActive] {reason}");
                return;
            }

            ability.Execute(gameObject);
        }

        bool TryGetAbility(out AbilityAction ability, out string reason)
        {
            ability = null;
            reason = null;
            var path = imprint.ChosenPathNodeIds;
            if (path == null || path.Count == 0)
            {
                reason = "No chosen path.";
                return false;
            }

            var tailId = path[path.Count - 1];
            if (!imprint.Graph.TryFindNode(tailId, out var node) || node.activeAbilities == null ||
                activeIndex < 0 || activeIndex >= node.activeAbilities.Count)
            {
                reason = $"No active at index {activeIndex} on node '{tailId}'.";
                return false;
            }

            ability = node.activeAbilities[activeIndex];
            if (ability == null)
            {
                reason = "Ability reference is null.";
                return false;
            }

            return true;
        }
    }
}
#else
namespace JRogue.Racial
{
    /// <summary>Release builds omit <see cref="SpiritImprintDevActiveInvoker"/> (N4.4).</summary>
    internal static class SpiritImprintDevActiveInvokerExcluded { }
}
#endif
