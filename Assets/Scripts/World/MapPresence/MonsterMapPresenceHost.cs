using JRogue.Controller.Enemy;
using JRogue.Data.Enemy;
using UnityEngine;

namespace JRogue.World.MapPresence
{
  public sealed class MonsterMapPresenceHost : MonoBehaviour
  {
    [SerializeField] MonsterMapPresenceProfile profileOverride;
    [SerializeField] EnemyController enemy;

    MonsterMapPresenceContext _context;
    bool _bound;

    void Awake()
    {
      if (enemy == null)
        enemy = GetComponent<EnemyController>();
    }

    void Start() => Bind();

    void OnDestroy()
    {
      if (_bound)
        Unbind();
    }

    public void Bind()
    {
      if (_bound)
        return;

      MonsterMapPresenceProfile profile = ResolveProfile();
      if (profile == null || profile.effects == null || profile.effects.Length == 0)
        return;

      if (enemy == null)
        enemy = GetComponent<EnemyController>();

      _context = new MonsterMapPresenceContext(enemy, profile);
      Debug.Log(
        $"{MonsterMapPresenceService.LogPrefix} Binding '{_context.LogLabel}' with profile '{profile.displayName}'.");

      MonsterMapPresenceService.Instance?.NotifyBound(this);

      for (int i = 0; i < profile.effects.Length; i++)
      {
        MonsterMapPresenceEffect effect = profile.effects[i];
        if (effect != null)
          effect.Apply(_context);
      }

      _bound = true;
      Debug.Log($"{MonsterMapPresenceService.LogPrefix} Bind complete for '{_context.LogLabel}'.");
    }

    public void Unbind()
    {
      if (!_bound)
        return;

      string label = _context?.LogLabel ?? gameObject.name;
      Debug.Log($"{MonsterMapPresenceService.LogPrefix} Unbinding '{label}'.");

      _context?.RevertAll();
      MonsterMapPresenceService.Instance?.NotifyUnbound(this);

      _context = null;
      _bound = false;
      Debug.Log($"{MonsterMapPresenceService.LogPrefix} Unbind complete for '{label}'.");
    }

    MonsterMapPresenceProfile ResolveProfile()
    {
      if (profileOverride != null)
        return profileOverride;

      if (enemy == null)
        enemy = GetComponent<EnemyController>();

      if (enemy != null && enemy.Species != null && enemy.Species.mapPresenceProfileAsset != null)
        return enemy.Species.mapPresenceProfileAsset as MonsterMapPresenceProfile;

      return null;
    }
  }
}
