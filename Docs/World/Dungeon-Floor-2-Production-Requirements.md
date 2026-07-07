# Production dungeon — Floor 2 — Requirements (draft)

**Status:** **Draft** — initial v0 scope; geometry, tiles, and content are **expected to change** in later milestones.

**Purpose:** Specify the **first production descent floor** (`dungeon_floor_02`) and the **Floor 1 changes** required to reach it. Floor 2 v0 is intentionally **minimal**: a small rectangular map, placeholder tiles, normal ambient lighting, and a **return portal** to Floor 1. Floor 1 gains a **mandatory descent plinth** in `northern_dark` that becomes the **only** Floor 1 → Floor 2 portal.

**Depends on:** [Dungeon Floor 1 production](Dungeon-Floor-1-Production-Requirements.md) (production shell, zones, persistence), [Dynamic dungeon floor generation](Dynamic-Dungeon-Floor-Generation-Requirements.md) (multi-floor park/persist, portal bindings, `PersistFullFloorState`), [Dungeon monster spawn schedules](Dungeon-Monster-Spawn-Schedule-Requirements.md) (day-start reinforcements), [Dungeon floor & wall tiles](Dungeon-Floor-And-Wall-Tiles-Requirements.md), [Dungeon time](Dungeon-Time-Requirements.md), [XP & progression](../Progression/) (party XP award hook — exact API TBD).

**Supersedes (Floor 1):** §8.2 **random north-edge portal** on row **y = 79** — replaced by **plinth activation** (§4). Floor 1 requirements doc should be amended when Floor 2 Phase 1 lands.

**Explicitly out of scope (this milestone):** Floor 3+; save/load across sessions; procedural room variety on Floor 2; enemies, traps, hazards, vaults, or loot on Floor 2; custom art pass (placeholder dirt/stone tiles only); offering-altar mechanics; changing `DungeonFloorTest` test-floor pair unless needed for regression.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Reachable descent** — Player can progress Floor 1 → Floor 2 → Floor 1 in the **production** dungeon run without debug teleport. |
| **G2** | **Floor 1 gate is a plinth** — Mandatory interactable near the **north** of `northern_dark`; first bump transforms it into a portal and awards **2 XP**; state persists for the run. |
| **G3** | **Floor 2 v0 shell** — **10 × 20** rectangle; **only** meaningful feature is the **south-edge** return portal. |
| **G4** | **Symmetric arrival** — Returning from Floor 2 places the party **adjacent** to the Floor 1 plinth portal (not a random `luminescent_cavern` spawn). |
| **G5** | **Floor 1 state preserved** — Dead monsters, opened doors, transformed plinth, explored fog, etc. remain when parking Floor 1 and returning later ([dynamic §1.3](Dynamic-Dungeon-Floor-Generation-Requirements.md)). |
| **G6** | **Schedule-aware repopulation** — If the player spends **a full dungeon day** (or more) away from Floor 1, **day-start** spawn schedule may add monsters per existing Floor 1 rules; immediate round-trip does **not** resurrect kills. |
| **G7** | **Comfort lighting on Floor 2** — Ambient visibility like **`luminescent_cavern`** (no torch required for v0). |
| **G8** | **Data-driven** — Floor 2 dimensions, tiles, and portal bindings authored on `DungeonFloorDefinition` / layout assets — not hard-coded in scene scripts. |
| **G9** | **Iterate later** — Document and implementation must make it easy to replace the 10×20 box with real zones, enemies, and art in a future milestone. |

---

## 2. Implementation phases (overview)

| Phase | Name | Summary | Depends on |
|-------|------|---------|------------|
| **0** | **Requirements capture** | This document | — |
| **1** | **Floor 1 plinth & portal link** | Replace north-edge portal rule; mandatory plinth vault; bump → portal + 2 XP; persist transformed state | Floor 1 production baseline |
| **2** | **Floor 2 definition & layout** | `Floor_prod_dungeon_floor_02`, 10×20 rectangle, dirt floor / stone wall palettes | Phase 1 |
| **3** | **Portals & arrival bindings** | South portal on Floor 2; `arrivalBindings` so return lands beside Floor 1 plinth | Phase 2 |
| **4** | **Persistence & schedule QA** | Verify park/unpark, kill persistence, day-advance spawns on Floor 1 return | Phase 1–3 |
| **5** | **Playtest & doc sync** | Round-trip checklist; amend Floor 1 doc §7.6 / §8.2 | Phase 4 |

