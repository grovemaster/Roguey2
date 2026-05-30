using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.MapPresence
{
  public sealed class MonsterMapPresenceService : MonoBehaviour
  {
    public const string LogPrefix = "[MapPresence]";

    public static MonsterMapPresenceService Instance { get; private set; }

    readonly List<MonsterMapPresenceHost> _hosts = new List<MonsterMapPresenceHost>();

    public int ActiveHostCount => _hosts.Count;

    void Awake()
    {
      if (Instance == null)
        Instance = this;
      else if (Instance != this)
      {
        Destroy(gameObject);
        return;
      }
    }

    void OnDestroy()
    {
      if (Instance == this)
        Instance = null;
    }

    public void NotifyBound(MonsterMapPresenceHost host)
    {
      if (host == null || _hosts.Contains(host))
        return;

      _hosts.Add(host);
      Debug.Log($"{LogPrefix} Host bound: {host.gameObject.name} (active hosts: {_hosts.Count}).");
    }

    public void NotifyUnbound(MonsterMapPresenceHost host)
    {
      if (host == null)
        return;

      _hosts.Remove(host);
      Debug.Log($"{LogPrefix} Host unbound: {host.gameObject.name} (active hosts: {_hosts.Count}).");
    }
  }
}
