using System;
using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation.Vaults
{
    public static class VaultFileParser
    {
        public static bool TryParse(string text, out VaultBlueprint blueprint, out string error)
        {
            blueprint = null;
            error = null;

            if (string.IsNullOrWhiteSpace(text))
            {
                error = "Vault file is empty.";
                return false;
            }

            var lines = new List<string>();
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                lines.Add(line);
            }

            if (lines.Count == 0)
            {
                error = "Vault file has no content.";
                return false;
            }

            blueprint = new VaultBlueprint();
            int lineIndex = 0;
            string first = lines[lineIndex];

            if (!first.StartsWith("VAULT ", StringComparison.OrdinalIgnoreCase))
            {
                error = "Expected VAULT header.";
                return false;
            }

            blueprint.VaultId = first.Substring(6).Trim();
            lineIndex++;

            bool inMap = false;
            var mapRows = new List<string>();

            while (lineIndex < lines.Count)
            {
                string line = lines[lineIndex];
                if (line.Equals("END", StringComparison.OrdinalIgnoreCase))
                    break;

                if (line.Equals("ENDMAP", StringComparison.OrdinalIgnoreCase))
                {
                    if (!blueprint.FinalizeTileGlyphs(out error))
                        return false;

                    if (!TryFinalizeMap(blueprint, mapRows, out error))
                        return false;

                    inMap = false;
                    mapRows.Clear();
                    lineIndex++;
                    continue;
                }

                if (line.Equals("MAP", StringComparison.OrdinalIgnoreCase))
                {
                    inMap = true;
                    mapRows.Clear();
                    lineIndex++;
                    continue;
                }

                if (inMap)
                {
                    mapRows.Add(line);
                    lineIndex++;
                    continue;
                }

                if (!TryParseHeaderLine(blueprint, line, out error))
                    return false;

                lineIndex++;
            }

            if (inMap)
            {
                error = "MAP block missing ENDMAP.";
                return false;
            }

            if (string.IsNullOrEmpty(blueprint.VaultId))
            {
                error = "VAULT id is required.";
                return false;
            }

            if (blueprint.Width <= 0 || blueprint.Height <= 0)
            {
                error = "MAP dimensions are invalid.";
                return false;
            }

            if (!blueprint.FinalizeTileGlyphs(out error))
                return false;

            return true;
        }

        static bool TryParseHeaderLine(VaultBlueprint blueprint, string line, out string error)
        {
            error = null;

            if (line.StartsWith("WEIGHT ", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(line.Substring(7).Trim(), out int weight) || weight < 0)
                {
                    error = $"Invalid WEIGHT: {line}";
                    return false;
                }

                blueprint.Weight = weight;
                return true;
            }

            if (line.StartsWith("MIN_DISTANCE_FROM_PLAYER_START ", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(line.Substring(31).Trim(), out int distance) || distance < 0)
                {
                    error = $"Invalid MIN_DISTANCE_FROM_PLAYER_START: {line}";
                    return false;
                }

                blueprint.MinDistanceFromPlayerStart = distance;
                return true;
            }

            if (line.StartsWith("ORIGIN ", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = line.Substring(7).Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2
                    || !int.TryParse(parts[0], out int ox)
                    || !int.TryParse(parts[1], out int oy))
                {
                    error = $"Invalid ORIGIN: {line}";
                    return false;
                }

                blueprint.Origin = new Vector2Int(ox, oy);
                return true;
            }

            if (line.StartsWith("TILES ", StringComparison.OrdinalIgnoreCase))
                return TryParseTilesLine(blueprint, line.Substring(6).Trim(), out error);

            if (line.StartsWith("TILE ", StringComparison.OrdinalIgnoreCase))
                return TryParseTileLine(blueprint, line.Substring(5).Trim(), out error);

            if (line.StartsWith("ITEM ", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseAtPlacement(line, "ITEM", out error, out string itemId, out int ix, out int iy, out string itemExtra))
                    return false;

                int qty = 1;
                if (!string.IsNullOrEmpty(itemExtra) && (!int.TryParse(itemExtra, out qty) || qty < 1))
                {
                    error = $"Invalid ITEM quantity: {line}";
                    return false;
                }

                blueprint.AddItem(itemId, ix, iy, qty);
                return true;
            }

            if (line.StartsWith("INTERACTABLE ", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseAtPlacement(line, "INTERACTABLE", out error, out string interactableId, out int ux, out int uy, out _))
                    return false;

                blueprint.AddInteractable(interactableId, ux, uy);
                return true;
            }

            if (line.StartsWith("HAZARD ", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseAtPlacement(line, "HAZARD", out error, out string hazardId, out int hx, out int hy, out _))
                    return false;

                blueprint.AddHazard(hazardId, hx, hy);
                return true;
            }

            if (line.StartsWith("DOOR ", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseAtPlacement(line, "DOOR", out error, out string doorId, out int x, out int y, out string flags))
                    return false;

                bool unlocked = !flags.Contains("LOCKED", StringComparison.OrdinalIgnoreCase);
                blueprint.AddDoor(doorId, x, y, unlocked);
                return true;
            }

            if (line.StartsWith("ENEMY ", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseAtPlacement(line, "ENEMY", out error, out string enemyId, out int ex, out int ey, out _))
                    return false;

                blueprint.AddEnemy(enemyId, ex, ey);
                return true;
            }

            error = $"Unknown line: {line}";
            return false;
        }

        /// <summary>
        /// TILES floor=key;wall=key (legacy) or TILES char=key;char2=key2 (per-glyph).
        /// Role aliases: floor→'.', wall→'W', door→'D' (door value is registry id).
        /// </summary>
        static bool TryParseTilesLine(VaultBlueprint blueprint, string payload, out string error)
        {
            error = null;
            string[] pairs = payload.Split(';');
            for (int i = 0; i < pairs.Length; i++)
            {
                string pair = pairs[i].Trim();
                if (pair.Length == 0)
                    continue;

                int eq = pair.IndexOf('=');
                if (eq <= 0)
                {
                    error = $"Invalid TILES pair: {pair}";
                    return false;
                }

                string left = pair.Substring(0, eq).Trim();
                string right = pair.Substring(eq + 1).Trim();
                if (left.Length == 0 || right.Length == 0)
                {
                    error = $"Invalid TILES pair: {pair}";
                    return false;
                }

                if (left.Length == 1)
                {
                    if (!TryBindGlyphKey(blueprint, left[0], right, out error))
                        return false;

                    continue;
                }

                switch (left.ToLowerInvariant())
                {
                    case "floor":
                        blueprint.FloorTileKey = right;
                        break;
                    case "wall":
                        blueprint.WallTileKey = right;
                        break;
                    case "door":
                        blueprint.DefaultDoorRegistryId = right;
                        break;
                    default:
                        error = left.Length > 1
                            ? $"Unknown TILES role '{left}'. Glyph keys must be a single character (e.g. 1={right}; use MAP ..1.2.. not ..{left}..)."
                            : $"Unknown TILES role '{left}'. Use a single MAP character or floor/wall/door.";
                        return false;
                }
            }

            return true;
        }

        /// <summary>TILE &lt;char&gt; floor|wall|door &lt;registryKeyOrDoorId&gt;</summary>
        static bool TryParseTileLine(VaultBlueprint blueprint, string payload, out string error)
        {
            error = null;
            string[] parts = payload.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                error = $"Expected 'TILE <char> floor|wall|door <key>': TILE {payload}";
                return false;
            }

            if (parts[0].Length != 1)
            {
                error = $"TILE character must be exactly one symbol: {parts[0]}";
                return false;
            }

            char ch = parts[0][0];
            string role = parts[1].ToLowerInvariant();
            string value = parts[2];

            switch (role)
            {
                case "floor":
                    blueprint.BindGlyph(ch, VaultTileGlyph.Floor(value));
                    return true;
                case "wall":
                    blueprint.BindGlyph(ch, VaultTileGlyph.Wall(value));
                    return true;
                case "door":
                    string floorKey = blueprint.FloorTileKey;
                    if (blueprint.TryGetDefaultFloorTileKey(out string defaultFloor))
                        floorKey = defaultFloor;

                    if (string.IsNullOrEmpty(floorKey))
                    {
                        error = "TILE door requires a floor tile (set TILES floor= or TILE . floor ... first).";
                        return false;
                    }

                    blueprint.BindGlyph(ch, VaultTileGlyph.Door(floorKey, value));
                    return true;
                default:
                    error = $"Unknown TILE role '{role}'.";
                    return false;
            }
        }

        static bool TryBindGlyphKey(VaultBlueprint blueprint, char ch, string value, out string error)
        {
            error = null;
            if (ch == 'D' || ch == 'd')
            {
                string floorKey = blueprint.FloorTileKey;
                if (blueprint.TryGetDefaultFloorTileKey(out string defaultFloor))
                    floorKey = defaultFloor;

                if (string.IsNullOrEmpty(floorKey))
                {
                    error = "Door glyph requires a floor tile key (TILES floor= or .=...).";
                    return false;
                }

                blueprint.BindGlyph(ch, VaultTileGlyph.Door(floorKey, value));
                return true;
            }

            VaultCellKind kind = InferKindFromExistingGlyphs(blueprint, ch);
            if (kind == VaultCellKind.Wall)
            {
                blueprint.BindGlyph(ch, VaultTileGlyph.Wall(value));
                return true;
            }

            blueprint.BindGlyph(ch, VaultTileGlyph.Floor(value));
            return true;
        }

        static VaultCellKind InferKindFromExistingGlyphs(VaultBlueprint blueprint, char ch)
        {
            if (ch == 'W' || ch == 'w')
                return VaultCellKind.Wall;

            if (ch == 'D' || ch == 'd')
                return VaultCellKind.Door;

            if (blueprint.Glyphs.TryGetValue(ch, out VaultTileGlyph existing))
                return existing.Kind;

            return VaultCellKind.Floor;
        }

        static bool TryParseAtPlacement(
            string line,
            string keyword,
            out string error,
            out string id,
            out int x,
            out int y,
            out string extra)
        {
            error = null;
            id = null;
            x = 0;
            y = 0;
            extra = null;

            string rest = line.Substring(keyword.Length).Trim();
            int atIndex = rest.IndexOf(" AT ", StringComparison.OrdinalIgnoreCase);
            if (atIndex <= 0)
            {
                error = $"Expected '{keyword} <id> AT <x> <y>': {line}";
                return false;
            }

            id = rest.Substring(0, atIndex).Trim();
            string tail = rest.Substring(atIndex + 4).Trim();
            string[] parts = tail.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2
                || !int.TryParse(parts[0], out x)
                || !int.TryParse(parts[1], out y))
            {
                error = $"Invalid AT coordinates: {line}";
                return false;
            }

            if (parts.Length > 2)
                extra = string.Join(" ", parts, 2, parts.Length - 2);

            return true;
        }

        static bool TryFinalizeMap(VaultBlueprint blueprint, List<string> mapRows, out string error)
        {
            error = null;
            if (mapRows.Count == 0)
            {
                error = "MAP block is empty.";
                return false;
            }

            int width = mapRows[0].Length;
            for (int i = 1; i < mapRows.Count; i++)
            {
                if (mapRows[i].Length != width)
                {
                    error = $"MAP rows must share width (row {i} differs).";
                    return false;
                }
            }

            int height = mapRows.Count;
            blueprint.SetMapDimensions(width, height);

            for (int row = 0; row < height; row++)
            {
                string rowText = mapRows[row];
                int localY = height - 1 - row;
                for (int x = 0; x < width; x++)
                {
                    char ch = rowText[x];
                    if (!blueprint.TryResolveGlyph(ch, out VaultTileGlyph glyph))
                    {
                        error = $"Unbound MAP character '{ch}' at ({x},{localY}). Add TILES/TILE entry.";
                        return false;
                    }

                    if (glyph.Kind == VaultCellKind.Empty)
                        continue;

                    blueprint.AddCell(x, localY, glyph);
                }
            }

            return true;
        }
    }
}
