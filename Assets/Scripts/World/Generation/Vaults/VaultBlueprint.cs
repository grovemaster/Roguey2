using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation.Vaults
{
    public sealed class VaultBlueprint
    {
        public string VaultId { get; set; }
        public int Weight { get; set; } = 1;
        public int MinDistanceFromPlayerStart { get; set; } = 8;
        public Vector2Int Origin { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>Legacy default floor key; also used when MAP uses '.' without an explicit glyph.</summary>
        public string FloorTileKey { get; set; }
        /// <summary>Legacy default wall key; also used when MAP uses 'W' without an explicit glyph.</summary>
        public string WallTileKey { get; set; }
        /// <summary>Legacy default door registry id for 'D' when not bound explicitly.</summary>
        public string DefaultDoorRegistryId { get; set; } = VaultTileGlyph.DefaultDoorRegistryId;

        readonly Dictionary<char, VaultTileGlyph> _glyphs = new Dictionary<char, VaultTileGlyph>();
        readonly List<VaultMapCell> _cells = new List<VaultMapCell>();
        readonly List<VaultItemPlacement> _items = new List<VaultItemPlacement>();
        readonly List<VaultInteractablePlacement> _interactables = new List<VaultInteractablePlacement>();
        readonly List<VaultHazardPlacement> _hazards = new List<VaultHazardPlacement>();
        readonly List<VaultDoorPlacement> _doors = new List<VaultDoorPlacement>();
        readonly List<VaultEnemyPlacement> _enemies = new List<VaultEnemyPlacement>();

        public IReadOnlyDictionary<char, VaultTileGlyph> Glyphs => _glyphs;
        public IReadOnlyList<VaultMapCell> Cells => _cells;
        public IReadOnlyList<VaultItemPlacement> Items => _items;
        public IReadOnlyList<VaultInteractablePlacement> Interactables => _interactables;
        public IReadOnlyList<VaultHazardPlacement> Hazards => _hazards;
        public IReadOnlyList<VaultDoorPlacement> Doors => _doors;
        public IReadOnlyList<VaultEnemyPlacement> Enemies => _enemies;

        public void SetMapDimensions(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public void BindGlyph(char ch, VaultTileGlyph glyph)
        {
            if (ch == ' ')
                return;

            _glyphs[ch] = glyph;
        }

        public bool TryResolveGlyph(char ch, out VaultTileGlyph glyph)
        {
            if (ch == ' ')
            {
                glyph = VaultTileGlyph.Empty;
                return true;
            }

            return _glyphs.TryGetValue(ch, out glyph);
        }

        /// <summary>Applies legacy floor/wall/door defaults and validates MAP glyphs after parsing.</summary>
        public bool FinalizeTileGlyphs(out string error)
        {
            error = null;

            if (!_glyphs.ContainsKey('.') && !string.IsNullOrEmpty(FloorTileKey))
                BindGlyph('.', VaultTileGlyph.Floor(FloorTileKey));

            if (!_glyphs.ContainsKey('W') && !string.IsNullOrEmpty(WallTileKey))
                BindGlyph('W', VaultTileGlyph.Wall(WallTileKey));

            if (!_glyphs.ContainsKey('w') && _glyphs.TryGetValue('W', out VaultTileGlyph wallGlyph))
                BindGlyph('w', wallGlyph);

            string doorFloorKey = FloorTileKey;
            if (string.IsNullOrEmpty(doorFloorKey))
            {
                if (_glyphs.TryGetValue('.', out VaultTileGlyph floorGlyph) && floorGlyph.Kind == VaultCellKind.Floor)
                    doorFloorKey = floorGlyph.TileKey;
            }

            if (!_glyphs.ContainsKey('D') && !string.IsNullOrEmpty(doorFloorKey))
                BindGlyph('D', VaultTileGlyph.Door(doorFloorKey, DefaultDoorRegistryId));

            if (!_glyphs.ContainsKey('d') && _glyphs.TryGetValue('D', out VaultTileGlyph doorGlyph))
                BindGlyph('d', doorGlyph);

            return true;
        }

        public bool TryGetDefaultFloorTileKey(out string tileKey)
        {
            if (_glyphs.TryGetValue('.', out VaultTileGlyph glyph) && glyph.Kind == VaultCellKind.Floor)
            {
                tileKey = glyph.TileKey;
                return !string.IsNullOrEmpty(tileKey);
            }

            if (!string.IsNullOrEmpty(FloorTileKey))
            {
                tileKey = FloorTileKey;
                return true;
            }

            tileKey = null;
            return false;
        }

        public void AddCell(int x, int y, VaultTileGlyph glyph) =>
            _cells.Add(new VaultMapCell(x, y, glyph));

        public void AddItem(string itemId, int x, int y, int quantity = 1) =>
            _items.Add(new VaultItemPlacement(itemId, x, y, quantity));

        public void AddInteractable(string interactableId, int x, int y) =>
            _interactables.Add(new VaultInteractablePlacement(interactableId, x, y));

        public void AddHazard(string hazardId, int x, int y) =>
            _hazards.Add(new VaultHazardPlacement(hazardId, x, y));

        public void AddDoor(string doorId, int x, int y, bool unlocked) =>
            _doors.Add(new VaultDoorPlacement(doorId, x, y, unlocked));

        public void AddEnemy(string enemyId, int x, int y) =>
            _enemies.Add(new VaultEnemyPlacement(enemyId, x, y));

        public Vector3Int LocalToWorld(Vector3Int placementOrigin, int localX, int localY) =>
            placementOrigin + new Vector3Int(localX - Origin.x, localY - Origin.y, 0);

        public IEnumerable<VaultMapCell> OccupiedCells()
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                if (_cells[i].Glyph.Kind != VaultCellKind.Empty)
                    yield return _cells[i];
            }
        }
    }

    public readonly struct VaultMapCell
    {
        public readonly int X;
        public readonly int Y;
        public readonly VaultTileGlyph Glyph;

        public VaultCellKind Kind => Glyph.Kind;

        public VaultMapCell(int x, int y, VaultTileGlyph glyph)
        {
            X = x;
            Y = y;
            Glyph = glyph;
        }
    }

    public readonly struct VaultItemPlacement
    {
        public readonly string ItemId;
        public readonly int X;
        public readonly int Y;
        public readonly int Quantity;

        public VaultItemPlacement(string itemId, int x, int y, int quantity)
        {
            ItemId = itemId;
            X = x;
            Y = y;
            Quantity = quantity;
        }
    }

    public readonly struct VaultInteractablePlacement
    {
        public readonly string InteractableId;
        public readonly int X;
        public readonly int Y;

        public VaultInteractablePlacement(string interactableId, int x, int y)
        {
            InteractableId = interactableId;
            X = x;
            Y = y;
        }
    }

    public readonly struct VaultHazardPlacement
    {
        public readonly string HazardId;
        public readonly int X;
        public readonly int Y;

        public VaultHazardPlacement(string hazardId, int x, int y)
        {
            HazardId = hazardId;
            X = x;
            Y = y;
        }
    }

    public readonly struct VaultDoorPlacement
    {
        public readonly string DoorId;
        public readonly int X;
        public readonly int Y;
        public readonly bool Unlocked;

        public VaultDoorPlacement(string doorId, int x, int y, bool unlocked)
        {
            DoorId = doorId;
            X = x;
            Y = y;
            Unlocked = unlocked;
        }
    }

    public readonly struct VaultEnemyPlacement
    {
        public readonly string EnemyId;
        public readonly int X;
        public readonly int Y;

        public VaultEnemyPlacement(string enemyId, int x, int y)
        {
            EnemyId = enemyId;
            X = x;
            Y = y;
        }
    }
}
