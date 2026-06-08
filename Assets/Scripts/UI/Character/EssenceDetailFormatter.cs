using System.Collections.Generic;
using System.Text;
using JRogue.Ability;
using JRogue.Item.Essence;
using UnityEngine;

namespace JRogue.UI.Character
{
    public static class EssenceDetailFormatter
    {
        public static string FormatTitle(EssenceData essence) =>
            string.IsNullOrWhiteSpace(essence?.essenceName) ? "(Essence)" : essence.essenceName.Trim();

        public static string FormatSubtitle(EssenceData essence, int slotIndex)
        {
            if (essence == null)
                return $"Slot {slotIndex + 1}";

            return $"Tier {essence.tier} · Essence slot {slotIndex + 1}";
        }

        public static string FormatBody(EssenceData essence)
        {
            if (essence == null)
                return string.Empty;

            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(essence.description))
                sb.AppendLine(essence.description.Trim());

            AppendStatSection(sb, essence.statModifiers);
            AppendResistanceSection(sb, essence.resistanceModifiers);
            AppendPassiveSection(sb, essence.complexPassives);
            AppendActiveSection(sb, essence.activeAbilities);

            sb.AppendLine();
            sb.Append("<color=#6a7884><i>Assign actives on the ability hotbar to use in combat.</i></color>");

            return sb.ToString().Trim();
        }

        public static string FormatEmptySlot(int slotIndex) =>
            $"No essence in slot {slotIndex + 1}.\n\nAcquire essences in the dungeon or from events.";

        static void AppendStatSection(StringBuilder sb, List<AttributeModifier> mods)
        {
            if (mods == null || mods.Count == 0)
                return;

            sb.AppendLine("<color=#cfd6dd><b>Stat modifiers</b></color>");
            foreach (AttributeModifier mod in mods)
            {
                string sign = mod.value >= 0 ? "+" : string.Empty;
                sb.AppendLine($" • {sign}{mod.value} {mod.attribute}");
            }
        }

        static void AppendResistanceSection(StringBuilder sb, List<DamageResistanceModifier> mods)
        {
            if (mods == null || mods.Count == 0)
                return;

            sb.AppendLine("<color=#cfd6dd><b>Resistances</b></color>");
            foreach (DamageResistanceModifier mod in mods)
            {
                string sign = mod.value >= 0 ? "+" : string.Empty;
                sb.AppendLine($" • {sign}{mod.value}% {mod.type}");
            }
        }

        static void AppendPassiveSection(StringBuilder sb, List<PassiveEffect> passives)
        {
            int count = passives?.Count ?? 0;
            sb.AppendLine($"<color=#cfd6dd><b>Passives ({count})</b></color>");
            if (count == 0)
            {
                sb.AppendLine(" — none —");
                return;
            }

            foreach (PassiveEffect passive in passives)
            {
                if (passive == null)
                    continue;

                sb.AppendLine($" • {passive.name}");
                if (!string.IsNullOrWhiteSpace(passive.effectDescription))
                    sb.AppendLine(passive.effectDescription.Trim());
            }
        }

        static void AppendActiveSection(StringBuilder sb, List<AbilityAction> actives)
        {
            int count = actives?.Count ?? 0;
            sb.AppendLine($"<color=#cfd6dd><b>Actives ({count})</b></color>");
            if (count == 0)
            {
                sb.AppendLine(" — none —");
                return;
            }

            foreach (AbilityAction active in actives)
            {
                if (active == null)
                    continue;

                string name = !string.IsNullOrWhiteSpace(active.abilityName)
                    ? active.abilityName
                    : active.name;
                sb.AppendLine($" • {name}");
                if (!string.IsNullOrWhiteSpace(active.description))
                    sb.AppendLine(active.description.Trim());

                sb.AppendLine(FormatAbilityMeta(active));
            }
        }

        static string FormatAbilityMeta(AbilityAction active)
        {
            var parts = new List<string>();
            if (active.soulPowerCost != 0)
                parts.Add($"Soul {active.soulPowerCost}");
            if (active.magicPowerCost != 0)
                parts.Add($"Magic {active.magicPowerCost}");
            if (active.divinePowerCost != 0)
                parts.Add($"Divine {active.divinePowerCost}");
            if (active.cooldownTurns != 0)
                parts.Add($"CD {active.cooldownTurns}");

            return parts.Count == 0 ? string.Empty : string.Join(" · ", parts);
        }
    }
}
