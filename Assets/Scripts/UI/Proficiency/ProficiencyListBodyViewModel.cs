using System.Collections.Generic;
using System.Text;
using JRogue.Actors;
using JRogue.Progression.Proficiency;
using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.UI.Proficiency
{
    public sealed class ProficiencyRowViewModel
    {
        public ProficiencyKind Kind;
        public string DisplayName = string.Empty;
        public ProficiencyMenuCategory Category;
        public bool Eligible;
        public int StoredLevel;
        public int Pxp;
        public int TrainingCap;
        public int CharacterLevel;
        public int Aptitude;
        public int XpToNext;
        public string IneligibilityReason = string.Empty;

        public bool IsAboveTrainingCap => Eligible && StoredLevel > TrainingCap;

        public bool IsAtTrainingCap => Eligible && StoredLevel == TrainingCap && StoredLevel < ProficiencyRules.MaxLevel;

        public bool ShowProgressBar =>
            Eligible
            && StoredLevel < ProficiencyRules.MaxLevel
            && StoredLevel < TrainingCap;

        public float ProgressFraction =>
            ShowProgressBar && XpToNext > 0 ? Pxp / (float)XpToNext : 0f;

        public string LevelDisplayText
        {
            get
            {
                if (!Eligible)
                    return "N/A";

                if (IsAboveTrainingCap)
                    return $"{StoredLevel} (cap {TrainingCap})";

                return $"{StoredLevel} / {TrainingCap}";
            }
        }

        public string AptitudeDisplayText =>
            Eligible ? $"{ProficiencyAptitudeFormatter.FormatSigned(Aptitude)} apt" : string.Empty;

        public string PxpHintText =>
            ShowProgressBar ? $"{Pxp} / {XpToNext} pxp" : string.Empty;
    }

    public sealed class ProficiencySheetModel
    {
        public BaseActor Actor;
        public string SummaryLine = string.Empty;
        public string CapWarningLine = string.Empty;
        public IReadOnlyList<ProficiencyRowViewModel> Rows = System.Array.Empty<ProficiencyRowViewModel>();

        public ProficiencyKind ResolveDefaultSelection()
        {
            ProficiencyRowViewModel firstEligible = null;

            foreach (ProficiencyRowViewModel row in Rows)
            {
                if (row.Eligible && row.StoredLevel > 0)
                    return row.Kind;

                if (firstEligible == null && row.Eligible)
                    firstEligible = row;
            }

            if (firstEligible != null)
                return firstEligible.Kind;

            return ProficiencyKind.Fighting;
        }

        public ProficiencyRowViewModel FindRow(ProficiencyKind kind)
        {
            for (int i = 0; i < Rows.Count; i++)
            {
                if (Rows[i].Kind == kind)
                    return Rows[i];
            }

            return null;
        }
    }

    public static class ProficiencyListBodyViewModel
    {
        public static ProficiencySheetModel Build(BaseActor actor)
        {
            if (actor == null)
            {
                return new ProficiencySheetModel
                {
                    Actor = null,
                    SummaryLine = string.Empty,
                    CapWarningLine = string.Empty,
                    Rows = System.Array.Empty<ProficiencyRowViewModel>(),
                };
            }

            CharacterStats stats = actor.stats;
            ProficiencyRuntime runtime = ProficiencyRuntime.EnsureOn(actor.gameObject);
            int trainingCap = stats != null
                ? ProficiencyRules.GetTrainingCap(stats.level)
                : ProficiencyRules.GetTrainingCap(1);

            var rows = new List<ProficiencyRowViewModel>();
            bool anyAboveCap = false;

            foreach (ProficiencyMenuCategory section in ProficiencyCategories.GetAllSections())
            {
                IReadOnlyList<ProficiencyKind> kinds = ProficiencyCategories.GetKindsInCategory(section);
                var sectionRows = new List<ProficiencyRowViewModel>(kinds.Count);

                for (int i = 0; i < kinds.Count; i++)
                {
                    ProficiencyKind kind = kinds[i];
                    ProficiencyRowViewModel row = BuildRow(stats, runtime, kind, trainingCap);
                    sectionRows.Add(row);

                    if (row.IsAboveTrainingCap)
                        anyAboveCap = true;
                }

                sectionRows.Sort(CompareRows);
                rows.AddRange(sectionRows);
            }

            return new ProficiencySheetModel
            {
                Actor = actor,
                SummaryLine = BuildSummaryLine(actor, stats, trainingCap),
                CapWarningLine = anyAboveCap
                    ? "Some proficiencies are above today's training cap — bonuses remain; new levels unlock when character level rises."
                    : string.Empty,
                Rows = rows,
            };
        }

        static ProficiencyRowViewModel BuildRow(
            CharacterStats stats,
            ProficiencyRuntime runtime,
            ProficiencyKind kind,
            int trainingCap)
        {
            bool eligible = stats != null && ProficiencyEligibility.CanTrain(stats, kind);
            int storedLevel = eligible && runtime != null ? runtime.GetLevel(kind) : 0;
            int pxp = eligible && runtime != null ? runtime.GetPxp(kind) : 0;
            int aptitude = stats != null ? ProficiencyAptitudeService.GetAptitude(stats, kind) : 0;
            int xpToNext = ProficiencyRules.GetXpToNextLevel(storedLevel, aptitude);

            return new ProficiencyRowViewModel
            {
                Kind = kind,
                DisplayName = ProficiencyDisplayNames.Get(kind),
                Category = ProficiencyCategories.GetCategory(kind),
                Eligible = eligible,
                StoredLevel = storedLevel,
                Pxp = pxp,
                TrainingCap = trainingCap,
                CharacterLevel = stats != null ? stats.level : 1,
                Aptitude = aptitude,
                XpToNext = xpToNext,
                IneligibilityReason = stats != null
                    ? ProficiencyEligibility.GetIneligibilityReason(stats, kind)
                    : string.Empty,
            };
        }

        static int CompareRows(ProficiencyRowViewModel left, ProficiencyRowViewModel right)
        {
            int levelCompare = right.StoredLevel.CompareTo(left.StoredLevel);
            if (levelCompare != 0)
                return levelCompare;

            return string.Compare(left.DisplayName, right.DisplayName, System.StringComparison.Ordinal);
        }

        static string BuildSummaryLine(BaseActor actor, CharacterStats stats, int trainingCap)
        {
            if (stats == null)
                return actor.DisplayName;

            var sb = new StringBuilder();
            sb.Append(actor.DisplayName);
            sb.Append(" · ");
            sb.Append(stats.race);

            if (stats.race == Race.Human && stats.humanClass != HumanClass.None)
            {
                sb.Append(' ');
                sb.Append(stats.humanClass);
            }

            sb.Append(" · Character level ");
            sb.Append(stats.level);
            sb.Append(" · Training cap ");
            sb.Append(trainingCap);
            sb.Append(" · Max ");
            sb.Append(ProficiencyRules.MaxLevel);
            return sb.ToString();
        }
    }

    public static class ProficiencyDetailFormatter
    {
        public static string BuildTitle(ProficiencyRowViewModel row) =>
            row != null ? $"DETAILS · {row.DisplayName}" : "DETAILS";

        public static string BuildBody(ProficiencyRowViewModel row)
        {
            if (row == null)
                return string.Empty;

            if (!row.Eligible)
            {
                var unavailable = new StringBuilder();
                unavailable.AppendLine("<b>Not available</b> for this character.");
                if (!string.IsNullOrEmpty(row.IneligibilityReason))
                    unavailable.AppendLine(row.IneligibilityReason);
                return unavailable.ToString().TrimEnd();
            }

            var sb = new StringBuilder();
            sb.Append("Level ");
            sb.Append(row.StoredLevel);
            sb.Append(" (stored) · Trainable to ");
            sb.Append(row.TrainingCap);
            sb.Append(" · Absolute max ");
            sb.Append(ProficiencyRules.MaxLevel);
            sb.AppendLine();

            if (row.ShowProgressBar)
            {
                sb.Append("Progress: ");
                sb.Append(row.Pxp);
                sb.Append(" / ");
                sb.Append(row.XpToNext);
                sb.Append(" pxp to level ");
                sb.Append(row.StoredLevel + 1);
                sb.AppendLine();
            }

            sb.Append("Aptitude ");
            sb.Append(ProficiencyAptitudeFormatter.FormatSigned(row.Aptitude));
            sb.Append(" (");
            sb.Append(ProficiencyAptitudeFormatter.GetBlurb(row.Aptitude));
            sb.AppendLine(")");

            if (row.IsAboveTrainingCap)
            {
                sb.AppendLine();
                sb.Append("<color=#C8A045><b>Training paused</b></color> — character level ");
                sb.Append(row.CharacterLevel);
                sb.Append(" caps trainable proficiencies at ");
                sb.Append(row.TrainingCap);
                sb.Append(". Stored level ");
                sb.Append(row.StoredLevel);
                sb.Append(" still applies to combat bonuses.");
                if (row.Pxp > 0)
                {
                    sb.Append(" Banked progress: ");
                    sb.Append(row.Pxp);
                    sb.Append(" pxp.");
                }

                sb.AppendLine();
            }
            else if (row.IsAtTrainingCap && row.Pxp > 0)
            {
                sb.AppendLine();
                sb.Append("Banked progress at training cap: ");
                sb.Append(row.Pxp);
                sb.AppendLine(" pxp.");
            }

            sb.AppendLine();
            sb.Append("Benefits: ");
            sb.Append(ProficiencyBenefitFormatter.GetSummary(row.Kind, row.StoredLevel));
            sb.AppendLine();
            sb.Append("Trained by: ");
            sb.Append(ProficiencyTrainedByFormatter.GetHint(row.Kind));

            return sb.ToString().TrimEnd();
        }
    }
}
