# Production dungeon — Floor 1 — Requirements (draft)

**Status:** **Implemented** (Phases 1–6). Core §4–§9 locked; automated QA via `JRogue → Dungeon → Phase 6 — Validate Production QA` and `DungeonFloor1Phase6Tests`.

**Production floor asset (locked):** Fork to **`Floor_prod_dungeon_floor_01`** — test floor keeps shared `Floor_dungeon_floor_01` on `DungeonFloorTest` for wild experiments.

**Purpose:** Specify the **first production dungeon floor** (`dungeon_floor_01`) and its **production Unity scene shell** — distinct from the **`DungeonFloorTest`** iteration scene. Define scene organization, town ↔ dungeon routing, floor dimensions, macro **zones**, **vaults**, **player spawn** rules (first floor: **not** adjacent to the inter-floor portal), and **enemy** population — while keeping existing **test** town/dungeon pairs unchanged.

**Depends on:** [Dynamic dungeon floor generation](Dynamic-Dungeon-Floor-Generation-Requirements.md) (multi-floor park/persist, portal bindings, generation pipeline), [Dungeon zone layout](Dungeon-Zone-Layout-Requirements.md) (`ZoneComposite`, jigsaw pieces, selection rules), [Dungeon zone population](Dungeon-Zone-Population-Requirements.md), [Dungeon monster spawn schedules](Dungeon-Monster-Spawn-Schedule-Requirements.md), [Dungeon floor & wall tiles](Dungeon-Floor-And-Wall-Tiles-Requirements.md), [Dungeon time](Dungeon-Time-Requirements.md), [Vaults §9](Dynamic-Dungeon-Floor-Generation-Requirements.md), [Town hub — multi-floor](Town-Hub-Multi-Floor-Requirements.md) (hub portal markers), `DungeonEntryService`, `DungeonExitService`, `TownToDungeonPortalInteractable`.

**Related scenes (routing — locked intent):**

| Scene | Role | Dungeon target |
|-------|------|----------------|
| **`TownTest.unity`** | Legacy town iteration | **`DungeonFloorTest.unity`** (unchanged test pair) |
| **`DungeonFloorTest.unity`** | Test dungeon shell + Generate button | Self — iteration only |
| **`DimensionSquareTest.unity`** | Scene-painted hub prototype | **Production dungeon** — **not** `DungeonFloorTest` |
| **`DistrictTownTest.unity`** | District hub (new town work) | **Production dungeon** (when wired; same target as Dimension Square) |
| **`Assets/Scenes/Dungeon/DungeonFloor/`** (new) | **Production** dungeon scene(s) | **`dungeon_floor_01`** + future floors in same shell |

**Related assets (existing baseline):**

| Asset | Location | Notes |
|-------|----------|-------|
| `Floor_dungeon_floor_01` | `Assets/Resources/Dungeon/` | **Test** iteration (`DungeonFloorTest`) — keep for experiments |
| `Floor_prod_dungeon_floor_01` | `Assets/Resources/Dungeon/` | **Production** Floor 1 fork (§10.2) |
| `Layout_Floor01_Zones` | `Assets/Data/Dungeon/Layouts/` | 30×30 jigsaw prototype — **subject to redesign** (§5) |
| `Zone_Dungeon` + habitat zones | `Assets/Data/Dungeon/Zones/` | Center hub + optional north/south/east/west pieces |
| `Schedule_Floor01_Dungeon` | `Assets/Data/Dungeon/SpawnSchedules/` | Day-driven skeleton groups — **subject to redesign** (§8) |
| `Floor1_VaultCatalog` | `Assets/Data/Vaults/` | Shrine + ambush corridor prototypes — **test floor only** |
| `Floor1_Production_VaultCatalog` | `Assets/Data/Vaults/` | Monument, ponds, altar — **production Floor 1** (§7) |

**Explicitly out of scope (this milestone):** Full dungeon chain (Floors 2–N production content); save/load across sessions; procedural room-and-corridor generator (v1); changing `SampleScene.unity`; removing or repurposing `DungeonFloorTest`; rewiring `TownTest` away from `DungeonFloorTest`.

### Amendment — Floor 2 descent plinth (2026-07-06)

**Status:** **Pending implementation** — supersedes parts of §7.6 and §8.2 below when [Floor 2 production](Dungeon-Floor-2-Production-Requirements.md) Phase 1 ships.

| Topic | Was (implemented) | Now (locked intent) |
|-------|-------------------|---------------------|
| **Floor 1 → Floor 2 gate** | Random walkable portal on **`northern_dark` north edge** (row **y = 79**, seed-driven **x**) | **Mandatory descent plinth** vault in `northern_dark`, **within Chebyshev 3** of row **y = 79** |
| **Activation** | Step-on portal from generation | **First bump** transforms plinth → portal; awards **+2 party XP once**; remains a portal for the run |
| **Floor 2 → Floor 1 arrival** | `arrivalBindings` in `luminescent_cavern` (deferred) | Party lands **adjacent to the plinth portal cell** — not random cavern spawn |
| **Persistence** | (unchanged) | Parked Floor 1 keeps kills, plinth state, fog; schedule may add **new** spawns after a **dungeon day** advances ([Floor 2 §9](Dungeon-Floor-2-Production-Requirements.md)) |

**Portal link ids unchanged:** `link_floor01_to_floor02`, `link_floor02_to_floor01`.

**Acceptance (replaces AC4-3 for production):** Plinth always placed; first bump → portal + XP; reachable from spawn; round-trip preserves Floor 1 state per Floor 2 AC4-1–AC4-3.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Production vs test separation** — Hub portals (`DimensionSquareTest`, later `DistrictTownTest` / production town) load a **production dungeon scene**, not `DungeonFloorTest`. |
| **G2** | **Test pair preserved** — `TownTest` ↔ `DungeonFloorTest` remain connected for QA and feature iteration. |
| **G3** | **Organized scene paths** — Production dungeon Unity scene(s) live under **`Assets/Scenes/Dungeon/DungeonFloor/`**. |
| **G4** | **First-floor spawn rule** — On `dungeon_floor_01`, the party **does not spawn adjacent to the inter-floor portal** (portal to Floor 2). Entry from town uses a dedicated **`playerStart`** away from that portal. |
| **G5** | **Recognizable Floor 1 identity** — Macro geography (zones, compass slots) is **stable per run**; internal room topology varies by seed ([zone layout §1](Dungeon-Zone-Layout-Requirements.md)). |
| **G6** | **Data-driven authoring** — Dimensions, zones, vaults, and enemies authored as ScriptableObjects / `.vault` files under `Assets/Data/Dungeon/` and `Assets/Data/Vaults/` — not hard-coded in scene scripts. |
| **G7** | **Reuse v0/v0b pipeline** — `DungeonFloorInstanceManager`, `ZoneComposite` phases, vault placement, monster spawn schedules — no parallel dungeon stack. |
| **G8** | **Phased delivery** — Ship in ordered phases (§2) so routing and shell land before content polish. |

---

## 2. Implementation phases (overview)

Each phase has its own acceptance criteria (§12). **Do not skip Phase 1** — routing must be correct before investing in Floor 1 content.

| Phase | Name | Summary | Depends on |
|-------|------|---------|------------|
| **0** | **Requirements capture** | This document; design decisions in §5–§9 filled in via review prompts | — |
| **1** | **Scene shell & routing** | Create `Assets/Scenes/Dungeon/DungeonFloor/` production scene; split `DungeonEntryService` target by source scene; build settings | Phase 0 locked |
| **2** | **Floor dimensions & zone layout** | Lock grid size, jigsaw pieces, zone selection rules, tile palettes | Phase 1 |
| **3** | **Vaults** | Floor 1 vault catalog, placements, gating by zone | Phase 2 |
| **4** | **Player spawn & portals** | `playerStart` piece, safe radius, Floor 2 portal placement, town return (exit dungeon) stub | Phase 2 |
| **5** | **Enemies & population** | Scatter vs schedule mode per zone; day schedules; boss/once spawns | Phase 2–4 |
| **6** | **Playtest & polish** | Lighting, hazards/traps/items pass, QA checklist, doc status → Implemented | Phase 1–5 |

**Refinement workflow:** Use follow-up prompts to fill **§5–§9** (marked **TBD — author input**). Lock each section before starting the phase that consumes it.

---

## 3. Scene strategy

### 3.1 — Folder layout (locked)

```text
Assets/Scenes/Dungeon/
├── DungeonFloorTest.unity          ← test iteration (unchanged location)
├── DungeonFloor.unity              ← legacy stub; migrate or supersede (§3.2)
└── DungeonFloor/                   ← NEW — production dungeon scene(s)
    └── DungeonFloor.unity          ← primary production shell (recommended name)
```

**Rule:** New production work goes under **`DungeonFloor/`**. The flat `Assets/Scenes/Dungeon/DungeonFloor.unity` may be moved, replaced, or aliased during Phase 1 — pick one path and update Build Settings once.

### 3.2 — One reusable production shell (recommended)

