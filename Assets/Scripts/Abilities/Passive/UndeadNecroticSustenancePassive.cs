using JRogue.Racial;
using UnityEngine;

namespace JRogue.Ability.Passive
{
    /// <summary>
    /// Folk baseline paired benefit: documents necrotic sustenance channel (inventory/combat hooks later).
    /// </summary>
    [CreateAssetMenu(fileName = "UndeadNecroticSustenance", menuName = "JRogue/Passives/Undead Necrotic Sustenance")]
    public class UndeadNecroticSustenancePassive : PassiveEffect
    {
        public override void OnApply(GameObject user) => Debug.Log($"{user.name}: Necrotic sustenance channel available (non-potion recovery).");

        public override void OnRemove(GameObject user) { }
    }
}
