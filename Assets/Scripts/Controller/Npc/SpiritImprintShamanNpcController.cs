using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Racial;
using UnityEngine;

namespace JRogue.Controller.Npc
{
    public sealed class SpiritImprintShamanNpcController : NpcController
    {
        public override void BeginDialog(BaseActor speaker)
        {
            var session = new SpiritImprintShamanDialogSession(speaker, this);
            session.Start();
        }
    }
}
