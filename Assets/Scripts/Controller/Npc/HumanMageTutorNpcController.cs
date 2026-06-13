using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Racial;

namespace JRogue.Controller.Npc
{
    public sealed class HumanMageTutorNpcController : NpcController
    {
        public override void BeginDialog(BaseActor speaker)
        {
            var session = new HumanMageTutorDialogSession(speaker, this);
            session.Start();
        }
    }
}
