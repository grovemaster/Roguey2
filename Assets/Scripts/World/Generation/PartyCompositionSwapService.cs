#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Manager.Grid;
using JRogue.Manager.Party;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JRogue.World.Generation
{
    public static class PartyCompositionSwapService
    {
        public static bool TryApplyPreset(PartyCompositionPreset preset, out string reason)
        {
            reason = null;
            string[] paths = PartyCompositionPresets.GetPrefabPaths(preset);
            if (paths == null || paths.Length == 0)
            {
                reason = $"Unknown preset {preset}.";
                return false;
            }

            GameObject[] prefabs = LoadPrefabs(paths, out string missingPath);
            if (prefabs == null)
            {
                reason = $"Missing prefab at {missingPath}. Create it or fix the path, then retry.";
                return false;
            }

            int bootstrapCount = ApplyToBootstraps(prefabs);
            if (bootstrapCount == 0 && !Application.isPlaying)
            {
                reason = "No DungeonRunBootstrap found in loaded scenes. Open TownTest or DungeonFloorTest first.";
                return false;
            }

            if (Application.isPlaying)
            {
                if (!TryRebuildLiveParty(prefabs, out reason))
                    return false;
            }

            string mode = Application.isPlaying ? "live party and bootstrap" : "bootstrap";
            Debug.Log(
                $"[PartyComposition] Applied roster \"{PartyCompositionPresets.GetDisplayName(preset)}\" to {bootstrapCount} bootstrap(s) ({mode}).");
            return true;
        }

        static GameObject[] LoadPrefabs(string[] paths, out string missingPath)
        {
            missingPath = null;
            var loaded = new GameObject[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
#if UNITY_EDITOR
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
#else
                GameObject prefab = null;
#endif
                if (prefab == null)
                {
                    missingPath = paths[i];
                    return null;
                }

                loaded[i] = prefab;
            }

            return loaded;
        }

        static int ApplyToBootstraps(GameObject[] prefabs)
        {
            DungeonRunBootstrap[] bootstraps = Object.FindObjectsByType<DungeonRunBootstrap>(
                FindObjectsInactive.Include);

            for (int i = 0; i < bootstraps.Length; i++)
                bootstraps[i].DevSetPartyMemberPrefabs(prefabs);

            return bootstraps.Length;
        }

        static bool TryRebuildLiveParty(GameObject[] prefabs, out string reason)
        {
            reason = null;
            PartyManager party = PartyManager.Instance;
            if (party == null)
            {
                reason = "No PartyManager in the scene.";
                return false;
            }

            DungeonRunBootstrap bootstrap = Object.FindAnyObjectByType<DungeonRunBootstrap>();
            Transform parent = bootstrap != null && bootstrap.PartyContainer != null
                ? bootstrap.PartyContainer
                : party.transform;

            Vector3Int anchor = ResolveSpawnAnchor(party);
            DestroyCurrentMembers(party);

            var members = new List<BaseActor>(prefabs.Length);
            for (int i = 0; i < prefabs.Length; i++)
            {
                GameObject prefab = prefabs[i];
                if (prefab == null)
                    continue;

                GameObject instance = Object.Instantiate(prefab, parent);
                GridMover mover = instance.GetComponent<GridMover>();
                if (mover != null)
                    mover.enabled = false;

                BaseActor actor = instance.GetComponent<BaseActor>();
                if (actor != null)
                    members.Add(actor);
            }

            if (members.Count == 0)
            {
                reason = "No party members were instantiated from the preset prefabs.";
                return false;
            }

            party.partyMembers = members;
            party.DevPrepareForRosterRebuild();

            if (!PartySpawnService.TrySpawnFormationAtAnchor(anchor, null, out _))
            {
                for (int i = 0; i < members.Count; i++)
                {
                    GridMover mover = members[i].GetComponent<GridMover>();
                    if (mover == null)
                        continue;

                    mover.InitializeAtGridAnchor(anchor + new Vector3Int(0, -i, 0));
                    mover.enabled = true;
                }

                party.SnapHistoryToCurrentPositions();
                party.InitializeRosterAfterDeferredSpawn();
                party.RefreshCameraFollow();
            }

            if (party.MainCharacter == null && members.Count > 0)
                party.TryDesignateMainCharacter(members[0]);

            return true;
        }

        static Vector3Int ResolveSpawnAnchor(PartyManager party)
        {
            BaseActor active = party.GetActiveMember();
            if (active != null)
                return active.GridPosition;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member != null)
                    return member.GridPosition;
            }

            return Vector3Int.zero;
        }

        static void DestroyCurrentMembers(PartyManager party)
        {
            GridManager grid = GridManager.Instance;
            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null)
                    continue;

                if (grid != null)
                    grid.UnregisterActor(member.GridPosition);

                Object.Destroy(member.gameObject);
            }

            party.partyMembers.Clear();
        }
    }
}
#endif
