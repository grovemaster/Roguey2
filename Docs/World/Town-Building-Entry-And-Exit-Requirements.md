# Town buildings — Requirements

**Status:** **Partial (demo building updated)** — step-on enter/exit, interior exit door overlay, and 7×4 exterior facade are wired in data; run **JRogue → Town → Fix TownTest Scene** to regenerate stamps/overlays in Unity.

**Purpose:** Specify how **buildings** work on the town hub map: which tiles block whom, how facades read from a distance, how **entrances** and **interior exits** are authored, how enter/exit transitions feel, and how interiors integrate with floor-instance and safe-zone systems. Town building access is a core JRPG hub loop (shops, inns, story NPCs, services) and should feel snappy while supporting distinct interior spaces.

**Depends on:** [Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md) (`DungeonFloorInstance`, `DungeonFloorInstanceManager`, `PortalInteractable`), [Safe zones](Safe-Zone-Requirements.md), [Town time & calendar](Town-Time-And-Calendar-Requirements.md), [Shop NPCs](Shop-NPC-Requirements.md), [NPC dialog](NPC-Dialog-Requirements.md), [Door requirements](Door-Requirements.md) (future doorway tiles), [Party Control HUD](../UI/Party-Control-HUD-Requirements.md), `PlayfieldLayout`, `CameraFollow`, `PartySpawnService`, `RunPartyPersistence`.

**Related scenes:** `TownTest.unity` (production **Town** scene TBD); `town_main` plaza floor; one `DungeonFloorDefinition` per building interior (v0).

**Explicitly out of scope (v0):** Multi-floor buildings (stairs between interior levels); destructible interiors; combat inside shops; NPC pathfinding between exterior and interior; save/load of interior actor positions across game sessions; procedural interior layout; gamepad-specific transition UX; building interiors as separate Unity scenes; habitat-style zone composites inside town.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | Player can **enter** authored buildings from the town plaza and **exit** back to the same exterior door location. |
| **G2** | Transitions feel **responsive** — no full scene load for ordinary buildings; target **&lt; 300 ms** perceived on revisit, **&lt; 1 s** on first enter (including generation). |
| **G3** | **Exterior town layout stays readable** — building facades are visible on the plaza; doors are obvious interaction points. |
| **G4** | **Interior spaces are distinct** — separate tilemap footprint per building (or shared stamp with per-building markers), not a modal fake overlay for walkable interiors. |
| **G5** | **Gameplay parity** — party HUD, hotbar, camera band, formation, and safe-zone rules work identically in plaza and interiors unless explicitly overridden. |
| **G6** | **Run persistence** — interior state (NPC stock, opened chests, quest flags) survives enter/exit and town ↔ dungeon travel via DDOL services; floor `GameObject`s may be parked, not destroyed. |
| **G7** | **Authoring scales** — new building = stamp + floor definition + marker pair + optional setup phase; no bespoke C# per shop. |
| **G8** | **Performance budget** — walking the plaza does not generate or simulate unvisited interiors; memory for parked interiors stays within a documented cap. |
| **G9** | **Building tiles are solid** — party, NPCs, and monsters cannot walk through building mass (§7.1). |
| **G10** | **Facades read from a distance** — multi-row wall/window/roof footprint visible before the player reaches the door (§7.2). |
| **G11** | **Entrances and exits are obvious floor tiles** — perimeter step-on doors in; interior step-on doors out (§7.3–§7.4). |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Town plaza** | Exterior hub floor `town_main` — paved stamp, NPCs, portal, levers. |
| **Building interior** | A separate **`DungeonFloorInstance`** (e.g. `town_interior_mira_home`) activated when the player enters a door. |
| **Floor transition** | `DungeonFloorInstanceManager.TryTransitionPortalForWholeParty` — park active floor, activate target, respawn party at portal anchor. **Same mechanism as dungeon floor portals.** |
| **Scene transition** | `SceneManager.LoadScene(..., Single)` — town ↔ dungeon only today; **not** the default for buildings (v0). |
| **Building mass tile** | Non-walkable facade cell (stone wall, window, corner, roof) on the **wall** tilemap — blocks all actors. |
| **Entrance tile** | Walkable **floor** cell on the building **perimeter** with an open/door visual; stepping on it enters the building (§7.3). |
| **Exit tile** | Walkable **floor** cell on the **interior** perimeter with an open/door visual; stepping on it returns to the plaza (§7.4). |
| **Door interactable** | Runtime portal registration on an entrance or exit cell — **step-on** activation (§6). |
| **Portal link pair** | Matched ids (`building_mira_enter` / `building_mira_exit`) binding exterior door ↔ interior arrival marker. |
| **Parked floor** | Generated `DungeonFloorInstance` disabled under `Floors` root; state preserved until run ends. |
| **Transition curtain** | Brief full-screen or playfield fade during floor swap (§5) — **not** a separate game screen. |
| **Full-screen service UI** | Shop menu, dialog, inventory — overlays gameplay; **does not replace** walkable interior maps. |

