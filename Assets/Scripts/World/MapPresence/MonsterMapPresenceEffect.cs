using UnityEngine;

namespace JRogue.World.MapPresence
{
  public abstract class MonsterMapPresenceEffect : ScriptableObject
  {
    public abstract void Apply(MonsterMapPresenceContext context);

    public abstract void Revert(MonsterMapPresenceContext context);
  }
}
