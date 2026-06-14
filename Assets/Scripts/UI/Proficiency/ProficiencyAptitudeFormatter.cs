namespace JRogue.UI.Proficiency
{
    public static class ProficiencyAptitudeFormatter
    {
        public static string FormatSigned(int aptitude) =>
            aptitude >= 0 ? $"+{aptitude}" : aptitude.ToString();

        public static string GetBlurb(int aptitude) =>
            aptitude switch
            {
                >= 4 => "learns much faster",
                3 => "learns significantly faster",
                2 => "learns twice as fast",
                1 => "learns faster",
                0 => "normal learning speed",
                -1 => "learns slower",
                -2 => "learns half as fast",
                -3 => "learns much slower",
                <= -4 => "learns very slowly",
            };
    }
}
