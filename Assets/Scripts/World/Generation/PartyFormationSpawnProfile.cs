using System;
using UnityEngine;

namespace JRogue.World.Generation
{
    [CreateAssetMenu(fileName = "PartyFormationSpawnProfile", menuName = "JRogue/World/Party Formation Spawn Profile")]
    public sealed class PartyFormationSpawnProfile : ScriptableObject
    {
        [SerializeField] FormationOffsetSet[] layouts = Array.Empty<FormationOffsetSet>();

        public bool TryGetOffsetsForCount(int memberCount, out Vector3Int[] offsets)
        {
            offsets = null;
            if (memberCount <= 0 || layouts == null || layouts.Length == 0)
                return false;

            for (int i = 0; i < layouts.Length; i++)
            {
                FormationOffsetSet set = layouts[i];
                if (set.memberCount != memberCount)
                    continue;

                offsets = set.relativeOffsets;
                return offsets != null && offsets.Length >= memberCount;
            }

            if (layouts.Length > 0 && layouts[0].relativeOffsets != null)
            {
                offsets = layouts[0].relativeOffsets;
                return true;
            }

            return false;
        }

        [Serializable]
        public struct FormationOffsetSet
        {
            [Range(1, 6)] public int memberCount;
            public Vector3Int[] relativeOffsets;
        }

        public void EnsureDefaultLayouts()
        {
            if (layouts != null && layouts.Length > 0)
                return;

            layouts = CreateDefaultLayoutSets();
        }

        public static PartyFormationSpawnProfile CreateDefaultRuntime()
        {
            var profile = CreateInstance<PartyFormationSpawnProfile>();
            profile.layouts = CreateDefaultLayoutSets();
            return profile;
        }

        static FormationOffsetSet[] CreateDefaultLayoutSets() => new FormationOffsetSet[]
        {
            new FormationOffsetSet { memberCount = 1, relativeOffsets = new[] { Vector3Int.zero } },
            new FormationOffsetSet
            {
                memberCount = 2,
                relativeOffsets = new[] { Vector3Int.zero, new Vector3Int(0, -1, 0) },
            },
            new FormationOffsetSet
            {
                memberCount = 3,
                relativeOffsets = new[]
                {
                    Vector3Int.zero,
                    new Vector3Int(0, -1, 0),
                    new Vector3Int(1, 0, 0),
                },
            },
        };
    }
}
