# Town hub — Multi-floor districts & transitions — Requirements (draft)

**Status:** **Draft** — architecture specified; `dimension_square` is the scene-painted prototype; `town_main` remains stamp-based until migrated.

**Purpose:** Specify how an **expansive town hub** is built from **many town floors** (districts, plazas, corridors) and **pre-painted building interiors**, all within **one Unity hub scene**. Define **transition points** — including **multiple exits per district** — so walking onto authored tiles moves the party to another town floor (JRPG “screen change” feel) without `SceneManager.LoadScene`. Define **data-driven NPC and interactable population** so districts scale with flags, quests, and story characters.

**Depends on:** [Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md) (`DungeonFloorInstance`, `DungeonFloorInstanceManager`, `FloorLayoutMode.ScenePainted`), [Dungeon floor & wall tiles](Dungeon-Floor-And-Wall-Tiles-Requirements.md) (`DungeonTilePalette`, weighted variation), [Town buildings](Town-Building-Entry-And-Exit-Requirements.md) (interior enter/exit, fade curtain), [Safe zones](Safe-Zone-Requirements.md), [Town time & calendar](Town-Time-And-Calendar-Requirements.md), [Shop NPCs](Shop-NPC-Requirements.md), [NPC dialog](NPC-Dialog-Requirements.md), [Quest system](Quest-Requirements.md) (`GameStoryFlagService`, `FlagPrecondition`), `PortalEntryService`, `TownTransitionService`, `PartySpawnService`, `StaticHubMarker`, `ScenePaintedMarkerUtility`.

**Related scenes:** `DistrictTownTest.unity` (DistrictTest hub — **use this for new work**); `DimensionSquareTest.unity` (legacy prototype); `TownTest.unity` → future **`TownHub.unity`**; `town_main` (legacy stamp plaza).

**Related assets (v0 hub slice):** `Assets/Resources/Town/DistrictTest/` — organized town districts under `TownArea/`, buildings under `Building/` (see §6). Legacy flat assets remain at `Assets/Resources/Town/` until migrated.

**Explicitly out of scope (v0 hub milestone):** Town ↔ dungeon scene load changes; procedural district layout; multi-floor building stairs; NPC pathfinding across districts; save/load hub layout across game sessions; gamepad-specific transition UX; separate Unity scene per district; camera scroll across one giant tilemap without floor swap; fast-travel menu (future); transition tiles that require Confirm instead of step-on (exceptions only).

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | Town is composed of **many logical floors** (districts), each a **`DungeonFloorInstance`**, not one monolithic map or many Unity scenes. |
| **G2** | District layouts are **pre-painted in the editor** (`FloorLayoutMode.ScenePainted`) — tiles saved in the hub `.unity`, not stamped at runtime. |
| **G3** | **Multiple transition tiles per district** — JRPG-style: walk north/east/south/west (or through doorways) onto authored cells to reach **different** destination floors (e.g. dimension square → market, dimension square → residential). |
| **G4** | **Bidirectional travel** — each district link has matched **portal link ids** and **arrival anchors** so return trips land at the correct door/corridor. |
| **G5** | **Building interiors** are **scene-painted floors** (shops, homes, guild halls) — same park/activate model as districts; no runtime stamp paint for new interiors. |
| **G6** | Transitions use **fade curtain** + floor swap — not a loading splash or separate Unity scene ([Town buildings §5](Town-Building-Entry-And-Exit-Requirements.md)). |
| **G7** | **Run persistence** — parked districts and interiors retain NPC positions, shop state, and opened interactables within a run. |
| **G8** | **Authoring scales** — new district = painted floor + floor definition + markers + population profile; new transition = marker(s) + portal spec row; no bespoke C# per street. |
| **G9** | **Flag-driven population** — NPCs and optional props spawn (or stay absent) from **`TownFloorPopulationProfile`** + `FlagPrecondition`. |
| **G10** | **Story characters** separate spawn from story logic — complex NPCs = prefab + dialog profile + quest assets; population profile only gates **presence**. |
| **G11** | **Gameplay parity** — party HUD, formation, safe zone, and camera band work on every hub floor unless data overrides. |
| **G12** | **Performance** — unvisited districts are not simulated; first visit runs population phases only (no tile paint); revisits are park/activate. |
| **G13** | **Tile variety** — districts and interiors use **multiple floor and wall tiles** per logical terrain (DCSS / JRPG towns), not a single repeating `floorTile` / `wallTile`. |
| **G14** | **Organized assets** — each new district and building has a **dedicated folder** under `DistrictTest/` with room for multiple related assets per floor. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Town hub** | The single Unity scene hosting all town `DungeonFloorInstance` roots (e.g. `TownHub.unity`). |
| **Town floor** | Any hub `floorId` with `combatPolicy: SafeZone` (or hub-safe region) — districts **and** building interiors. |
| **District floor** | Exterior town area the player walks outdoors (e.g. `town_plaza`, `dimension_square`, `town_market`). |
| **Interior floor** | Walkable space inside a building (e.g. `town_interior_greta_shop`) — see [Town buildings](Town-Building-Entry-And-Exit-Requirements.md). |
| **Scene-painted floor** | `FloorLayoutMode.ScenePainted` — tilemaps authored in the scene; `ScenePaintedLayoutPhase` does not repaint tiles. |
| **Transition point** | One or more **walkable floor cells** on a district that trigger a floor swap when stepped on. |
| **Transition link** | Logical connection: `portalLinkId` + `targetFloorId` + optional `listLabel`; may span **multiple cells** on the source floor. |
| **Portal link pair** | Matched ids on two floors (e.g. `square_to_market` / `market_to_square`) with **arrival bindings** on each side. |
| **Arrival anchor** | Grid cell where the party formation spawns after entering via a given `portalLinkId`. |
| **Static hub marker** | `StaticHubMarker` under `Floors/<floorId>/Markers/` — authoring anchor for spawn, transitions, NPCs, dungeon portal, etc. |
| **Population profile** | `TownFloorPopulationProfile` asset — lists what to spawn at which markers, with optional flag gates. |
| **Parked floor** | Inactive `DungeonFloorInstance` under `Floors/`; tilemaps and `DynamicViewsRoot` children preserved. |
| **JRPG screen change** | Player walks to map edge or doorway → brief fade → adjacent district map; **same engine scene**, different active floor. |
| **Town tile palette** | `DungeonTilePalette` asset listing **multiple** eligible floor or wall `TileBase` entries (with weights) for one district or interior theme. |
| **District asset folder** | `DistrictTest/TownArea/<Name>/` — all ScriptableObjects for one outdoor town floor (definition, palettes, population, …). |
| **Building asset folder** | `DistrictTest/Building/<Name>/` — exterior + interior assets for one building (e.g. `AdventureGuildExchange/`). |
| **Legacy town assets** | Flat files at `Assets/Resources/Town/` (`Floor_town_main`, stamps, demo interior) — pre-`DistrictTest` authoring. |