Align with [Dynamic dungeon §1](Dynamic-Dungeon-Floor-Generation-Requirements.md):

- **One** production `.unity` shell hosts **`DungeonRunBootstrap`**, **`DungeonFloorInstanceManager`**, UI, and DDOL party.
- Each **`floorId`** (`dungeon_floor_01`, …) is a **`DungeonFloorInstance`** child under `Floors/` — created on first visit, parked on floor transition, destroyed on exit dungeon.
- **No** separate Unity scene per floor **per run**; optional separate scenes only as **authoring templates** (not required for Floor 1).

### 3.3 — Test scene unchanged

| Property | `DungeonFloorTest` | Production `DungeonFloor/` |
|----------|-------------------|----------------------------|
| **Generate Test Floor** button | **Yes** | **No** |
| **`DungeonFloorTestController`** | **Yes** | **No** |
| **Floor definitions** | May share `Floor_dungeon_floor_01` early; fork when production diverges | Authoritative for player-facing content |
| **Build Settings** | Stays enabled | Must be enabled |
| **Entry from town** | **`TownTest` only** | **`DimensionSquareTest`**, **`DistrictTownTest`**, future production hub |

---

## 4. Portal & scene routing (locked intent)

### 4.1 — Entry routing matrix

| Source scene | Portal interactable | Loads scene | Start `floorId` |
|--------------|---------------------|-------------|-----------------|
| **`TownTest`** | Town dungeon portal | **`DungeonFloorTest`** | `dungeon_floor_01` (test data) |
| **`DimensionSquareTest`** | `StaticHubMarker` / `TownToDungeonPortalInteractable` | **Production** (`DungeonFloor/DungeonFloor.unity`) | `dungeon_floor_01` |
| **`DistrictTownTest`** | Same pattern on `dimension_square` | **Production** | `dungeon_floor_01` |
| **Future production town** | Hub dungeon portal marker | **Production** | `dungeon_floor_01` |

### 4.2 — Code changes (Phase 1 — specified, not implemented)

Today `DungeonEntryService.DungeonSceneName` is hard-coded to **`DungeonFloorTest`** for all towns.

**Required behavior:**

1. **`DungeonEntryService`** resolves target scene from **active town scene** (or explicit enum):
   - `TownTest` → `DungeonFloorTest`
   - All other calendar-enabled hub scenes → production dungeon scene name **`DungeonFloor`** (must match `.unity` file name in Build Settings)
2. **`DungeonExitService.TownSceneName`** already returns via `RunPartyPersistence.GetReturnTownSceneName()` — no change needed for round-trip.
3. **No** change to `TownToDungeonPortalInteractable` beyond calling the updated entry service.

**Locked (Q1):** Production scene file **`Assets/Scenes/Dungeon/DungeonFloor/DungeonFloor.unity`** → Build Settings scene name **`DungeonFloor`**.

### 4.3 — Exit dungeon (Floor 1 scope)

| Exit type | v0 behavior |
|-----------|-------------|
| **Forced return** (dungeon time expiry only) | §9.11 — modal → **`DimensionSquareTest`**; survivor refresh; permadead stay dead |
| **Voluntary retreat** | **None** in v0 — no leave-dungeon interactable, menu, or town portal on the floor |
| **Floor 1 → Floor 2** | In-dungeon portal link — **no** scene load |
| **Town portal** | Only on **enter** — not on Floor 1 map |

---

## 5. Floor dimensions — **Locked (2026-06-19)**

### 5.1 — Production canvas (locked)

| Property | Value | Notes |
|----------|-------|-------|
| **`floorWidth`** | **50** | Full floor width in tiles |
| **`floorHeight`** | **80** | Full floor height in tiles |
| **Macro shape** | **Single rectangle** | No L-shape or compass arms in v1; two stacked horizontal bands |
| **Size per run** | **Fixed** | Same dimensions every visit to `dungeon_floor_01` |
| **Configurability** | **`DungeonFloorZoneLayout` asset** | Change `floorWidth` / `floorHeight` in data — **not** hard-coded in C#. When resizing, update piece **`normalizedRect`** fractions (§6.3) so zone bands stay aligned. |

**Authoring asset:** `Layout_Floor01_Production` (proposed name) — fork from prototype `Layout_Floor01_Zones` during Phase 2.

### 5.2 — Vertical stack (locked)

NorthernDark sits **directly north of** LuminescentCavern — full width for both bands, shared horizontal edge at **y = 60** (bottom-left origin). **Three corridor entrances** (§6.3.1) are the only passages between the zones.

```text
 y=79 ┌──────────────────────────────────────┐ 50 × 20
      │         NorthernDark (north)         │
 y=60 ├──────○────────○────────○──────────────┤ ← shared boundary (3 entrances)
      │                                      │
      │      LuminescentCavern (center)      │ 50 × 60
      │         player start piece           │
  y=0 └──────────────────────────────────────┘
      x=0                                 x=49
```

| Band | Piece id | Tile footprint | Share of floor height |
|------|----------|----------------|---------------------|
| **LuminescentCavern** | `center` | 50 × 60 | 75% (rows 0–59) |
| **NorthernDark** | `north` | 50 × 20 | 25% (rows 60–79) |

**Sum check:** 60 + 20 = **80** ✓ — no fallback/rock gutter between bands.

### 5.3 — Layout kind & piece anchoring (implementation note)

| Property | Value |
|----------|--------|
| **`layoutKind`** | `CompassSlots` (two-piece vertical stack) |
| **Piece anchoring** | **`NormalizedRect`** on each piece — **not** the built-in compass preset fractions (those target the old 30×30 center+north+east prototype and do **not** yield 60/20 on 50×80) |

**Locked normalized rects** (0–1 fractions of `floorWidth` × `floorHeight`):

| Piece | `xMin` | `yMin` | `xMax` | `yMax` |
|-------|--------|--------|--------|--------|
| `center` | 0 | 0 | 1 | 0.75 |
| `north` | 0 | 0.75 | 1 | 1 |

**Outer boundary:** `defaultOuterBoundary: Wall`; `fallbackZoneId: rock` (fills any unassigned cell — none expected when both bands cover the rectangle).

### 5.4 — Prototype reference (superseded)

| Property | Prototype (`Layout_Floor01_Zones`) | Production |
|----------|-----------------------------------|------------|
| Grid | 30×30 | **50×80** |
| Pieces | center + north + east (optional biomes) | **center + north only** (both mandatory) |
| Zones | `dungeon`, `snow`, `desert`, `empty` | **`luminescent_cavern`**, **`northern_dark`** |

### 5.5 — Future expansion (deferred)

Additional compass pieces (east, west, south) may be added later without changing the locked v1 canvas — either grow `floorWidth`/`floorHeight` or carve bands from existing rects. **Do not** block Phase 2 on optional arms.

---

## 6. Zones — **Locked (2026-06-19)**

### 6.1 — Production zone table (locked)

| Piece id | Anchor | Zone id | Display name | Weight | Mandatory | Player start? | Tile band |
|----------|--------|---------|--------------|--------|-----------|---------------|-----------|
| `center` | `NormalizedRect` (§5.3) | **`luminescent_cavern`** | Luminescent Cavern | 100% | **Yes** | **Yes** | 50 × 60 (rows 0–59) |
| `north` | `NormalizedRect` (§5.3) | **`northern_dark`** | Northern Dark | 100% | **Yes** | No | 50 × 20 (rows 60–79) |

**Geography (locked):** **`northern_dark`** is **directly north of** **`luminescent_cavern`** — full-width bands, no side wings in v1.

**Selection rules (locked):** No optional zones, no weighted candidates, no mutual exclusion. Both zones appear every run.

**Player learnability:** Floor 1 identity is a **bright cavern hub below** + **dark northern band above** — stable compass, variable internal room graph per seed.

### 6.2 — Zone definitions (authoring targets)

| Zone id | Asset path (proposed) | `minWidth` × `minHeight` | `maxWidth` × `maxHeight` | Fill mode |
|---------|----------------------|--------------------------|--------------------------|-----------|
| `luminescent_cavern` | `Assets/Data/Dungeon/Zones/Zone_LuminescentCavern.asset` | 50 × 60 | 50 × 60 | **`Cave`** (§6.4.3) |
| `northern_dark` | `Assets/Data/Dungeon/Zones/Zone_NorthernDark.asset` | 50 × 20 | 50 × 20 | **`RoomCorridor`** (§6.4.3) |

Set min = max to the locked band sizes so the solver cannot shrink bands on this layout profile.

### 6.3 — Piece connectivity — **Locked (2026-06-20; updated 2026-06-21)**

| Edge | Neighbor | `boundaryKind` | Entrances | Notes |
|------|----------|----------------|-----------|-------|
| `center` ↔ `north` | shared row **y = 60** | **`Corridor`** | **Exactly 3** | **Only** connection points between zones — remainder of shared edge is **wall** |

#### 6.3.1 — Zone boundary entrances (locked)

