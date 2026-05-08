using JRogue.Actors;
using JRogue.Manager.Map;
using UnityEngine;

namespace JRogue.Service.Sensing
{
    /// <summary>
    /// Push-based noise propagation. Any actor that produces noise calls
    /// <see cref="Broadcast"/>; this service computes the effective volume
    /// at every listener and notifies those that hear it.
    /// </summary>
    public static class AcousticsService
    {
        public static void Broadcast(BaseActor source, int volume)
        {
            if (source == null || volume <= 0) return;
            Broadcast(source, source.GridPosition, volume);
        }

        public static void Broadcast(BaseActor source, Vector3Int origin, int volume)
        {
            if (volume <= 0) return;

            MapManager map = MapManager.Instance != null
                ? MapManager.Instance
                : Object.FindAnyObjectByType<MapManager>();

            BaseActor[] listeners = Object.FindObjectsByType<BaseActor>(FindObjectsInactive.Exclude);
            foreach (BaseActor listener in listeners)
            {
                if (listener == null) continue;
                if (source != null && listener.gameObject == source.gameObject) continue;

                int effective = HearingUtility.CalculateEffectiveVolume(
                    origin, listener.GridPosition, volume, map);

                if (effective > 0)
                {
                    listener.OnHearNoise(source, origin, volume, effective);
                }
            }
        }
    }
}
