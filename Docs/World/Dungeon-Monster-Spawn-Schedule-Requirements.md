# Dungeon monster spawn schedules — Requirements

**Status:** Implemented (v1). MS3 map-presence spawn lock stubbed; per-region hall anchors deferred.

**Purpose:** Model *Surviving the Game as a Barbarian* (StGaaB) **day-driven monster groups**: each dungeon **day**, configured **spawn groups** in specific **floor areas** receive **incremental** reinforcements toward a **target composition**. Targets usually rise over days. Some entries spawn **once per dungeon run** and never return if killed. This doc defines authoring, runtime state, and integration with [dungeon time](Dungeon-Time-Requirements.md) and [zone layout](Dungeon-Zone-Layout-Requirements.md).

**Does not cover:** **Event-driven / conditional spawns** (levers, quests, map flags, scripted encounters). Those remain a **separate pipeline** — see [Conditional enemy spawn](../Combat/Conditional-Enemy-Spawn-Requirements.md). They are **never** members of a day schedule spawn group.

**Depends on:** [Dungeon time](Dungeon-Time-Requirements.md) (`DungeonTimeService`, `ElapsedCycles`, Day/Night phases), [Dungeon zone layout](Dungeon-Zone-Layout-Requirements.md) (`zoneId`, `zoneInstanceId`, `ZoneCellMap`), [Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md) (`DungeonFloorInstance` persistence §1.3), `EnemySpawnDefinition`, `EnemySpawnService`, [Multi-tile enemies](../Combat/Multi-Tile-Enemy-Requirements.md), [Monster map presence](Monster-Map-Presence-Requirements.md) (future global spawn lock while boss alive).

**Related:** [Dungeon zone population](Dungeon-Zone-Population-Requirements.md) (today’s **random scatter** at first visit — different model; see §3), [Conditional enemy spawn](../Combat/Conditional-Enemy-Spawn-Requirements.md) (lever/scripted one-offs), vault embedded enemies ([Dynamic dungeon floors §9](Dynamic-Dungeon-Floor-Generation-Requirements.md)).

**Explicitly out of scope (v1):** Save/load of spawn ledger mid-run across sessions; repopulation when leaving and re-entering a floor **without** day advancing (revisit is frozen — same as v0); experience-budget packs; spawn groups keyed to **town** calendar; enemy patrol routes; spawns during **night** phase (v1 fires at **day** boundaries only).

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Day schedules** — Designers author per-**group**, per-**dungeon-day** target compositions. |
| **G2** | **Incremental refill** — Each day spawns only the **delta** needed to reach targets (respecting alive counts), not a full wipe-and-replace. |
| **G3** | **Once-per-dungeon specials** — A row can spawn **at most once per run** (e.g. Giant Skeleton day 2); if still alive, it persists; if killed, it never respawns. |
| **G4** | **Area-bound groups** — Groups attach to **zone instances**, **zone ids**, or **authored regions** on a floor (see §6). |
| **G5** | **Composable with time** — Spawn pass runs on **dungeon day boundaries**, using the same run clock as [dungeon time §4](Dungeon-Time-Requirements.md). |
| **G6** | **Composable with bosses** — Daily spawn pass respects [monster map presence](Monster-Map-Presence-Requirements.md) global gates (e.g. Riakas disables all spawns). |
| **G7** | **Debuggable** — Logs use prefix **`[MonsterSpawn]`** with group id, day index, targets, deltas, and failures. |
| **G8** | **Orthogonal to conditional spawns** — Lever, quest, and other **triggered** enemies are **outside** spawn groups; they do not count toward group targets and are not refilled by the day pass. |

---

## 2. Glossary

