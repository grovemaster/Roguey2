# Dungeon Zone Layout — Macro Regions & Habitat Composition

**Purpose:** Specify **data-driven dungeon floor geography** inspired by *Surviving the Game as a Barbarian* — each floor has a **recognizable macro shape** (e.g. “desert is always to the east,” “central dungeon hub,” “snow on the north edge”) while **internal layout varies per run** via seeded RNG. Introduces **zones** (generic habitat regions) as a layer **above** procedural fill and **below** hand-crafted **vaults**.

**Status:** Requirements draft — **not implemented**. Intended as **v1** companion to [Dynamic dungeon floor generation](Dynamic-Dungeon-Floor-Generation-Requirements.md) (v0 pre-baked stamp + vaults).

**Depends on:** [Dynamic dungeon floor generation](Dynamic-Dungeon-Floor-Generation-Requirements.md) (`DungeonFloorDefinition`, `DungeonGenerationPipeline`, `DungeonGenerationContext`, run seed, floor instance persistence §1.3), [Vaults §9](Dynamic-Dungeon-Floor-Generation-Requirements.md) (`VaultPlacementPhase`, `.vault` files, `DungeonVaultCatalog`), [Portals §8](Dynamic-Dungeon-Floor-Generation-Requirements.md) (`TaggedRegionEdge`, `portalLinkId`), [Lighting](Lighting-Requirements.md) (per-region ambient), [Safe zones](Safe-Zone-Requirements.md) (distinct from habitat zones — see §2.2).

**Related:** [Dungeon zone population](Dungeon-Zone-Population-Requirements.md) (enemy/hazard/trap/item/interactable tables **per zone**). [Conditional enemy spawn](../Combat/Conditional-Enemy-Spawn-Requirements.md). [Improved illumination](Improved-Illumination-Requirements.md) (ambient per region id).

**Explicitly out of scope (this milestone):** Full DCSS branch weight tables; online sync; disk save of zone maps (in-memory per floor instance only, same as v0); automatic art for every zone size; replacing vault format; town floor zones (town remains stamp-driven).

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Recognizable floor identity** — players learn where major habitats sit (compass / landmark), not the exact room graph each run. |
| **G2** | **Per-run variation** — walkable topology, room sizes, and scatter inside a zone differ by seed; macro placement stable for a given `floorId` + layout profile. |
| **G3** | **Zones ≠ vaults** — zones are **generic** regions (tileset, ambience, population hooks); vaults are **authored pockets** stamped **inside** zones. |
| **G4** | **Optional zones** — weighted selection, **mutual inclusion** (A requires B), **mutual exclusion** (A forbids B). |
| **G5** | **Jigsaw-level authoring** — support simple compass slots **and** complex multi-piece tessellations later. |
| **G6** | **Vault gating** — vault catalog entries may require a **host zone id** (e.g. shrine vault only in `dungeon` zone). |
| **G7** | **Pipeline integration** — zone resolution runs **before** vault placement and **before** population; downstream phases read a **per-cell zone map**. |
| **G8** | **Persistence** — zone map + generated topology frozen on **first visit**; return visits unchanged (§1.3 parent doc). |

---

## 2. Glossary