---

## 3. Reference — JRPG multi-exit towns

Classic hub towns use **several leave points per map**, not a single portal in the center:

| Pattern | Example | JRogue mapping |
|---------|---------|----------------|
| **Cardinal edge strips** | Walk off the north edge of square → market | Multiple transition cells along north wall, **same** `portalLinkId` |
| **Named exits** | “South Gate”, “Harbor Road” | Distinct `listLabel`; may share or split link ids |
| **Doorway thresholds** | Arch into inn | Single cell or 2-wide doorway, `building_*` or `district_*` link |
| **Return symmetry** | Exit market south → arrive square north | **Reciprocal** `PortalArrivalBinding` on each floor |
| **Blocked exit** | Gate closed until quest | Transition cells exist in layout; portal registration skipped or blocked when flag false (§8.4) |

**Locked direction:** Support **many transition points per district** and **many destination floors per district** from day one of the hub milestone. A floor like `dimension_square` may expose **north → market**, **east → shops row**, **west → residential**, and **center → dungeon** simultaneously.

---

## 4. Current baseline (as-is)

| Area | Today | Gap |
|------|-------|-----|
| **Hub scene** | `TownTest.unity` — `town_main` + one demo interior | Single plaza stamp; not multi-district |
| **Scene-painted prototype** | `DimensionSquareTest.unity` — `dimension_square`, 40×40 painted plus, `StaticHubMarker` for spawn / dungeon portal / NPC slots | No district transitions; markers not used for `PortalSetupPhase` |
| **Layout mode** | `ScenePaintedLayoutPhase` + shared population tail | Portal cells resolved from **stamp** or raw `portalCell` only — not scene markers (§7.2) |
| **District transitions** | `PortalInteractable` + `PortalEntryService` exist for buildings | No `district_*` link convention; no multi-cell link authoring |
| **NPC spawn** | `TownNpcSetupPhase` hardcoded for `town_main` + stamp markers | Not data-driven; ignores `StaticHubMarker` |
| **Building interiors** | Stamp-generated `town_interior_demo` | Should migrate to scene-painted for new buildings |
| **Catalog** | `TownCatalog.asset` (2 floors) | Need `DistrictTestCatalog` / `TownHubCatalog` with N district + M interior defs |
| **Asset folders** | Flat `Assets/Resources/Town/*.asset` | **`DistrictTest/TownArea/`** and **`DistrictTest/Building/`** created; example folders `DimensionSquare`, `AdventureGuildExchange` |
| **Tile appearance** | `DungeonFloorDefinition` exposes one `floorTile` + one `wallTile` per floor | **Insufficient** for JRPG/DCSS-style variety; `defaultFloorPalette` / `defaultWallPalette` exist on type but unused for town |
| **Multi-tile paint (prototype)** | `DimensionSquareSceneCreator` picks among **4** DCSS floor tiles when auto-painting | Ad-hoc code list; not yet data-driven palettes in `DistrictTest/` |

---

## 5. Architecture decision — one scene, many floors

### 5.1 — Options considered

| Option | Description | Verdict |
|--------|-------------|---------|
| **A — One hub scene, many `DungeonFloorInstance`s (scene-painted)** | All districts and interiors as children of `Floors/`; park/activate one at a time | **Recommended (locked)** |
| **B — One giant tilemap** | Scroll camera across single map | **Reject** — fog, lighting, population, and memory do not scale |
| **C — Unity scene per district** | `LoadScene("Market")` | **Reject** — breaks DDOL floor manager, shop snapshots, quest state |
| **D — Stamp-only districts** | `PreBakedStamp` per district | **Legacy** — OK for tiny tests; **not** for expansive authored town |
| **E — Hybrid** | Scene-painted districts + scene-painted interiors + stamp legacy for `town_main` until migrated | **Accepted transition path** |

### 5.2 — Locked hub hierarchy

