namespace JRogue.Party.Recruitment
{
  public interface IRecruitCostCalculator
  {
    bool TryCalculate(PartyRecruitDefinition recruit, out int goldCost, out string summary);
  }
}