| Term | Meaning |
|------|---------|
| **Zone** (habitat / area) | A **typed macro region** on a floor: tile theme, ambient light, size bounds, population profile hooks, optional proc-gen profile. **Not** a hand-crafted vault. |
| **Vault** | Authored `.vault` chunk (fixed tiles + entities). Stamped into **valid anchors inside a zone** ([§9 parent](Dynamic-Dungeon-Floor-Generation-Requirements.md)). |
| **Layout stamp (v0)** | Monolithic pre-baked floor/wall grid for entire floor — **single** tileset. Still valid for test floors and town. |
| **Zone layout profile** | Authored **composition** for one `floorId`: which zones exist, where they sit, selection rules, jigsaw pieces. |
| **Slot** | A **reserved macro footprint** on the floor grid (rect or polygon) bound to a compass or graph role — e.g. `Center`, `North`, `East`. |
| **Zone piece** | One jigsaw cell: `{ slotId, candidateZoneIds[], minSize, maxSize, adjacency }`. |
| **Zone map** | Runtime `zoneId` per walkable cell (and optionally wall cells) — **`DungeonGenerationContext.ZoneCellMap`**. |
| **Zone fill** | Algorithm that turns an empty slot into floor/wall tiles (proc rooms, cave, sub-stamp, noise). |
| **Gameplay safe zone** | **Unrelated** — runtime combat policy ([Safe zones](Safe-Zone-Requirements.md)). Do **not** overload `zoneId` for safe-zone policy without an explicit flag. |
| **Ambient region** | Lighting term ([Lighting](Lighting-Requirements.md)) — zone may **bind** `ambientRegionId` for receiver cells. |

### 2.1 — Zones vs vaults (locked)

| | **Zone** | **Vault** |
|---|----------|-----------|
| **Authored as** | `DungeonZoneDefinition` + layout profile | `.vault` text + catalog entry |
| **Size** | Min/max **region** (e.g. 12×12 – 20×18) | Fixed WxH blueprint |
| **Tiles** | Theme set + proc fill | Exact per-cell glyphs |
| **Entities** | Table-driven population pass (future doc) | Embedded enemy/item/altar placements |
| **Count per floor** | Few (2–6 macro regions) | Many weighted injections |
| **Player expectation** | “Desert east” | “Oh, a shrine room” |

### 2.2 — Reference: *Surviving the Game as a Barbarian*

| Barbarian pattern | JRogue mapping |
|-------------------|----------------|
| Floor 3 always has orc castle, witch forest, mountain | `DungeonFloorZoneLayout` with **mandatory** zone entries + compass slots |
| Static geography, memorable landmarks | **Slot positions fixed** per floor profile; **internal** layout RNG |
| Special rooms (boss, shop, puzzle) | **Vaults** inside the matching zone |
| Branch identity | `floorId` + zone set defines “this is Floor 3” |

**Deliberate difference:** Barbarian floors were **fully static**. JRogue keeps **macro static + micro RNG** unless a zone uses a fixed sub-stamp for critical landmarks.

---

## 3. Current state vs gap

| Area | Exists (v0) | Gap (this doc) |
|------|-------------|----------------|
| **Layout** | `LayoutStampPhase` paints one stamp | No multi-theme regions on one floor |
| **Vaults** | `VaultPlacementPhase`, zone-agnostic anchors | No `requiredZoneId` on catalog entries |
| **Portals** | `TaggedRegionEdge` **deferred** until region tags | Needs **`zoneId`** or slot id on cells |
| **Population** | Per-**floor** tables only | Per-**zone** tables (next doc) |
| **Lighting** | Floor-wide default ambient | Per-zone ambient / cycle overrides |
| **Tilesets** | One `floorTile` / `wallTile` per floor def | Per-zone tile references (SandTheme, SnowTheme already in vault art paths) |

---

## 4. Authoring model

### 4.1 — `DungeonZoneDefinition` (ScriptableObject)

**Menu:** `JRogue/World/Dungeon Zone Definition`

| Field | Type | Notes |
|-------|------|-------|
| **`zoneId`** | string | Stable id: `dungeon`, `desert`, `snow`, `orc_castle`, `witch_forest`. |
| **`displayName`** | string | UI / debug. |
| **`floorTile` / `wallTile`** | `TileBase` | Theme for cells assigned to this zone. |
| **`ambientRegionId`** | int | Optional; binds receivers to regional ambient ([Lighting](Lighting-Requirements.md)). |
| **`defaultAmbientLight`** | int | Used when no regional schedule. |
| **`dayNightCycle`** | optional ref | Override floor cycle inside zone only (future). |
| **`minWidth` / `minHeight`** | int | **Minimum** slot size when this zone is placed. |
| **`maxWidth` / `maxHeight`** | int | **Maximum** slot size. |
| **`fillProfile`** | `ZoneFillProfile` | How to generate walkable topology inside the slot (§6). |
| **`populationProfile`** | ref | [Zone population profile](Dungeon-Zone-Population-Requirements.md) — enemies, hazards, traps, items, interactables. |
| **`tags`** | string[] | e.g. `outdoor`, `boss-adjacent`, `no-portals`. |
| **`vaultTagsAllowed`** | string[] | Filter for vault `placementTags` (§8). |

