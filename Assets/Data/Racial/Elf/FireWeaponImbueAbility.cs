using JRogue.Ability;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "FireWeaponImbue", menuName = "JRogue/Racial/Fire Weapon Imbue")]
    public class FireWeaponImbueAbility : AbilityAction
    {
        [Tooltip("Must match ElementalSpiritDefinition.spiritId.")]
        public string spiritId;
        public int fireDamageBonus = 2;

        public override bool CanExecute(GameObject user)
        {
            var contracts = user.GetComponent<ElementalSpiritContractsRuntime>();
            return contracts != null && contracts.IsSpiritSummoned(spiritId);
        }

        protected override bool ExecuteCore(GameObject user)
        {
            var contracts = user.GetComponent<ElementalSpiritContractsRuntime>();
            if (contracts == null)
                return false;
            return contracts.TryToggleFireWeaponImbue(spiritId, this, fireDamageBonus);
        }
    }
}