| Term | Meaning |
|------|---------|
| **Dungeon day** | 1-based index of the current **day phase** in the run. **Day 1** = run start (`ElapsedCycles == 0`). After each completed day–night cycle, `ElapsedCycles` increments; entering the next **day phase** is **dungeon day** `ElapsedCycles + 1`. |
| **Spawn group** | A stable **`groupId`** with an **area binding** and a **day schedule**. All enemies spawned by the group share that id for counting. |
| **Target composition** | For a given group and dungeon day: desired **count per species row** (e.g. 2× Skeleton in group `north_wing_a`). |
| **Alive count** | Enemies in the group still on the grid (not dead). |
| **Refill delta** | `max(0, targetCount - aliveCount)` for a refill row; spawn up to that many new enemies. |
| **Once row** | Schedule row with **`fillPolicy: OncePerDungeonIfAbsent`** — at most one spawn ever per run for that row; never refills after death. |
| **Group activation** | A group with **no schedule row** for the current day contributes **nothing** (and does not refill). A group whose **first row** appears on day 2 is inactive on day 1. |
| **Scatter population** | Existing [zone population](Dungeon-Zone-Population-Requirements.md) random placement at **first visit** only. |
| **Conditional spawn** | Enemy created by an **event trigger** (lever, quest, flag, interactable effect) — **not** in any **`groupId`**; managed by [conditional enemy spawn](../Combat/Conditional-Enemy-Spawn-Requirements.md). |

---

## 3. Relationship to existing population

| System | When | Model |
|--------|------|--------|
| **Vault enemies** | First visit stamp | Fixed cells in `.vault`; unchanged |
| **Zone scatter** (`EnemyPopulationPhase`) | First visit | Random walkable cells in zone; **no day scaling** |
| **Scheduled groups (this doc)** | First visit **day 1** + each **new dungeon day** | Fixed **groups** in **areas**; **incremental** toward targets |
| **Conditional spawn** (lever, quest) | Event-driven | One-off; orthogonal |

**Locked (v1):** A floor (or zone) uses **either** scatter **or** scheduled groups for **routine** enemies, not both in the same zone instance.

| `monsterPopulationMode` (on floor or zone profile) | Behavior |
|-----------------------------------------------------|----------|
| **`Scatter`** (default today) | [Zone population](Dungeon-Zone-Population-Requirements.md) only |
| **`ScheduledGroups`** | This doc only; **no** scatter enemies in bound areas |
| **`Hybrid`** (optional v1.1) | Scatter for items/traps; scheduled groups for enemies only |

Traps, hazards, floor items may continue to use zone scatter regardless.

### 3.1 — Conditional / scripted spawns (separate system)

**Locked:** Enemies spawned because the player **flips a lever**, completes a **quest step**, sets a **map flag**, or similar are **not** part of day schedule spawn groups.

| Property | Scheduled group enemy | Conditional spawn |
|----------|----------------------|-------------------|
| **Trigger** | Dungeon day boundary (+ day 1 at entry) | Player / script event |
| **`MonsterSpawnGroupMembership`** | **Yes** — counts toward group refill | **No** |
| **Day pass refill** | Yes (if in schedule) | **No** |
| **Implementation** | `MonsterSpawnScheduleService` | `SpawnEnemyInteractableEffect`, future quest triggers |
| **Log prefix** | `[MonsterSpawn]` | `[EnemySpawn]` / interactable logs |

**Example (Floor 1, v0 today):** `LeverSwitch_First` → `SpawnEnemyInteractableEffect` → `Spawn_Skeleton_NorthOfLever.asset` summons **one skeleton** when the lever activates. That skeleton:

- Is **not** listed in any `MonsterSpawnGroupDefinition`
- Is **not** counted in hallway group alive totals
- Is **not** respawned on dungeon day 2 unless the **lever effect fires again** (v0: latched lever does not re-fire)

Conditional spawns may still use the same **`EnemySpawnDefinition`** asset type and **`EnemySpawnService`** for placement — only **ownership and scheduling** differ.

**Future boss gates:** A conditional or vault boss may still disable **scheduled** spawns via [monster map presence](Monster-Map-Presence-Requirements.md); conditional spawns already placed are unaffected unless a separate rule says otherwise.

---

## 4. StGaaB reference behavior

| StGaaB | JRogue mapping |
|--------|----------------|
| New monsters each dungeon day | **`MonsterSpawnScheduleService`** runs on **day boundary** |
| Counts usually increase | Higher **`targetCount`** on later **`dungeonDay`** rows |
| Sometimes new species after a day | New **schedule row** or new **group** starting on that day |
| Special spawn only once | **`OncePerDungeonIfAbsent`** row |
| Areas of the map have their own spawns | **`MonsterSpawnGroupDefinition.areaBinding`** → zone / region |
| Boss suppresses all spawns | [Monster map presence](Monster-Map-Presence-Requirements.md) **`DisableMonsterSpawnsWhileAliveEffect`** |