```text
TownHub.unity
├── DungeonTestSystems (managers, UI, party bootstrap)
│   └── Floors/
│       ├── dimension_square      ← scene-painted, may have 3+ transition exits
│       ├── town_market
│       ├── town_docks
│       ├── town_interior_greta_shop
│       └── …
└── DDOL services (flags, quests, shop state, town time, …)
```

**Rules:**

1. **Exactly one** active town floor at a time (same as today).
2. **Do not** `LoadScene` for district ↔ district or building enter/exit.
3. **Do** use `DungeonFloorInstanceManager.TryTransitionPortalForWholeParty` (+ fade for building links).
4. All hub floor definitions for the **DistrictTest** milestone live in **`DistrictTestCatalog`** (or merged **`TownHubCatalog`**) referenced by `DungeonFloorInstanceManager.floorDefinitions`.

---

## 6. Town asset organization (`DistrictTest`)

New hub content for the initial multi-floor implementation lives under **`Assets/Resources/Town/DistrictTest/`**. Legacy assets (`Floor_town_main`, `Stamp_TownPlaza_20x20`, demo building stamps, flat `FacadeOverlay_*`) remain at `Assets/Resources/Town/` until migrated — **do not** add new districts there.

### 6.1 — Top-level layout (locked)

```text
Assets/Resources/Town/
├── DistrictTest/                          ← v0 hub implementation root
│   ├── DistrictTestCatalog.asset          ← floor catalog for this slice (→ TownHubCatalog later)
│   ├── TownArea/                          ← outdoor district floors
│   │   └── <DistrictName>/                ← one folder per town floor
│   │       └── …                          ← see §6.2
│   └── Building/                          ← enterable buildings (exterior + interior data)
│       └── <BuildingName>/                ← one folder per building
│           └── …                          ← see §6.3
├── Npc/                                   ← shared NPC prefabs (unchanged)
└── …                                      ← legacy stamps, TownCatalog, demo assets
```

**Example folders (authoring started):**

| Path | Role |
|------|------|
| `DistrictTest/TownArea/DimensionSquare/` | District floor `dimension_square` and related assets |
| `DistrictTest/Building/AdventureGuildExchange/` | Adventure Guild Exchange exterior + interior assets |

### 6.2 — `TownArea/<DistrictName>/` (one folder per district floor)

Each **outdoor town floor** gets its own folder. Name the folder in **PascalCase** matching the district (e.g. `DimensionSquare`, `Market`, `Docks`). The runtime **`floorId`** remains snake_case (e.g. `dimension_square`, `town_market`).

| Asset (v0 minimum) | Naming pattern | Purpose |
|--------------------|----------------|---------|
| **Floor definition** | `Floor_<floorId>.asset` | `DungeonFloorDefinition` — `layoutMode`, portals, arrivals, safe zone |
| **Floor tile palette** | `Palette_<floorId>_floor.asset` | `DungeonTilePalette` — eligible **floor** tiles + weights (§8) |
| **Wall tile palette** | `Palette_<floorId>_wall.asset` | `DungeonTilePalette` — eligible **wall** tiles + weights (§8) |
| **Population profile** *(future)* | `Population_<floorId>.asset` | `TownFloorPopulationProfile` — NPCs at markers |
| **Catalog entry** | via `DistrictTestCatalog` | Registers this floor with the hub |

**Future assets (same folder — do not require new top-level paths):**

- `FacadeOverlay_<buildingOnDistrict>.asset` when a building facade is tied to this district map  
- `Transitions_<floorId>.asset` — optional authoring aid listing portal link ids (if not only on floor def)  
- District-specific torch / interactable tables  

**Rule:** All ScriptableObjects **primarily used by one district floor** live in that district’s `TownArea/<Name>/` folder.

### 6.3 — `Building/<BuildingName>/` (exterior + interior together)

Each **enterable building** gets one folder (e.g. `AdventureGuildExchange/`). Buildings span **two logical floors**:

| Layer | Typical `floorId` | Assets in building folder |
|-------|-------------------|---------------------------|
| **Exterior footprint** | Lives on a **parent district** scene (facade + door on `dimension_square`, etc.) | `FacadeOverlay_<building>.asset`, entrance portal specs (or cross-ref on district floor def), optional `Building_<name>_Exterior.asset` authoring stub |
| **Interior** | `town_interior_<building_snake>` | `Floor_town_interior_<building>.asset`, interior palettes, `Population_*` |

**Minimum interior set (v0):**

| Asset | Purpose |
|-------|---------|
| `Floor_town_interior_adventure_guild_exchange.asset` | Scene-painted interior floor definition |
| `Palette_town_interior_adventure_guild_exchange_floor.asset` | Interior floor tile list |
| `Palette_town_interior_adventure_guild_exchange_wall.asset` | Interior wall tile list |

**Exterior on district map:** Door / facade cells are painted on the **parent** `TownArea` scene; portal `building_adventure_guild_enter` → interior `floorId` is declared on the **district** floor definition’s `portals` list, while **interior** assets stay in `Building/AdventureGuildExchange/`.

**Rule:** Exterior and interior **data** for one building stay in the **same** `Building/<Name>/` folder even though exterior tiles live on another floor’s tilemap in the hub scene.

### 6.4 — Catalog

| Catalog | Scope |
|---------|--------|
| **`DistrictTestCatalog`** | All floor definitions under `DistrictTest/` for the first hub milestone |
| **`TownHubCatalog`** *(later)* | Superset catalog when legacy `town_main` is merged into the hub scene |

`DungeonFloorInstanceManager.floorDefinitions` references the active catalog’s floor list. `hubScenePath` points at the hub `.unity` scene containing painted `Floors/<floorId>/` instances.

