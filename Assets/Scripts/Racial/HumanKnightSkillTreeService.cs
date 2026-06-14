using JRogue.Actors;
using JRogue.World.Generation;

namespace JRogue.Racial
{
    /// <summary>
    /// Safe-zone gate for Human Knight skill point spending from UI.
    /// </summary>
    public static class HumanKnightSkillTreeService
    {
        public static bool TrySpendPoint(BaseActor actor, string nodeId, out string failureReason)
        {
            failureReason = null;
            if (actor == null)
            {
                failureReason = "No actor.";
                return false;
            }

            if (!SafeZonePolicyService.TryAllowHumanKnightSkillSpend(out failureReason))
                return false;

            HumanClassSkillTreeRuntime runtime = actor.GetComponent<HumanClassSkillTreeRuntime>();
            if (runtime == null)
            {
                failureReason = "No Human class skill tree runtime.";
                return false;
            }

            return runtime.TrySpendPoint(nodeId, out failureReason);
        }
    }
}
