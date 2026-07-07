# Dungeon floor time limits — Requirements (draft)

**Status:** **Draft** — extends [dungeon time](Dungeon-Time-Requirements.md) with **per-floor time limits** on the **shared global clock** (StGaaB-style: each floor has its own limit; deeper floors **happen to allow more time**).

**Purpose:** Specify the **time limit for a particular dungeon floor**: how it is authored, how it uses the **global** `elapsedCycles` counter, when the party is **forced to town**, and when portals **to** an expired floor stop working.

**Depends on:** [Dungeon time](Dungeon-Time-Requirements.md) (global clock tick, day/night phases), [Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md) (park/activate, portals), [Dungeon Floor 1 production](Dungeon-Floor-1-Production-Requirements.md) (§9.8), [Dungeon Floor 2 production](Dungeon-Floor-2-Production-Requirements.md), `DungeonTimeService`, `PortalEntryService`, `PortalInteractable`, `DungeonFloorDefinition`.

**Supersedes (for main-chain production floors):** Using `additionalDayNightCycles` to **extend a single run-wide `maximumCycles`** on first descent — that model is **not** how per-floor limits work (§4).

**Explicitly out of scope (this milestone):** Per-floor clocks that pause while parked; save/load; UI countdown per floor; Floor 3+ values (pattern only).

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **StGaaB parity** — Each floor has its **own** day–night cycle limit. |
| **G2** | **Global clock** — One `elapsedCycles` counter per run; **does not reset** when changing floors ([dungeon time §5.2](Dungeon-Time-Requirements.md)). |
| **G3** | **Exit on active floor** — If the party is **on** floor `F` when the global clock reaches **`F`’s limit**, the dungeon **ends** (forced town exit). |
| **G4** | **Deeper = longer** — Floor 2’s limit (**6**) is **greater than** Floor 1’s (**4**); this is **not** implemented as “+2 bonus time added to the run on first visit.” |
| **G5** | **Production Floor 1** — Limit **4** day–night cycles (author may change later). |
| **G6** | **Production Floor 2** — Limit **6** day–night cycles. |
| **G7** | **Expired floor = no entry** — From dungeon **day 5** (`elapsedCycles >= 4`), portals **to Floor 1** do nothing; the party on Floor 2 **cannot return** to Floor 1. |
| **G8** | **Data-driven** — Limits on `DungeonFloorDefinition`; no hard-coded floor ids in portal/time code. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Global run clock** | `DungeonTimeService`: `elapsedCycles`, current day/night phase, turn budgets from the **active** floor def. |
| **Dungeon day** | `elapsedCycles + 1` at the **start** of a day phase (day 1 when `elapsedCycles == 0`). |
| **Floor day–night cycle limit** | `floorDayNightCycleLimit` — this floor’s cap on the **global** `elapsedCycles` scale (§4). |
| **Floor expired** | `elapsedCycles >= floor.floorDayNightCycleLimit` — portals **to** that floor are inactive (§6). |
| **Forced exit** | `DungeonExitService.RequestForcedExitToTown()` — party leaves the dungeon (§5). |

---

## 3. StGaaB reference

| StGaaB behavior | JRogue mapping |
|-----------------|----------------|
| Each floor has a time limit | `floorDayNightCycleLimit` per `DungeonFloorDefinition` |
| Higher floors allow more time | F2 limit **6** > F1 limit **4** (independent authored values) |
| Time runs out while on a floor → leave dungeon | Forced exit when **active** floor’s limit is reached (§5) |
| Cannot return to a floor whose time has passed | Portals **to** expired floor **do nothing** (§6) |
| One dungeon calendar | Single global `elapsedCycles` (§4.1) |

**Not this model:** “Visiting Floor 2 adds 2 cycles to a shared run deadline.” Deeper floors do **not** extend a global `maximumCycles`; they have a **higher per-floor limit** on the same clock.

---

## 4. Time model (locked)

### 4.1 — Global clock (unchanged)