---

## 3. Current baseline (as-is)

| Area | Today |
|------|--------|
| **Town map** | 20×20 pre-baked stamp (`Stamp_TownPlaza_20x20`); generic `Town_WallBuilding` border walls plus a **demo stone facade** overlay (`FacadeOverlay_town_main`) for one 5×3 building. |
| **Building transitions** | `PortalInteractable` + `TownTransitionService` + fade curtain; demo links `building_demo_enter` / `building_demo_exit` between `town_main` and `town_interior_demo`. |
| **Town ↔ dungeon** | Modal dialog → **full scene load** (`DungeonEntryService` / `DungeonExitService`). |
| **Shops** | Plaza NPCs still open **full-screen shop UI** over the plaza; demo interior has dialog Host only. |
| **Doors (dungeon)** | [Door requirements](Door-Requirements.md); town `doorPolicy: None`. |
| **Markers** | `building_demo_door`, `building_demo_arrival`, `building_demo_exit`, `building_demo_npc`. |

### 3.1 — TownTest demo building vs §7

| Requirement | Expected (§7) | TownTest demo |
|-------------|---------------|---------------|
| **Building mass blocks all actors** | Party, NPCs, monsters cannot walk wall/roof cells. | Stamp + wall-layer facade; `TownNpc5` moved off footprint to `(9,8)`. |
| **Readable from a distance** | Multi-row facade (walls + windows + **roof row**). | **7×4** overlay (`FacadeOverlay_town_main`); door at `(14,8)`. |
| **Entrance = perimeter floor + step-on** | Walkable door tile on outer edge; step-on enters. | South-edge door at `(14,8)`; `adjacentConfirmOnly: 0` → `PortalInteractable` step-on. |
| **Exit = interior open floor + step-on** | Walkable door tile on interior perimeter; step-on exits. | `FacadeOverlay_town_interior_demo` paints door tile at exit `(2,3)`; step-on portal. |
| **Zero entrances allowed** | Decorative facade-only buildings valid. | Not demonstrated. |
| **Multiple entrances** | Multiple entrance portals per building. | Demo has **one** entrance only. |
| **Confirm-adjacent enter** | Exceptional doors only (`adjacentConfirmOnly`). | Demo uses step-on; `TownBuildingDoorSetupPhase` retained for exceptions. |

---

## 4. Architecture decision — how buildings are implemented

### 4.1 — Options considered

| Option | Description | Load cost | State | Verdict |
|--------|-------------|-----------|-------|---------|
| **A — Floor instance (same Unity scene)** | Each interior = `floorId` + stamp; `PortalInteractable` / door service swaps active floor | **First visit:** one stamp generate. **Revisit:** park/activate only (~instant). | Parked instances retain tile/actor state. | **Recommended (locked v0)** |
| **B — Separate Unity scene per building** | `LoadScene("MiraHome")` like town↔dungeon | **High** — destroys/recreates hierarchy, reloads DDOL-adjacent bindings | Awkward with existing `RunPartyPersistence` | **Reject** for ordinary buildings |
| **C — Single map, scroll/zoom interior** | Camera pans into building facade; interior drawn on same tilemap | Low | Hard to isolate NPCs, lighting, fog | **Reject** — fights stamp/floor pipeline |
| **D — Full-screen static interior** | Image + menu buttons, no grid movement | Lowest | No exploration; poor fit for party formation HUD | **Reject** for walkable hubs; OK for **pure menu** shops (partial v0 today) |
| **E — Hybrid** | Walkable interior (A) + full-screen UI when talking to shopkeeper inside | Medium | Best UX for CRPG shops | **Recommended** where shop NPC is inside |

### 4.2 — Locked recommendation (v0)