---

## 3. Related assets (target)

| Asset | Location | Notes |
|-------|----------|-------|
| `Floor_prod_dungeon_floor_01` | `Assets/Resources/Dungeon/` | **Amend** — plinth portal replaces edge portal rule |
| `Floor_prod_dungeon_floor_02` | `Assets/Resources/Dungeon/` | **New** — production Floor 2 |
| `Floor_dungeon_floor_02` | `Assets/Resources/Dungeon/` | Legacy v0a 20×20 stamp — **not** production target |
| `Layout_Floor02_v0` or stamp | `Assets/Data/Dungeon/Layouts/` | Simple 10×20 shell (TBD exact path) |
| `Palette_Floor02_Floor` / `Palette_Floor02_Wall` | `Assets/Data/Dungeon/Palettes/` | Placeholder dirt + stone (§6) |
| `vault_descent_plinth_*` | `Assets/Data/Vaults/` | Mandatory plinth in `northern_dark` (§4) |

**Production floor id (locked):** `dungeon_floor_02`  
**Portal link ids (locked):** `link_floor01_to_floor02`, `link_floor02_to_floor01`

---

## 4. Floor 1 prerequisite — descent plinth (locked intent)

Floor 1 **must** be updated before Floor 2 is playable in production. This replaces the current §8.2 rule (*portal on `northern_dark` north edge, row y = 79, random x*).

### 4.1 — Plinth vault (mandatory)

| Rule | Detail |
|------|--------|
| **Presence** | **Exactly one** descent plinth per `dungeon_floor_01` run — **mandatory vault**, not optional weighted scatter. |
| **Zone** | Footprint entirely in **`northern_dark`**. |
| **Placement** | **Near the northern edge** of the zone: anchor cell must satisfy **Chebyshev distance ≤ 3** from the zone’s **north map edge** (row **y = 79** on the 50×80 Floor 1 grid). Position along **x** may vary by seed. |
| **Pipeline** | Same guarantee pattern as monument / altar — retry candidates until one valid stamp succeeds; fail generation if impossible. |
| **Overlap** | Must not overlap monument reserved cells, pond footprints, or zone boundary **entrance** cells (§6.3.1 connectivity). |
| **Art (v0)** | Reuse or extend existing altar / plinth overlay from Floor 1 vault catalog until dedicated art lands. |

### 4.2 — Bump → portal transformation (locked)

| Property | Value |
|----------|--------|
| **Trigger** | Party member **bumps** the plinth (same bump pipeline as monument / flavor altar — checked before walkability). |
| **First activation** | Plinth **visually transforms** into an active **portal**; awards **+2 XP** to the party **once** per dungeon run (exact XP API: party pool vs per-member — **default: +2 party XP total**). |
| **Subsequent visits** | Plinth **remains** a portal for the rest of the run — no second XP award. |
| **Portal target** | Always `dungeon_floor_02` via `link_floor01_to_floor02`. |
| **Persistence** | Transformed state stored on the **Floor 1 instance** (interactable / portal registry snapshot) — survives park when player descends and returns. |

### 4.3 — Floor 1 portal cell (locked)

| Property | Value |
|----------|--------|
| **`portalLinkId`** | `link_floor01_to_floor02` |
| **`targetFloorId`** | `dungeon_floor_02` |
| **`portalCell`** | Center (or designated portal cell) of the plinth footprint **after** transformation |
| **Pre-activation** | No portal registration — bump-only interactable until first activation |

**Design intent:** The player must **find and activate** the plinth in `northern_dark`; progression is not a naked tile on the map edge.

### 4.4 — Floor 1 doc amendments (when implemented)

Update [Dungeon Floor 1 production](Dungeon-Floor-1-Production-Requirements.md):