### 4.2 — `DungeonFloorZoneLayout` (ScriptableObject)

**Menu:** `JRogue/World/Dungeon Floor Zone Layout`

One asset per **`floorId`** (or per layout variant of that floor).

| Field | Type | Notes |
|-------|------|-------|
| **`floorWidth` / `floorHeight`** | int | Total grid (same origin as v0: bottom-left `(0,0)`). |
| **`layoutKind`** | enum | §5 — `CompassSlots`, `ExplicitPieces`, `Hybrid`. |
| **`selectionRules`** | `ZoneSelectionRule[]` | Weight + inclusion/exclusion (§7). |
| **`pieces`** | `ZoneLayoutPiece[]` | Jigsaw / slot definitions (§5). |
| **`defaultOuterBoundary`** | `ZoneBoundaryKind` | Floor edge treatment when a zone touches the map border (§6.2). |
| **`fallbackZoneId`** | string | Fills unassigned cells (often `dungeon` or `rock`). |

### 4.3 — `DungeonFloorDefinition` extension

| Field | When |
|-------|------|
| **`layoutMode`** | Add **`ZoneComposite`** to enum ([§2.4 parent](Dynamic-Dungeon-Floor-Generation-Requirements.md)). |
| **`zoneLayout`** | Reference to `DungeonFloorZoneLayout` when `layoutMode == ZoneComposite`. |
| **`layoutStamp`** | Still used for `PreBakedStamp` floors and as **optional skeleton** in `Hybrid` mode. |

**Locked:** `ZoneComposite` **replaces** monolithic stamp for topology; vaults and population still run afterward.

---

## 5. Layout kinds (macro shape)

### 5.1 — `CompassSlots` (recommended v1a)

Fixed **named slots** on a floor — easy “desert east, snow north, dungeon center.”

```text
┌─────────────────────────────┐
│          North slot          │  ← snow (optional)
│         (snow_zone)          │
├──────────┬──────────────────┤
│  West    │                  │
│ (optional│   Center slot    │  ← dungeon (mandatory)
│  vault)  │   (dungeon)      │
│          │                  │
│          ├──────────────────┤
│          │    East slot       │  ← desert (optional)
└──────────┴──────────────────┘
```

| `ZoneLayoutPiece` field | Purpose |
|-------------------------|---------|
| **`pieceId`** | e.g. `center`, `north`, `east`. |
| **`anchorKind`** | `Compass` + `CompassDirection` **or** `NormalizedRect` (0–1 fractions of floor). |
| **`rect`** | Min/max footprint in tiles (from zone def or override). |
| **`candidateZoneIds`** | Weighted list resolved via §7. |
| **`mandatory`** | If true, always selected (subject to feasibility). |
| **`connectsTo`** | piece ids that share an interface with this piece (§6.2). |
| **`defaultBoundary`** | optional | Default `ZoneBoundaryKind` for **all** edges of this piece when no per-edge override. |
| **`edgeBoundaries`** | optional | Per-neighbor overrides: `{ neighborPieceId, boundaryKind, corridorWidth }`. |

**Player learnability:** Compass slots use **consistent anchors per floor profile** — Floor 1 east is **always** the east slot; only **whether desert appears** and **desert room graph** vary.

### 5.2 — `ExplicitPieces` (jigsaw — v1b)