```text
TownTest.unity (single scene)
├── Floors/
│   ├── town_main          ← active on plaza
│   ├── town_interior_*    ← generated on first enter, then parked
│   └── …
└── DDOL systems (party, town time, shop state, …)
```

**Enter building:** door interactable → optional transition curtain (§5) → `TryTransitionPortalForWholeParty(enterLinkId, interiorFloorId)`.

**Exit building:** interior exit cell/door → same API with **exit** link → `town_main` at exterior door anchor.

**Do not** use `SceneManager.LoadScene` for building entry.

**Rationale:** Reuses proven dungeon portal path (`PortalInteractable`, `PartySpawnService`, `BindVisibilityToActiveFloor`); revisits are cheap; aligns with [Shop NPC §10](Shop-NPC-Requirements.md) persistence model (DDOL snapshot + parked floor).

---

## 5. Transition UX — separate screen vs fade

### 5.1 — Definitions

| Pattern | What the player sees | Use when |
|---------|----------------------|----------|
| **Separate screen** | Cut to loading splash, title card, or non-gameplay view | **Town ↔ dungeon** (already modal + scene load); optional future **fast travel** |
| **Partial fade (curtain)** | Gameplay view dims 0.25–0.5 s while floor swaps behind it; party hidden briefly | **Building enter/exit** (v0 **recommended**) |
| **Hard cut** | Instant tilemap swap | Debug only — visible pop, NPC flicker |
| **Full-screen service UI** | Shop/inventory over **frozen or live** map | **After** entering interior, talking to merchant — not a substitute for entry |

### 5.2 — Locked UX (v0)

| Transition | Visual | Audio (optional v0.1) |
|------------|--------|------------------------|
| **Plaza → interior (first visit)** | **Fade out → generate (if needed) → fade in**; total ≤ **1 s** typical | Door open SFX |
| **Plaza → interior (revisit)** | **Short fade** 0.25–0.35 s or slide curtain | Same |
| **Interior → plaza** | Same short fade; spawn at exterior door facing outward | Exit SFX |
| **Shop open inside interior** | **No map transition** — `ShopNpcMenuUI` full-screen over interior (existing pattern) | — |
| **Town ↔ dungeon** | **Keep existing modal** (`EnterDungeonDialogUI` / `DungeonEndedDialogUI`); scene load unchanged | — |

**Answer to “separate screen or partial fade?”**

- **Partial fade (transition curtain)** for **enter/exit walkable buildings** — preserves spatial continuity, hides tilemap swap, cheap to implement.
- **Separate screen** only for **scene-scale** moves (town ↔ dungeon) or future **narrative interstitials** — not for every shop door.
- **Full-screen UI** for **commerce and dialog** — layered on top **after** entry, not instead of entry.

### 5.3 — `TownTransitionService` (new, v0)

Centralize building transitions (greenfield — no fade infra today):

| Responsibility | Detail |
|----------------|--------|
| **`TryEnterBuilding(doorDef)`** | Block if `GameplayModalGate`; play fade; call floor manager; refresh camera; fade in |
| **`TryExitBuilding(exitDef)`** | Symmetric |
| **Reentrancy guard** | Ignore input during transition (mirror `_portalTransitionInProgress`) |
| **Failure** | Fade in without swap; log reason (door locked, phase closed) |

**Implementation sketch:** `CanvasGroup` overlay on gameplay canvas (sort order above playfield, below modals) or dedicated `TownTransitionCurtainUI` DontDestroyOnLoad singleton.

**Not v0:** directional wipe, interior preview thumbnail, loading progress bar (unnecessary for stamp-sized maps).

---

## 6. Interaction model

### 6.1 — Exterior entrance (plaza)

| Rule | Detail |
|------|--------|
| **Trigger** | **Step-on entrance tile** (same as dungeon floor portals). `adjacentConfirmOnly` is for **exceptional** doors only (locked vault, story gate). |
| **Tile** | **Walkable floor** on the building **perimeter** with an open/door sprite — not a wall cell the player cannot reach. |
| **Count** | **Zero or more** entrances per building. No portal specs → facade is decorative only. |
| **Preflight (optional)** | Locked door → toast (`"Closed at night."`); no transition. Entrance tile may show closed-door art. |
| **Open entrance** | `TownTransitionService` → interior floor. |
| **Turn cost** | **No turn** in safe zone. |
| **Party** | **Whole party** teleports to interior arrival anchor. |

