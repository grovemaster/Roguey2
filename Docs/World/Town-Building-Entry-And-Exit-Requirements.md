# Town building entry & exit — Requirements

**Status:** **Implemented (v0 demo)** — floor-instance building transitions, fade curtain, demo 5×5 interior with Host NPC. Run **JRogue → Town → Fix TownTest Scene** in Unity to regenerate stamp/floor assets and wire the scene.

**Purpose:** Specify how the player **enters and exits buildings** in the town hub: transition feel (fade vs separate screen), map/sprite authoring, performance, and integration with existing floor-instance and safe-zone systems. Town building access is a core JRPG hub loop (shops, inns, story NPCs, services) and should feel snappy while supporting distinct interior spaces.

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

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Town plaza** | Exterior hub floor `town_main` — paved stamp, NPCs, portal, levers. |
| **Building interior** | A separate **`DungeonFloorInstance`** (e.g. `town_interior_mira_home`) activated when the player enters a door. |
| **Floor transition** | `DungeonFloorInstanceManager.TryTransitionPortalForWholeParty` — park active floor, activate target, respawn party at portal anchor. **Same mechanism as dungeon floor portals.** |
| **Scene transition** | `SceneManager.LoadScene(..., Single)` — town ↔ dungeon only today; **not** the default for buildings (v0). |
| **Door interactable** | Exterior/interior cell that triggers enter or exit (step-on or **`Enter`** adjacent — see §6). |
| **Portal link pair** | Matched ids (`building_mira_enter` / `building_mira_exit`) binding exterior door ↔ interior arrival marker. |
| **Parked floor** | Generated `DungeonFloorInstance` disabled under `Floors` root; state preserved until run ends. |
| **Transition curtain** | Brief full-screen or playfield fade during floor swap (§5) — **not** a separate game screen. |
| **Full-screen service UI** | Shop menu, dialog, inventory — overlays gameplay; **does not replace** walkable interior maps. |

---

## 3. Current baseline (as-is)

| Area | Today |
|------|--------|
| **Town map** | Single 20×20 pre-baked stamp (`Stamp_TownPlaza_20x20`); `Town_WallBuilding` tiles are **walls**, not enterable spaces. |
| **Transitions** | **Town ↔ dungeon:** modal dialog → **full scene load** (`DungeonEntryService` / `DungeonExitService`). **In-dungeon:** floor-instance portal (no scene load). |
| **Fade / loading UI** | **None** — no `ScreenFade`, wipe, or async loading bar. |
| **Shops** | NPC talk on plaza opens **full-screen shop UI** over the plaza; no interior map. |
| **Doors** | Spec exists ([Door requirements](Door-Requirements.md)); town `doorPolicy: None`. |
| **Markers** | `StampMarkerIds` for portal, NPCs, levers, torches, `playerStart` — **no building door markers yet**. |

**Gap:** No enter/exit building loop; no transition polish; shop UX is overlay-only.

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

### 6.1 — Exterior door (plaza)

| Rule | Detail |
|------|--------|
| **Trigger** | **Step-on doorway tile** (same as dungeon floor portals). Confirm-adjacent remains available via `adjacentConfirmOnly` for special doors. |
| **Preflight (optional)** | Locked door → `"Closed at night."` toast; no transition. |
| **Open door** | `TownTransitionService.TryEnterBuilding` → interior floor. |
| **Turn cost** | **No turn** in safe zone (same as shop talk). |
| **Party** | **Whole party** teleports to interior anchor (mirror dungeon portal). |

### 6.2 — Interior exit

| Rule | Detail |
|------|--------|
| **Exit cell** | Marker `building_<id>_exit` on interior stamp; visible mat or door tile. |
| **Trigger** | Step-on **or** Enter — **locked: step-on** for exits (fast leave) with Enter fallback for accessibility follow-up. |
| **Spawn** | Exterior `building_<id>_door` marker; party faces **away from building** (outward). |

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

## 7. Town & sprite construction

### 7.1 — Exterior plaza (`town_main`)