Author **arbitrary adjacency** — L-shapes, diagonal neighbors, multiple center fragments.

| Field | Purpose |
|-------|---------|
| **`adjacency`** | `{ north, south, east, west }` → neighbor `pieceId` or null |
| **`sharedEdgeMinTiles`** | Minimum contact width between neighbors |
| **`placementGrid`** | Optional: pre-defined **polyomino** coordinates relative to a root piece |

**Solver (runtime):**

1. Select active zone set (§7).
2. Assign each active piece a **zone definition** from candidates.
3. Pack pieces into `[0, width) × [0, height)` respecting adjacency + min/max sizes.
4. Fail piece → try alternate zone candidate or drop optional piece.

Supports “jigsaw puzzle” floors without compass semantics.

### 5.3 — `Hybrid` (v1c optional)

1. Stamp or generate a **connectivity skeleton** (corridors + chokepoints only).
2. Assign cells to zones via **weighted Voronoi** or **flood fill from slot seeds** with min/max area.
3. Re-fill each zone with its tileset + proc profile.

**Trade-off:** Less rigid compass learning; better for organic caves.

---

## 6. Zone fill (micro layout RNG)

Each zone slot, once sized and placed, needs **walkable topology**.

### 6.1 — `ZoneFillProfile` modes

| Mode | Behavior | Use |
|------|----------|-----|
| **`SubStamp`** | Pick one weighted `DungeonLayoutStamp` sized ≤ slot; embed centered or anchored | Hand-tuned dungeon core |
| **`RoomCorridor`** | DCSS-style proc inside rect ([§17 parent](Dynamic-Dungeon-Floor-Generation-Requirements.md)) | Generic dungeon / castle |
| **`Cave`** | Cellular automata / drunk walk | Witch forest, mines |
| **`OpenPocket`** | ≥70% floor, sparse pillars | Desert, snow field |
| **`VaultOnly`** | Minimal floor; rely on vaults for structure | Rare challenge pocket |

| Field | Notes |
|-------|-------|
| **`mode`** | enum above |
| **`generatorParams`** | Room count, corridor width, cave density — TBD on `ZoneGeneratorProfile` asset |
| **`subStampTable`** | Weighted stamps for `SubStamp` mode |
| **`ensureConnectivity`** | **true** — slot must be one connected component **within the zone** |
| **`innerWallDensity`** | optional | Walls **inside** the zone only (rooms/caves) — unrelated to zone interfaces (§6.2) |

### 6.2 — Zone boundaries (locked)

Zone interfaces are **independent** of zone fill. A boundary can be **anything or nothing** — authored **per edge** (between two pieces / zones) or as a piece-wide default.

#### 6.2.1 — `ZoneBoundaryKind`

| Kind | Topology | Presentation | Typical use |
|------|----------|--------------|-------------|
| **`None`** | No special border geometry | Zone map id may still change at the cell line | Internal sub-regions, overlapping assignment, “same play space” |
| **`Open`** | **Entire shared edge is walkable floor** | Abrupt **tileset change** at the zone cell boundary; **no walls** on the interface | Desert bleeding into dungeon — walk straight across sand → stone |
| **`Wall`** | **Wall cells** along all or part of the shared edge | Hard block unless carved | Mountain rim, castle curtain, impassable biome edge |
| **`Corridor`** | One or more **floor openings** through an otherwise walled or open interface | May use either zone’s wall/floor tiles in the throat | Classic dungeon choke into another biome |
| **`Mixed`** | Author **segments** on the shared edge (each segment: Open / Wall / Corridor + width) | Cliff face with a single pass, partial river ford | Advanced jigsaw pieces |

**Locked:**

- **`Open`** does **not** mean “same tileset” — it means **passable along the full edge** with **visual transition only**.
- **`Wall`** and **`Corridor`** may coexist on different edges of the same zone (north open, east walled with two corridors).
- **`None`** is valid for floor **outer** border if `defaultOuterBoundary` is also `None` (zone ends at map edge with no extra ring).
- Zone fill (`RoomCorridor`, `Cave`, etc.) must **not** assume a global “always wall at interface” rule.