### 6.2 — Interior exit

| Rule | Detail |
|------|--------|
| **Exit tile(s)** | One or more **walkable floor** cells on the **interior perimeter** painted with open/door art — the player must **see** a doorway, not a blank floor square. |
| **Marker** | `building_<id>_exit` on each exit cell (or primary exit when multiple). |
| **Trigger** | **Step-on** (locked for v0). |
| **Spawn** | Exterior `building_<id>_door` anchor (or matching entrance when multiple); party faces **outward**. |

### 6.3 — Sequence

```text
[Plaza] Player steps onto door tile
  → TownTransitionService fade out
  → TryTransitionPortalForWholeParty("building_mira_enter", "town_interior_mira")
  → PartySpawnService at interior anchor
  → CameraFollow refresh
  → fade in

[Interior] Player step on exit
  → fade out
  → TryTransitionPortalForWholeParty("building_mira_exit", "town_main")
  → anchor at exterior door
  → fade in
```

---

## 7. Building tiles — semantics & authoring

This section is the **authoritative** definition of what makes a tile part of a building. Enter/exit **transitions** (§4–§6) depend on these rules being correct in stamps and facade overlays.

### 7.0 — Tile categories

| Category | Tilemap | Walkable? | Blocks party / NPC / monster? | Purpose |
|----------|---------|-----------|----------------------------------|---------|
| **Plaza floor** | Floor | Yes | No | Open town pavement. |
| **Building mass** | Wall | No | **Yes — all actor kinds** | Stone walls, corners, windows, roof segments. |
| **Entrance** | Floor | Yes | No | Perimeter doorway into interior; hosts enter portal. |
| **Interior floor** | Floor | Yes | No | Walkable room inside building instance. |
| **Interior wall** | Wall | No | **Yes** | Interior room shell. |
| **Exit** | Floor | Yes | No | Perimeter doorway back to plaza; hosts exit portal. |
| **Facade prop** (optional) | `DynamicViewsRoot` sprite | — | No collision | Awning, sign — visual only. |

**Locked rule B1:** If a cell is **building mass**, `MapManager.IsWalkable(cell)` is **false**. Pathfinding for party members, town NPC wander, and monsters (if ever spawned on hub floors) must respect this — no special-case “NPCs can clip through shops.”

**Locked rule B2:** Only **entrance** and **exit** cells may be walkable **and** registered as building portals. Never register a portal on a wall-layer cell.

### 7.1 — Walkability & occupancy

| Actor | Building mass | Entrance / exit | Plaza |
|-------|---------------|-----------------|-------|
| **Party** | Blocked | Step-on triggers transition | Walk |
| **Town NPC** | Blocked | Blocked (NPCs do not use portals v0) | Walk |
| **Monster** | Blocked | Blocked | Walk (hub is safe zone; rule still applies if policy changes) |

Implementation: existing `MapManager.IsWalkable` — wall tile present ⇒ not walkable. Population phases (`PopulationPlacementUtility`, `TownNpcSetupPhase`) must only place NPCs on walkable cells. **Acceptance:** no NPC or party member may end a move inside a building mass cell.

### 7.2 — Visibility from a distance

Players must recognize a **building as a building** before they step on its door — not merely see a one-tile wall strip at ground level.

| Rule | Detail |
|------|--------|
| **Multi-row footprint** | A facade occupies **multiple grid rows** (typical: door row + wall/window row(s) + **roof row**). Each row is building mass on the wall map except entrance cells. |
| **Approach axis** | Door row sits on the **perimeter edge** facing the plaza path (usually south or toward open pavement). Roof and upper walls occupy cells **deeper** into the building footprint (north / away from approach in default orientation). |
| **Town fog** | Party LOS + fog on the hub as usual. **Facade tiles** also become visible when within party **sight range** (Chebyshev), even if another building wall would block shadow-cast LOS — so roof rows read as one structure. Out-of-range facade tiles stay unseen. |
| **Dungeon fog** | If a building-like facade appears in a dungeon, building mass uses the same opacity as walls (`!IsWalkable` in [Fog of war](Fog-Of-War-Requirements.md) §G4). Explored memory snapshots include roof/window tiles. |
| **Sorting** | Roof and wall tiles on the wall map render above plaza floor; optional Y-sorted props on `DynamicViewsRoot` for overhangs. |
| **Not required v0** | True height extrusion, separate “upper floor” gameplay layer, or parallax — **tile rows on the same grid** are sufficient. |