---

## 5. Worked example (authoring intent)

**Area:** `dungeon` zone, three hallway groups `hall_a`, `hall_b`, `hall_c`.

### Day 1 (dungeon start)

| Group | Schedule row | Target | Action |
|-------|--------------|--------|--------|
| `hall_a` | Skeleton, RefillToTarget | 1 | Spawn 1 |
| `hall_b` | Skeleton, RefillToTarget | 1 | Spawn 1 |
| `hall_c` | Skeleton, RefillToTarget | 1 | Spawn 1 |

**Result:** 3 groups × 1 skeleton.

### Day 2

| Group | Schedule row | Target | Action |
|-------|--------------|--------|--------|
| `hall_a` … `hall_d` | Skeleton, RefillToTarget | 2 | Each group: alive 0–2 → spawn **1 or 2** new skeletons to reach 2 |
| `boss_antechamber` | Giant Skeleton, **OncePerDungeonIfAbsent** | 1 | Spawn 1 giant **only if** never spawned this run |

Notes:

- **Fourth group `hall_d`** appears in the day-2 schedule (group **activates** on day 2).
- Groups at cap 2: if a group still has 2 alive, spawn **0** there.
- Giant: if player killed it on day 2, day 3 **does not** spawn another.

### Day 3

| Group | Schedule row | Target | Action |
|-------|--------------|--------|--------|
| `hall_a` … `hall_c` | Skeleton, RefillToTarget | 3 | Refill to 3 each |
| `hall_d` | Skeleton, RefillToTarget | 2 | Refill to 2 (not 3 — per-group day row) |
| `boss_antechamber` | Giant Skeleton, Once | — | **Skipped** (already spawned once; dead or alive) |

**Result:** Fixed per-group caps for day 3; giant never respawns if already used the once slot.

---

## 6. Area binding (where groups live)

Groups must resolve **candidate spawn cells** inside an **area** on a **`DungeonFloorInstance`**.

### 6.1 — Binding kinds (priority order at resolve time)

| Kind | Field | Use |
|------|-------|-----|
| **`ZoneInstance`** | `zoneInstanceId` (e.g. `center:dungeon`) | One jigsaw piece / compass slot instance |
| **`ZoneId`** | `zoneId` (e.g. `witch_forest`) | All instances with that habitat on the floor |
| **`SpawnRegion`** | `regionId` on floor layout | Designer rects or marker sets (see §6.2) |
| **`StampMarkers`** | `markerIds[]` | Pre-baked stamp floors: cells from `DungeonLayoutStamp` |

**Locked v1:** At least **`ZoneInstance`** and **`ZoneId`** for `ZoneComposite` floors.

### 6.2 — Spawn regions (optional v1.1)

On `DungeonFloorZoneLayout` or `DungeonFloorDefinition`:

```text
spawnRegions[]:
  regionId: "orc_barracks_north"
  zoneInstanceId: "west:orc_castle"   // optional narrow scope
  cells[] | normalizedRect | markerIds[]
```

Groups reference **`regionId`** to share one authored area among multiple schedule rows.

### 6.3 — Group anchors

Each group defines **one or more anchor cells** (absolute, or relative to region centroid / marker):

| Field | Purpose |
|-------|---------|
| **`anchors[]`** | Preferred spawn origins |
| **`anchorPolicy`** | `AtAnchor` / `NearestWalkableInArea` (default) / `RandomInArea` |
| **`minChebyshevBetweenSpawns`** | Avoid stacking in one tile (default 1) |

Placement reuses **`EnemySpawnPlacementResolver`** (footprint-aware) from [conditional spawn §3](../Combat/Conditional-Enemy-Spawn-Requirements.md).

**Global exclusions:** player safe zone, portal reserved cells, vault footprints — same as [zone population §5](Dungeon-Zone-Population-Requirements.md).

---

## 7. Authoring model

### 7.1 — Asset types

| Asset | Menu | Purpose |
|-------|------|---------|
| **`MonsterSpawnScheduleProfile`** | `JRogue/World/Monster Spawn Schedule Profile` | All groups for one floor **or** one zone habitat |
| **`MonsterSpawnGroupDefinition`** | Embedded in profile | One `groupId`, area binding, anchors, day table |
| **`MonsterSpawnDaySchedule`** | Embedded | Rows keyed by **`dungeonDay`** |
| **`MonsterSpawnCompositionRow`** | Embedded | One species line for that day |

