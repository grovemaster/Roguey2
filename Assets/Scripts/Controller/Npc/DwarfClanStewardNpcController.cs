using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Racial;

namespace JRogue.Controller.Npc
{
    public sealed class DwarfClanStewardNpcController : NpcController
    {
        public DwarfClanDefinition clan;

        public override void BeginDialog(BaseActor speaker)
        {
            var session = new DwarfClanJoinDialogSession(speaker, this, clan);
            session.Start();
        }
    }
}