**Authoring minimum:** At least **3 rows** of facade depth (door + wall + roof) and **≥ 3 cells** width for a readable shop front. The TownTest demo (5×3) is the **floor**, not the target for production buildings.

### 7.3 — Exterior plaza (`town_main`)

| Authoring rule | Detail |
|----------------|--------|
| **Stamp layout** | Paint building footprint on the stamp: **wall** cells for mass, **floor** cells for entrances (see `PaintDemoBuildingFacade` in `TownPackCreator`). |
| **Facade overlay** | `TownBuildingFacadeOverlay` overpaints Kenney stone/roof/door tiles via `TownBuildingFacadeVisualPhase` — visual only; must not change walkability from stamp. |
| **Entrance cells** | **Walkable floor** on the outer perimeter row; door sprite on floor layer (`TownFacadePaintLayer.Floor` + `Town_Building_Door` or open pavement). |
| **Building mass** | All non-entrance footprint cells → **wall** layer in stamp + matching overlay tiles (corner, wall, window, roof). |
| **Markers** | `building_<id>_door` per entrance cell (or one marker per entrance). |
| **Zero entrances** | Omit portal specs; paint mass only — valid for ruins, background facades. |
| **Multiple entrances** | Multiple floor cells + portal specs with distinct `portalLinkId`s or shared target floor; arrival binding picks exterior return cell per exit link. |

### 7.4 — Interior floors (`town_interior_<id>`)

| Authoring rule | Detail |
|----------------|--------|
| **Floor definition** | `DungeonFloorDefinition`: `floorId`, `PreBakedStamp`, `SafeZone`, `doorPolicy: None`, no enemies. |
| **Stamp size** | **12×10 to 16×12** cells typical; one room + counter + exit. |
| **Interior shell** | Walls on border; floor inside. |
| **Exit tiles** | **Walkable floor** on interior edge (usually south wall) painted with **open/door interior tile** — same visual language as exterior entrance. Marker `building_<id>_exit` on that cell. |
| **Arrival** | `building_<id>_arrival` one row **inside** from entrance (not on the exit tile). |
| **Markers** | Arrival, exit(s), NPC, shop counter facing. |
| **Layers** | Floor tilemap + wall tilemap; props on `DynamicViewsRoot`. |

**TownTest gap (resolved in data):** interior exit door overlay added; re-run **Fix TownTest Scene** after pulling so plaza stamp matches the 7×4 footprint.

### 7.5 — Shared vs unique interior stamps

| Strategy | When |
|----------|------|
| **Unique stamp per building** | Story homes, distinct layouts (Mira, shaman hut) — **preferred** |
| **Shared “generic shop” stamp** | Multiple merchants differ only by NPC marker + `ShopNpcDefinition` — acceptable v0.1 |
| **Palette swap** | Same geometry, recolored tiles — cheap variant |

### 7.6 — Sprite / tile performance

| Concern | Mitigation |
|---------|------------|
| **Tilemap draw calls** | One tilemap per floor instance; only **active** floor visible (`BindVisibilityToActiveFloor`). |
| **Parked floors** | `SetActive(false)` on inactive instances — **no** Update/render cost. |
| **Texture memory** | Share one town tileset material across plaza + all interiors. |
| **Prop sprites** | Keep per-interior prop count low (&lt; 20); pool common prefabs. |

---

## 8. Performance — loading & moving through town

### 8.1 — Budgets (locked targets)

| Event | Target | Mechanism |
|-------|--------|-----------|
| **Walk plaza** | 60 fps; **zero** interior generation | Only `town_main` active |
| **First enter building** | ≤ **1 s** on dev hardware | `GenerateFirstVisit` once; stamp-only pipeline |
| **Re-enter building** | ≤ **300 ms** perceived | Parked instance reactivate |
| **Exit to plaza** | ≤ **300 ms** | Reactivate `town_main` (always resident after first town load) |
| **Parked interior count** | ≤ **8** instances run-time (v0 cap) | LRU teardown of unvisited optional v0.1 |
| **Memory** | Plaza + N interiors ≪ single dungeon habitat | Small stamps; no zone composite |

### 8.2 — Lazy generation (locked)