Referenced from:

- **`DungeonFloorDefinition.monsterSpawnSchedule`** — floor-wide (stamp floors), or
- **`DungeonZoneDefinition.monsterSpawnSchedule`** — overrides / adds groups for that **`zoneId`** when using `ZoneComposite`.

### 7.2 — `MonsterSpawnGroupDefinition`

| Field | Type | Notes |
|-------|------|-------|
| **`groupId`** | string | Stable id (`hall_a`, `witch_forest_depth`) |
| **`displayName`** | string | Debug / logs |
| **`areaBinding`** | struct | §6.1 |
| **`anchors`** | `Vector3Int[]` or marker refs | §6.3 |
| **`anchorPolicy`** | enum | Default `NearestWalkableInArea` |
| **`daySchedules`** | `MonsterSpawnDaySchedule[]` | Sparse: only days with changes need rows |

### 7.3 — `MonsterSpawnDaySchedule`

| Field | Type | Notes |
|-------|------|-------|
| **`dungeonDay`** | int ≥ 1 | Applies at start of this dungeon day |
| **`composition`** | `MonsterSpawnCompositionRow[]` | Multiple species in one group same day |

**Inheritance (locked v1.1, optional v1):** If day **N** has no row, use the **most recent earlier** day’s row for that group. **v1 simpler rule:** missing row ⇒ **no spawn pass** for that group that day (designer authors every active day explicitly).

### 7.4 — `MonsterSpawnCompositionRow`

| Field | Type | Notes |
|-------|------|-------|
| **`spawnDefinition`** | `EnemySpawnDefinition` | Prefab + placement policy |
| **`targetCount`** | int ≥ 0 | Desired **alive** count after pass |
| **`fillPolicy`** | enum | §8 |
| **`speciesFilter`** | optional string | Count only matching `EnemySpeciesDefinition.speciesId` toward target |

---

## 8. Fill policies

| Policy | Behavior |
|--------|----------|
| **`RefillToTarget`** (default) | Let `alive` = living enemies in group matching row. Spawn `min(delta, targetCount - alive)` using anchor policy. |
| **`OncePerDungeonIfAbsent`** | If **`onceLedger[rowId]`** already set for this run → skip. Else if `alive >= 1` → mark ledger, skip spawn. Else spawn **1**, mark ledger. **Never** spawn again after death. |
| **`SpawnExactly`** (rare) | Spawn exactly `targetCount` new regardless of alive (story ambush); not used in StGaaB hallway example |

**Multi-tile enemies:** `targetCount` counts **entities**, not tiles.

---

## 9. Runtime

### 9.1 — Dungeon day index

```text
dungeonDay = ElapsedCycles + 1   // while CurrentPhase == Day after boundary
```

| Event | `ElapsedCycles` | `CurrentPhase` | Dungeon day for schedule |
|-------|-----------------|----------------|--------------------------|
| Run begin | 0 | Day | **1** |
| After 1st night completes | 1 | Day | **2** |
| After 2nd night completes | 2 | Day | **3** |

### 9.2 — When the spawn pass runs

| Hook | Pass |
|------|------|
| **`BeginDungeonRun`** / first visit gen complete | Apply **day 1** schedule (same algorithm as boundary) |
| **`DungeonTimeService`** Night → Day transition (`CycleCompleted`) | Apply **`dungeonDay = ElapsedCycles + 1`** |
| **Revisit floor, same day** | **No** pass |

**Scope (locked v1):** Run pass on **active floor only**. **v1.1:** optional pass on all **visited** generated floors when day advances (StGaaB-style off-screen progression).

**Gates:** If `MonsterMapPresenceService` reports spawn suppression → skip entire pass (log once).

### 9.3 — Algorithm (per group, per day)

```text
for each group G on active floor:
  row = G.schedule for dungeonDay (or inherit — see §7.3)
  if row is null: continue

  for each composition row R in row:
    alive = CountAlive(G, R.speciesFilter)
    switch R.fillPolicy:
      RefillToTarget:
        need = max(0, R.targetCount - alive)
        repeat need times: TrySpawnInGroup(G, R.spawnDefinition)
      OncePerDungeonIfAbsent:
        if ledger[G,R]: continue
        if alive > 0: ledger[G,R] = true; continue
        if TrySpawnInGroup(G, R): ledger[G,R] = true
```