- One `elapsedCycles` per dungeon run; advances on player-phase boundaries per [dungeon time §4–§6](Dungeon-Time-Requirements.md).
- `playerTurnsPerDay` / `playerTurnsPerNight` still come from the **currently active** floor def.
- Parking a floor does **not** pause or reset `elapsedCycles`.

### 4.2 — Per-floor limit (new)

Each main-chain floor defines **`floorDayNightCycleLimit`** (int ≥ 1):

| Floor | Production limit | Meaning |
|-------|------------------|---------|
| `dungeon_floor_01` | **4** | Floor 1’s window on the global calendar ends after night **4** completes (`elapsedCycles` becomes **4** at start of day **5**). |
| `dungeon_floor_02` | **6** | Floor 2’s window ends after night **6** completes (`elapsedCycles` becomes **6** at start of day **7** — forced exit if still on F2). |

Limits are **absolute positions on the global calendar**, not “cycles spent on this floor since entry.”

### 4.3 — Production timeline (example: descend before day 5)

| Dungeon day | `elapsedCycles` (start of day) | On Floor 1 | On Floor 2 |
|-------------|-------------------------------|------------|------------|
| 1–4 | 0–3 | OK (limit 4) | OK (limit 6) |
| **5** | **4** | **Expired** — forced exit if still here | OK — **cannot return to F1** |
| 6 | 5 | Expired | OK |
| After night 6 | **6** | Expired | **Expired** — forced exit if still here |

---

## 5. Forced dungeon exit (locked)

### 5.1 — Trigger

Evaluate after each calendar tick that **completes a day–night cycle** (when `elapsedCycles` increments), and when checking whether the party may remain on the **active** floor:

```text
activeFloor = currently active DungeonFloorDefinition
if activeFloor participates in dungeon time
   and elapsedCycles >= activeFloor.floorDayNightCycleLimit:
  RequestForcedExitToTown()
```

**Locked:** Forced exit is because the party is **on** a floor whose **own** limit has been reached — not because a separate “run maximum” was extended or reduced by visiting another floor.

### 5.2 — Examples

| Situation | Result |
|-----------|--------|
| On Floor 1 entire run; night 4 ends | `elapsedCycles → 4`; active F1 limit **4** reached → **forced exit** |
| Descend to Floor 2 on day 3; still on F2 when night 4 ends | `elapsedCycles → 4`; F2 limit **6** not reached → **continue**; F1 return portal **blocked** |
| On Floor 2 when night 6 ends | `elapsedCycles → 6`; F2 limit **6** reached → **forced exit** |
| Never visit Floor 2 | Never subject to F2 limit; only F1 limit matters if staying on F1 |

### 5.3 — Relation to legacy `maximumCycles`

Existing `baseDayNightCycles` / `additionalDayNightCycles` / `maximumCycles` in `DungeonTimeService` may remain for **legacy test floors** until migrated.

**Production main chain (locked intent):** Forced exit for production floors is driven by **`floorDayNightCycleLimit` on the active floor**, not by incrementing a run-wide `maximumCycles` when first visiting a deeper floor.

---

## 6. Portal behavior (locked)

### 6.1 — Portals to an expired floor

Before transitioning to `targetFloor`:

```text
if elapsedCycles >= targetFloor.floorDayNightCycleLimit:
  block (portal does nothing)
  log [DungeonTime] floor expired for portal
  return false
```

**Production:** Floor 2 south return (`targetFloorId = dungeon_floor_01`) blocked when `elapsedCycles >= 4` (day **5**+).

**v0 UX:** No modal; optional future flavor line.

### 6.2 — Portals from an non-expired floor

Descending Floor 1 → Floor 2 remains allowed while `elapsedCycles < 6` (F2 not expired). No special “unlock” or time bonus on first visit.

---

## 7. Authoring — `DungeonFloorDefinition`

### 7.1 — Primary field (new)

| Field | Type | Floor 1 (prod) | Floor 2 (prod) |
|-------|------|----------------|----------------|
| **`floorDayNightCycleLimit`** | int ≥ 1 | **4** | **6** |

### 7.2 — Existing fields (production use)