| Rule | Detail |
|------|--------|
| **Count** | **3** walkable openings along the **50-cell** shared edge at **y = 60** |
| **Width per opening** | **1–3 tiles** each (seed-driven; openings may differ in width) |
| **Role** | These are the **sole** `luminescent_cavern` ↔ `northern_dark` passages — bidirectional (same cells both ways) |
| **Placement** | Evenly spaced along the edge span — [`ZoneBoundaryApplicator`](../../Assets/Scripts/World/Generation/Zones/ZoneBoundaryApplicator.cs) distributes opening centers when `corridorCount > 1` |
| **Approximate x (50-wide edge)** | Opening centers near **x ≈ 12, 24, 36** (seed-independent spacing; widths vary) |
| **Layout authoring** | On **`center`** piece in `Layout_Floor01_Production`: `edgeBoundaries` → `{ neighborPieceId: "north", boundaryKind: Corridor, corridorCount: 3 }`; **per-opening width 1–3** applied at generation time (§6.4.3b — not a single static `corridorWidth`) |
| **Fill interaction** | Cave / room proc must **connect** internal walkable area to all 3 openings (`ensureConnectivity: true`) |

**Not in scope:** Extra ad-hoc passages carved outside these 3 boundary openings; side wings; full-width open boundary.

**Lighting transition (locked):** No ambient step at the zone boundary. Darkness begins where **luminescent emitter tiles** end and unlit `northern_dark` cells begin. Player explores `luminescent_cavern` by cavern glow; **`northern_dark` requires torch / carried light** ([Improved illumination](Improved-Illumination-Requirements.md) gate).

**Progression (locked):** Player spawns in `luminescent_cavern`, must venture **north** through one of the **3 entrances** into `northern_dark` to reach the **Floor 2 portal** (§8.2).

### 6.4 — Tilesets, fill & lighting — **Locked (2026-06-20)**

**Art strategy (locked):** **B — DCSS dungeon tileset** as base; slice tiles into `TileBase` assets; recolor where needed. Register keys in `VaultAssetRegistry` under `CcssCavernTheme:NN` (exact prefix TBD at import).

**Palette variance (locked):** Per [Dungeon floor & wall tiles](Dungeon-Floor-And-Wall-Tiles-Requirements.md) — floor **6–10** weighted entries, wall **3–5** weighted entries; non-emitter tiles use standard DCSS cavern floor/wall sprites.

#### 6.4.1 — Shared tile baseline (both zones)

| Layer | Source | Notes |
|-------|--------|-------|
| **Non-light floor** | DCSS **cavern floor** tiles | **Same palette** in both zones — visual continuity lit → dark |
| **Wall (non-emitting)** | DCSS **cavern wall** tiles | Used in **`northern_dark`** and as non-emitter walls in **`luminescent_cavern`** if any |

#### 6.4.2 — `luminescent_cavern` (lit-by-tiles, zero ambient)

| Property | Value |
|----------|--------|
| **`defaultAmbientLight`** | **0** (total darkness baseline — visibility from emitters only) |
| **`ambientRegionId`** | Default / shared dungeon region — **TBD** at implementation |
| **Floor palette** | DCSS cavern floor (6–10 variants, weighted) — **non-emitter only** during paint |
| **Glow floor palette** | Separate **`Palette_LuminescentCavern_GlowFloor`** — **1–2** cyan nerve tiles; placed only by **gap-fill pass** (§6.4.3b) |
| **Wall palette** | DCSS cavern wall tiles **recolored light blue** = emitter walls (**all wall palette entries** emit in v1) |
| **Floor emitter role** | **Stopgap only** — fills walkable cells still under-lit after wall emitters register |
| **Player experience** | Explorable **without** torch while inside emitter-lit cavern cells |

#### 6.4.3 — Fill profiles (locked)

| Zone | `ZoneFillMode` | Intent |
|------|----------------|--------|
| **`luminescent_cavern`** | **`Cave`** | **DCSS-like cavern** — organic chambers with **substantial wall mass**; **emitter walls** are the primary light source |
| **`northern_dark`** | **`RoomCorridor`** | **More claustrophobic** — narrower corridors, **smaller** open rooms vs cavern |

**`SubStamp` clarification (locked):** **`SubStamp` embeds a hand-authored `DungeonLayoutStamp`** inside a zone slot (fixed room graph). Use for **special handcrafted pockets** (e.g. boss antechamber vault layout), **not** for the main cavern/corridor proc fill of these two zones.

**DCSS cave parity (Phase 2 AC):** Cellular-automata `GenerateCave` with tuned `innerWallDensity` / CA iterations — playtest acceptance: “reads as DCSS cavern” with **enough walls** to carry emitter lighting; glow floors only in residual dark pockets.

#### 6.4.3a — Procedural density & corridor width (locked 2026-06-21, retuned)

| Zone | Parameter | Locked target |
|------|-----------|---------------|
| **`luminescent_cavern`** | Cave wall mass | **`innerWallDensity: 55`**, **`caSmoothingIterations: 5`** — more rock than v0 open fill; still navigable, not a maze |
| **`luminescent_cavern`** | Primary lighting | **All wall palette entries** emit (`CavernGlow` profile) |
| **`luminescent_cavern`** | Glow floor fallback | **`glowFloorGapFill`** — place glow tiles only where `receivedLight < 1` after walls; **`glowFloorMinSpacing: 6`** |
| **`northern_dark`** | Room size | **Smaller** rooms than cavern open areas; higher room/corridor ratio |
| **`northern_dark`** | Corridor width | **1–3 tiles** depending on local area (wider near junctions / room mouths; narrow elsewhere) |
| **Zone boundary** | Entrance width | Each of the **3** entrances: **1–3 tiles** wide (independent rolls per opening) |

**Implementation note (Phase 2 gap):** Today [`GenerateRoomCorridor`](../../Assets/Scripts/World/Generation/Zones/ZoneRectProcGenerator.cs) carves **1-tile** corridors and [`ZoneBoundaryApplicator`](../../Assets/Scripts/World/Generation/Zones/ZoneBoundaryApplicator.cs) applies a **single** `corridorWidth` to all openings. Extend generator + boundary applicator for **variable 1–3** corridor/entrance widths (seed-driven, data-tunable caps).

**Numeric tuning:** `innerWallDensity`, CA iterations, glow spacing, and corridor width roll weights remain **data-configurable** — tune experimentally in playtest without code changes.

#### 6.4.3b — Glow floor gap-fill (implemented)

| ID | Rule |
|----|------|
| **GF1** | During `ZoneFillPhase`, cavern floor paint uses **non-emitter** entries only when `glowFloorGapFill` is enabled |
| **GF2** | After wall tile emitters register in `LightingInitPhase`, **`ZoneGlowFloorGapFillApplicator`** scans walkable cavern cells |
| **GF3** | Place a glow floor tile + emitter only when `receivedLight < glowFloorMinReceivedLight` (default **1**) |
| **GF4** | Respect **`glowFloorMinSpacing`** Chebyshev distance between glow placements (default **6**) |
| **GF5** | Glow palette: **`Palette_LuminescentCavern_GlowFloor`** — 2 cyan nerve variants, equal weight |

**Visual darkness note (2026-06):** If cavern still *looks* dark while logically lit, suspect **dark grey DCSS floor art** + binary fog tint — not broken emitter math. Vaults on test floors confirm backend works.

**Connectivity invariant (locked):** Generation must **always** produce a walkable path from the **`luminescent_cavern`** interior through the **3** zone entrances into **`northern_dark`** and to the **Floor 2 portal** (§8.2). If proc params would isolate the portal, **retry or relax** generation — never ship an unreachable portal.

#### 6.4.4 — `northern_dark` (dark cavern, zero ambient)

| Property | Value |
|----------|--------|
| **`defaultAmbientLight`** | **0** |
| **Floor palette** | **Same non-light cavern floor** set as `luminescent_cavern` (no emitter floor tiles) |
| **Wall palette** | Standard DCSS **cavern wall** (no blue recolor, **no emitters**) |
| **Player experience** | **Torch / carried light required** for live visibility in most cells |

#### 6.4.5 — Light-emitting tiles (new implementation — §6.5)

| Rule | Detail |
|------|--------|
| **Floor emitters** | **Up to 3** DCSS glow floor sprites from author reference strip; **1–2** used as weighted emitter palette entries |
| **Wall emitters** | Author **light-blue recolors** of DCSS cavern wall tiles; **`luminescent_cavern` wall palette** (all entries emit in v1) |
| **Default emitter profile** | Reuse **`Assets/Resources/Lighting/Torch.asset`** — `baseEmissionMax: 6`, `falloffRadius: 8`, `falloffPerTile: 1` |
| **Per-tile emission override** | Palette entry supports optional **`emitLight`** + optional **`LightEmitterDefinition`** |
| **Runtime mutation (future)** | `LightingService.SetEmission` for cavern glow events |
| **Emitter props (deferred)** | Crystal / fungus prefabs **out of scope v1** — §6.5 |

#### 6.4.5a — DCSS source tiles — **Confirmed in pack (2026-06-20)**

**Extract path:** `Assets/Sprites/DCSS/Dungeon Crawl Stone Soup Full/` (unzipped from `Assets/Sprites/Dungeon Crawl Stone Soup Full.zip`).

