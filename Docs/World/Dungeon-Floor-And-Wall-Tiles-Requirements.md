# Dungeon floor & wall tiles — Multi-tile zones (DCSS-style variation)

**Status:** **Implemented (v1a).** Zones paint from weighted **`DungeonTilePalette`** assets per habitat; legacy single `floorTile` / `wallTile` still work as fallbacks. Run **JRogue → Dungeon → Create Tile Palettes** and **Create Floor 1/3 Zone Pack** (or batch `DungeonEditorBatchRunner.CreateTilePalettesAndZonePacks`) after pulling.

**Purpose:** Specify **visual tile variation** for dungeon floors — each **zone** (and optionally each **floor**) chooses from **eligible sets** of floor and wall tiles, with set size varying by zone and floor. Inspired by *Dungeon Crawl Stone Soup*, where a branch level uses many floor/wall feature tiles for texture while remaining one logical terrain type.

**Depends on:** [Dynamic dungeon floor generation](Dynamic-Dungeon-Floor-Generation-Requirements.md) (`DungeonFloorDefinition`, `LayoutStampPhase`, `MapManager.SetCellFloor` / `SetCellWall`), [Dungeon zone layout](Dungeon-Zone-Layout-Requirements.md) (`DungeonZoneDefinition`, `ZoneFillPhase`, `ZoneTilePainter`), [Vaults §9](Dynamic-Dungeon-Floor-Generation-Requirements.md) (`VaultAssetRegistry`, `.vault` `TILES` lines), [Town building facades](Town-Building-Entry-And-Exit-Requirements.md) (separate per-cell override system — §2.3).

**Related:** [Dungeon zone population](Dungeon-Zone-Population-Requirements.md) (population is orthogonal to tile appearance). [Door requirements](Door-Requirements.md) (door tiles are explicit glyphs, not palette picks).

**Explicitly out of scope (v1):** Animated tiles; slope / height tiles; autotiling rule engines (Wang / blob); per-tile gameplay terrain flags (lava vs floor — hazards use overlay layers); town plaza multi-tile (town uses stamp + facade overlays); runtime tile swaps on revisit; downloading new art packs (authoring assumes existing Kenney / Scavengers sheets).

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **DCSS-like visual richness** — snow, sand, stone, and dungeon zones show **multiple floor and wall sprites** on one logical terrain, not a flat repeating single tile. |
| **G2** | **Zone-scoped palettes** — each `DungeonZoneDefinition` declares its own **eligible floor set** and **eligible wall set**; set lengths may differ between zones and between floor/wall on the same zone. |
| **G3** | **Floor-scoped defaults** — `DungeonFloorDefinition` may supply **fallback palettes** when a zone omits a set; floor-wide `floorTile` / `wallTile` remain valid as **1-entry palettes** for backward compatibility. |
| **G4** | **Deterministic per run** — weighted picks at paint time are **stable on first visit** and unchanged on revisit (same persistence rules as topology — parent doc §1.3). |
| **G5** | **Vault compatibility** — authored vault glyphs keep **exact** tiles; proc fill around vaults uses zone palettes without fighting vault stamps. |
| **G6** | **Shared asset keys** — reuse **`Theme:Index`** string keys from `VaultAssetRegistry` where possible so vault authors and zone authors reference the same `TileBase` assets. |
| **G7** | **Performance** — palette resolution is O(1) per cell at paint time; no per-frame tile churn. |

---

## 2. Glossary

| Term | Meaning |
|------|---------|
| **Logical terrain** | Gameplay category: walkable floor, blocking wall, door, hazard overlay. Tile **appearance** does not change walkability. |
| **Tile palette** | Ordered list of eligible `TileBase` assets (or registry keys) for one layer (floor or wall). |
| **Palette entry** | One tile + **weight** (default 1) for weighted selection at paint time. |
| **Theme** | Named group of tiles from one sprite sheet family, e.g. `SnowTheme`, `SandTheme`, `DungeonStone`. Matches vault `TILES` naming. |
| **Zone tile profile** | Floor + wall palettes bound to a **`zoneId`** via `DungeonZoneDefinition`. |
| **Floor tile profile** | Optional defaults on `DungeonFloorDefinition` used when zone profile is missing or partial. |
| **Tile pick** | The chosen `TileBase` for one cell at generation time, stored implicitly in the painted tilemap. |
| **Variation mode** | Algorithm that maps `(cell, seed, palette)` → pick index (§5). |

