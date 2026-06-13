using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Racial;
using UnityEngine;

namespace JRogue.Controller.Npc
{
    public sealed class TieflingForgemasterNpcController : NpcController
    {
        [SerializeField] TieflingForgemasterDefinition forgemasterCatalog;

        public TieflingForgemasterDefinition ForgemasterCatalog =>
            forgemasterCatalog != null
                ? forgemasterCatalog
                : TieflingImplantForgemasterService.DefaultCatalog;

        public override void BeginDialog(BaseActor speaker)
        {
            var session = new TieflingForgemasterDialogSession(speaker, this, ForgemasterCatalog);
            session.Start();
        }
    }
}