### 6.5 — Shared vs per-floor assets

| Shared (stay outside district folders) | Per-district / per-building |
|----------------------------------------|-----------------------------|
| NPC prefabs (`Resources/Town/Npc/`) | Floor definitions, palettes, population |
| Global Kenney / DCSS `TileBase` under `Assets/TileMaps/Town/` | Facade overlays for buildings on that district |
| `PartyFormationSpawnProfile` defaults | Building interior + exterior portal bindings |

---

## 7. Scene-painted floor authoring

### 7.1 — Per-floor scene object

Each town floor is a child GameObject named **`floorId`** with:

| Child | Purpose |
|-------|---------|
| `Grid/FloorMap`, `Grid/WallMap`, … | **Pre-painted** tilemaps (saved in scene) |
| `Markers/` | `StaticHubMarker` components |
| `Enemies/`, `DynamicViews/` | Runtime spawn roots (same as dungeon instances) |

`DungeonFloorDefinition.layoutMode = ScenePainted`. **No** `layoutStamp` required for pure scene-painted floors.

### 7.2 — First visit vs revisit

| Visit | Behavior |
|-------|----------|
| **First** | `GenerateFirstVisit` runs **population tail only** (portals, NPCs, torches, lighting) — tiles unchanged |
| **Revisit** | Park/activate; **no** regeneration if `IsGenerated && HasPaintedFloorTiles()` |

### 7.3 — Editor workflow (target)

| Step | Action |
|------|--------|
| 1 | Add floor child under `Floors/` (or menu **JRogue → Town → Add Hub Floor**) |
| 2 | Paint tiles in Scene view using district **floor/wall palette** brushes (§8) |
| 3 | Place `StaticHubMarker` objects for spawn, transitions, NPC slots, dungeon portal |
| 4 | Create assets in `DistrictTest/TownArea/<Name>/` — floor def + floor/wall palettes |
| 5 | Create / assign `TownFloorPopulationProfile` when population phase lands |
| 6 | Register in `DistrictTestCatalog`; run **Fix Town Hub Scene** validator |

`DimensionSquareSceneCreator` is the reference for steps 1–3; palettes and folder layout should move into `DistrictTest/TownArea/DimensionSquare/` (§6).

---

## 8. Town floor & wall tile variety

### 8.1 — Problem (current `DungeonFloorDefinition`)

Today each floor definition exposes a single **`floorTile`** and **`wallTile`** (`TileBase`). That matches legacy `town_main` and early demos but **does not** match JRPG towns or DCSS branches, where many cosmetic sprites share one logical walkable floor or blocking wall.

The type already has **`defaultFloorPalette`** and **`defaultWallPalette`** ([Dungeon floor & wall tiles](Dungeon-Floor-And-Wall-Tiles-Requirements.md)) — dungeon zones use these with weighted picks. **New town floors must use palettes**, not lone tiles.

### 8.2 — Reference — DCSS & JRPG towns

| Source | Pattern | JRogue mapping |
|--------|---------|----------------|
| **DCSS** | Many `FLOOR_*` / `WALL_*` sprites per branch theme; weighted `pick_dngn_tile()` | `DungeonTilePalette` entries + weights |
| **JRPG towns** | Cobble variants, grass patches, stone trim, distinct shop floorboards | Hand-painted in scene **and/or** palette for fill tools |
| **Dimension Square (today)** | Four `Dcss_Floor_RectGray*` tiles in procedural paint | Prototype only — should become `Palette_dimension_square_floor` asset |

**Locked:** A town floor is defined by **lists** of eligible tiles (with weights), not one floor sprite and one wall sprite.

### 8.3 — Locked data shape — reuse `DungeonTilePalette`

Do **not** invent a parallel palette system for town v0. Store town palettes as **`DungeonTilePalette`** assets in the district or building folder:

```text
DistrictTest/TownArea/DimensionSquare/
├── Floor_dimension_square.asset
├── Palette_dimension_square_floor.asset    ← entries[]: tile + weight
└── Palette_dimension_square_wall.asset
```

| Field on `DungeonFloorDefinition` | Hub usage |
|-----------------------------------|-----------|
| `defaultFloorPalette` | **Required** for new `DistrictTest` floors |
| `defaultWallPalette` | **Required** for new `DistrictTest` floors |
| `floorTile` / `wallTile` | **Fallback only** — first palette entry or legacy compat; omit for new content |

Palette **`paletteId`** should match the floor theme (e.g. `dimension_square_floor`, `adventure_guild_interior_wall`).

### 8.4 — Scene-painted authoring (primary)

For **`ScenePainted`** districts and interiors, **visual variety is authored by painting multiple tile types** directly on the floor/wall tilemaps in the hub scene. The tilemaps in `Floors/<floorId>/Grid/` are the source of truth (saved in the `.unity` file).

| Role | Detail |
|------|--------|
| **Artist workflow** | Unity Tile Palette brush — pick any tile from the district’s floor/wall palette assets |
| **Runtime** | `ScenePaintedLayoutPhase` does **not** repaint or randomize tiles |
| **Palette assets** | Document which tiles belong to this district; drive editor palettes and optional menu fill tools |

This is how JRPG towns are usually built: deliberate placement of variants (pavement A/B, curb stones, grass edges), not one repeating cell.

### 8.5 — Procedural fill tools (secondary)