### 2.1 — DCSS comparison (informative)

DCSS and JRogue both use **multiple cosmetic sprites** per logical floor/wall, picked **once per cell** when the level is built and then stored for that level. DCSS is the reference for **weighted variation** and **fixed picks**; JRogue adds **zone palettes** because most dungeon floors here are **multi-habitat** (`ZoneComposite`), not single-theme branch levels.

| Aspect | DCSS (proc gen, excluding vaults) | JRogue (this doc) |
|--------|-----------------------------------|-------------------|
| **Eligible tile set** | All variants under one **tile enum** in rltiles (e.g. `FLOOR_SAND_STONE` + unnamed sibling images) | **`DungeonTilePalette`** entries on **`DungeonZoneDefinition`** |
| **Scope of set** | Usually **one branch/depth theme** for the whole level | **Per zone** on the same floor (snow north, desert east, dungeon center) |
| **Default pick** | **Weighted** via `pick_dngn_tile()` and `%weight` in tiledef ([rltiles guide](https://crawl.develz.org/wiki/doku.php?id=dcss%3Ahelp%3Arltiles)) | **`WeightedRandom` at paint time** (locked default v1) |
| **Determinism** | Hash from birth time, branch, depth, **x, y** → stored in `tile_env.flv` | Hash from **run seed**, floor id, zone id, **x, y**, layer → stored in **tilemap** |
| **Floor vs wall** | Separate hash salts for floor and wall picks | Separate **`layerSalt`** (or equivalent) for floor vs wall |
| **Gameplay terrain** | `dungeon_feature_type` per cell | Stamp / zone fill topology (walkable vs wall) |

**JRogue extension — zone palettes:** DCSS rarely mixes multiple floor **themes** on one proc-generated level; branch defaults (`default_flavour.floor` / `.wall`) cover most cells. JRogue’s [zone layout](Dungeon-Zone-Layout-Requirements.md) intentionally places **several habitats on one floor** for most (if not all) dungeon floors. **Zone-scoped palettes** are therefore a JRogue requirement, not a DCSS clone: each zone chooses its own eligible floor and wall sets, with sizes that may differ per zone and per layer.

**What we adopt from DCSS:** cosmetic multi-sprite floors/walls; **weighted** picks (rarer variants use lower `weight`); pick **once at generation**; separate floor and wall rolls.

**What we do not adopt v1:** DCSS feature enums, branch-wide `LFLOORTILE`, `%variation` colour plumbing, domino/neighbour floor correlation, or depth-dependent wall **type** tables (Depths brick sets). Those may be follow-ups.

### 2.2 — Current implementation (as-is)

| Layer | Today | Limitation |
|-------|-------|------------|
| **`DungeonZoneDefinition`** | `floorTile`, `wallTile` — single `TileBase` each | Entire zone uses one floor sprite and one wall sprite |
| **`DungeonFloorDefinition`** | `floorTile`, `wallTile` — single fallback | Pre-baked stamp floors: one theme for whole map |
| **`ZoneTilePainter`** | `ResolveFloorTile` / `ResolveWallTile` return one tile per zone | No per-cell variation |
| **`ZonePieceFiller` / `ZoneSolidPainter`** | Call `ZoneTilePainter.PaintFloor/Wall` per cell | Same tile every cell in zone |
| **`LayoutStampPhase`** | `PaintLayoutStamp` → one `wallPaintTile` / `floorPaintTile` | Monolithic stamp |
| **`VaultAssetRegistry`** | Keys like `SnowTheme:32`, `SandTheme:50` | Used by **vaults only**; zone pack wires one snow floor + one snow wall |
| **Town facades** | `TownBuildingFacadeOverlay` per-cell overrides | Building-specific; not zone proc fill |

**Example today:** `Zone_Snow` uses `SnowTheme_32` floor and `SnowTheme_48` wall for **every** snow cell. The `SnowTheme.png` sheet contains **additional** floor indices (e.g. `:40`, `:41` in vault tests) that are never used in proc fill.

### 2.3 — Related systems (do not conflate)

| System | Scope | Purpose |
|--------|-------|---------|
| **Zone tile palettes (this doc)** | Proc-generated zone cells | Visual variety within a habitat |
| **Vault `.vault` glyphs** | Fixed footprint | Exact authored layout |
| **Town building facade overlay** | Listed cells on `town_main` | Distinct building art |
| **Hazard / trap / door overlays** | Separate tilemaps | Gameplay overlays on top of floor |

---

## 3. Authoring model

### 3.1 — `DungeonTilePalette` (ScriptableObject) — **new**

**Menu:** `JRogue/World/Dungeon Tile Palette`

Reusable palette asset shared by zones, floors, and (optionally) vault registry generation.

| Field | Type | Notes |
|-------|------|-------|
| **`paletteId`** | string | Stable id, e.g. `snow_floor`, `snow_wall`, `dungeon_stone_walls`. |
| **`layer`** | enum | `Floor` \| `Wall`. |
| **`entries`** | `DungeonTilePaletteEntry[]` | **1..N** tiles; N varies per palette. |
| **`defaultVariationMode`** | enum | §5; default **`WeightedRandom`**. |

**`DungeonTilePaletteEntry`:**

| Field | Type | Notes |
|-------|------|-------|
| **`tile`** | `TileBase` | Direct reference (preferred in Unity). |
| **`registryKey`** | string | Optional; e.g. `SnowTheme:32`. If set, resolved via `VaultAssetRegistry` at edit time / bake. |
| **`weight`** | int ≥ 1 | Relative pick frequency at paint time; default **1**. Eye-catching variants should use **lower** weight (DCSS `%weight` convention). |

**Locked:** A palette must have **at least one** valid entry. Empty palette is an authoring error.

### 3.2 — `DungeonZoneDefinition` extension

Replace single-tile fields with palette references (keep legacy fields as migration shim — §9).

| Field | Type | Notes |
|-------|------|-------|
| **`floorTilePalette`** | `DungeonTilePalette` | Eligible **floor** tiles for this zone. |
| **`wallTilePalette`** | `DungeonTilePalette` | Eligible **wall** tiles for this zone. |
| **`floorTile` / `wallTile`** | `TileBase` | **Deprecated** — treated as 1-entry palette until assets migrated. |

**Set size examples:**

| Zone | Floor palette size | Wall palette size |
|------|-------------------|-------------------|
| `snow` | 4–8 snow floor variants | 2–4 snow/ice wall variants |
| `desert` | 3–6 sand floor variants | 2–3 sandstone walls |
| `dungeon` | 6–12 stone floor variants | 3–5 brick/stone walls |
| `rock` (fallback) | 1 coarse stone | 1 coarse wall |

Zones **may** share palette assets (e.g. two zones reuse `Palette_Dungeon_Stone_Floor`) or define unique subsets.

### 3.3 — `DungeonFloorDefinition` extension

| Field | Type | When |
|-------|------|------|
| **`defaultFloorPalette`** | `DungeonTilePalette` | Fallback for zones without `floorTilePalette`; fallback for `PreBakedStamp` mode (§7). |
| **`defaultWallPalette`** | `DungeonTilePalette` | Same for walls. |
| **`floorTile` / `wallTile`** | `TileBase` | **Deprecated** — 1-entry fallback. |

**Per-floor variation:** Floor 1 might use richer dungeon palettes than Floor 3; each floor definition points at different palette assets even when `zoneId` strings match (`dungeon` on floor 1 vs floor 5).

### 3.4 — `DungeonFloorZoneLayout` (optional overrides)

| Field | Type | Notes |
|-------|------|-------|
| **`zonePaletteOverrides`** | `ZonePaletteOverride[]` | Rare: same `zoneId` on one floor uses a **narrower** palette in the north slot vs east slot (v1.1). |

**Locked v1:** Overrides **not required** — palette on `DungeonZoneDefinition` is sufficient. Slot-level override is a follow-up.

### 3.5 — Registry alignment (`VaultAssetRegistry`)

Extend registry to hold **all** theme indices used by palettes, not only vault defaults:

```text
SnowTheme:32   → snow floor A
SnowTheme:40   → snow floor B
SnowTheme:41   → snow floor C
SnowTheme:48   → snow wall A
SnowTheme:49   → snow wall B
SandTheme:32   → sand floor A
…
```

**Authoring rule:** Palette entries may reference `TileBase` directly **or** registry keys; both must resolve to the same assets vault `.vault` files use.

**Editor menu:** Extend `JRogue/Dungeon/Create Floor 1 Vault Pack` (or new **Create dungeon tile palettes**) to register full theme sheets into `VaultAssetRegistry` + create `DungeonTilePalette` assets under `Assets/Data/Dungeon/TilePalettes/`.

---

## 4. Runtime selection

### 4.1 — When picks happen

| Layout mode | Phase | Behavior |
|-------------|-------|----------|
| **`ZoneComposite`** | `ZoneFillPhase` / `ZoneBoundaryPhase` | Each painted cell: **one weighted pick** from zone palette at **first visit** |
| **`PreBakedStamp`** | `LayoutStampPhase` | v1: optional palette on floor def; v0 compat: single tile only |
| **Vault stamp** | `VaultStamper` | **No palette** — explicit glyph tile per cell |

### 4.2 — Resolver API (proposed)

Centralize in `ZoneTilePainter` (or new `DungeonTilePaletteResolver`):

```csharp
TileBase ResolveFloorTile(
    Vector3Int cell,
    DungeonFloorZoneLayout layout,
    DungeonFloorDefinition floorDef,
    string zoneId,
    DungeonGenerationContext context);

TileBase ResolveWallTile(/* same */);
```

**Resolution order:**

1. Zone `floorTilePalette` / `wallTilePalette` if non-null and non-empty.
2. Else zone legacy `floorTile` / `wallTile` (1-entry).
3. Else floor `defaultFloorPalette` / `defaultWallPalette`.
4. Else floor legacy `floorTile` / `wallTile`.
5. Else log warning and skip paint.

### 4.3 — Walkability unchanged

`MapManager.IsWalkable` / stamp topology / `ZoneCellMap` decide logic. Palette only affects which sprite `SetCellFloor` / `SetCellWall` receives. **All entries in a floor palette must be walkable floor art**; wall palette entries must be wall art (blocking when on wall layer).

---

## 5. Variation modes

All modes run **at paint time** during first-visit generation (`ZoneFillPhase` / boundary paint). The chosen `TileBase` is written to the tilemap and never recomputed on revisit (same persistence model as DCSS `tile_env.flv`).

### 5.1 — `WeightedRandom` (locked default v1)

Per cell, derive a deterministic integer from context, then **weighted** select among palette entries (mirrors DCSS `pick_dngn_tile()`):

```text
seed = Hash(runSeed, floorSalt, floorId, zoneId, cell.x, cell.y, layerSalt)
roll = seed mod totalWeight          // totalWeight = sum(entry.weight)
pick first entry where cumulative weight > roll
```

**Properties:**

- **Stable** for a given run seed and cell (meets **G4**).
- **Non-uniform** when weights differ — use low weight for loud variants (large rocks, sparkle snow), high weight for common fill tiles.
- Floor and wall use **different `layerSalt`** values so the same cell gets independent picks (DCSS uses separate hash salts for floor vs wall).

**Authoring guidance:** Match DCSS practice — the more visually distinct a variant, the **lower** its weight unless it should dominate the zone.

### 5.2 — `DeterministicHash` (optional)

Uniform pick among entries (ignores weight except all must be ≥ 1):

```text
hash = Hash(runSeed, floorSalt, floorId, zoneId, cell.x, cell.y, layerSalt)
index = hash mod entryCount
```

Use when every variant should appear equally often. Prefer **`WeightedRandom`** with equal weights instead unless profiling shows a need for this mode.

### 5.3 — `Single` (migration)

Palette size 1 — current behavior.

### 5.4 — Explicitly deferred

| Mode | Reason |
|------|--------|
| **Perlin noise** | Harder to keep deterministic across platforms; revisit v1.1 |
| **Autotile neighbor rules** | Separate milestone (corner walls, edges) |
| **Rotated / flipped variants** | Use distinct `TileBase` entries instead v1 |

---

## 6. Pipeline integration

### 6.1 — Call sites to update

| File | Change |
|------|--------|
| **`ZoneTilePainter.PaintFloor/Wall`** | Pass `Vector3Int cell` + `DungeonGenerationContext` into resolver |
| **`ZonePieceFiller`** | Already has cell coords — no structural change |
| **`ZoneSolidPainter`** | Same |
| **`ZoneBoundaryApplicator`** | Boundary walls use **wall palette** of the zone on either side (locked: **host zone** = cell’s zone from `ZoneCellMap`) |
| **`LayoutStampPhase`** | Optional: per-floor palette for pre-baked stamps (v1.1) |

### 6.2 — Vault interaction

```text
ZoneFill paints palette tiles
  → VaultPlacementPhase stamps vault
  → VaultStamper overwrites cells with explicit glyphs
```

Vault wins locally. Palette tiles surround vault without seam logic v1.

### 6.3 — Persistence

Tile picks are **implicit** in painted `Tilemap` state on `DungeonFloorInstance`. **No separate pick map** required v1. On revisit, instance is reactivated — tilemaps unchanged.

### 6.4 — Diagnostics

`ZoneFillPhase` log line extension:

```text
ZoneFillPhase: painted 30x30; snow floorPalette=6 wallPalette=3; desert floorPalette=4 wallPalette=2
```

Editor gizmo (future): show palette id per zone bounds.

---

## 7. Layout modes

### 7.1 — `ZoneComposite` (primary target)

Each zone piece uses its zone’s palettes during fill. **Different zones on the same floor may use different set sizes** — snow north with 6 floor tiles, desert east with 4, dungeon center with 10.

### 7.2 — `PreBakedStamp`

**v1:** Optional `defaultFloorPalette` / `defaultWallPalette` on floor def; `LayoutStampPhase` varies tiles by **weighted pick** over walkable/wall bits.

**v0 compat:** Single `floorTile` / `wallTile` unchanged.

### 7.3 — Town / interiors

Town (`town_main`, building interiors) stays on **single-tile stamp + facade overlays**. Multi-tile zone palettes apply to **dungeon** `ZoneComposite` and optionally dungeon pre-baked floors only.

---

## 8. Worked examples

### 8.1 — Snow zone (north band, Floor 1)

**Palette `Palette_Snow_Floor`** (6 entries, example weights):

| Key | Role | Weight |
|-----|------|--------|
| `SnowTheme:32` | Light snow A | 5 |
| `SnowTheme:40` | Light snow B | 5 |
| `SnowTheme:41` | Packed snow | 4 |
| `SnowTheme:42` | Slight drift | 3 |
| `SnowTheme:33` | Shadowed snow | 2 |
| `SnowTheme:34` | Sparkle variant | 1 |

**Palette `Palette_Snow_Wall`** (3 entries):

| Key | Role |
|-----|------|
| `SnowTheme:48` | Ice wall A |
| `SnowTheme:49` | Ice wall B |
| `SnowTheme:50` | Rocky snow cap |

`Zone_Snow` references both palettes. Proc fill north band → visually varied snow field; movement unchanged.

### 8.2 — Dungeon hub zone (center, Floor 1)

**Floor palette:** 8 Scavengers stone floor indices. **Wall palette:** 4 brick variants. Larger floor set, smaller wall set — matches DCSS pattern (floors vary more than walls).

### 8.3 — Floor 3 vs Floor 1

Both use `zoneId: dungeon`, but:

- `Floor_dungeon_floor_01` → `Palette_F01_Dungeon_Floor` (8 entries)
- `Floor_dungeon_floor_03` → `Palette_F03_Dungeon_Floor` (12 entries, darker art)

Same zone id, **different floor-level palette assignment** via zone def reference or floor-specific zone asset variants.

---

## 9. Migration

| Step | Action |
|------|--------|
| 1 | Add `DungeonTilePalette` + resolver; **`floorTile` → auto 1-entry palette** in resolver |
| 2 | Create palette assets for `snow`, `desert`, `dungeon` from existing `Assets/TileMaps/Vault/*` + Scavengers sheet indices |
| 3 | Update `DungeonZonePackCreator` to assign palettes instead of single tiles |
| 4 | Extend `VaultAssetRegistry` with additional theme indices |
| 5 | Deprecate direct `floorTile` / `wallTile` on zone def in inspector tooltips |
| 6 | Remove legacy fields in v2 after all assets migrated |

**No breaking change:** Floors with only `floorTile` set behave exactly as today.

---

## 10. Acceptance criteria (v1)

| ID | Test |
|----|------|
| **A1** | Snow zone on Floor 1 shows **≥ 2 distinct floor sprites** and **≥ 2 wall sprites** in a 10×10 sample at stable positions for fixed seed; with unequal weights, low-weight entry appears **less often** than high-weight entry across a 30×30 snow patch. |
| **A2** | Same seed + same floor → identical tilemap on second first-visit generate; revisit activates parked instance unchanged. |
| **A3** | Vault shrine overwrites its footprint; cells outside vault use palette variation. |
| **A4** | Zone with 1-entry palette matches current single-tile appearance. |
| **A5** | Desert and snow on same floor use **different** palette sizes without cross-contamination. |
| **A6** | `MapManager.IsWalkable` unchanged vs pre-palette floor for same stamp topology. |

---

## 11. Implementation phases

| Phase | Scope |
|-------|-------|
| **v1a** | `DungeonTilePalette`, resolver, **`WeightedRandom` default**, `ZoneTilePainter` + fill call sites, migrate Floor 1 zone pack |
| **v1b** | Full registry keys, editor palette creator from sprite sheets, diagnostics |
| **v1.1** | `DeterministicHash` mode, pre-baked stamp palettes, slot-level overrides |
| **v2** | Autotile wall sets (corners/edges), remove legacy single-tile fields |

---

## 12. Open questions

| # | Question | Default if unset |
|---|----------|------------------|
| **Q1** | Should floor and wall palettes **share one ScriptableObject** with two entry arrays, or stay separate assets? | **Separate assets** (set sizes differ) |
| **Q2** | Zone boundary between snow and desert: which wall palette on the seam cell? | **Host zone** = cell’s zone id from `ZoneCellMap` |
| **Q3** | Sub-stamp hybrid fill (`ZoneFillMode.SubStamp`): inherit zone palette or stamp’s implicit tiles? | **Zone palette** for proc cells; sub-stamp walls that are border-skipped stay zone wall palette |
| **Q4** | Unify `VaultAssetRegistry` and palette registry into one `DungeonTileCatalog`? | **Extend** `VaultAssetRegistry` v1; unify v2 |

---

## 13. Cross-references to update when implemented

- [Dungeon zone layout §4.1](Dungeon-Zone-Layout-Requirements.md) — replace single `floorTile` / `wallTile` with palette refs.
- [Dynamic dungeon floor generation §2.4](Dynamic-Dungeon-Floor-Generation-Requirements.md) — link this doc under tile / art pipeline.
- `DungeonZonePackCreator`, `DungeonVaultPackCreator` — palette asset generation menus.