- §7.6 — Altar flavor vault → **descent plinth** behavior (§4.2) or separate vault id.
- §8.2 — Remove random **y = 79** edge portal; reference plinth (§4).
- §8.1 **R2** — Spawn must not be adjacent to **plinth portal cell** once activated (validate at activation time or use plinth placement buffer).
- AC4-3 — Replace edge-portal criterion with plinth reachability + activation.

---

## 5. Floor 2 — dimensions & layout (locked v0)

| Property | Value |
|----------|--------|
| **Floor id** | `dungeon_floor_02` |
| **Width × height** | **10 × 20** (bottom-left origin: **x ∈ [0,9]**, **y ∈ [0,19]**) |
| **Layout mode** | **`PreBakedStamp`** or **single-zone `ZoneComposite`** — implementer picks simplest path; v0 is a **filled rectangle** with perimeter walls and interior floor. |
| **Connectivity** | Fully connected interior; no islands. |
| **Content** | **No** monsters, traps, hazards, vaults, or floor loot in v0. |
| **Player first visit** | Party arrives via portal from Floor 1 — **not** random spawn. |

### 5.1 — Return portal placement (locked)

| Property | Value |
|----------|--------|
| **`portalLinkId`** | `link_floor02_to_floor01` |
| **`targetFloorId`** | `dungeon_floor_01` |
| **Edge** | **South** map edge — row **y = 0** |
| **Cell** | **Fixed** for v0: center south — **(4, 0)** or **(5, 0)** (choose one anchor and document in pack creator); must be **walkable** door/floor cell on the south perimeter. |
| **RNG** | **None** for v0 — fixed cell simplifies “adjacent to Floor 1 portal” pairing. *Future:* seed-driven x along south edge. |

---

## 6. Tiles & palettes (placeholder — locked v0)

Tiles are **stand-ins**; a later art pass will replace them.

| Layer | Source (v0) | Notes |
|-------|-------------|-------|
| **Floor** | **Dirt** cavern / town dirt palette — same family as **Barbarian Holy Land** outdoor dirt (`grey_dirt_*` under `Assets/TileMaps/Dcss/Cavern/`) | Single variant OK for v0 |
| **Wall** | **Stone** building / town stone — same family as **city** building stone walls (`Town_Building_StoneWall` or holy-land wall palette) | Perimeter only |

Author **`Palette_Floor02_Floor`** and **`Palette_Floor02_Wall`** (or reference existing palettes) on `Floor_prod_dungeon_floor_02`.

---

## 7. Lighting (locked v0)

| Zone / floor | Ambient | Torch |
|--------------|---------|-------|
| **`dungeon_floor_02` (entire map)** | **Normal** — party can see without a torch, same practical visibility as **`luminescent_cavern`** on Floor 1 | Torch optional; not required to navigate |

**Implementation notes:**

- Set zone / floor ambient so `ZoneVisionPolicy` does **not** apply `northern_dark` pitch-black rules on Floor 2.
- No tile emitters required on Floor 2 for v0.
- If Floor 2 uses a single implicit zone, treat it like a lit cavern for vision tests.

---

## 8. Portals & arrival bindings (locked)

### 8.1 — Link table

| Direction | `portalLinkId` | Source | Target | Activation |
|-----------|----------------|--------|--------|------------|
| **Down** | `link_floor01_to_floor02` | Floor 1 plinth (after transform) | `dungeon_floor_02` | Bump plinth → portal; step on portal |
| **Up** | `link_floor02_to_floor01` | Floor 2 south edge **(§5.1)** | `dungeon_floor_01` | Step on portal |

### 8.2 — Arrival bindings

| Arriving from | `portalLinkId` | Destination floor | `arrivalAnchor` (locked intent) |
|---------------|----------------|-------------------|----------------------------------|
| Floor 1 plinth | `link_floor01_to_floor02` | `dungeon_floor_02` | Cell **north of** Floor 2 south portal — e.g. **(4, 1)** or **(5, 1)** — one step into the map from the return portal |
| Floor 2 return | `link_floor02_to_floor01` | `dungeon_floor_01` | Cell **Chebyshev-adjacent** to the **Floor 1 plinth portal cell** (same adjacency rules as town building exits) — **not** random `luminescent_cavern` spawn |

