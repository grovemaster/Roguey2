using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Racial;

namespace JRogue.Controller.Npc
{
    public sealed class HumanKnightDrillMasterNpcController : NpcController
    {
        public override void BeginDialog(BaseActor speaker)
        {
            var session = new HumanKnightDrillMasterDialogSession(speaker, this);
            session.Start();
        }
    }
}