**Important:** The zip contains **base** rltiles PNGs. In-game **cyan** glow (`FLOOR_NERVES_CYAN`) is a **hue variation** applied at DCSS build time — **not** a separate file in the pack. Author **cyan emitter floors** by recoloring the red `floor_nerves_*` art (same pipeline as light-blue wall recolors).

**Screenshot match:** User reference strip (2026-06-20) matches **`floor_nerves_*`** patterns after cyan recolor — verified by HSV hue-shift preview against the screenshot.

##### Glow floor emitters (1–2 required; 3 optional)

| Role | Source file (relative to `dungeon/floor/`) | Pattern | Registry key (proposed) |
|------|-------------------------------------------|---------|------------------------|
| **Emitter #1** | **`floor_nerves_2_new.png`** | Cross / plus rune | `DcssCavern:floor_nerves_2_cyan` |
| **Emitter #2** | **`floor_nerves_4_new.png`** | Square-spiral / labyrinth | `DcssCavern:floor_nerves_4_cyan` |
| Optional palette | **`floor_nerves_6.png`** | Sparse rune accents | `DcssCavern:floor_nerves_6_cyan` |

Prefer **`_new`** variants over **`_old`** (current DCSS art). Full set for palette weights: `floor_nerves_0.png`, `floor_nerves_1_new` … `floor_nerves_5_new`, `floor_nerves_6.png`.

##### Non-light cavern floor (both zones)

| Source files | Notes |
|--------------|-------|
| **`grey_dirt_0_new.png` … `grey_dirt_7_new.png`** | Primary cavern floor — 8 variants |
| **`grey_dirt_b_0.png` … `grey_dirt_b_7.png`** | Secondary dirt variation — mix into palette |

Registry prefix: `DcssCavern:grey_dirt_N_new`, `DcssCavern:grey_dirt_b_N`.

Optional extras (lower weight): `rough_red_0`–`3`, `rect_gray_*_new` (already used in town).

##### Cavern walls — `northern_dark`

| Source files | Notes |
|--------------|-------|
| **`dungeon/wall/stone2_gray_2_new.png`**, **`stone2_gray_3_new.png`** | Primary gray stone |
| **`stone2_dark_2_new.png`**, **`stone2_dark_3_new.png`** | Darker stone variant |

Registry: `DcssCavern:wall_stone2_gray_2_new`, etc.

##### Cavern walls — `luminescent_cavern` (emitters)

Recolor **`stone2_gray_*_new`** + **`stone2_dark_*_new`** → light blue; all wall palette entries emit in v1.

##### Not used for Floor 1 cavern

| Asset | Why skip |
|-------|----------|
| `green_bones_*` | Teal tint but bone theme — wrong biome |
| `crystal_floor_*` | Crystal vault theme, not cavern dirt |
| `labyrinth_*` / `etched_*` | Different floor family (not nerves glow) |
| `abyss_*` walls | Abyss branch, not Depths cavern |

**GPL attribution:** `Assets/Art/Dungeon/ThirdParty/Dcss/CREDITS.md`.

#### 6.4.6 — Palette asset plan (proposed)

| Asset | Zone | Entries |
|-------|------|---------|
| `Palette_LuminescentCavern_Floor` | `luminescent_cavern` | 6–10 normal cavern + 1–2 emitter glow floors |
| `Palette_LuminescentCavern_Wall` | `luminescent_cavern` | 3–5 light-blue emitter walls |
| `Palette_NorthernDark_Floor` | `northern_dark` | Same normal cavern floors as luminescent (no emitters) |
| `Palette_NorthernDark_Wall` | `northern_dark` | 3–5 standard cavern walls |

**Tile import path (proposed):** `Assets/TileMaps/Dcss/Cavern/` + `Assets/Art/Dungeon/ThirdParty/Dcss/Cavern/` (source PNG + `CREDITS.md` — GPL attribution; project already uses DCSS art for town NPCs / rect-gray floors).

#### 6.4.7 — Authoring checklist

- [ ] Import & slice DCSS cavern floor / wall / glow tiles
- [ ] Create light-blue wall recolor sprites
- [ ] Create four palette assets (§6.4.6)
- [ ] Create `Zone_LuminescentCavern` + `Zone_NorthernDark` defs
- [ ] Implement §6.6 palette-emitter registration at zone paint time
- [ ] Tune `GenerateCave` params for DCSS-like cavern
- [ ] Population / spawn schedules (§9) — **locked**

### 6.5 — Light-emitting palette entries — implementation spec (Phase 2)

**Gap today:** `DungeonTilePaletteEntry` has **no** emitter fields; dungeon zone paint does **not** register `LightCellData` emitters (town uses `TownTorchSetupPhase` on known cells only).

**Required for Floor 1 production:**

| ID | Requirement |
|----|-------------|
| **LE1** | Extend palette entry (or companion asset) with **`isLightEmitter`**, optional **`emitLight`**, optional **`LightEmitterDefinition`** |
| **LE2** | During `ZoneFillPhase` / tile paint, when an emitter palette entry is placed, call **`LightingService.RegisterPending`** (or equivalent) with torch-compatible defaults |
| **LE3** | Default **`emitLight`** = `Torch.baseEmissionMax` (**6**); **data-configurable** per entry without code change |
| **LE4** | Non-emitter cells in both zones remain **receivers** with **`defaultAmbientLight: 0`** via `ZoneAmbientApplicator` |
| **LE5** | **Future:** `SetEmission` / interactable effects may mutate cavern glow at runtime — no redesign of palette format |

**Explicitly deferred:** Free-standing emitter **props** (crystal, mushroom prefabs on `DynamicViewsRoot`) — no dungeon prop-emitter pipeline yet; glowing **tiles** suffice for v1.

### 6.6 — Prototype zone table (reference only)

| Piece id | Zones (prototype) | Mandatory |
|----------|-------------------|-----------|
| `center` | `dungeon` | Yes |
| `north` | `snow` / `empty` | No |
| `east` | `desert` / `empty` | No |

### 6.7 — Floor population scope — **Locked (2026-06-21)**

Floor 1 v0 is intentionally **simple** — content is **enemies + vaults + Floor 2 portal** only.

| Category | v0 rule |
|----------|---------|
| **Floor item scatter** | **None** — loot comes **only** from enemy death drops (§9.2) |
| **Doors** | **None** |
| **Interactables** | **Portal only** (Northern Dark) **plus** vault features (monument / altar bump dialogs) |
| **Quest hooks** | **None** — deferred to later floors / milestones |
| **Hazards** | **None** (§9.7) |

**Population profiles:** `enemyPopulation`, `floorItemPopulation`, `interactablePopulation`, and `hazardPopulation` are **empty** on both zone profiles except traps (§9.6) and scheduled enemies (§9.1).

---

## 7. Vaults — **Locked (2026-06-20)**

**Mock assets:** `Assets/Data/Vaults/Floor1/Production/*.vault`  
**Catalog:** `Assets/Data/Vaults/Floor1_Production_VaultCatalog.asset`

### 7.1 — Rules (locked)

- Vaults stamp **inside** a resolved zone instance — catalog entries may require **`requiredZoneId`** ([zone layout §G6](Dungeon-Zone-Layout-Requirements.md)).
- Vault entities **win** over scatter population ([zone population §G6](Dungeon-Zone-Population-Requirements.md)).
- **`minDistanceFromPlayerStart`** may be **0** for zone-fixed or ambient features (monument, ponds, altar).
- **Shallow ponds** are **walkable** — water glyphs stamp **floor** tiles only; no movement penalty or hazard.
- **Monument** and **altar** are **mandatory once per floor** (monument at cavern center; altar at random anchor in `northern_dark`).
- **Monument** and **altar** use **bump** flavor dialogs (not offering-altar **`E`** interact in v1).

### 7.2 — Prototype vaults (reference — test floor only)

| Vault id | Size | Host zone (prototype) | Purpose |
|----------|------|------------------------|---------|
| `vault_shrine_5x5` | 5×5 | Any (zone-agnostic) | Shrine / altar pocket |
| `vault_ambush_corridor_7x4` | 7×4 | Any | Ambush corridor |

### 7.3 — Production vault table (locked)

