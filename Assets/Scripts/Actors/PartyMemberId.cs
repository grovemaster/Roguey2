using JRogue.Actors;
using UnityEngine;

namespace JRogue.Quest
{
    /// <summary>Stable roster key for character-bound quest objectives (e.g. BarbarianWarrior).</summary>
    public sealed class PartyMemberId : MonoBehaviour
    {
        [SerializeField] string memberId;

        public string MemberId => string.IsNullOrWhiteSpace(memberId) ? string.Empty : memberId.Trim();

        public void ConfigureMemberId(string id) => memberId = id ?? string.Empty;

        public static string GetMemberId(BaseActor actor)
        {
            if (actor == null)
                return string.Empty;

            PartyMemberId marker = actor.GetComponent<PartyMemberId>();
            return marker != null ? marker.MemberId : string.Empty;
        }
    }
}
