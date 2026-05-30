using System;
using System.Collections.Generic;
using JRogue.Controller.Enemy;

namespace JRogue.World.MapPresence
{
  public sealed class MonsterMapPresenceContext
  {
    readonly List<Action> _revertActions = new List<Action>();

    public MonsterMapPresenceContext(EnemyController owner, MonsterMapPresenceProfile profile)
    {
      Owner = owner;
      Profile = profile;
    }

    public EnemyController Owner { get; }

    public MonsterMapPresenceProfile Profile { get; }

    public string LogLabel =>
      Owner != null ? Owner.DisplayName : Profile != null ? Profile.displayName : "Unknown";

    public void RegisterRevert(Action revert)
    {
      if (revert != null)
        _revertActions.Add(revert);
    }

    public void RevertAll()
    {
      for (int i = _revertActions.Count - 1; i >= 0; i--)
        _revertActions[i]?.Invoke();

      _revertActions.Clear();
    }
  }
}
