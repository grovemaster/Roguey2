using UnityEngine;

namespace JRogue.World.MapPresence
{
  [CreateAssetMenu(
    fileName = "MonsterMapPresenceProfile",
    menuName = "JRogue/World/Monster Map Presence Profile")]
  public sealed class MonsterMapPresenceProfile : ScriptableObject
  {
    public string displayName = "Map Presence";

    [Tooltip("Applied when the host binds; reverted on death unless an effect is permanent (future).")]
    public MonsterMapPresenceEffect[] effects = System.Array.Empty<MonsterMapPresenceEffect>();

    [Tooltip("Future: effects marked permanent are not reverted when the host dies.")]
    public bool permanentOnSpawn;
  }
}