When editor menus **auto-paint** an area (e.g. `DimensionSquareSceneCreator.PaintDimensionSquareLayout`), they must **`PickTile` from `defaultFloorPalette` / `defaultWallPalette`** (weighted), not hardcode tile arrays in C#.

| Tool | Palette source |
|------|----------------|
| District bootstrap / “fill rectangle” | Floor def’s `defaultFloorPalette` / `defaultWallPalette` |
| Legacy stamp fill (`LayoutStampPhase`) | Same fields if stamp floors are ever used for town |

### 8.6 — Building facades & doors

Building **exterior** mass, windows, roof, and **door** tiles remain **explicit per-cell** choices ([Town buildings §7](Town-Building-Entry-And-Exit-Requirements.md)) — `TownBuildingFacadeOverlay` or hand-painted wall map. Do **not** randomize facade cells from floor palettes.

**Interior** shops use normal floor/wall palettes for boards, counters, and wallpaper variety.

### 8.7 — Walkability vs appearance

As in dungeon palettes: tile **appearance** does not change walkability. Logical floor vs wall is determined by which **tilemap layer** holds the cell (`FloorMap` vs `WallMap`), not which sprite was chosen from the palette.

---

## 9. Transition points (district ↔ district)

### 9.1 — Transition types

| Type | `portalLinkId` prefix | Trigger | Example |
|------|----------------------|---------|---------|
| **District transition** | `district_` | Step-on walkable floor cell | Square north edge → market |
| **Building enter** | `building_` | Step-on entrance tile | Shop door → interior |
| **Building exit** | `building_` | Step-on interior exit | Interior → plaza door anchor |
| **Dungeon portal** | (town portal service) | Step-on + town-time gate | Center of square → dungeon |

District and building transitions share **`PortalInteractable`** + **`PortalEntryService`**; building links additionally use **`TownTransitionService`** fade (existing `building_*` detection).

### 9.2 — Multiple activation cells per link (JRPG edge strips)

A single **transition link** MAY activate from **one or many** grid cells on the source floor.

| Authoring style | Data shape | Use when |
|-----------------|------------|----------|
| **Single tile** | One marker / one `portalCell` | Doorway, narrow path |
| **Multi-tile strip** | Several markers **or** one spec with `portalCells[]` sharing `portalLinkId` | Map edge — player can step off anywhere along north wall |
| **Multiple destinations** | **Different** `portalLinkId` per exit on same floor | Square north → market, square east → shops |

**Implementation (locked):**

- `DungeonPortalSpec` gains optional **`portalMarkerIds[]`** (or repeated specs with the **same** `portalLinkId` and `targetFloorId`) so `PortalSetupPhase` registers one `PortalInteractable` per cell, all invoking the same transition.
- Scene-painted floors resolve marker positions via **`ScenePaintedMarkerUtility`** + marker kind **`FloorTransition`** (§11).

### 9.3 — Bidirectional link authoring

Every district connection is authored **in pairs**:

```text
dimension_square                          town_market
├── transition: district_square_to_market   ├── transition: district_market_to_square
│   cells: north edge strip (y=39, x=15..24)│   cells: south edge strip (y=0, x=15..24)
│   target: town_market                     │   target: dimension_square
└── arrivalBinding: district_market_to_square └── arrivalBinding: district_square_to_market
    anchor: (20, 1, 0)  (just inside south)     anchor: (20, 38, 0) (just inside north)
```

**Naming convention:**

- `district_<source>_to_<dest>` for forward link id
- `district_<dest>_to_<source>` for return link id
- Arrival binding on floor **F** uses the link id the player **entered through** (existing `PortalArrivalBinding` model).

### 9.4 — Step-on rules (locked)

| Rule | Detail |
|------|--------|
| **Trigger** | **Step-on** — party member enters activation cell (`PortalEntryService`; formation: active member only per building fix). |
| **Party** | **Whole party** teleports via `TryTransitionPortalForWholeParty`. |
| **Turn cost** | **No turn** in safe zone. |
| **Fade** | **Short curtain** (0.25–0.5 s) for district links; same as buildings. |
| **Facing** | v0: preserve leader facing or snap to arrival “outward” facing — **TBD** (§19 Q3). |
| **Blocked link** | If `FlagPrecondition` fails, **do not register** portal at generation, or register with `TryActivate` gate + toast (`"The gate is locked."`). |

### 9.5 — Example — dimension square with three district exits

```text
dimension_square (40×40)
  North strip (many cells)  → town_market      link: district_square_to_market
  East strip                → town_shops_row   link: district_square_to_shops
  West strip                → town_residential link: district_square_to_residential
  Center cell               → dungeon          TownPortalSetupPhase (existing)
```

`town_market` might additionally expose:

- South strip → `dimension_square`
- East strip → `town_docks`
- Single doorway → `town_interior_adventure_guild_exchange` (`building_adventure_guild_enter`)

---

## 10. Building interiors (scene-painted)

### 10.1 — Locked approach

| Layer | Layout | Transitions |
|-------|--------|-------------|
| **Exterior district** | Scene-painted | `district_*` links at streets; `building_*` at door cells |
| **Interior** | **Scene-painted** floor instance | `building_*` enter/exit pair; see [Town buildings](Town-Building-Entry-And-Exit-Requirements.md) |

**Rationale:** Pre-painted shop layouts are faster to iterate, avoid stamp/regenerate bugs, and load only population on first enter. Aligns with district authoring.

### 10.2 — Interior floor checklist

