using JRogue.Traps;
using UnityEngine;

namespace JRogue.World.MapPresence
{
  [CreateAssetMenu(
    fileName = "TrapWhileAliveMapEffect",
    menuName = "JRogue/World/Map Presence/Trap While Alive")]
  public sealed class TrapWhileAliveMapEffect : MonsterMapPresenceEffect
  {
    public Vector3Int cell;
    public TrapDefinition trapDefinition;
    public string logTag = "pit";

    public override void Apply(MonsterMapPresenceContext context)
    {
      if (trapDefinition == null)
      {
        Debug.LogWarning($"{MonsterMapPresenceService.LogPrefix} TrapWhileAlive has no TrapDefinition.");
        return;
      }

      TrapService traps = TrapService.Instance;
      if (traps == null)
      {
        Debug.LogWarning($"{MonsterMapPresenceService.LogPrefix} TrapWhileAlive: no TrapService.");
        return;
      }

      if (traps.IsFloorTrapAt(cell))
      {
        Debug.LogWarning(
          $"{MonsterMapPresenceService.LogPrefix} TrapWhileAlive ({logTag}): floor trap already at {cell}; skip.");
        return;
      }

      traps.Register(cell, trapDefinition);
      Debug.Log(
        $"{MonsterMapPresenceService.LogPrefix} Apply TrapWhileAlive ({logTag}) at {cell} for '{context.LogLabel}'.");

      Vector3Int capturedCell = cell;
      context.RegisterRevert(() =>
      {
        if (TrapService.Instance != null && TrapService.Instance.TryUnregisterFloorTrap(capturedCell))
        {
          Debug.Log(
            $"{MonsterMapPresenceService.LogPrefix} Revert TrapWhileAlive ({logTag}) at {capturedCell} for '{context.LogLabel}'.");
        }
      });
    }

    public override void Revert(MonsterMapPresenceContext context)
    {
      // Revert stack runs from context.RevertAll(); per-effect Revert is a no-op.
    }
  }
}