#### 6.2.2 — Authoring (`ZoneEdgeBoundary`)

Per shared edge between piece **A** and piece **B** (or A and map exterior):

| Field | Purpose |
|-------|---------|
| **`neighborPieceId`** | Other piece id, or **`__exterior__`** for floor edge |
| **`boundaryKind`** | `ZoneBoundaryKind` |
| **`corridorCount`** | For `Corridor` / `Mixed` — number of openings (default **1**) |
| **`corridorWidth`** | Tiles wide per opening (default **1–3**) |
| **`wallInset`** | For `Wall` — optional inset from nominal edge (aesthetic) |
| **`segments`** | For `Mixed` — ordered spans along edge length |

Resolution order: **`edgeBoundaries`** entry for (A↔B) → **`defaultBoundary`** on piece A → layout **`defaultOuterBoundary`** (exterior only) → **`None`**.

#### 6.2.3 — `ZoneBoundaryPhase` (pipeline)

Runs **after** `ZoneFillPhase`, **before** vaults:

```text
for each resolved interface (A, B):
  apply boundaryKind:
    None     → no tile change beyond zone map ids
    Open     → ensure all interface cells on both sides are walkable floor (correct per-zone tiles)
    Wall     → paint wall cells along edge (both sides or single-sided — author flag)
    Corridor → place wall along edge except corridorWidth openings; carve floor throats
    Mixed    → apply segments
optional: global connectivity pass — only if layout marks connectsTo and kind includes Corridor
```

**Connectivity (locked):** Layout **may** require global reachability via **`connectsTo`** + **`Corridor`** edges. Layout **may** leave zones **disconnected** if all interfaces are **`Wall`** (intentional — key/vault/portal required). Do **not** force-connect every zone.

#### 6.2.4 — Examples

| Floor 1 layout | North (snow) ↔ center (dungeon) | East (desert) ↔ center |
|----------------|-----------------------------------|-------------------------|
| Option A | **`Open`** — step from stone to snow freely | **`Open`** — sand meets stone |
| Option B | **`Wall`** with **`Corridor`×1** — arctic pass | **`Open`** |
| Option C | **`None`** — snow region purely cosmetic overlay on shared cells *(discouraged)* | **`Corridor`×2** wide |

### 6.3 — Deprecated: monolithic connector profile

Earlier draft used a single `connectorProfile` and “shared wall between zones.” **Superseded by §6.2** — corridors are one **`ZoneBoundaryKind`**, not the only interface type.

---

## 7. Zone selection rules (RNG)

### 7.1 — `ZoneSelectionRule`

| Field | Purpose |
|-------|---------|
| **`zoneId`** | Candidate zone definition id |
| **`weight`** | Relative selection weight (0 = never roll alone) |
| **`mandatory`** | Always included if layout feasible |
| **`requiresAll`** | string[] — all listed zones must also be selected |
| **`requiresAny`** | string[] — at least one must be selected |
| **`excludes`** | string[] — if this zone is selected, none of these may be selected |
| **`maxInstances`** | Per floor (usually 1 for macro zones) |
| **`minFloorVersion`** | Future gating |

### 7.2 — Selection algorithm (locked)

```text
1. Start with all mandatory zones / pieces.
2. Build conflict graph from excludes + requires*.
3. Weighted random draw for optional zones/pieces until budget met OR roll fails feasibility.
4. Validate: mutual inclusion satisfied; no exclude violations; sum of min sizes fits floor.
5. If invalid → retry selection (cap N attempts) → fallback drop lowest-weight optional piece.
6. Log chosen set with run seed for debug.
```

**Example — Floor 1:**