| Vault id | WxH | Water / feature tiles | Required zone | Placement | Count / run | Gameplay |
|----------|-----|----------------------|---------------|-----------|-------------|----------|
| **`vault_monument_8x8`** | 8×8 | 2×2 **`W`** monument + 4 glow floors | `luminescent_cavern` | **Fixed zone center** (§7.5) | **Exactly 1** | Bump monument → *"There is a faded inscription on the monument."* |
| **`vault_pond_line_3`** | 3×1 | 3 shallow-water floor cells | `luminescent_cavern` | Random anchor | §7.7 | Walk-through decorative water |
| **`vault_pond_pool_4`** | 2×2 | 4 | `luminescent_cavern` | Random | §7.7 | Same |
| **`vault_pond_cross_5`** | 3×3 | 5 | `luminescent_cavern` | Random | §7.7 | Same |
| **`vault_pond_l_5`** | 3×3 | 5 | `luminescent_cavern` | Random | §7.7 | Same |
| **`vault_pond_rect_6`** | 3×2 | 6 | `luminescent_cavern` | Random | §7.7 | Same |
| **`vault_pond_t_7`** | 3×3 | 7 | `luminescent_cavern` | Random | §7.7 | Same |
| **`vault_pond_hook_7`** | 4×3 | 7 | `luminescent_cavern` | Random | §7.7 | Same |
| **`vault_pond_snake_8`** | 3×4 | 8 | `luminescent_cavern` | Random | §7.7 | Same |
| **`vault_altar_3x3`** | 3×3 | Center altar overlay on floor | `northern_dark` | **Random in zone (mandatory)** (§7.6) | **Exactly 1** | Bump altar → *"There are 3 small indentations and 1 larger indentation."* No offering logic v1. |

### 7.4 — DCSS sprite mapping (confirmed from author screenshots)

| Feature | DCSS source path (under `dungeon/`) | Registry key (proposed) | Notes |
|---------|--------------------------------------|-------------------------|-------|
| **Monument wall** (2×2, impassable) | `wall/stone2_gray_2_new.png` | `DcssCavern:wall_stone2_gray_2_new` | Grey beveled stone with center mark — matches author screenshot #1 |
| **Glow corners** (monument) | `floor/floor_nerves_2_new.png`, `floor_nerves_4_new.png` | `DcssCavern:floor_nerves_2_cyan`, `DcssCavern:floor_nerves_4_cyan` | Cyan recolor per §6.4.5; diagonal to each monument corner |
| **Shallow pond fill** | `water/shoals_shallow_water_1_new.png` … `shoals_shallow_water_4_new.png` | `DcssCavern:shoals_shallow_water_N_new` | Light-blue caustic tiles — matches author screenshot #2 strip |
| **Altar overlay** | `altars/misc_altar.png` | `DcssCavern:altar_misc` | Tiered grey stone with rounded top — matches author screenshot #3 |

**Monument layout (local MAP, `ORIGIN 3 3`):**

```text
........
........
..g1.g2..    ← glow emitters at (2,2), (5,2)
...WW...    ← 2×2 wall monument
...WW...
..g2.g1..    ← glow at (2,5), (5,5)
........
........
```

### 7.5 — Monument placement (locked)

| Rule | Detail |
|------|--------|
| **Always present** | Every `dungeon_floor_01` run includes exactly one `vault_monument_8x8`. |
| **Anchor** | Geographic **center of `luminescent_cavern`** band (50×60): target world cell **(25, 30)** using `ORIGIN 3 3` so the 2×2 monument straddles zone center. |
| **Pipeline** | **Not** satisfied by random `VaultPlacer` alone — add **`placementRule: ZoneCenter`** (or equivalent) on catalog entry; stamp **before** pond pass so ponds avoid reserved footprint. |
| **Interact** | Four `INTERACTABLE bump_monument_inscription` lines (one per monument wall cell); registry entries share one bump-dialog effect. Bump checked **before** walkability ([`BaseActor.TryMove`](../../Assets/Scripts/Controller/BaseActor.cs)). |

### 7.6 — Altar placement (locked)

