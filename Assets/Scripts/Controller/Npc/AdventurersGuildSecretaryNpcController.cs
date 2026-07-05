using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Manager.Party;
using JRogue.Organizations;
using UnityEngine;

namespace JRogue.Controller.Npc
{
    public sealed class AdventurersGuildSecretaryNpcController : NpcController
    {
        [Header("Guild")]
        [SerializeField] OrganizationDefinition organization;

        BaseActor _interactionSpeaker;

        new void Awake()
        {
            if (organization == null)
                organization = OrganizationDefinition.LoadAdventurersGuild();
        }

        public override void BeginDialog(BaseActor speaker)
        {
            if (organization == null)
            {
                Debug.LogWarning($"[GuildSecretary] {DisplayName} has no organization definition.");
                return;
            }

            _interactionSpeaker = speaker;
            PartyManager party = PartyManager.Instance;
            var session = new AdventurersGuildSecretaryDialogSession(speaker, this, organization, party);
            session.Start();
        }
    }
}
