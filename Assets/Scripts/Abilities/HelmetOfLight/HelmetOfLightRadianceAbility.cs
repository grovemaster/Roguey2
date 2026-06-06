using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.World.Lighting;
using UnityEngine;

namespace JRogue.Ability.HelmetOfLight
{
    [CreateAssetMenu(fileName = "HelmetOfLight_Radiance", menuName = "JRogue/Abilities/Helmet of Light Radiance")]
    public sealed class HelmetOfLightRadianceAbility : AbilityAction
    {
        [Min(1)]
        public int lightDurationTurns = LightSourceItemRules.DefaultHelmetLightDurationTurns;

        public override bool CanExecute(GameObject user)
        {
            if (user == null)
                return false;

            EquipmentManager equipment = user.GetComponent<EquipmentManager>();
            ItemInstance helmet = equipment?.GetEquippedInstance(EquipmentSlot.Head);
            if (helmet?.Definition is not LightSourceItemData)
                return false;

            return LightSourceItemRules.CanActivateTimedLight(helmet, this);
        }

        protected override bool ExecuteCore(GameObject user)
        {
            EquipmentManager equipment = user.GetComponent<EquipmentManager>();
            ItemInstance helmet = equipment?.GetEquippedInstance(EquipmentSlot.Head);
            if (helmet?.Definition is not LightSourceItemData)
                return false;

            if (!LightSourceItemRules.CanActivateTimedLight(helmet, this))
                return false;

            LightSourceItemRules.BeginHelmetRadiance(helmet, this, lightDurationTurns);
            PartyLightEmitterBridge.RefreshParty();
            LightingService.Instance?.OnPartyVisionActivity();
            return true;
        }
    }
}