| Item | Requirement |
|------|-------------|
| `floorId` | `town_interior_<building_snake>` (assets in `DistrictTest/Building/<Name>/`) |
| `layoutMode` | `ScenePainted` |
| Tiles | Counter, shelves, back room — painted in editor using **interior palettes** (§8) |
| Exit marker | `StaticHubMarker` kind `BuildingExit` |
| NPC markers | `NpcSlot` per character (`markerId` stable for population profile) |
| Portal spec | Exit link → parent district + arrival at exterior door |

### 10.3 — Exterior building on district map

Building **mass** remains non-walkable wall tiles; **entrance** is walkable floor with door art ([Town buildings §7](Town-Building-Entry-And-Exit-Requirements.md)). Entrance cell(s) registered as `building_*` portal — not `district_*`.

### 10.4 — Flag-gated exits and interiors

| Case | Behavior |
|------|----------|
| **Quest opens new district** | Register `district_*` portal when flag set; or pre-register with activate gate |
| **NPC only inside after quest** | Population profile entry with `FlagPrecondition` |
| **Shop closed at night** | [Town time](Town-Time-And-Calendar-Requirements.md) on interactable or portal gate (future) |

---

## 11. Static hub markers

### 11.1 — Marker kinds (extend `StaticHubMarkerKind`)

| Kind | Purpose | `markerId` example |
|------|---------|-------------------|
| `PlayerStart` | Initial spawn when floor loads without portal link | — |
| `DungeonPortal` | Town → dungeon (hub floors) | — |
| **`FloorTransition`** | District ↔ district activation cell(s) | `square_market_north` |
| **`BuildingEntrance`** | Exterior door → interior | `greta_shop_door` |
| **`BuildingExit`** | Interior → exterior district | `greta_shop_exit` |
| `NpcSlot` | NPC spawn anchor | `market_npc_greta` |
| **`InteractableSlot`** | Lever, altar, sign (future) | `market_quest_board` |

Multiple markers **may share** the same `portalLinkId` when they are part of one edge strip (§9.2). Authoring convention: suffix `_01`, `_02` for strip segments, identical `portalLinkId` in floor definition portal spec list.

### 11.2 — Marker resolution order

At generation, phases resolve grid cells in this order:

1. `ScenePaintedMarkerUtility` + kind + `markerId` (scene-painted)
2. `DungeonLayoutStamp.TryGetMarker` (stamp legacy)
3. Explicit `portalCell` on `DungeonPortalSpec`

---

## 12. Data model

### 12.1 — `DungeonFloorDefinition` (per town floor)

| Field | Hub usage |
|-------|-----------|
| `floorId` | Stable id (`dimension_square`, `town_market`, `town_interior_adventure_guild_exchange`, …) |
| `layoutMode` | **`ScenePainted`** for new `DistrictTest` content |
| **`defaultFloorPalette`** | **`DungeonTilePalette`** — multiple floor tiles + weights (**required** new) |
| **`defaultWallPalette`** | **`DungeonTilePalette`** — multiple wall tiles + weights (**required** new) |
| `floorTile` / `wallTile` | Legacy fallback only; do not use as sole tile source for new floors |
| `combatPolicy` | **`SafeZone`** for town districts and interiors |
| `participatesInDungeonTime` | Hub districts: typically **false** |
| `portals` | All `district_*` and `building_*` links originating on this floor |
| `arrivalBindings` | Where to spawn when entering **from** each incoming link id |
| `formationProfile` | Party spawn layout at arrivals |
| **`townPopulationProfile`** *(new)* | Reference to `TownFloorPopulationProfile` |

**Asset path:** floor definition lives in `DistrictTest/TownArea/<Name>/` or `DistrictTest/Building/<Name>/` per §6.

### 12.2 — `DistrictTestCatalog` / `TownHubCatalog`

| Field | Purpose |
|-------|---------|
| `hubScenePath` | `Assets/Scenes/Town/TownHub.unity` (or `DimensionSquareTest.unity` during prototype) |
| `startFloorId` | e.g. `dimension_square` |
| `floors[]` | All `DungeonFloorDefinition` assets under `DistrictTest/` |

### 12.3 — `TownFloorPopulationProfile` *(new ScriptableObject)*

```text
TownFloorPopulationProfile
├── floorId: string (must match definition)
└── entries[]:
    ├── markerId: string          → NpcSlot / InteractableSlot on this floor
    ├── prefab: GameObject        → Resources path or direct ref
    ├── spawnCondition: FlagPrecondition | Always
    ├── optional: shopDefinition, npcId override
    └── optional: despawnWhenFalse (default: never spawned if false at first visit)
```

**Rules:**

- One profile per floor (district or interior).
- **Story complexity** lives on prefab (`NpcDialogProfile`, quests), not in the profile.
- Profile answers: **who spawns where, and when**.

### 12.4 — Portal spec extensions

| Field | Purpose |
|-------|---------|
| `portalLinkId` | Unique per directed link (or shared across cells of same link) |
| `targetFloorId` | Destination floor |
| `portalMarkerId` | Single marker (stamp or scene) |
| **`portalMarkerIds[]`** *(new, optional)* | Multi-cell / edge strip |
| `portalCell` | Fallback explicit cell |
| `listLabel` | Debug / future UI ("North to Market") |
| `adjacentConfirmOnly` | **false** default for hub transitions |
| **`activationCondition`** *(new, optional)* | `FlagPrecondition` — gate registration or activate |

---

## 13. Generation pipeline

### 13.1 — Scene-painted hub phases