```text
Town scene load
  → Generate town_main only

First enter building_mira
  → Generate town_interior_mira (if not in _instances)
  → Park town_main
  → Activate interior

Exit
  → Park interior
  → Activate town_main (already generated)
```

**Do not** pre-generate all interiors at town Play.

### 8.3 — Town ↔ dungeon vs building transitions

| Transition | Scene load | Floor instances | Fade |
|------------|------------|-----------------|------|
| **Building enter/exit** | **No** | Park/swap | **Yes** (v0) |
| **Town → dungeon** | **Yes** | Teardown all on dungeon exit | Modal (existing) |
| **Dungeon floor portal** | **No** | Park/swap | Optional future fade |

Returning from dungeon **regenerates** `town_main` today (`TownArrivalService`) — interior **parked** instances on DDOL manager (if `useDontDestroyOnLoad`) should survive; document must require **`DungeonFloorInstanceManager.useDontDestroyOnLoad = true`** on town scene or equivalent persistence for interior state. **Open question Q2.**

### 8.4 — Moving through town (runtime)

| System | Plaza | Interior |
|--------|-------|----------|
| **Grid movement / formation** | Yes | Yes |
| **Fog of war** | On (party LOS) | On (party LOS) |
| **Camera follow** | Yes + HUD offset | Same; clamp to interior bounds if smaller |
| **Minimap** | Optional plaza labels | Hide or static room icon (v0.1) |

---

## 9. Safe zone, time, and services

| Rule | Detail |
|------|--------|
| **Combat** | All town interiors **`SafeZone`** — `SafeZonePolicyService` blocks hostile actions. |
| **Town phase** | **Frozen while inside interior** (same as dungeon — [Town time §3](Town-Time-And-Calendar-Requirements.md)); levers only on plaza. |
| **Phase-gated doors** | `"Open at night only"` checks `TownTimeService.currentPhase` before transition. |
| **Shop state** | Unchanged — `TownShopStateService` DDOL; NPC inside interior reads same snapshot. |
| **Quest / dialog** | `ZoneEnterTracker` **not** used; optional `BuildingEntered` event on `TownTransitionService` for quests. |

---

## 10. Data model & authoring checklist

### 10.1 — New assets per building

| Asset | Example |
|-------|---------|
| `Floor_town_interior_mira.asset` | `floorId: town_interior_mira` |
| `Stamp_MiraHome_14x10.asset` | layout + markers |
| `BuildingDoorDefinition` (ScriptableObject) | exterior cell, interior floor id, link ids, phase gate |
| Catalog entry | Add to `TownCatalog` or `TownBuildingCatalog` |

### 10.2 — Stamp marker ids (proposed)

Extend `StampMarkerIds`:

| Marker | Purpose |
|--------|---------|
| `building_<id>_door` | Exterior door + portal anchor return |
| `building_<id>_arrival` | Interior spawn on enter |
| `building_<id>_exit` | Interior exit trigger |

Portal link ids: `building_<id>_enter` / `building_<id>_exit`.

### 10.3 — Setup phases

| Phase | Role |
|-------|------|
| `PortalSetupPhase` | Registers **step-on** `PortalInteractable` for building entrances/exits when `adjacentConfirmOnly: 0` (default). |
| `TownBuildingFacadeVisualPhase` | Applies `TownBuildingFacadeOverlay` stone/roof/door art on top of stamp walkability. |
| `TownBuildingDoorSetupPhase` | Legacy **Confirm-adjacent** path when `adjacentConfirmOnly: 1` — exceptional doors only; do not use for ordinary shops. |

**Editor:** `TownPackCreator` (`JRogue/Town/Fix TownTest Scene`) — stamp footprint, facade overlay, interior exit door tile (§7.4 backlog).

---

## 11. Integration map

| System | Change |
|--------|--------|
| `DungeonFloorInstanceManager` | Register interior floor defs; ensure park/activate works for `town_main` ↔ interiors |
| `PortalEntryService` / adjacent interact | Route building doors to `TownTransitionService` (not dungeon portal dialog) |
| `TownTransitionService` | **New** — fade + floor swap |
| `TownTransitionCurtainUI` | **New** — fade overlay |
| `PlayfieldLayout` / `CameraFollow` | No change v0; optional interior camera clamp v0.1 |
| `GameplayModalGate` | Block door during shop/dialog |
| `ShopNpcMenuUI` | Works inside interior floor |
| `DungeonExitService` | Verify interior parked state survives dungeon round-trip (Q2) |

