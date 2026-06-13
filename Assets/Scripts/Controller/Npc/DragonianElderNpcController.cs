using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Racial;
using UnityEngine;

namespace JRogue.Controller.Npc
{
    public sealed class DragonianElderNpcController : NpcController
    {
        [SerializeField] DragonianElderDefinition elderDefinition;

        public DragonianElderDefinition ElderDefinition =>
            DragonianElderRegistry.Resolve(elderDefinition, NpcId);

        public override void BeginDialog(BaseActor speaker)
        {
            var session = new DragonianElderDialogSession(speaker, this, ElderDefinition);
            session.Start();
        }
    }
}