```text
GenerateFirstVisit (scene-painted town floor):
  1. ScenePaintedLayoutPhase        → PlayerStart from marker
  2. PortalSetupPhase               → district_*, building_* portals (incl. multi-cell)
  3. TownPortalSetupPhase           → dungeon portal (hub floors only)
  4. TownBuildingDoorSetupPhase     → adjacentConfirmOnly exceptions only
  5. TownPopulationSetupPhase       → NEW: replaces TownNpcSetupPhase + interior variant
  6. TownTorchSetupPhase            → if torch markers present
  7. TownBuildingFacadeVisualPhase  → SKIP for pure interiors; optional on districts with overlay data
  8. LightingInitPhase              → hub daylight
  (skip LayoutStampPhase, enemy/hazard/trap population)
```

### 13.2 — `TownPopulationSetupPhase` (replaces hardcoded NPC phase)

| Step | Action |
|------|--------|
| 1 | Load `townPopulationProfile` from floor definition |
| 2 | For each entry, resolve cell from marker |
| 3 | Evaluate `spawnCondition` |
| 4 | Instantiate under `DynamicViewsRoot`; `GridMover.InitializeAtGridAnchor` |
| 5 | Hydrate `ShopNpcController` from `TownShopStateService` if applicable |

**Backward compatibility:** If no profile assigned, fall back to legacy `TownNpcSetupPhase` table for `town_main` stamp markers until migration completes.

---

## 14. Runtime transition flow

```text
Player steps on transition cell
  → PortalEntryService (active party member if formation on)
  → PortalInteractable.TryActivatePartyTeleport
  → if building_* : TownTransitionService.RunTransition (fade)
  → else district_* : TownTransitionService or direct floor manager with fade
  → DungeonFloorInstanceManager.TryTransitionPortalForWholeParty(linkId, targetFloorId)
  → park source floor, activate target (generate first visit if needed)
  → PartySpawnService at arrival binding for linkId
  → camera follow refresh, visibility/lighting bind
```

---

## 15. Persistence & services

| State | Storage | Notes |
|-------|---------|-------|
| Story flags | `GameStoryFlagService` (DDOL) | Gates portals and spawns |
| Quest progress | `QuestService` (DDOL) | Independent of floor layout |
| Shop gold/stock | `TownShopStateService` (DDOL) | NPCs scene-bound; state snapshot keyed by shop id |
| Talk counts | `NpcTalkCounterService` | Per `npcId` |
| Parked floor actors | `DynamicViewsRoot` on instance | Survive district leave |
| Tilemaps | Scene + instance | Never regenerated if scene-painted |

**Town ↔ dungeon round-trip:** Parked hub floors must survive scene load; on return, re-bind active floor and respawn party per `TownArrivalService` / existing dungeon exit flow.

---

## 16. Relationship to other specs

| Spec | Relationship |
|------|--------------|
| [Town buildings](Town-Building-Entry-And-Exit-Requirements.md) | **Interiors** and `building_*` links — subset of this hub model; building doc §7 tile semantics unchanged |
| [Safe zones](Safe-Zone-Requirements.md) | All hub floors default safe |
| [NPC dialog](NPC-Dialog-Requirements.md) | Talk targets spawned by population profile |
| [Shop NPCs](Shop-NPC-Requirements.md) | Shop interiors + DDOL snapshots |
| [Quest](Quest-Requirements.md) | Flags gate population and optional portal activation |
| [Dungeon floor & wall tiles](Dungeon-Floor-And-Wall-Tiles-Requirements.md) | **`DungeonTilePalette`** reused for town; scene-painted primary, weighted fill secondary |
| [Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md) | Same instance manager; hub uses `ScenePainted` not zone composite |

---

## 17. v0 hub milestone roster (illustrative)

| Floor id | Type | Asset folder | Transitions out (examples) |
|----------|------|--------------|----------------------------|
| `dimension_square` | District | `DistrictTest/TownArea/DimensionSquare/` | N→`town_market`, E→`town_shops_row`, dungeon center |
| `town_market` | District | `DistrictTest/TownArea/Market/` *(TBD)* | S→`dimension_square`, E→`town_docks`, door→`town_interior_adventure_guild_exchange` |
| `town_docks` | District | `DistrictTest/TownArea/Docks/` *(TBD)* | W→`town_market` |
| `town_interior_adventure_guild_exchange` | Interior | `DistrictTest/Building/AdventureGuildExchange/` | exit→`town_market` at door anchor |

Exact cells authored in scene markers; not fixed in this doc.

---

## 18. Acceptance criteria (v0 hub milestone)

### Asset organization

1. New hub floors and buildings are authored under **`Assets/Resources/Town/DistrictTest/`** — not as new flat files in `Resources/Town/`.  
2. **`TownArea/DimensionSquare/`** contains at least `Floor_dimension_square.asset` + floor/wall palette assets.  
3. **`Building/AdventureGuildExchange/`** exists with interior floor (+ palettes) assets defined or stubbed.  

### Tile variety

4. `dimension_square` shows **≥ 3 distinct floor** tile sprites on the painted map (or palette has ≥ 3 weighted floor entries used in scene).  
5. New `DistrictTest` floor definitions reference **`defaultFloorPalette`** and **`defaultWallPalette`** — not only `floorTile` / `wallTile`.  
6. Editor fill tools (e.g. Dimension Square creator) pick from palette assets, not hardcoded tile arrays.  

### Scene-painted floors