**Locked pairing:** The Floor 2 south portal cell and both `arrivalBindings` must be authored so a round-trip places the party **beside** the plinth portal on return, facing back into `northern_dark`.

**Contrast — town entry:** Floor 1 entry from town remains **random spawn in `luminescent_cavern`** (unchanged).

### 8.3 — Scene / instance flow

Same as [dynamic dungeon §1.3](Dynamic-Dungeon-Floor-Generation-Requirements.md):

```text
Activate plinth portal on Floor 1 → step on portal
  → Park Floor 1 (plinth stays transformed; dead enemies remain dead)
  → First visit Floor 2: generate 10×20 layout once
  → Spawn party at Floor 2 arrival binding (north of south portal)
Return via Floor 2 south portal
  → Park Floor 2
  → Reactivate Floor 1 instance unchanged
  → Spawn party adjacent to plinth portal cell
```

---

## 9. Floor 1 monster & schedule persistence (locked)

This milestone **does not** introduce new persistence machinery — it **requires** existing multi-floor park behavior and documents expected player-visible outcomes.

### 9.1 — Immediate round-trip (same dungeon day)

| State on Floor 1 | After Floor 1 → Floor 2 → Floor 1 |
|------------------|-------------------------------------|
| **Killed enemies** | **Stay dead** — no respawn |
| **Living enemy positions** | **Unchanged** |
| **Traps / hazards** | Unchanged (triggered stays triggered) |
| **Plinth** | **Still a portal** |
| **XP from plinth** | **Not awarded again** |
| **Fog / explored tiles** | Preserved |
| **Taken items** | Preserved |

**Example (locked acceptance):** Player kills a goblin in `luminescent_cavern`, descends to Floor 2, immediately returns — **that goblin is still gone**.

### 9.2 — Return after dungeon day advances

When the player is on Floor 2 (or elsewhere) and a **dungeon day boundary** fires ([spawn schedule §9](Dungeon-Monster-Spawn-Schedule-Requirements.md)):

| Mechanism | Behavior |
|-----------|----------|
| **`MonsterSpawnScheduleService.OnDungeonDayStarted`** | Runs for the **active** floor by default (v1); Floor 1 may receive **refill / reinforcement** rows when player is **not** parked on Floor 1 if v1.1 off-floor scheduling is enabled — **minimum bar for this milestone:** when player **returns to Floor 1 on a later dungeon day**, day-start pass applies if not yet applied for that day |
| **`RefillToTarget` groups** | May spawn **new** goblins (and other scheduled species) up to schedule targets — **does not** resurrect specific corpses |
| **`OncePerDungeonIfAbsent` groups** | Unchanged — ledger prevents duplicate once-spawns |

**Example (locked acceptance):** Player kills a goblin, goes to Floor 2, waits until a **full dungeon day** elapses (day counter advances per [dungeon time](Dungeon-Time-Requirements.md)), then returns to Floor 1 — **new schedule spawns may place additional monsters** per `Schedule_Floor01_Production`; the **original** kill is not undone.

### 9.3 — Visit policy

| Floor | Policy |
|-------|--------|
| `dungeon_floor_01` | **`PersistFullFloorState`** (default) |
| `dungeon_floor_02` | **`PersistFullFloorState`** (default) |

**Forbidden for main chain:** `RegenerateOnEveryVisit` on either floor.

---

## 10. Enemy population — Floor 2 (locked v0)

| Property | Value |
|----------|--------|
| **Monsters** | **None** |
| **Traps / hazards** | **None** |
| **Dungeon time** | May inherit run clock participation from floor def (recommend **`participatesInDungeonTime: true`** so days advance while on Floor 2) — no spawn tables attached |

---

## 11. XP award (locked v0)

| Property | Value |
|----------|--------|
| **Trigger** | First successful plinth bump that transforms it |
| **Amount** | **+2 XP** |
| **Recipient** | **Party** (single award — not per member) unless progression code requires per-actor split |
| **Once per run** | Yes — track flag on floor instance or run state |
| **Floor 2** | No XP sources in v0 |

