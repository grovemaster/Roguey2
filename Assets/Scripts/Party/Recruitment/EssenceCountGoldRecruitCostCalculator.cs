namespace JRogue.Party.Recruitment
{
  public sealed class EssenceCountGoldRecruitCostCalculator : IRecruitCostCalculator
  {
    public const int BaseGold = 1;
    public const int PerEssenceGold = 1;

    public bool TryCalculate(PartyRecruitDefinition recruit, out int goldCost, out string summary)
    {
      goldCost = 0;
      summary = null;

      if (recruit == null)
        return false;

      goldCost = BaseGold + PerEssenceGold * recruit.EssenceCount;
      summary = $"{goldCost} gold";
      return true;
    }
  }
}