| Field | Floor 1 | Floor 2 | Notes |
|-------|---------|---------|-------|
| `participatesInDungeonTime` | `true` | `true` | |
| `playerTurnsPerDay` / `playerTurnsPerNight` | 20 / 20 | 5 / 5 | Phase length while active |
| `baseDayNightCycles` | **4** | — | Keep aligned with F1 limit for legacy / run start; **not** the F2 exit rule |
| `additionalDayNightCycles` | **0** | **0** | **Do not** use +2 on F2 for production per-floor limits |

**Authoring invariant (main chain):** Deeper floors should have **equal or higher** `floorDayNightCycleLimit` than shallower floors.

### 7.3 — Assets (implementation)

| Asset | `floorDayNightCycleLimit` | `additionalDayNightCycles` |
|-------|---------------------------|----------------------------|
| `Floor_prod_dungeon_floor_01` | **4** | **0** |
| `Floor_prod_dungeon_floor_02` | **6** | **0** (not **2**) |

---

## 8. Implementation sketch

| Component | Change |
|-----------|--------|
| `DungeonFloorDefinition` | Add `floorDayNightCycleLimit` |
| `DungeonTimeService.TryTickAfterPlayerPhase` | On cycle complete, if active floor limit reached → forced exit (§5) |
| `PortalInteractable` / floor transition | Block if target floor expired (§6) |
| `DungeonTimeLogic` | `IsFloorExpired(floor, elapsedCycles)`, `IsFloorEnterable(...)` |
| Pack creators | Author §7.3; remove F2 `additionalDayNightCycles: 2` intent |

---

## 9. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC1** | Floor 1 `floorDayNightCycleLimit == 4`; party **on Floor 1** when `elapsedCycles` reaches **4** after night end → forced exit. |
| **AC2** | Floor 2 `floorDayNightCycleLimit == 6`; party **on Floor 2** when `elapsedCycles` reaches **6** after night end → forced exit. |
| **AC3** | Party **on Floor 2** when `elapsedCycles` reaches **4** → **no** forced exit; return portal to Floor 1 **does nothing**. |
| **AC4** | First visit to Floor 2 does **not** change a global `maximumCycles` from 4 to 6. |
| **AC5** | While `elapsedCycles < 4`, Floor 2 return portal works. |
| **AC6** | Limits read from floor defs only. |

---

## 10. Examples

### 10.1 — Never leave Floor 1

Days 1–4 on Floor 1 → after night 4, forced exit. Floor 2 limit irrelevant.

### 10.2 — Deep run (locked)

1. Floor 1 days 1–2, descend to Floor 2.  
2. Days 3–4 on Floor 2 → return to Floor 1 still OK.  
3. Day **5** on Floor 2 → cannot return to Floor 1; continue on Floor 2.  
4. Day **6** on Floor 2 → after night 6, forced exit.

### 10.3 — Late descent

1. Floor 1 through day 4 morning, descend on day 4.  
2. Night 4 ends on Floor 2 → `elapsedCycles == 4`, F2 limit 6 → continue; F1 closed.

---

## 11. Open questions

| ID | Question | Default |
|----|----------|---------|
| **Q1** | Field name | `floorDayNightCycleLimit` |
| **Q2** | UI when portal blocked | Log only v0 |
| **Q3** | Migrate legacy `maximumCycles` tick for test floors | Keep both paths until test catalog migrated |

---

## 12. Doc sync checklist

- [ ] [Dungeon time §5](Dungeon-Time-Requirements.md) — cross-link; clarify production uses per-floor limits  
- [ ] [Floor 1 §9.8](Dungeon-Floor-1-Production-Requirements.md) — `floorDayNightCycleLimit: 4`  
- [ ] [Floor 2 production](Dungeon-Floor-2-Production-Requirements.md) — limit **6**, return portal §6, no `additional +2`  

---

## 13. Revision log

| Date | Change |
|------|--------|
| 2026-07-06 | Initial draft |
| 2026-07-06 | **Correction:** per-floor limits only; **no** run extension on first F2 visit; forced exit when **on** active floor at that floor’s limit |