| Authoring rule | Detail |
|----------------|--------|
| **Facade tiles** | `Town_WallBuilding` (or dedicated `Town_BuildingFacade_*`) on **non-walkable** edge cells; read as building fronts. |
| **Door cell** | Single **walkable** threshold tile in facade row; registered as door interactable (not a hole through wall into void). |
| **Depth illusion** | Optional: darker doorway tile, awning prop sprite on `DynamicViewsRoot` — **no** separate camera layer required v0. |
| **Stamp markers** | Per building: `building_<id>_door` at exterior threshold; optional `building_<id>_label` for debug. |
| **NPC placement** | Service NPCs **inside** interiors (post-v0 migration) or remain on plaza until interior stamps exist ([Shop §4](Shop-NPC-Requirements.md) cells remain valid for plaza-only v0). |

**Scale guidance:** v0 plaza can grow beyond 20×20 via stamp resize; keep **door row** along plaza edges so camera panning from `CameraFollow` still frames facades.

### 7.2 — Interior floors (`town_interior_<id>`)

| Authoring rule | Detail |
|----------------|--------|
| **Floor definition** | `DungeonFloorDefinition`: `floorId`, `PreBakedStamp`, `SafeZone`, `doorPolicy: None` (v0), no enemies, no dungeon time. |
| **Stamp size** | **Small** — target **12×10 to 16×12** cells per shop/home; one room + counter + exit. |
| **Tiles** | Reuse Kenney Tiny Town **interior** subset (floor, wall, counter, rug) — same tilemap pipeline as plaza. |
| **Markers** | `playerStart` or `building_<id>_arrival` (portal anchor), `building_<id>_exit`, NPC markers, shop counter facing. |
| **Layers** | Same sorting as dungeon: floor tilemap + optional prop sprites on instance `DynamicViewsRoot`. |
| **Lighting** | Interior ambient via `TownLightingSync` hook or per-floor override ([Improved Illumination](Improved-Illumination-Requirements.md) backlog); torches optional. |

### 7.3 — Shared vs unique interior stamps

| Strategy | When |
|----------|------|
| **Unique stamp per building** | Story homes, distinct layouts (Mira, shaman hut) — **preferred** |
| **Shared “generic shop” stamp** | Multiple merchants differ only by NPC marker + `ShopNpcDefinition` — acceptable v0.1 |
| **Palette swap** | Same geometry, recolored tiles — cheap variant |

### 7.4 — Sprite / tile performance

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
| **Fog of war** | Off (town) | Off |
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

### 10.3 — Setup phase (optional)

`TownBuildingDoorSetupPhase` — reads `BuildingDoorCatalog`, registers interactables on `town_main` at door markers (mirror `TownPortalSetupPhase`).

**Editor:** extend `TownPackCreator` menu (`JRogue/Town/Add Building Door…`).

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

1. At least **one** enterable building on `town_main` with visible facade + door marker.  
2. **Enter** triggers fade → interior floor → party at arrival marker; **no** `LoadScene`.  
3. **Exit** returns to **same** exterior door cell/ facing; fade plays.  
4. **Second enter** to same building completes in ≤ **300 ms** perceived (parked reuse).  
5. Unvisited buildings **not** generated until first enter.  
6. Shop NPC **inside** interior opens full-screen shop UI; plaza movement blocked while shop open.  
7. Formation, party HUD, and hotbar work in interior.  
8. Safe zone enforced — no combat abilities targeting hostiles.  
9. Town phase does not advance while inside interior (lever on plaza still works after exit).  
10. Transition blocked while `GameplayModalGate.BlocksFloorGameplay`.  

---

## 14. Open questions

| # | Question | Default if unresolved |
|---|----------|------------------------|
| **Q1** | Enter trigger: **Enter adjacent** vs **step-on** for exterior doors? | **Enter adjacent** (NPC parity) |
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
| **D4** | When to generate interiors? | **Lazy** on first enter only. |

---

## 16. Document history

| Version | Date | Notes |
|---------|------|-------|
| Draft | 2026-06-07 | Initial requirements: transition UX, floor-instance architecture, authoring, performance budgets. |
| Implemented (v0 demo) | 2026-06-07 | Demo building `town_interior_demo`, fade transitions, Confirm-adjacent enter, step-on exit. |
