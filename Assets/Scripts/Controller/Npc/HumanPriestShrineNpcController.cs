using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Racial;

namespace JRogue.Controller.Npc
{
    public sealed class HumanPriestShrineNpcController : NpcController
    {
        public override void BeginDialog(BaseActor speaker)
        {
            var session = new HumanPriestShrineDialogSession(speaker, this);
            session.Start();
        }
    }
}