`TrySpawnInGroup` picks anchor / cell in area, calls `EnemySpawnService`, attaches **`MonsterSpawnGroupMembership`**.

### 9.4 — `MonsterSpawnGroupMembership` (component)

| Field | Purpose |
|-------|---------|
| **`groupId`** | Which group owns this enemy |
| **`compositionRowId`** | Optional stable row id for once-ledger |
| **`spawnedOnDungeonDay`** | Audit / debug |

On **`EnemyController.Die`**: decrement alive pool; **do not** clear once-ledger.

**Conditional enemies:** No `MonsterSpawnGroupMembership` component — day pass ignores them entirely (§3.1).

### 9.5 — Persistence (in-memory per floor instance)

Store on **`DungeonFloorInstance`** (same lifetime as [dynamic floors §1.3](Dynamic-Dungeon-Floor-Generation-Requirements.md)):

| State | Purpose |
|-------|---------|
| **`onceSpawnLedger`** | `HashSet<(groupId, rowId)>` or string keys |
| **`lastAppliedDungeonDay`** | Idempotency if hook fires twice |
| **Spawned enemies** | Live objects under `EnemyContainer` + membership component |

**Return visit:** No schedule pass unless dungeon day advanced while away; dead enemies stay dead.

---

## 10. Pipeline integration

### 10.1 — Generation (first visit)

| Order | Phase | Change |
|-------|-------|--------|
| … | `ZoneBoundaryPhase` | unchanged |
| … | `VaultPlacementPhase` | unchanged |
| … | population phases | If **`ScheduledGroups`**: **skip** `EnemyPopulationPhase` for bound zones |
| **New** | **`MonsterSpawnSchedulePhase`** (or service call at end) | Apply **dungeon day 1** schedule only |

Alternative: no new phase — **`DungeonTimeService.BeginDungeonRun`** invokes schedule service after floor activation. **Recommendation:** single **`MonsterSpawnScheduleService.ApplyForDungeonDay(...)`** called from both **gen end** and **time hook**.

### 10.2 — Day boundary

```text
DungeonTimeService.TryTickAfterPlayerPhase()
  → phase advances Night → Day, CycleCompleted
  → MonsterSpawnScheduleService.OnDungeonDayStarted(dungeonDay, activeFloor)
```

### 10.3 — Logging

Prefix **`[MonsterSpawn]`**:

```text
[MonsterSpawn] Day 2 floor=dungeon_floor_01 group=hall_a skeleton target=2 alive=1 spawned=1
[MonsterSpawn] Day 2 group=boss_antechamber giant_once skipped (ledger)
[MonsterSpawn] Day 3 suppressed (Riakas map presence)
```

---

## 11. Designer workflow

1. **Pick area** — zone piece (`west:orc_castle`), whole `zoneId`, or spawn region (§6).
2. **Create groups** — stable `groupId` per hallway / camp / boss antechamber.
3. **Place anchors** — markers in layout editor or pick cells in [zone layout preview editor](Dungeon-Zone-Layout-Requirements.md).
4. **Author day tables** — for each group, add `MonsterSpawnDaySchedule` rows for days 1…N.
5. **Set composition** — `targetCount` + `fillPolicy` per species row.
6. **Assign profile** — attach to `DungeonZoneDefinition` or `DungeonFloorDefinition`; set **`monsterPopulationMode = ScheduledGroups`**.
7. **Playtest** — advance days via gameplay or debug **`[DungeonTime]`** overlay; verify deltas in **`[MonsterSpawn]`** logs.

**Example profile location (proposed):**

```text
Assets/Data/Dungeon/SpawnSchedules/Schedule_Floor01_Dungeon.asset
Assets/Data/Dungeon/SpawnSchedules/Schedule_Floor03_WitchForest.asset
```

---

## 12. Implementation plan

### Phase MS0 — Core service + day hook