| Zone | Weight | Mandatory | Rules |
|------|--------|-----------|-------|
| `dungeon` | — | **yes** | center slot |
| `desert` | 60 | no | `excludes: [snow]` if design wants hot/cold clash |
| `snow` | 40 | no | `excludes: [desert]` |
| `shrine_vault_zone` | 20 | no | `requiresAll: [dungeon]` |

Run A: dungeon + desert (east). Run B: dungeon + snow (north). Run C: dungeon only.

### 7.3 — Seeding

```text
zoneSelectionRng = Derive(runSeed, floorId, "ZoneSelect")
zoneFillRng       = Derive(runSeed, floorId, "ZoneFill", pieceId)
vaultRng          = existing vault phase salt
populationRng     = existing population salt
```

Same run seed → same zone set and fills (debug reproducibility).

---

## 8. Vault ↔ zone binding

Extend **`DungeonVaultCatalogEntry`** ([existing vault pipeline](../Assets/Scripts/World/Generation/Vaults/DungeonVaultCatalog.cs)):

| Field | Rule |
|-------|------|
| **`requiredZoneId`** | Empty = any walkable anchor; else anchor cell must have `ZoneCellMap[cell] == requiredZoneId` |
| **`forbiddenZoneIds`** | Optional deny list |

**Placement change in `VaultPlacementPhase`:**

```text
pick anchor cell
  → must be walkable, not safe zone, fit vault footprint
  → NEW: all footprint cells must match requiredZoneId (if set)
  → stamp vault (may overwrite zone tiles — vault wins locally)
```

**Example:** `vault_orc_boss_throne` → `requiredZoneId: orc_castle` only.

---

## 9. Runtime: zone cell map

### 9.1 — `DungeonGenerationContext`

| Field | Purpose |
|-------|---------|
| **`ZoneCellMap`** | `Dictionary<Vector3Int, string>` or dense `string[]` indexed by x,y |
| **`ZoneBounds`** | `zoneId` → `RectInt` for debug overlay / minimap (future) |
| **`SelectedZones`** | Ordered list of `{ pieceId, zoneId, rect }` for logging |

**Consumers:**

| System | Usage |
|--------|-------|
| **`VaultPlacementPhase`** | §8 |
| **`PortalPlacementPhase`** | `TaggedRegionEdge` → filter by `zoneId` |
| **`EnemyPopulationPhase`** | [Zone population](Dungeon-Zone-Population-Requirements.md) |
| **`LightingInitPhase`** | Set receiver `ambientRegionId` from zone |
| **`HazardPopulationPhase`** | [Zone population](Dungeon-Zone-Population-Requirements.md) |
| **Quest / triggers** | “Enter witch_forest” zone event (future) |

### 9.2 — Persistence

On **`MarkGenerated`**, store **`ZoneCellMap`** snapshot on `DungeonFloorInstance` (same lifetime as tilemaps — §1.3 parent).

**Return visit:** Do not re-run zone selection or fill; vault/population already applied.

---

## 10. Generation pipeline (proposed)

Insert **before** vaults; replace **`LayoutStampPhase`** when `layoutMode == ZoneComposite`.

| Order | Phase | Responsibility |
|-------|-------|----------------|
| 1 | **`ClearPreviousFloorPhase`** | unchanged |
| 2 | **`ZoneLayoutPhase`** | Select zones; assign pieces to slots; build `ZoneCellMap` bounds |
| 3 | **`ZoneFillPhase`** | Per-piece proc/sub-stamp → paint `MapManager` with zone tilesets |
| 4 | **`ZoneBoundaryPhase`** | Per-edge boundary kinds (§6.2); optional connectivity validation |
| 5 | **`VaultPlacementPhase`** | Zone-filtered anchors |
| 6 | **`PortalPlacementPhase`** | `TaggedRegionEdge` uses `zoneId` |
| 7 | … | Population phases use [zone population](Dungeon-Zone-Population-Requirements.md) |

**`LayoutStampPhase`:** Still runs when `layoutMode == PreBakedStamp`.

