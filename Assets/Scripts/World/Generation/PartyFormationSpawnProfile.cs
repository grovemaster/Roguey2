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

            offsets = CreateVerticalLineOffsets(memberCount);
            return offsets != null;
        }

        static Vector3Int[] CreateVerticalLineOffsets(int memberCount)
        {
            if (memberCount <= 0)
                return null;

            var offsets = new Vector3Int[memberCount];
            for (int i = 0; i < memberCount; i++)
                offsets[i] = new Vector3Int(0, -i, 0);

            return offsets;
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
            new FormationOffsetSet
            {
                memberCount = 4,
                relativeOffsets = new[]
                {
                    Vector3Int.zero,
                    new Vector3Int(0, -1, 0),
                    new Vector3Int(0, -2, 0),
                    new Vector3Int(0, -3, 0),
                },
            },
        };
    }
}