---

## 12. v0 building roster (illustrative)

| Building | Interior floor | Entry | Notes |
|----------|----------------|-------|-------|
| **Mira home** | `town_interior_mira` | Plaza north facade | Dialog NPC inside |
| **General store** | `town_interior_shop_a` | Plaza east | Migrate buy-only NPC inside |
| **Weapon shop** | `town_interior_shop_b` | Plaza west | Sell-only NPC |
| **Shaman hut** | reuse or extend shaman stamp | Near existing marker | Spirit imprint dialog |

Exact cells TBD in stamp authoring.

---

## 13. Acceptance criteria (v0)

### Building tiles & facades

1. Building **mass** cells (walls, windows, roof) are **non-walkable** for party, NPCs, and monsters.  
2. Exterior **entrance** is a **walkable floor** tile on the building perimeter with door/open art; **step-on** enters (not Confirm-adjacent by default).  
3. Interior **exit** is a **walkable floor** tile on the interior perimeter with door/open art; **step-on** exits.  
4. Facade occupies **≥ 3 rows** (door + wall + roof minimum) so the building is recognizable **before** the player reaches the door.  
5. A building with **zero** entrances is valid (facade only, no portals).  

### Enter / exit transitions

6. At least **one** enterable building on `town_main` with facade + entrance tile.  
7. **Enter** triggers fade → interior floor → party at arrival marker; **no** `LoadScene`.  
8. **Exit** returns to the **same** exterior door cell/facing; fade plays.  
9. **Second enter** to same building completes in ≤ **300 ms** perceived (parked reuse).  
10. Unvisited buildings **not** generated until first enter.  
11. Formation, party HUD, and hotbar work in interior.  
12. Safe zone enforced — no combat abilities targeting hostiles.  
13. Town phase does not advance while inside interior.  
14. Transition blocked while `GameplayModalGate.BlocksFloorGameplay`.  

---

## 14. Open questions

| # | Question | Default if unresolved |
|---|----------|------------------------|
| **Q1** | Enter trigger: **Enter adjacent** vs **step-on** for exterior doors? | **Step-on** on entrance floor tile (§6.1); `adjacentConfirmOnly` for exceptions only |
| **Q2** | Do parked interiors survive **dungeon round-trip** when `town_main` regenerates? | Require DDOL floor manager + re-bind plaza on `TownArrivalService` |
| **Q3** | Migrate all shop NPCs **into** interiors for v0, or plaza overlay first? | **One** interior shop as pilot; plaza shops remain until migrated |
| **Q4** | Camera clamp when interior smaller than viewport? | Soft clamp v0.1; center anchor v0 |
| **Q5** | Audio + footstep tile change on threshold? | v0.1 |

---

## 15. Resolved decisions (draft)

| # | Decision | Resolution |
|---|----------|------------|
| **D1** | Separate screen vs fade for buildings? | **Partial fade curtain** for enter/exit; **not** a new Unity scene or loading splash. |
| **D2** | Walkable interior vs menu-only? | **Walkable floor instance**; full-screen UI only for shop/dialog **after** entry. |
| **D3** | Implementation mechanism? | **`DungeonFloorInstance` park/activate** in same town scene (Option A). |
| **D5** | Default enter interaction? | **Step-on** perimeter entrance tile; deprecate Confirm-adjacent as the default building path. |
| **D6** | What blocks movement on building cells? | **Wall-layer building mass** — same rule for party, NPCs, monsters. |
| **D7** | How is a building visible from far away? | **Multi-row facade** on the grid (wall + roof tiles), not a single ground-edge strip. |

---

## 16. Document history

| Version | Date | Notes |
|---------|------|-------|
| Draft | 2026-06-07 | Initial requirements: transition UX, floor-instance architecture, authoring, performance budgets. |
| Implemented (v0 demo) | 2026-06-07 | Demo building `town_interior_demo`, fade transitions, step-on portals. |
| Expanded | 2026-06-16 | Building tile semantics (§7): walkability, distance visibility, entrance/exit authoring; TownTest gap analysis (§3.1); goals G9–G11. |
| Implemented (demo tiles) | 2026-06-18 | 7×4 exterior facade, interior exit door overlay (`FacadeOverlay_town_interior_demo`), `TownDemoBuildingLayout` constants. |