> **Amendment (2026-07-06):** Production path will replace the flavor-only altar with a **descent plinth** (bump → portal + 2 XP). See [amendment](#amendment--floor-2-descent-plinth-2026-07-06) and [Floor 2 §4](Dungeon-Floor-2-Production-Requirements.md). Until implemented, behavior below remains in builds.

| Rule | Detail |
|------|--------|
| **Always present** | Every `dungeon_floor_01` run includes exactly one `vault_altar_3x3` **somewhere in `northern_dark`**. |
| **Anchor** | **Random** valid floor anchor within the zone (seed-driven) — position varies per run; unlike the monument, **not** fixed to zone center. |
| **Pipeline** | Catalog entry is **mandatory**, not optional weighted scatter — placement pass must **guarantee** one successful stamp in `northern_dark` (retry candidates / relax constraints before failing generation). Stamp **after** monument, **before or alongside** pond pass. |
| **Constraints** | Footprint must lie entirely in `northern_dark`; must not overlap monument reserved cells or Floor 2 portal cell(s). |

### 7.7 — Pond placement (locked)

| Rule | Detail |
|------|--------|
| **Templates** | Eight pond vault ids (3–8 water cells each); weighted pick per placement attempt. |
| **Count per floor** | Roll **2–5** ponds (typical); **never fewer than 2**; **15%** chance of **6–8** ponds; **hard cap 8** (one per template max) |
| **Walkable** | Pond cells remain **floor**; monsters and party may enter freely. |
| **RNG** | `runSeed` + `floorId` + `"pond_vaults"` — reproducible. |
| **Overlap** | Pond footprints must not overlap monument, altar, or each other. |

### 7.8 — Interactable registry (implementation — not yet authored)

| Registry id | Kind | `blocksOccupancy` | `bumpEnabled` | Dialog line |
|-------------|------|-------------------|---------------|-------------|
| `bump_monument_inscription` | Flavor bump | **Yes** (on wall cells) | **Yes** | *There is a faded inscription on the monument.* |
| `bump_altar_indentations` | Flavor bump | **Yes** | **Yes** | *There are 3 small indentations and 1 larger indentation.* |

Use **`NpcDialogBoxUI.ShowLine`** (same pattern as [`InnBedSleepPromptService`](../../Assets/Scripts/World/Town/InnBedSleepPromptService.cs)). Altar is **not** an offering altar ([Altar requirements](Altar-And-Map-Interact-Requirements.md) §3.1 adjacent-interact path deferred for this feature).

### 7.9 — Implementation gaps (Phase 3)

1. **`ZoneCenter` vault placement** for monument.  
2. **Mandatory zone placement** for altar — always exactly one `vault_altar_3x3` in `northern_dark`.  
3. **Pond count pass** (min 2, weighted 2–5+, optional overflow).  
4. **Register** §7.4 tile keys + §7.8 interactables in `VaultAssetRegistry`.  
5. **Wire** `Floor1_Production_VaultCatalog` on **`Floor_prod_dungeon_floor_01`** (§10.2).  
6. **Monument wall + overlay:** stamp `W` on wall map **and** register bump interactable on same cells (overlay sprite optional — wall tile provides art).

---

## 8. Player spawn & portals — **Locked (2026-06-21)**

### 8.1 — First-floor spawn rules (locked)

| Rule | Detail |
|------|--------|
| **R1** | Floor 1 is the **dungeon entry floor** — party arrives from **town scene load**, not from an upper-floor portal. |
| **R2** | Spawn anchor must **not** be Chebyshev-adjacent to the **Floor 2 descent portal**. *(Automatically satisfied when spawn is confined to `luminescent_cavern` and portal is in `northern_dark` — still validate in code.)* |
| **R3** | **`playerSafeRadius`** (Chebyshev **5**, default) excludes enemy / trap / hazard scatter near spawn — **already implemented** via `SafeZoneCells` + population phases. |
| **R4** | **`isPlayerStartPiece: true`** on **`center`** piece (`luminescent_cavern`) in `DungeonFloorZoneLayout`. |
| **R5** | Floor 2 portal is **not** at `playerStart` — north edge of `northern_dark` (§8.2). |
| **R6** | **Random spawn** — no fixed spawn cell; differs per run (seed-driven). |
| **R7** | Spawn anchor must lie in zone **`luminescent_cavern`** only — never in `northern_dark`. |
| **R8** | Anchor must **not** overlap a **vault footprint** (`context.ReservedCells` after vault phase). |
| **R9** | Full party **`formationProfile`** must fit: every member cell walkable, in zone, not reserved — **contiguous line** per formation offsets (see §8.3). |

**Design intent (locked):** Player may spawn **near or far** from the `northern_dark` boundary — exploration required; no guaranteed distance to the dark zone.

**Contrast — deeper floors:** Floor 2 → Floor 1 uses **`arrivalBindings`**, not random spawn.

### 8.2 — Portal placement (locked)

> **Amendment (2026-07-06):** **North-edge random portal (below) is superseded** by the mandatory **descent plinth** near row **y = 79**. See [amendment](#amendment--floor-2-descent-plinth-2026-07-06).

| Portal | Link id | Placement | Target | Notes |
|--------|---------|-----------|--------|-------|
| **Floor 1 → Floor 2** | `link_floor01_to_floor02` | ~~**`northern_dark` north map edge** — row **y = 79**; **x random**~~ → **descent plinth** near north edge of `northern_dark` (amendment) | `dungeon_floor_02` | Was: edge portal / stub. Now: plinth activation |
| **Floor 2 → Floor 1** | `link_floor02_to_floor01` | On Floor 2 (deferred) | `dungeon_floor_01` | ~~Arrival in `luminescent_cavern`~~ → **adjacent to plinth portal** ([Floor 2 §8.2](Dungeon-Floor-2-Production-Requirements.md)) |
| **Town entry** | — | — | — | Scene load only; no on-map town portal on Floor 1 |

**Reachability (locked):** The descent plinth (amendment) / Floor 2 portal must **always** be reachable on foot from a valid player spawn — no debug teleport required (AC4-3). Post-generation validation must confirm **global connectivity** cavern → dark → plinth. *(Legacy edge rule: walkable cell on **y = 79**; re-roll or snap if wall.)*

**RNG:** Plinth **x** along north band — `runSeed` + `floorId` + placement salt (TBD in Floor 2 pack). ~~`"portal_floor02"` for edge **x**~~ superseded.

### 8.3 — Random spawn resolution — **Locked (2026-06-20)**

| Property | Value |
|----------|--------|
| **Selection scope** | Walkable cells in **`luminescent_cavern`** after **zone fill + vault placement** |
| **RNG** | `runSeed` + `floorId` + `"player_spawn"` — reproducible per run |
| **Candidate filter** | Zone = `luminescent_cavern`; not in `ReservedCells`; full formation valid |
| **Formation** | Floor `formationProfile` — contiguous line (default vertical offsets in [`PartyFormationSpawnProfile`](../../Assets/Scripts/World/Generation/PartyFormationSpawnProfile.cs)) |
| **Safe zone** | After anchor chosen → `BuildSafeZoneForFloor` → **`playerSafeRadius` 5** excludes enemies/traps/hazards (existing) |
| **Fallback** | If zero candidates: try `PartySpawnService` ±2 cell shift; never spawn in `northern_dark` |

**Pipeline change (Phase 2):** Move final spawn from `ZoneLayoutPhase` (centroid today) to new **`PlayerSpawnPhase`** after `VaultPlacementPhase`. See §8.1 **R6–R9**.

### 8.4 — Author notes

```text
Random spawn — distance to NorthernDark varies by cave layout and roll.
Torches required once emitter tile coverage ends.
```

---

## 9. Enemies, traps & dungeon time — **Locked (2026-06-20)**

**Pack creator (Unity menu):** `JRogue/Dungeon/Create Floor 1 Production Content Pack`  
**Schedule asset:** `Assets/Data/Dungeon/SpawnSchedules/Schedule_Floor01_Production.asset`

### 9.1 — Population mode (locked)

| Zone | Mode | Schedule / scatter |
|------|------|-------------------|
| **`luminescent_cavern`** | **`ScheduledGroups`** | `Schedule_Floor01_Production` — cavern groups only |
| **`northern_dark`** | **`ScheduledGroups`** | Same schedule — dark goblin groups |

**Locked:** No scatter enemies in either zone. No hazards on this floor (§9.8).

### 9.2 — Enemy roster (locked)

All three species are **tier-9** (always drop **one tier-9 mana stone** on death; **5%** chance to drop their essence).

| Species | `speciesId` | DCSS sprite (confirmed) | Notes |
|---------|-------------|------------------------|-------|
| **Goblin** | `goblin` | `monster/goblin_new.png` | Goblin exists in DCSS — **not** kobold |
| **Dire Wolf** | `dire_wolf` | `monster/animals/wolf.png` | Standard DCSS wolf |
| **Ghoul** | `ghoul` | `monster/undead/ghoul.png` | Undead ghoul |

**Shared combat stats (v1 — tune on prefabs):** `hp: 10`, `attackPower: 1`, `visionRange: 2` (matches skeleton baseline).

**Post-v0 tuning (locked intent):** Per-species stat divergence is **manual** after first playtest — author will edit prefabs / species assets (§9.2.1) and **account for party member starting stats** when balancing.

**v0 exclusions (locked):** No **bosses** or **once-per-run** enemies; no **vault-triggered** enemy spawns; **no day vs night behavior differences** for any species.

#### 9.2.1 — Stat tuning paths (edit these assets)

| What to tune | Asset path |
|--------------|------------|
| **HP, attack, vision, sprite** | `Assets/Prefabs/Actor/Enemy/Production/GoblinEnemy.prefab` |
| | `Assets/Prefabs/Actor/Enemy/Production/DireWolfEnemy.prefab` |
| | `Assets/Prefabs/Actor/Enemy/Production/GhoulEnemy.prefab` |
| **XP, species id, loot table ref** | `Assets/Data/Enemy/Production/GoblinSpecies.asset` |
| | `Assets/Data/Enemy/Production/DireWolfSpecies.asset` |
| | `Assets/Data/Enemy/Production/GhoulSpecies.asset` |
| **Drop rates & payloads** | `Assets/Data/Enemy/Loot/Production/EnemyLootTable_Goblin.asset` |
| | `Assets/Data/Enemy/Loot/Production/EnemyLootTable_DireWolf.asset` |
| | `Assets/Data/Enemy/Loot/Production/EnemyLootTable_Ghoul.asset` |
| **Spawn wiring** | `Assets/Data/Spawn/Production/Spawn_Goblin_Floor01.asset` (etc.) |

### 9.3 — Essences (locked — data authored; gameplay stubs)

Each essence: **tier 9**, stat modifiers on **`EssenceData`**, active on linked **`EssenceDesignAbility`** stub (implementation Phase 5). All fields editable without code.

| Essence | Asset | Stat modifiers | Active ability asset | Design summary |
|---------|-------|----------------|----------------------|----------------|
| **Goblin Essence** | `Assets/Resources/Item/Essence/Production/GoblinEssence.asset` | Dexterity **+10** | `…/GoblinEssence_PoisonWeapon.asset` | **Poison Weapon** — 10 soul power; 10 turns; 10% poison on weapon attacks (incl. ranged/thrown; excl. staves/wands); **no turn cost** |
| **Ghoul Essence** | `…/GhoulEssence.asset` | Agility **+10**, Hearing **+10** | `…/GhoulEssence_Dash.asset` | **Dash** — 3 turns; user moves **2 tiles/turn** (party followers unchanged); **no turn cost** |
| **Dire Wolf Essence** | `…/DireWolfEssence.asset` | Strength **+10**, Smell **+10** | `…/DireWolfEssence_AdrenalineRush.asset` | **Adrenaline Rush** — 10 soul power; Defense **−10**, Strength **+10**; **no turn cost** |

**Loot table rule (all three):** `dropChance: 1.0` → 1× tier-9 mana stone; `dropChance: 0.05` → essence.

### 9.4 — Spawn groups — `luminescent_cavern` (locked)

**11 groups**, anchors **evenly spaced** (tunable in schedule asset). **`AtAnchor`** policy.

| groupId prefix | Count | Species | Days 1–2 | Day 3+ |
|----------------|-------|---------|----------|--------|
| `cavern_goblin_0` … `_4` | **5** | Goblin | **1** each | **2** in groups **0–3**; group **`_4` stays 1** |
| `cavern_ghoul_0` … `_2` | **3** | Ghoul | **1** each | **1** each (unchanged) |
| `cavern_dire_wolf_0` … `_2` | **3** | Dire Wolf | **1** each | **1** each (unchanged) |

**Day-3 goblin scaling** is configurable per group in `Schedule_Floor01_Production` (`targetCount` on `dungeonDay: 3` and `4` rows).

**Placeholder anchors** (world cells, 50×60 cavern band):

| Group | Anchor |
|-------|--------|
| `cavern_goblin_0` … `_4` | (10,12), (25,12), (40,12), (15,45), (35,45) |
| `cavern_ghoul_0` … `_2` | (10,30), (25,30), (40,30) |
| `cavern_dire_wolf_0` … `_2` | (15,22), (35,22), (25,38) |

### 9.5 — Spawn groups — `northern_dark` (locked)

| groupId | Count | Species | All days |
|---------|-------|---------|----------|
| `dark_goblin_0` … `_4` | **5** | Goblin | **1** each (constant) |

**Placeholder anchors** (rows 60–79): (10,68), (20,68), (30,68), (40,68), (25,72).

**Spawn anchor review:** Placeholder positions are **tunable after first playtest** — adjust in `Schedule_Floor01_Production` without code changes.

### 9.6 — Traps (locked)

| Zone | Trap | Count | Visible | Vault exclusion | Portal exclusion |
|------|------|-------|---------|-----------------|------------------|
| **`luminescent_cavern`** | **Bear trap** (`TrapDefinition_Bear`) | **2–3** | **Yes** | **Never** in vault footprints | — |
| **`northern_dark`** | **Bear trap** | **3–5** | **Yes** | **Never** in vault footprints | **Never** within Chebyshev **5** of Floor 2 portal |

**Population profiles:**  
`Assets/Data/Dungeon/Zones/Population/Population_LuminescentCavern_Floor01.asset`  
`Assets/Data/Dungeon/Zones/Population/Population_NorthernDark_Floor01.asset`

**Implementation gap:** Portal-distance trap filter not in `TrapPopulationPhase` yet — enforce at generation (§9.10).

### 9.7 — Hazards (locked)

**None** on Floor 1 production — `hazardPopulation` empty on both zone profiles.

### 9.8 — Dungeon time (locked)

| Property | Value |
|----------|--------|
| **Day/night cycles** | **4** (`baseDayNightCycles: 4`) |
| **Player turns per day** | **20** |
| **Player turns per night** | **20** |
| **Applies to** | **`Floor_prod_dungeon_floor_01`** (§10.2) |

**Override (locked):** Global [dungeon time](Dungeon-Time-Requirements.md) default for floor 1 is **7** cycles; **production Floor 1 v0 explicitly uses 4** on the forked floor def. Test floor may keep **7** until forked.

Aligns spawn schedule days **1–4** with floor length.

### 9.9 — Teaching curve (locked 2026-06-21)

Floor 1 v0 teaches core dungeon systems — not deep combat mastery:

| System | How Floor 1 introduces it |
|--------|---------------------------|
| **Day / night cycle** | Visible turn counter + lighting shift over 4 cycles |
| **Time pressure** | Finite dungeon days before forced exit (§9.11) |
| **Zone transition** | Lit cavern → dark north via **3** boundary entrances (§6.3.1) |
| **Essences** | 5% drop from three species; active abilities (§9.3, Phase 5) |

### 9.10 — Spawn reinforcement timing (locked 2026-06-21)

| Rule | v0 |
|------|-----|
| **When new schedule rows spawn** | **Day start only** — reinforcements appear at the start of each dungeon day |
| **Mid-day / mid-night spawns** | **Deferred** — may add variations in later milestones |

### 9.11 — Dungeon time expiry & return to town (locked 2026-06-21)

**StGaaB parity (locked):** When dungeon time expires, the party returns on the **daytime of the next calendar day** — the day they **entered** was the portal window; they do **not** return to the same calendar day. This **overrides** [Town time §7](Town-Time-And-Calendar-Requirements.md) (normal dungeon return without day increment) for **forced time expiry only**.

**Hub scene (locked):** Load **`DimensionSquareTest`** — author uses “Dimension Square” and **`DimensionSquareTest`** interchangeably; both mean this scene for v0 production entry and exit.

When the **4th dungeon day/night cycle completes** (or equivalent “dungeon time ended” signal):

| Step | Behavior |
|------|----------|
| **1. Expiry modal** | Show static modal (§9.11.1) — informs player the dungeon ended and reports **highest floor reached** this run. **Single dismiss** (OK). **No** other buttons or gameplay in the modal. |
| **2. Immediate transition** | On modal dismiss → **immediate** forced exit (no second confirmation). Same frame or next frame: teardown + scene load — no lingering in dungeon. |
| **3. Survivors** | Living party members: **keep all inventory**; **full HP**; **full Soul Power** (and other class pools per [dungeon time §7.3](Dungeon-Time-Requirements.md)); **clear all status effects**. |
| **4. Dead members** | **Permadead** — do **not** return to town (StGaaB-style). |
| **5. Scene load** | **`DimensionSquareTest`** at authored dungeon-return spawn. |
| **6. Town calendar** | **`calendarDayIndex++`** and set town phase to **`Day`** (daytime — **not** Morning). Example: entered on **day 1 morning** (portal open) → expiry return lands on **day 2, Day phase**. |
| **7. Portal availability** | Town dungeon portal **closed** on arrival — player must advance town calendar/phases until the next portal window ([Town time §6](Town-Time-And-Calendar-Requirements.md): days **1, 4, 7…**, **Morning** only). |

**v0 scope:** Forced expiry is the **only** dungeon → town exit path (§4.3).

**Implementation (Phase 5 gap):** `DungeonTimeService` expiry → show §9.11.1 modal → on dismiss: `ApplySurvivorRules()` → permadeath filter → `TownTimeService` forced-exit hook (day++ , phase=Day) → load `DimensionSquareTest`.

#### 9.11.1 — Expiry modal copy (locked)

Static strings; **only** dynamic field is highest floor reached (display name or floor index).

| Field | Text |
|-------|------|
| **Title** | `The Dungeon Has Ended` |
| **Body** | `Your time in the dungeon is over. You reached Floor {N} before returning to town.` |
| **Button** | `Continue` |

`{N}` = deepest **`floorId`** visited this run (e.g. **1** if only `dungeon_floor_01`; **2** if the party reached `dungeon_floor_02`). Use human-readable label if a display name exists on the floor def; otherwise floor number.

**Presentation:** Reuse existing modal/dialog UI (`NpcDialogBoxUI` or equivalent one-button modal). Log `[DungeonTime] Forced exit — highest floor {N}` on show.

### 9.12 — Prototype schedule (reference — test only)

`Schedule_Floor01_Dungeon` — skeleton halls on legacy `center:dungeon` hub; **not** used for production.

### 9.13 — Implementation gaps (Phase 2–5)

1. Wire **`Schedule_Floor01_Production`** + population profiles on production zone assets (`Zone_LuminescentCavern`, `Zone_NorthernDark`).  
2. Set floor time on **`Floor_prod_dungeon_floor_01`**: **`baseDayNightCycles: 4`**, **`playerTurnsPerDay/Night: 20`**.  
3. Implement **`EssenceDesignAbility`** gameplay (Poison Weapon, Dash, Adrenaline Rush) + **free-action** hotbar path (`consumesPlayerTurn: false`) — **Phase 5 priority (author confirmed)**.  
4. Trap pass: exclude **vault cells** + **portal ±5** in `northern_dark`.  
5. Import DCSS sprites on enemy prefabs (run pack creator in Unity).  
6. **Variable corridor width 1–3** in `GenerateRoomCorridor` + **per-opening entrance width 1–3** in `ZoneBoundaryApplicator` (§6.4.3a).  
7. **Dungeon time expiry:** §9.11 modal → survivor refresh → `DimensionSquareTest` load → town **day++**, phase **Day** (override Town time §7 for forced exit).  
8. **Portal reachability validation** after generation (§8.2, §6.4.3a).

---

## 10. Asset organization (production)

### 10.1 — Scenes

```text
Assets/Scenes/Dungeon/DungeonFloor/
└── DungeonFloor.unity              # production shell
```

### 10.2 — Data (existing roots — extend, do not duplicate blindly)

```text
Assets/Resources/Dungeon/
├── Floor_dungeon_floor_01.asset        # test floor (DungeonFloorTest) — keep for experiments
├── Floor_prod_dungeon_floor_01.asset   # production Floor 1 fork (locked)
└── …

Assets/Data/Dungeon/
├── Layouts/Layout_Floor01_Production.asset   # production layout (§6.3.1)
├── Layouts/Layout_Floor01_Zones.asset          # test / legacy
├── Zones/Zone_*.asset
├── TilePalettes/Palette_*.asset
└── SpawnSchedules/Schedule_Floor01_*.asset

Assets/Data/Vaults/
├── Floor1/
│   ├── *.vault                         # test prototypes
│   └── Production/*.vault              # production Floor 1 mocks (§7)
├── Floor1_VaultCatalog.asset           # test floor
└── Floor1_Production_VaultCatalog.asset
```

**Fork naming (locked Q5):**

| Test | Production |
|------|------------|
| `Floor_dungeon_floor_01` | **`Floor_prod_dungeon_floor_01`** |

Test floor keeps wild ideas on **`DungeonFloorTest`** without polluting production data.

### 10.3 — Editor menu (Phase 1+)

Extend **`DungeonV0aPackCreator`** or add **`DungeonFloor1ProductionPackCreator`**:

- Create / fix production scene under `DungeonFloor/`
- Wire Build Settings
- Validate no `DungeonFloorTestController` on production scene

---

## 11. Relationship to existing docs

| Topic | Owner doc | This doc |
|-------|-----------|----------|
| Multi-floor persist, pipeline phases | [Dynamic dungeon](Dynamic-Dungeon-Floor-Generation-Requirements.md) | defers — no re-spec |
| Jigsaw / zone algorithms | [Zone layout](Dungeon-Zone-Layout-Requirements.md) | Floor 1 **content** only |
| Per-zone scatter tables | [Zone population](Dungeon-Zone-Population-Requirements.md) | Floor 1 **content** only |
| Day-driven groups | [Monster spawn schedules](Dungeon-Monster-Spawn-Schedule-Requirements.md) | Floor 1 **content** only |
| Test scene QA | [Dynamic dungeon §16 v0a](Dynamic-Dungeon-Floor-Generation-Requirements.md) | unchanged |

---

## 12. Acceptance criteria (by phase)

### Phase 0 — Requirements

| ID | Criterion |
|----|-----------|
| **AC0-1** | §5–§9 sections filled and marked **Locked** by author |
| **AC0-2** | Portal routing matrix (§4) reviewed — no hub scene points at `DungeonFloorTest` except `TownTest` |

### Phase 1 — Scene shell & routing

| ID | Criterion |
|----|-----------|
| **AC1-1** | `Assets/Scenes/Dungeon/DungeonFloor/DungeonFloor.unity` exists in Build Settings |
| **AC1-2** | `DimensionSquareTest` portal loads **production** scene |
| **AC1-3** | `TownTest` portal still loads **`DungeonFloorTest`** |
| **AC1-4** | Production scene has **`DungeonRunBootstrap`** + floor manager; **no** test Generate controller |
| **AC1-5** | Enter from `DimensionSquareTest` → `dungeon_floor_01` generates → party placed at `playerStart` |

### Phase 2 — Dimensions & zones

| ID | Criterion |
|----|-----------|
| **AC2-1** | Locked grid size matches §5 |
| **AC2-2** | Zone jigsaw matches §6 production table |
| **AC2-3** | Multiple floor/wall tile variants visible per zone ([tiles doc](Dungeon-Floor-And-Wall-Tiles-Requirements.md)) |
| **AC2-4** | `luminescent_cavern` explorable without torch via emitter tiles; crossing into `northern_dark` requires light source |
| **AC2-5** | Per-palette-entry `emitLight` configurable in data; defaults match `Torch.asset` |
| **AC2-6** | Exactly **3** walkable entrances on the `center` ↔ `north` boundary (§6.3.1); each **1–3 tiles** wide; all other boundary cells walled |

### Phase 3 — Vaults

| ID | Criterion |
|----|-----------|
| **AC3-1** | Monument always at cavern center; altar always present in `northern_dark`; ponds meet §7.7 count rules |
| **AC3-2** | Vault footprints respect `minDistanceFromPlayerStart` |

### Phase 4 — Spawn & portals

| ID | Criterion |
|----|-----------|
| **AC4-1** | Random `playerStart` in `luminescent_cavern`; vault-overlap rejected; formation fits |
| **AC4-2** | Same run seed → same spawn anchor; different seed → may differ |
| **AC4-3** | ~~Floor 2 portal on **y = 79** at seed-driven **x**~~ → **Descent plinth** near north edge of `northern_dark`; first bump → portal; always reachable from spawn ([amendment](#amendment--floor-2-descent-plinth-2026-07-06)) |

### Phase 5 — Enemies

| ID | Criterion |
|----|-----------|
| **AC5-1** | §9 spawn groups match locked day counts; tier-9 mana + 5% essence drops |
| **AC5-2** | Bear trap counts in range; no traps in vaults; dark-zone traps respect portal buffer |
| **AC5-3** | Day 2+ schedule deltas spawn incrementally at **day start** ([spawn schedule §G2](Dungeon-Monster-Spawn-Schedule-Requirements.md)) |
| **AC5-4** | Time expiry: §9.11.1 modal → dismiss → **`DimensionSquareTest`**; survivors refreshed (§9.11); dead permadead; town **day++** + **Day** phase; portal closed until next window |
| **AC5-5** | Reinforcements spawn at **day start only** (§9.10) |
| **AC5-6** | **`Floor_prod_dungeon_floor_01`** uses **4** cycles (overrides global floor-1 default of **7**) |

### Phase 6 — Polish

| ID | Criterion |
|----|-----------|
| **AC6-1** | Round-trip: town → Floor 1 → Floor 2 → Floor 1 → exit town preserves run state |
| **AC6-2** | `DungeonFloorTest` regression unchanged (Generate, two-floor persist) |
| **AC6-3** | Doc status updated to **Implemented** |

---

## 13. Open questions backlog

| ID | Question | Status |
|----|----------|--------|
| **Q1** | Production scene Build Settings name | **Locked: `DungeonFloor`** (`Assets/Scenes/Dungeon/DungeonFloor/DungeonFloor.unity`) |
| **Q2** | Floor 1 width × height | **Locked: 50×80** |
| **Q3** | Fixed vs rolled dimensions | **Locked: fixed every run** |
| **Q4** | Single vs multi-habitat Floor 1 | **Locked: two mandatory habitats (stacked north/center)** |
| **Q5** | Fork `Floor_dungeon_floor_01` asset for production vs share with test | **Locked: fork → `Floor_prod_dungeon_floor_01`** |
| **Q6** | When does `DistrictTownTest` switch to production dungeon? | With Phase 1 (same as Dimension Square) |
| **Q7** | Floor 2 production scope — same shell, separate milestone? | **In progress** — [Floor 2 production](Dungeon-Floor-2-Production-Requirements.md); plinth gate replaces edge portal |
| **Q8** | Exact DCSS cavern tile indices for glow floors + wall set | **Confirmed** — §6.4.5a |
| **Q9** | Which 1–2 of the 3 glow floors are emitter palette entries | **Locked: `floor_nerves_2_new` + `floor_nerves_4_new`** |
| **Q10** | Party spawn cell / narrative within `luminescent_cavern` | **Locked: random** (§8.3) |
| **Q11** | Floor 2 portal placement | **Superseded** — plinth near **y = 79**, not random edge tile ([amendment](#amendment--floor-2-descent-plinth-2026-07-06)) |
| **Q12** | Cavern vs dark proc density | **Locked** — §6.4.3a |
| **Q13** | Corridor / entrance width | **Locked: 1–3 tiles** — §6.4.3a, §6.3.1 |
| **Q14** | Floor loot / interactables / quests v0 | **Locked** — §6.7 |
| **Q15** | Pond overflow probability | **Locked: 15% for 6–8; cap 8** (§7.7) |
| **Q16** | Dungeon time expiry behavior | **Locked** — §9.11 (StGaaB next-day daytime; `DimensionSquareTest`; modal + immediate exit) |
| **Q17** | Spawn reinforcement timing | **Locked: day start only v0** (§9.10) |
| **Q18** | Spawn anchor positions | **Review after first playtest** (§9.5) |
| **Q19** | Per-species stat divergence | **Manual after v0** (§9.2) |
| **Q20** | Essence gameplay priority | **Confirmed — Phase 5 priority** (§9.13) |
| **Q21** | Voluntary dungeon retreat in v0 | **No** — time expiry only (§4.3) |
| **Q22** | Proc gen numeric params | **Experiment in Phase 2** — not locked (§6.4.3a) |
| **Q23** | Survivor refresh on forced exit | **Locked** — full HP/SP, keep inventory, clear statuses (§9.11) |
| **Q24** | Floor 1 cycle count vs global default | **Locked: 4 on production fork** (§9.8) |

---

## 14. Revision log

| Date | Change |
|------|--------|
| 2026-07-06 | **Amendment** — Floor 2 descent plinth supersedes §8.2 north-edge portal; §7.6 altar → plinth; AC4-3, Q7, Q11 updated; see [Floor 2 production](Dungeon-Floor-2-Production-Requirements.md) |
| 2026-06-19 | Initial draft — Phase 0 scaffold; test vs production routing; phased plan; TBD sections for dimensions, zones, vaults, spawn, enemies |
| 2026-06-19 | **§5–§6 locked** — 50×80 rectangle; `luminescent_cavern` (50×60 center) + `northern_dark` (50×20 north); tilesets deferred to §6.4 |
| 2026-06-20 | **§6.3–§6.5, §8.2 locked** — DCSS cavern tilesets; Cave + RoomCorridor fill; zero ambient both zones; tile emitters in luminescent; Floor 2 portal at north edge of `northern_dark` |
| 2026-06-21 | **§6.3.1 locked** — exactly **3** corridor entrances on `center` ↔ `north` boundary (`corridorCount: 3`) |
| 2026-06-20 | **§6.4.5a confirmed** — DCSS Full zip extracted; sprite paths locked for nerves/grey_dirt/stone2 walls |
| 2026-06-21 | **§6.3.1 updated** — 3 entrances with **per-opening width 1–3**; §6.4.3a proc density (open cavern vs claustrophobic dark) |
| 2026-06-21 | **§4, §10.2 locked** — scene **`DungeonFloor`**; fork **`Floor_prod_dungeon_floor_01`** |
| 2026-06-21 | **§6.7, §8.2, §9.9–§9.11 locked** — v0 population scope; random portal x; teaching curve; day-start spawns; time expiry → DimensionSquare + permadeath |
| 2026-06-21 | **§7.7 locked** — pond 15% overflow roll; cap 8 |
| 2026-06-21 | **§9.11 expanded** — StGaaB next-day **Day** phase; `DimensionSquareTest`; expiry modal copy (§9.11.1); survivor refresh; no voluntary retreat; 4-cycle override |
| 2026-06-21 | **§6.4.3a, §8.2** — proc numbers experimental; portal always reachable |
| 2026-07-03 | **Phases 1–6 implemented** — production routing, content pack, essence actives, expiry flow; Phase 6 QA validator + unit tests; status → Implemented |

---

## 15. Next refinement prompts (suggested)

Phase 0 requirements are **largely complete**. Remaining work is **implementation** (Phases 1–6):

1. **Phase 1** — Create `DungeonFloor` scene shell + `DungeonEntryService` routing; fork `Floor_prod_dungeon_floor_01`.
2. **Phase 2** — `Layout_Floor01_Production`, zone palettes, emitter tiles, variable corridor/entrance widths.
3. **Phase 3** — Vault pipeline gaps (monument center, mandatory altar, pond pass).
4. **Phase 4** — `PlayerSpawnPhase` random spawn in cavern.
5. **Phase 5** — Essence abilities (priority), dungeon time expiry flow, trap exclusions.
6. **Playtest** — Tune spawn anchors (§9.5) and per-species stats (§9.2) by hand.
