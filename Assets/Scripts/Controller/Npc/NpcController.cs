using JRogue.Actors;
using JRogue.Dialog;
using UnityEngine;

namespace JRogue.Controller.Npc
{
    public class NpcController : BaseActor, INpcTalkTarget
    {
        [Header("NPC")]
        [SerializeField] string npcId;
        [SerializeField] NpcDialogProfile dialogProfile;
        [SerializeField] PortraitDefinition portrait;

        public BaseActor Actor => this;
        public Vector3Int Cell => GridPosition;
        public NpcDialogProfile DialogProfile => dialogProfile;
        public PortraitDefinition Portrait => portrait;

        protected override void Die()
        {
            Debug.LogWarning($"[Npc] {DisplayName} received fatal damage — NPCs should not die in v0.");
        }

        protected override void OnBump(BaseActor target)
        {
            Debug.Log($"{DisplayName} blocked {target.DisplayName}.");
        }

        public virtual void BeginDialog(BaseActor speaker)
        {
            if (dialogProfile == null)
            {
                Debug.LogWarning($"[Npc] {DisplayName} has no dialog profile.");
                return;
            }

            var session = new NpcDialogSession(speaker, this);
            session.Start();
        }

        public string NpcId => string.IsNullOrWhiteSpace(npcId) ? gameObject.name : npcId.Trim();
    }
}
