using JRogue.Actors;

namespace JRogue.Organizations
{
    public interface IOrganizationRankScoreContributor
    {
        string OrganizationId { get; }
        int Contribute(BaseActor actor);
    }
}
