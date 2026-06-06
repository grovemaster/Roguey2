using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    [CreateAssetMenu(fileName = "FloorZoneLayout", menuName = "JRogue/World/Dungeon Floor Zone Layout")]
    public sealed class DungeonFloorZoneLayout : ScriptableObject
    {
        [Min(1)] [SerializeField] int floorWidth = 30;
        [Min(1)] [SerializeField] int floorHeight = 30;
        [SerializeField] ZoneLayoutKind layoutKind = ZoneLayoutKind.CompassSlots;
        [SerializeField] ZoneSelectionRule[] selectionRules = new ZoneSelectionRule[0];
        [SerializeField] ZoneLayoutPiece[] pieces = new ZoneLayoutPiece[0];
        [SerializeField] ZoneBoundaryKind defaultOuterBoundary = ZoneBoundaryKind.Wall;
        [SerializeField] string fallbackZoneId = ZoneIds.Rock;
        [SerializeField] DungeonZoneDefinition[] zoneDefinitions = new DungeonZoneDefinition[0];

        public int FloorWidth => floorWidth;
        public int FloorHeight => floorHeight;
        public ZoneLayoutKind LayoutKind => layoutKind;
        public ZoneSelectionRule[] SelectionRules => selectionRules;
        public ZoneLayoutPiece[] Pieces => pieces;
        public ZoneBoundaryKind DefaultOuterBoundary => defaultOuterBoundary;
        public string FallbackZoneId => fallbackZoneId;
        public DungeonZoneDefinition[] ZoneDefinitions => zoneDefinitions;

        public bool TryGetZoneDefinition(string zoneId, out DungeonZoneDefinition definition)
        {
            definition = null;
            if (string.IsNullOrEmpty(zoneId) || zoneDefinitions == null)
                return false;

            for (int i = 0; i < zoneDefinitions.Length; i++)
            {
                DungeonZoneDefinition candidate = zoneDefinitions[i];
                if (candidate == null || candidate.ZoneId != zoneId)
                    continue;

                definition = candidate;
                return true;
            }

            return false;
        }

        public void ReplaceAuthoringData(
            int width,
            int height,
            ZoneLayoutKind kind,
            string fallbackId,
            ZoneSelectionRule[] rules,
            ZoneLayoutPiece[] layoutPieces,
            DungeonZoneDefinition[] definitions = null)
        {
            floorWidth = width;
            floorHeight = height;
            layoutKind = kind;
            fallbackZoneId = fallbackId;
            selectionRules = rules ?? new ZoneSelectionRule[0];
            pieces = layoutPieces ?? new ZoneLayoutPiece[0];
            if (definitions != null)
                zoneDefinitions = definitions;
        }
    }
}