```text
if (def.layoutMode == ZoneComposite)
  ZoneLayout → ZoneFill → ZoneBoundary
else
  LayoutStampPhase
```

---

## 11. Worked example — Floor 1 (`dungeon_floor_01`)

### 11.1 — Design intent

| Slot | Compass | Zone candidates | Mandatory |
|------|---------|-----------------|-----------|
| Center | middle 50% | `dungeon` | yes |
| North band | top 35% | `snow` (w 40), `empty` (w 60) | no |
| East band | right 30% | `desert` (w 55), `empty` (w 45) | no |

**Player experience:** “There is usually something sandy to the east and cold to the north, but the central dungeon layout changes every run.”

### 11.2 — Assets (illustrative paths)

| Asset | Path |
|-------|------|
| Zone defs | `Assets/Data/Dungeon/Zones/Zone_Dungeon.asset`, `Zone_Desert.asset`, `Zone_Snow.asset` |
| Floor layout | `Assets/Data/Dungeon/Layouts/Layout_Floor01_Zones.asset` |
| Floor def | `Floor_dungeon_floor_01.asset` → `layoutMode: ZoneComposite`, `zoneLayout: Layout_Floor01_Zones` |

### 11.3 — Barbarian Floor 3 analogue (future)

| Zone | Slot | Vault examples |
|------|------|----------------|
| `orc_castle` | west mandatory | boss throne vault |
| `witch_forest` | center-north | witch hut vault |
| `mountain` | east | rope bridge, yeti vault |

`witch_forest` **`requiresAll: [orc_castle]`** — forest never rolls without castle.

---

## 12. Implementation plan (phased)

### Phase Z0 — Data & debug (no proc)

- [ ] `DungeonZoneDefinition`, `DungeonFloorZoneLayout`, selection rules
- [ ] `ZoneLayoutPhase` — compass slots only, **fixed rect sizes** (no fill RNG)
- [ ] Paint each slot with zone tileset as **solid rectangles** + outer walls
- [ ] `ZoneCellMap` on context + instance snapshot
- [ ] Debug overlay (editor gizmo or dev key) showing zone ids / bounds
- [ ] Unit tests: selection rules (include/exclude/mandatory)

### Phase Z1 — Zone fill + boundaries

- [ ] `ZoneFillProfile` + `SubStamp` + `OpenPocket` modes
- [ ] `ZoneFillPhase`, `ZoneBoundaryPhase` (Open / Wall / Corridor / Mixed)
- [ ] Swap Floor 1 test to `ZoneComposite` in `DungeonFloorTest`
- [ ] `requiredZoneId` on vault catalog entries

### Phase Z2 — Jigsaw + proc + population

- [ ] `ExplicitPieces` solver
- [ ] `RoomCorridor` / `Cave` fill profiles
- [ ] `TaggedRegionEdge` portals wired to `zoneId`
- [ ] [Zone population](Dungeon-Zone-Population-Requirements.md) + phase filtering

### Phase Z3 — Polish

- [ ] Hybrid mode, minimap zone labels, quest zone triggers
- [ ] Per-zone day/night overrides
- [ ] Editor: visual layout authoring window

---

## 13. Suggested algorithms (implementation notes)

### 13.1 — Compass slot sizing

Given floor `W×H`, slot `NormalizedRect (x0,y0,x1,y1)` in 0–1:

```text
rect = floor rect in tile coords
clamp to zone.minWidth/Height .. maxWidth/Height
shrink overlapping mandatory neighbors
```

### 13.2 — Jigsaw packing (ExplicitPieces)

- **Backtracking** on small piece graphs (≤8 pieces) — typical for Barbarian-scale floors.
- Order pieces by **constraint count** (most connected first).
- Randomize tie-break with seeded RNG.

### 13.3 — Room-and-corridor in rect

Reuse future `RoomCorridorGenerationPhase` scoped to `RectInt` with margin 1 for walls.

