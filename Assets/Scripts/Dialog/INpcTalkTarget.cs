using JRogue.Actors;
using UnityEngine;

namespace JRogue.Dialog
{
    public interface INpcTalkTarget
    {
        BaseActor Actor { get; }
        Vector3Int Cell { get; }
        NpcDialogProfile DialogProfile { get; }
        PortraitDefinition Portrait { get; }
        void BeginDialog(BaseActor speaker);
    }
}
