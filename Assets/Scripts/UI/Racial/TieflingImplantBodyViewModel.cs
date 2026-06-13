using System.Collections.Generic;
using System.Text;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Item.Essence;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.UI.Racial
{
    public static class ImplantSlotLabels
    {
        public static string GetLabel(ImplantSlot slot) =>
            slot switch
            {
                ImplantSlot.Head => "HEAD",
                ImplantSlot.LeftArm => "LEFT ARM",
                ImplantSlot.RightArm => "RIGHT ARM",
                ImplantSlot.Torso => "TORSO",
                ImplantSlot.Heart => "HEART",
                ImplantSlot.LeftLeg => "LEFT LEG",
                ImplantSlot.RightLeg => "RIGHT LEG",
                _ => slot.ToString().ToUpperInvariant()
            };

        public static string GetDisplayName(ImplantSlot slot) =>
            slot switch
            {
                ImplantSlot.Head => "Head",
                ImplantSlot.LeftArm => "Left Arm",
                ImplantSlot.RightArm => "Right Arm",
                ImplantSlot.Torso => "Torso",
                ImplantSlot.Heart => "Heart",
                ImplantSlot.LeftLeg => "Left Leg",
                ImplantSlot.RightLeg => "Right Leg",
                _ => slot.ToString()
            };
    }

    public sealed class TieflingImplantSlotCellModel
    {
        public ImplantSlot Slot;
        public bool Occupied;
        public CyborgImplantDefinition Implant;
        public string Label;
        public string Subtitle;
    }

    public sealed class TieflingImplantDetailModel
    {
        public ImplantSlot Slot;
        public bool Occupied;
        public string HeroTitle = string.Empty;
        public string HeroSubtitle = string.Empty;
        public string LeftColumnText = string.Empty;
        public string RightColumnText = string.Empty;
    }

    public sealed class TieflingImplantBodyViewModel
    {
        public const string BannerText =
            "View only — visit the Fleshmetal Forgemaster in town to install or change grafts.";

        public const string CannotHostGraftsMessage =
            "This character cannot host fleshmetal grafts.";

        public static readonly ImplantSlot[] DefaultSelectionOrder =
        {
            ImplantSlot.LeftArm,
            ImplantSlot.RightArm,
            ImplantSlot.Torso,
            ImplantSlot.Heart,
            ImplantSlot.Head,
            ImplantSlot.LeftLeg,
            ImplantSlot.RightLeg
        };

        public string FolkBaselineText = string.Empty;
        public List<TieflingImplantSlotCellModel> Cells = new();
        public TieflingImplantDetailModel Detail = new();
        public ImplantSlot SelectedSlot = ImplantSlot.LeftArm;

        public static TieflingImplantBodyViewModel Build(BaseActor tiefling, ImplantSlot? selectedSlot = null)
        {
            var vm = new TieflingImplantBodyViewModel();
            if (tiefling == null)
                return vm;

            var runtime = tiefling.GetComponent<TieflingImplantsRuntime>();
            var loadoutApplier = tiefling.GetComponent<RacialLoadoutApplier>();
            CharacterStats stats = tiefling.stats;

            vm.FolkBaselineText = BuildFolkBaselineSummary(loadoutApplier?.Loadout, stats);
            vm.SelectedSlot = selectedSlot ?? ResolveDefaultSelection(runtime);
            vm.Cells = BuildCells(runtime);
            vm.Detail = BuildDetail(vm.SelectedSlot, runtime);
            return vm;
        }

        public static ImplantSlot ResolveDefaultSelection(TieflingImplantsRuntime runtime)
        {
            if (runtime != null && runtime.TryGetInstalled(ImplantSlot.LeftArm, out _))
                return ImplantSlot.LeftArm;

            if (runtime != null)
            {
                foreach (ImplantSlot slot in DefaultSelectionOrder)
                {
                    if (runtime.TryGetInstalled(slot, out _))
                        return slot;
                }
            }

            return ImplantSlot.LeftArm;
        }

        static List<TieflingImplantSlotCellModel> BuildCells(TieflingImplantsRuntime runtime)
        {
            var cells = new List<TieflingImplantSlotCellModel>(DefaultSelectionOrder.Length);
            foreach (ImplantSlot slot in DefaultSelectionOrder)
            {
                CyborgImplantDefinition implant = null;
                bool occupied = runtime != null && runtime.TryGetInstalled(slot, out implant);
                cells.Add(new TieflingImplantSlotCellModel
                {
                    Slot = slot,
                    Occupied = occupied,
                    Implant = occupied ? implant : null,
                    Label = ImplantSlotLabels.GetLabel(slot),
                    Subtitle = occupied ? ResolveImplantTitle(implant) : ImplantSlotLabels.GetLabel(slot)
                });
            }

            return cells;
        }

        static TieflingImplantDetailModel BuildDetail(ImplantSlot slot, TieflingImplantsRuntime runtime)
        {
            var detail = new TieflingImplantDetailModel { Slot = slot };

            if (runtime == null)
            {
                detail.LeftColumnText = CannotHostGraftsMessage;
                detail.HeroTitle = "DETAILS";
                return detail;
            }

            if (runtime.TryGetInstalled(slot, out CyborgImplantDefinition implant))
            {
                detail.Occupied = true;
                detail.HeroTitle = ResolveImplantTitle(implant);
                detail.HeroSubtitle = ImplantSlotLabels.GetDisplayName(slot);
                BuildOccupiedColumns(implant, out detail.LeftColumnText, out detail.RightColumnText);
                return detail;
            }

            detail.HeroTitle = ImplantSlotLabels.GetDisplayName(slot);
            detail.HeroSubtitle = "Empty";
            detail.LeftColumnText = BuildEmptySlotBody(slot);
            return detail;
        }

        public static string BuildFolkBaselineSummary(RacialLoadoutDefinition loadout, CharacterStats stats)
        {
            var parts = new List<string> { "FOLK BASELINE" };

            if (loadout?.resistanceModifiers != null)
            {
                foreach (DamageResistanceModifier mod in loadout.resistanceModifiers)
                {
                    string sign = mod.value >= 0 ? "+" : string.Empty;
                    parts.Add($"{mod.type} resist {sign}{mod.value}");
                }
            }

            if (stats != null && (stats.bodyCapabilities & BodyCapabilityFlags.Horns) != 0)
                parts.Add("Horns (no horn-blocking helmets)");

            if (loadout?.passiveEffects != null)
            {
                foreach (PassiveEffect passive in loadout.passiveEffects)
                {
                    if (passive == null || string.IsNullOrWhiteSpace(passive.name))
                        continue;

                    parts.Add(passive.name.Trim());
                }
            }

            return string.Join(" · ", parts);
        }

        static string ResolveImplantTitle(CyborgImplantDefinition implant)
        {
            if (implant == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(implant.displayName))
                return implant.displayName.Trim();

            return string.IsNullOrWhiteSpace(implant.implantId) ? "Implant" : implant.implantId.Trim();
        }

        static void BuildOccupiedColumns(
            CyborgImplantDefinition implant,
            out string leftColumn,
            out string rightColumn)
        {
            var left = new StringBuilder();
            var right = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(implant.description))
                left.AppendLine(implant.description.Trim());

            AppendStatSection(left, implant.statModifiers);
            AppendResistanceSection(left, implant.resistanceModifiers);

            AppendPassiveSection(right, implant.passiveEffects);
            AppendActiveSection(right, implant.activeAbilities);
            AppendBenefitSection(right, implant.racialBenefits);
            AppendRestrictionSection(right, implant.racialRestrictions);

            leftColumn = left.ToString().Trim();
            rightColumn = right.ToString().Trim();

            if (string.IsNullOrEmpty(leftColumn))
                leftColumn = "—";
            if (string.IsNullOrEmpty(rightColumn))
                rightColumn = "—";
        }

        static string BuildEmptySlotBody(ImplantSlot slot)
        {
            string slotName = ImplantSlotLabels.GetDisplayName(slot);
            return
                $"{slotName} — Empty\n\n" +
                "No graft installed in this location.\n\n" +
                "Visit the Fleshmetal Forgemaster in town to install a cyborg implant here.";
        }

        static void AppendStatSection(StringBuilder sb, List<AttributeModifier> mods)
        {
            if (mods == null || mods.Count == 0)
                return;

            sb.AppendLine("STATS");
            foreach (AttributeModifier mod in mods)
            {
                string sign = mod.value >= 0 ? "+" : string.Empty;
                sb.AppendLine($"• {sign}{mod.value} {mod.attribute}");
            }
        }

        static void AppendResistanceSection(StringBuilder sb, List<DamageResistanceModifier> mods)
        {
            if (mods == null || mods.Count == 0)
                return;

            sb.AppendLine("RESISTANCES");
            foreach (DamageResistanceModifier mod in mods)
            {
                string sign = mod.value >= 0 ? "+" : string.Empty;
                sb.AppendLine($"• {sign}{mod.value}% {mod.type}");
            }
        }

        static void AppendPassiveSection(StringBuilder sb, List<PassiveEffect> passives)
        {
            int count = passives?.Count ?? 0;
            sb.AppendLine($"PASSIVES ({count})");
            if (count == 0)
            {
                sb.AppendLine("— none —");
                return;
            }

            foreach (PassiveEffect passive in passives)
            {
                if (passive == null)
                    continue;

                sb.AppendLine($"• {passive.name}");
                if (!string.IsNullOrWhiteSpace(passive.effectDescription))
                    sb.AppendLine(passive.effectDescription.Trim());
            }
        }

        static void AppendActiveSection(StringBuilder sb, List<AbilityAction> actives)
        {
            int count = actives?.Count ?? 0;
            sb.AppendLine($"ACTIVES ({count})");
            if (count == 0)
            {
                sb.AppendLine("— none —");
                return;
            }

            foreach (AbilityAction active in actives)
            {
                if (active == null)
                    continue;

                sb.AppendLine($"• {active.abilityName}");
                if (!string.IsNullOrWhiteSpace(active.description))
                    sb.AppendLine(active.description.Trim());

                string meta = FormatAbilityMeta(active);
                if (!string.IsNullOrEmpty(meta))
                    sb.AppendLine(meta);

                sb.AppendLine("Assign on the ability hotbar to use in combat.");
            }
        }

        static void AppendBenefitSection(StringBuilder sb, List<RacialBenefitDefinition> benefits)
        {
            if (benefits == null || benefits.Count == 0)
                return;

            sb.AppendLine("BENEFITS");
            foreach (RacialBenefitDefinition benefit in benefits)
            {
                if (benefit == null)
                    continue;

                sb.AppendLine($"• {benefit.name}");
            }
        }

        static void AppendRestrictionSection(StringBuilder sb, List<RacialRestrictionDefinition> restrictions)
        {
            if (restrictions == null || restrictions.Count == 0)
                return;

            sb.AppendLine("RESTRICTIONS");
            foreach (RacialRestrictionDefinition restriction in restrictions)
            {
                if (restriction == null)
                    continue;

                sb.AppendLine($"• {restriction.name}");
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