- [ ] `MonsterSpawnScheduleProfile`, group/day/row structs
- [ ] `MonsterSpawnGroupMembership` + alive counting
- [ ] `MonsterSpawnScheduleService.ApplyForDungeonDay`
- [ ] Hook from `DungeonTimeService` on Night → Day
- [ ] Day 1 apply on first visit / run begin
- [ ] Unit tests: refill math, once-ledger, day index

### Phase MS1 — Zone binding

- [ ] `areaBinding` → `zoneInstanceId` / `zoneId` + `ZoneCellMap`
- [ ] Anchor placement via `EnemySpawnPlacementResolver`
- [ ] `monsterPopulationMode` on zone def; skip scatter when scheduled
- [ ] Example schedule on Floor 1 `dungeon` zone (migrate from pure scatter)

### Phase MS2 — Regions + tooling

- [ ] `spawnRegions` on layout
- [ ] Editor: visualize groups / anchors in zone layout window
- [ ] Optional: all-visited-floors pass on day advance

### Phase MS3 — Boss gates

- [ ] Integrate `MonsterMapPresenceService` spawn suppression
- [ ] Riakas-style profile on species

---

## 13. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC1** | Day 1 at dungeon entry: 3 groups × 1 skeleton spawns as in §5. |
| **AC2** | Day 2: each of 4 skeleton groups refills to 2 (1–2 new each); giant spawns once in once-group. |
| **AC3** | Day 3: skeleton groups hit 3/3/3/2 targets; giant does **not** respawn if killed on day 2. |
| **AC4** | Living giant from day 2 remains on day 3 without duplicate spawn. |
| **AC5** | Groups bound to `witch_forest` never spawn in `orc_castle` cells. |
| **AC6** | Revisit floor same dungeon day: no extra spawns. |
| **AC7** | Vault enemies unchanged; scatter traps/items still work in scheduled mode. |
| **AC8** | `[MonsterSpawn]` logs group, day, target, alive, spawned for each row. |
| **AC9** | Floor 1 lever skeleton (conditional) spawns on lever only; never appears in group counts or day refill. |

---

## 14. Design decisions (locked)

| # | Decision | Rule |
|---|----------|------|
| 1 | **Day index** | **`dungeonDay = ElapsedCycles + 1`** at start of each **day phase** |
| 2 | **Spawn timing** | Pass at **day start** (including run start), not at night start |
| 3 | **Refill model** | **Incremental toward target**, not daily full repop |
| 4 | **Once spawns** | **`OncePerDungeonIfAbsent`** — ledger survives death; never respawns |
| 5 | **Area scope** | v1: **active floor** only |
| 6 | **vs scatter** | Same zone: **scheduled XOR scatter** for routine enemies (§3) |
| 7 | **Group identity** | Enemies tagged with **`groupId`** for alive counts |
| 8 | **Conditional spawns** | **Never** in spawn groups; no membership tag; not refilled by day pass (§3.1) |

---

## 15. Open questions (resolve in playtest)

| ID | Question | Default |
|----|----------|---------|
| **Q1** | Missing schedule row: inherit previous day or skip? | **Skip** in v1; add inherit in v1.1 if authoring burden is high |
| **Q2** | Apply day pass to all visited floors? | **Active only** v1 |
| **Q3** | `targetCount` rolled per day (min–max) or fixed? | **Fixed** int v1; extend later |
| **Q4** | Weighted random species within one row? | **One row = one `spawnDefinition`** v1 |

---

## 16. Cross-links

| Need | Section |
|------|---------|
| Calendar / day index | §9.1, [Dungeon time §4](Dungeon-Time-Requirements.md) |
| Zone areas | §6, [Dungeon zone layout §9](Dungeon-Zone-Layout-Requirements.md) |
| Current random spawns | [Dungeon zone population](Dungeon-Zone-Population-Requirements.md) |
| Lever / scripted spawns | [Conditional enemy spawn](../Combat/Conditional-Enemy-Spawn-Requirements.md), §3.1 |
| Boss blocks spawns | [Monster map presence §9](Monster-Map-Presence-Requirements.md) |
| Placement / footprints | [Conditional enemy spawn §3](../Combat/Conditional-Enemy-Spawn-Requirements.md) |

---

## 17. Document history

| Date | Note |
|------|------|
| 2026-06-06 | Initial draft — StGaaB day-driven spawn groups, incremental refill, once-per-dungeon rows, zone area binding |