7. At least **two district floors** + **one interior** exist as scene-painted instances under `Floors/` in one hub scene.  
8. First visit **does not repaint** tiles; population phases run once.  
9. Revisit activates parked instance in **≤ 300 ms** perceived.  

### Multi-exit transitions

10. `dimension_square` has **≥ 2 distinct `district_*` transitions** to **different** target floors.  
11. At least one transition uses **≥ 3 activation cells** (edge strip) sharing one `portalLinkId`.  
12. Step-on any strip cell triggers the same transition.  
13. Return link lands party on correct **arrival anchor** (facing rule per §19 Q3).  
14. No `LoadScene` for district ↔ district.  

### Building interior

15. **Adventure Guild Exchange** (or equivalent) — scene-painted interior with painted tiles and exit marker.  
16. Enter/exit uses `building_*` links + fade; party returns to exterior door anchor.  

### Population

17. `TownFloorPopulationProfile` drives spawn on **≥ 1** district and **≥ 1** interior.  
18. One NPC entry uses **`FlagPrecondition`** — absent when false, present when true (test flag).  
19. Story NPC uses **`NpcDialogProfile`** on prefab — no custom spawn phase.  

### Gameplay

20. Formation, HUD, safe zone work on all hub floors.  
21. Transition blocked while `GameplayModalGate.BlocksFloorGameplay`.  

---

## 19. Open questions

| # | Question | Default if unresolved |
|---|----------|------------------------|
| **Q1** | Migrate `town_main` stamp to scene-painted `town_plaza`, or keep stamp in parallel? | **New districts scene-painted**; migrate plaza when hub milestone ships |
| **Q2** | Register blocked portals at gen vs activate-time gate? | **Activate-time gate** with toast; layout markers remain for artists |
| **Q3** | Arrival facing: preserve leader facing vs snap outward from link? | **Snap outward** from arrival cell toward map interior |
| **Q4** | Max parked hub floors per run before memory warning? | **12** instances (document in performance §; soft log) |
| **Q5** | Single `TownHub.unity` vs keep `DimensionSquareTest` separate? | **Merge into `TownHub.unity`** when second district lands |
| **Q6** | Edge transitions: require full party on strip or leader only? | **Leader step-on** triggers whole-party teleport (current portal model) |

| **Q7** | Separate `TownTilePalette` type vs reuse `DungeonTilePalette`? | **Reuse `DungeonTilePalette`**; store under `DistrictTest/` paths |

---

## 20. Resolved decisions (draft)

| # | Decision | Resolution |
|---|----------|------------|
| **D1** | Unity scene per district? | **No** — one hub scene, many floor instances |
| **D2** | District layout authoring? | **`ScenePainted`** in editor (DimensionSquare model) |
| **D3** | Interior layout authoring? | **`ScenePainted`** — stamps deprecated for new interiors |
| **D4** | Multiple exits per district? | **Yes** — many `district_*` links and multi-cell strips per floor |
| **D5** | Transition feel? | **Fade curtain** — JRPG screen change without loading splash |
| **D6** | NPC extensibility? | **`TownFloorPopulationProfile`** + markers; story on prefab/dialog/quest |
| **D7** | Mechanism? | **`PortalInteractable`** + `TryTransitionPortalForWholeParty` (extend marker resolution) |
| **D8** | Single floor/wall tile per definition? | **No** — **`defaultFloorPalette` / `defaultWallPalette`** (tile lists + weights) |
| **D9** | Where do new hub assets live? | **`Assets/Resources/Town/DistrictTest/`** with `TownArea/<Name>/` and `Building/<Name>/` |
| **D10** | Scene-painted tile variety? | **Hand-painted tilemaps** in hub scene; palettes for tooling + documentation |

---

## 21. Implementation backlog (ordered)

| # | Task | Depends |
|---|------|---------|
| 1 | Extend `StaticHubMarkerKind` (`FloorTransition`, `BuildingEntrance`, `BuildingExit`, `InteractableSlot`) | — |
| 2 | `PortalSetupPhase`: resolve scene-painted markers; multi-cell per `portalLinkId` | 1 |
| 3 | `TownTransitionService`: fade for `district_*` links (not only `building_*`) | 2 |
| 4 | `TownFloorPopulationProfile` + `TownPopulationSetupPhase` | 1 |
| 5 | **`DistrictTestCatalog`** + move `Floor_dimension_square` into `TownArea/DimensionSquare/` | — |
| 6 | Create **`Palette_dimension_square_floor/wall`**; wire `defaultFloorPalette` / `defaultWallPalette` on floor def | 5 |
| 7 | Refactor `DimensionSquareSceneCreator` to load tiles from palettes, not hardcoded arrays | 6 |
| 8 | Editor **Fix Town Hub Scene** menu; scaffold `Building/AdventureGuildExchange/` assets | 5, 6 |
| 9 | Author `town_market` + links from `dimension_square` | 2, 8 |
| 10 | Scene-painted `town_interior_adventure_guild_exchange` pilot | 2, 4, 8 |
| 11 | Migrate legacy `TownNpcSetupPhase` to profiles for `town_main` | 4 |

---

## 22. Document history

| Version | Date | Notes |
|---------|------|-------|
| Draft | 2026-06-16 | Initial hub multi-floor spec: scene-painted districts, multi-exit JRPG transitions, population profiles, scene-painted interiors. |
| Draft | 2026-06-16 | §6 `DistrictTest/` asset layout (`TownArea/`, `Building/`); §8 tile palettes; `AdventureGuildExchange` example; multi-tile variety requirements. |