---

## 12. Acceptance criteria

### Phase 1 — Floor 1 plinth

| ID | Criterion |
|----|-----------|
| **AC1-1** | Every production Floor 1 run places **exactly one** descent plinth in `northern_dark` within **≤ 3** tiles of row **y = 79** |
| **AC1-2** | First bump transforms plinth → portal, registers `link_floor01_to_floor02`, awards **+2 XP** once |
| **AC1-3** | Second bump / later visits: portal works; **no** extra XP |
| **AC1-4** | Plinth portal state survives Floor 1 → Floor 2 → Floor 1 |

### Phase 2–3 — Floor 2 shell & portals

| ID | Criterion |
|----|-----------|
| **AC2-1** | `Floor_prod_dungeon_floor_02` generates a **10 × 20** walkable interior with stone perimeter walls |
| **AC2-2** | Dirt floor + stone wall tiles visible (placeholder palettes §6) |
| **AC2-3** | Floor 2 south portal at fixed cell §5.1; targets `dungeon_floor_01` via `link_floor02_to_floor01` |
| **AC2-4** | Descent from Floor 1 lands **north of** Floor 2 south portal |
| **AC2-5** | Return from Floor 2 lands **adjacent** to Floor 1 plinth portal — **not** random cavern spawn |
| **AC2-6** | Floor 2 navigable **without torch** (lighting §7) |

### Phase 4 — Persistence & schedule

| ID | Criterion |
|----|-----------|
| **AC4-1** | Kill a monster on Floor 1 → descend → immediate return → monster **still dead** |
| **AC4-2** | After **dungeon day** advances on Floor 2, return to Floor 1 → schedule may add **new** spawns per Floor 1 rules; prior kill **not** reversed |
| **AC4-3** | Round-trip: town → Floor 1 → activate plinth → Floor 2 → Floor 1 → exit town preserves run state ([Floor 1 AC6-1](Dungeon-Floor-1-Production-Requirements.md)) |

### Phase 5 — Regression

| ID | Criterion |
|----|-----------|
| **AC5-1** | `DungeonFloorTest` / legacy `Floor_dungeon_floor_02` stamp still works for QA unless explicitly migrated |
| **AC5-2** | Floor 1 production doc updated for plinth (§4.4) |

---

## 13. Open questions backlog

| ID | Question | Default if silent |
|----|----------|-----------------|
| **Q1** | Plinth vault template id — reuse `vault_altar_3x3` or new `vault_descent_plinth_3x3`? | New id; altar flavor dialog **removed** from production path |
| **Q2** | Exact south portal cell on 10×20 (x at y=0) | **(5, 0)** portal, **(5, 1)** arrival |
| **Q3** | Floor 2 `layoutMode` — stamp vs single-zone composite | Stamp preferred for v0 speed |
| **Q4** | Day-start schedule while player on Floor 2 — apply to parked Floor 1? | **v1:** active floor only; verify on return idempotency |
| **Q5** | XP API — `PartyProgression` vs per-actor | **+2 party XP** once |
| **Q6** | Fade / portal VFX on plinth transform | Optional v0 — instant swap OK |

---

## 14. Relationship to other specs

| Spec | Relationship |
|------|--------------|
| [Floor 1 production](Dungeon-Floor-1-Production-Requirements.md) | **Amend** portal + altar sections when Phase 1 ships |
| [Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md) | Park/persist, arrival bindings — **no re-spec** |
| [Monster spawn schedules](Dungeon-Monster-Spawn-Schedule-Requirements.md) | Day-start refill behavior on Floor 1 return |
| [Dungeon time](Dungeon-Time-Requirements.md) | Day counter advances while on Floor 2 |
| [Altar & map interact](Altar-And-Map-Interact-Requirements.md) | Bump path only for plinth v0 |

---

## 15. Revision history

| Date | Change |
|------|--------|
| 2026-07-06 | Initial draft — plinth gate, 10×20 Floor 2 shell, persistence examples, lit ambient |