### 13.4 — Validation retries

| Failure | Response |
|---------|----------|
| Optional zone doesn’t fit | Drop zone; retry selection |
| Mandatory zone doesn’t fit | **Error** — layout profile invalid |
| Global connectivity fail | Retry boundary carve or regen fill (max 3) **only if** layout requires `connectsTo` |
| Vault 0 placements | Warn; floor still playable |

---

## 14. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC1** | Same `runSeed` + `floorId` → identical zone set and cell map. |
| **AC2** | Different seeds → different micro layout inside `dungeon` center (SubStamp or RoomCorridor). |
| **AC3** | Floor 1 profile: `dungeon` always present; east slot never contains `snow` when `desert`/`snow` mutually exclusive. |
| **AC4** | Vault with `requiredZoneId: desert` never places outside desert cells. |
| **AC5** | Leave floor and return — zone map and tiles unchanged. |
| **AC6** | `PreBakedStamp` floors (town, legacy test) unaffected when `layoutMode != ZoneComposite`. |

| **AC7** | **`Open`** interface: full edge walkable; tileset changes at zone map boundary; no wall on interface. |
| **AC8** | **`Wall`** + **`Corridor`** interface: impassable except authored openings. |

---

## 15. Design decisions (locked)

| # | Decision | Rule |
|---|----------|------|
| 1 | **Zone boundaries** | **Per-edge** `ZoneBoundaryKind`: `None`, `Open`, `Wall`, `Corridor`, or `Mixed` — not a single global policy (§6.2). |
| 2 | Overlapping slots | **Forbidden** in compass mode; solver rejects |
| 3 | `empty` slot | Painted as `fallbackZoneId` (`rock`/`void`) — not walkable |
| 4 | Player start | **`playerStart` piece** must be `dungeon` or authored marker in zone layout |
| 5 | Multi-zone floors | **Allowed** (Floor 3: three mandatory zones) |
| 6 | Zone id vs lighting region id | **Separate namespaces**; zone def **maps** zone → ambient region |
| 7 | Forced global connectivity | **Optional** — only when layout authors `connectsTo` with corridor/wall mix requiring reachability |

---

## 16. Traceability

| User request | Section |
|--------------|---------|
| Barbarian static geography inspiration | §2.2, §11.3 |
| RNG inside areas | §6, §7.3 |
| Zones ≠ vaults | §2.1, §8 |
| Tileset, light, size min/max | §4.1 |
| Floor 1 dungeon + desert north + snow east example | §11 (note: user said desert **east**, snow **north** — doc uses that) |
| Recognizable shape, varying interior | §1 G1–G2, §5.1 |
| Jigsaw configuration | §5.2, §13.2 |
| Weights, mutual include/exclude | §7 |
| Vault zone gating | §8 |
| Enemy spawns per zone | [Dungeon zone population](Dungeon-Zone-Population-Requirements.md) |
| Flexible zone boundaries | §6.2 |

---

## 17. Document history

| Date | Note |
|------|------|
| 2026-06-06 | Locked zone boundary kinds (Open / Wall / Corridor / Mixed / None) |
| 2026-06-06 | Initial draft — zone layout, selection rules, pipeline, phased implementation |

---

## 18. Cross-links to add when implementing

- [Dynamic dungeon floor generation §2.4](Dynamic-Dungeon-Floor-Generation-Requirements.md) — add `ZoneComposite` to layout modes
- [Dynamic dungeon floor generation §8.3](Dynamic-Dungeon-Floor-Generation-Requirements.md) — enable `TaggedRegionEdge` via `zoneId`
- [Dynamic dungeon floor generation §9](Dynamic-Dungeon-Floor-Generation-Requirements.md) — vault `requiredZoneId`
- [Dynamic dungeon floor generation §17](Dynamic-Dungeon-Floor-Generation-Requirements.md) — room/corridor generator feeds `ZoneFillProfile`
