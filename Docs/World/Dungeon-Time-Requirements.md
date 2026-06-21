# Dungeon time — Requirements (draft)

**Status:** v0 implemented — calendar, forced town exit, per-floor authoring. See `DungeonTimeService`, `DungeonExitService`.

**Purpose:** Model **limited time inside the dungeon** (inspired by *Surviving the Game as a Barbarian* / StGaaB). When the run’s time budget is exhausted, the party is **forcibly returned to town** with specific survival rules. Time advances in **days and nights** driven by configurable **player-turn** budgets per phase, authored **per floor**.

**Inspiration (StGaaB):**

| StGaaB behavior | JRogue mapping |
|-----------------|----------------|
| Dungeon “ends” after a set number of **days/nights** | Run ends on **forced dungeon exit** (§7) |
| Player is transported back to **Rafdonia** (entry hub) | Party loads **Town scene** (§7.2) |
| Floor 1 ends after **7 days** (7 day–night cycles) | `baseDayNightCycles` on first dungeon floor (default **7**) |
| Each deeper floor adds **3 more days** | `additionalDayNightCycles` on each floor def; deadline **extends on first visit** (§5) |

**Depends on:** `TurnManager`, `PartyManager`, `DungeonRunState`, `DungeonFloorInstanceManager`, `DungeonFloorDefinition` / catalog, `CharacterStats` (`currentHP`, `currentSoulPower`, `MaxHP`, `MaxSoulPower`), `StatusEffectController`, [Party member death](../Party/Party-Member-Death-Requirements.md), [Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md) (DDOL run layer §1.2), [Lighting](Lighting-Requirements.md) (optional presentation sync §6.4), [Rest](../Progression/Rest-Requirements.md) (rest steps advance time §4.4), [Soul Power regeneration](../Progression/Soul-Power-Regeneration-Requirements.md), [Status effects](../Combat/Status-Effects-Requirements.md).

**Related scenes:**

| Scene | Role |
|-------|------|
| **`DungeonFloor.unity`** | Production dungeon shell — time clock runs while this run is active |
| **`DungeonFloorTest.unity`** | Test dungeon — same time rules for parity |
| **`Town.unity`** (TBD production name) | **Forced-exit destination** — party arrives here when time expires |
| **`TownTest.unity`** | Town iteration scene until production town ships |

**Explicitly out of scope (v0):** Save/load of dungeon clock mid-run; UI day/night calendar widget (logs + debug overlay OK); pausing time during modals; different clocks per party member; hunger; StGaaB-style “day only” floors without nights; town services (shop, heal NPC) beyond arrival rules in §7.3; reviving dead members on exit; returning dead members’ inventory from corpses; partial inventory strip on exit.

---

## 1. Goals

**G1 — StGaaB-style limited dungeon stay**  
A dungeon run has a **maximum number of day–night cycles** before the dungeon **forces an exit**.

**G2 — Configurable day/night length in player turns**  
Each floor defines how many **player turns** constitute **one day** and **one night** (independently configurable).

**G3 — Per-floor time authoring**  
Floors may use **longer days than nights** or the reverse (e.g. 80 turns day / 40 turns night).

**G4 — Cumulative deadline by depth**  
First floor sets the **base** budget; each subsequent floor adds **additional** cycles when first reached (§5).

**G5 — Forced exit to town**  
When time expires, transition immediately to the **Town scene** — no player confirmation required.

**G6 — Survivors keep inventory**  
Living party members **retain all inventory** (equipped, subspace, stacks, evocable charges per existing rules).

**G7 — Survivors fully refreshed (HP, Soul Power, statuses)**  
Living members heal to **maximum HP**, **maximum Soul Power** (or class-appropriate full resource where SP does not apply — §7.3), and **all status effects are removed**.

**G8 — Dead stay dead**  
Party members who **died during the dungeon** do **not** reappear in town. Their `GameObject` remains destroyed; they are not resurrected by forced exit.

**G9 — Party persists across scenes**  
The run party (living members + shared run state) **survives scene unload** of the dungeon the same way it survives floor switches today (DDOL / run layer).

**G10 — Single run clock**  
Time is tracked **once per dungeon run**, not reset when moving between dungeon floors (§5.2).

**G11 — Debuggability**  
Logs use prefix **`[DungeonTime]`** for phase changes, deadline extensions, warnings, and forced exit.

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Day–night cycle** | One **day phase** immediately followed by one **night phase**. StGaaB “7 days” means **7** such cycles (7 days + 7 nights of gameplay time). |
| **Day phase** | Calendar segment using `playerTurnsPerDay` (§4.2). |
| **Night phase** | Calendar segment using `playerTurnsPerNight` (§4.2). |
| **Player turn (time unit)** | One atomic advancement of the dungeon calendar (§4.1). |
| **Dungeon time tick** | Synonym for one player turn (time unit) applied to the calendar. |
| **Elapsed cycles** | Count of **completed** day–night cycles since run start (integer ≥ 0). |
| **Elapsed player turns (phase)** | Turns consumed in the **current** day or night phase (resets at phase boundary). |
| **Base budget** | `baseDayNightCycles` from the **first** floor in the dungeon chain (default **7**). |
| **Additional budget** | `additionalDayNightCycles` on a floor def; added to the run **once** when that floor is **first visited** (§5.3). |
| **Maximum cycles (deadline)** | `base + sum(additional for all floors first-visited this run)`; exceeding this triggers forced exit (§5.4). |
| **Forced dungeon exit** | Teardown of dungeon floor instances + town transition + §7.3 survivor rules. |
| **Living party member** | Actor still in `PartyManager.partyMembers` after death pipeline removed the dead. |

---

## 3. Reference — StGaaB vs JRogue

| Topic | StGaaB | JRogue (locked) |
|-------|--------|-----------------|
| Time unit | Days / nights | **Day phase** + **night phase**, each N **player turns** |
| Floor 1 limit | 7 days | `baseDayNightCycles = 7` (configurable) |
| Deeper floors | +3 days per floor tier | `additionalDayNightCycles` per floor (e.g. F2 **+3**, F3 **+4**) |
| On expiry | Return to Rafdonia | Load **Town** scene |
| Inventory | (story rules) | **Keep all** on survivors |
| HP / resources | (story rules) | **Full HP + full Soul Power** on survivors |
| Statuses | (story rules) | **Remove all** on survivors |
| Dead companions | (story rules) | **Stay dead**, not in town roster |

---

## 4. Calendar model

### 4.1 — What counts as one player turn (time unit)

**Locked (v0):** Advance the dungeon calendar by **one player turn** when the **player phase completes** — i.e. when `TurnManager` would begin the **enemy turn** because all **living** party members have finished their actions for that cycle (same boundary as `EvocableRechargeService.TickPartyAfterPlayerPhase()` / start of `EnemyTurnSequence()`).

| Mode | When to advance | v0 |
|------|-----------------|-----|
| **`PerPlayerPhase`** | Once per completed party player phase | **Default** |
| **`PerMemberAction`** | Each `OnPlayerActionComplete` for a living member | **Future** — only if playtesting shows per-phase is too coarse |

**Includes:** movement, attacks, abilities, door bumps, map interact, pickup commands, etc., that end a member’s action and participate in the normal turn pipeline.

**Includes (rest):** Each **rest step** counts as **one player turn** for calendar purposes (aligns with [Rest](Rest-Requirements.md) advancing dungeon time).

**Does not advance (v0):** Opening UI-only modals, targeting preview, camera nudge, failed command attempts that **do not** consume a turn, or time spent in `BUSY` / `GAME_OVER` without a completed player phase.

**Formation mode:** With `PerPlayerPhase`, a calendar tick still occurs only after **all** living members have acted (not per-member).

### 4.2 — Day and night phase lengths

Per `DungeonFloorDefinition` (or child `DungeonTimeProfile` asset — §9):

| Field | Type | Meaning |
|-------|------|--------|
| `playerTurnsPerDay` | int ≥ 1 | Player turns (§4.1) in one **day phase** while this floor’s calendar rules apply |
| `playerTurnsPerNight` | int ≥ 1 | Player turns in one **night phase** |

**While on a floor:** Use the **active floor’s** `playerTurnsPerDay` / `playerTurnsPerNight` for the **current** phase. When the party changes floors, the **next** phase boundary uses the **new** floor’s values (no retroactive change to the phase already in progress).

**Example:** Floor 1: 50 day / 30 night. Floor 2: 80 day / 40 night. Party travels F1 → F2 during a F1 night with 5 turns left: finish that night with F1’s 30-turn budget; the following **day** uses F2’s 80.

### 4.3 — Phase sequence

```text
Run start → DAY (floor F1 rules) → NIGHT (F1) → cycle 1 complete → DAY → NIGHT → …
```

- At run start: `currentPhase = Day`, `elapsedCycles = 0`, phase turn counter = 0.
- When phase turn counter reaches `playerTurnsPerDay` or `playerTurnsPerNight`: roll to next phase.
- When a **night** phase completes: `elapsedCycles++`.
- When `elapsedCycles >= maximumCycles` (§5.4): **forced exit** (§7) **before** starting the next day phase.

### 4.4 — Relationship to lighting day/night

[Lighting](Lighting-Requirements.md) may already define ambient regions with turn-based phases.

| Approach | v0 recommendation |
|----------|---------------------|
| **A — Dungeon time drives lighting** | When calendar enters Day/Night, notify `LightingService` / ambient region (preferred single source of truth). |
| **B — Independent clocks** | Allowed only for special floors with explicit `useIndependentLightingCycle` flag (debug / authored oddities). |

**Default:** **A** — dungeon calendar phase is authoritative for dungeon floors unless a floor opts into **B**.

---

## 5. Run deadline (cumulative budgets)

### 5.1 — Data per floor

On each `DungeonFloorDefinition` in the **main dungeon chain**:

| Field | Type | Default (floor 1) | Meaning |
|-------|------|-------------------|--------|
| `baseDayNightCycles` | int ≥ 1 | **7** | Only read from the **first** floor in the chain |
| `additionalDayNightCycles` | int ≥ 0 | **0** on F1; **3** on F2; etc. | Added to run deadline on **first visit** to this floor |
| `playerTurnsPerDay` | int ≥ 1 | TBD (design) | §4.2 |
| `playerTurnsPerNight` | int ≥ 1 | TBD (design) | §4.2 |

**Example chain:**

| Floor | `base` | `additional` | **Deadline after first reaching this floor** |
|-------|--------|--------------|-----------------------------------------------|
| `dungeon_floor_01` | 7 | 0 | **7** |
| `dungeon_floor_02` | (ignored) | 3 | **10** (= 7 + 3) |
| `dungeon_floor_03` | (ignored) | 4 | **14** (= 7 + 3 + 4) |

### 5.2 — Single run clock

- `DungeonRunState` (or dedicated `DungeonTimeService` on the run layer) holds:
  - `elapsedCycles`
  - `maximumCycles` (deadline)
  - `currentPhase` (Day / Night)
  - `phasePlayerTurnsElapsed`
  - `activeTimeFloorId` (which floor’s per-day/night values apply)
  - set of `floorIds` that have already applied their `additionalDayNightCycles`

- **Parking / revisiting floors** does **not** reset or pause the clock (v0).

### 5.3 — Extending the deadline

On **first visit** to floor `F` (generation or activation — same hook as “floor first created” in [Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md)):

```text
if F is first floor in chain:
  maximumCycles = F.baseDayNightCycles
else if F not in appliedAdditionalSet:
  maximumCycles += F.additionalDayNightCycles
  appliedAdditionalSet.Add(F)
```

**Revisit:** Returning to F2 after F1 → F2 → F1 does **not** add time again.

### 5.4 — Expiry condition

```text
if elapsedCycles >= maximumCycles:
  trigger ForcedDungeonExit()
```

**Timing:** Evaluate **immediately after** incrementing `elapsedCycles` (end of night). If the run is already at the limit at the start of a night, the night still plays out unless design chooses hard stop at **start** of final night — **locked:** allow the **final night** to complete, then exit at day boundary; if that feels too generous, tighten in playtest (document change in §12).

**v0 locked (initial):** Expire at **end of the last allowed night** (after `elapsedCycles` reaches `maximumCycles`), then forced exit **before** the next day begins.

### 5.5 — Warnings (recommended)

| Remaining cycles | Action |
|------------------|--------|
| 2 | `[DungeonTime]` log + optional UI toast (future) |
| 1 | stronger warning |
| 0 (expiry) | forced exit §7 |

---

## 6. Turn integration

### 6.1 — Hook point

Subscribe at the **player phase → enemy phase** boundary (same place rest counts a step):

1. `DungeonTimeService.OnPlayerPhaseCompleted()`
2. Increment `phasePlayerTurnsElapsed`
3. If phase limit reached → advance phase / maybe increment `elapsedCycles`
4. If expired → `ForcedDungeonExit()` and **do not** start enemy turn (or abort enemy turn if already started — prefer **before** enemy phase)

**Order (locked):**

```text
Player phase completes
  → DungeonTime tick (may force exit)
  → if not forced exit: EvocableRechargeService.TickPartyAfterPlayerPhase()
  → EnemyTurnSequence()
```

If forced exit runs, **skip** enemy phase for that tick.

### 6.2 — Rest

Each `TurnManager.ExecuteRestPlayerPhaseStep` counts as **one** dungeon time player turn (§4.1) using the **active floor’s** day/night lengths.

### 6.3 — Busy / game over

- **`GAME_OVER`:** Time does not advance (run ending handled elsewhere).
- **`BUSY`:** No advance until a player phase completes.

---

## 7. Forced dungeon exit → Town

### 7.1 — Trigger

`ForcedDungeonExit()` is called only from dungeon time expiry (v0). (Manual retreat from dungeon may share the same pipeline later with different survivor rules — **out of scope**.)

### 7.2 — Scene transition

1. Set `GameState` to block new player commands (`BUSY` or dedicated `FORCED_EXIT`).
2. Run **dungeon teardown** (same as voluntary exit):
   - `DungeonFloorInstanceManager.ExitDungeon()` / `DungeonRunState.ExitDungeon()` — destroy all floor instances, clear floor services ([Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md) §1.3).
3. Apply **survivor rules** (§7.3) on the DDOL party **before** or **after** unload as long as inventory references remain valid (prefer **before** scene unload).
4. `SceneManager.LoadScene` → hub scene (**v0:** **`DimensionSquareTest`** per [Floor 1 §9.11](Dungeon-Floor-1-Production-Requirements.md); production name TBD).
5. Town bootstrap places living party at authored **dungeon return spawn** (TBD marker on town stamp).

**Narrative:** “The dungeon collapses / ejects you” — exact copy TBD; mechanics are authoritative.

### 7.3 — Survivor rules (living members only)

For each actor in `PartyManager.partyMembers` (dead already removed per [Party member death](../Party/Party-Member-Death-Requirements.md)):

| Rule | Behavior |
|------|----------|
| **Inventory** | **Unchanged** — all containers, equipment, subspace, quantities |
| **HP** | `currentHP = MaxHP` |
| **Soul Power** | `currentSoulPower = MaxSoulPower` for members where `HumanClassRules.UsesSoulPower` |
| **Other class pools** | Mage/Priest: when Magic/Divine Power exist, set to **max** for that pool (parallel to SP) |
| **Status effects** | **Remove all** active statuses (`StatusEffectController.ClearAll` or equivalent) |
| **Death** | Do not spawn or restore dead members |

**Does not change (v0):** Experience level, essence unlocks, equipment identification, curse state, evocable recharge timers (unless a separate rule says otherwise — default **preserve**).

### 7.4 — Dead members

- Remain **absent** from `partyMembers` in town.
- **No** resurrection, **no** ghost followers, **no** inventory merge from destroyed corpses (v0).
- Future memorial / run log remains [Party member death](../Party/Party-Member-Death-Requirements.md) §10.

### 7.5 — Party across scenes

Align with [Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md) **§1.2 Run layer (DDOL)**:

| Object / system | On forced exit |
|-----------------|----------------|
| `PartyManager` + living prefabs | **Persist** (DDOL) |
| `DungeonRunBootstrap` / input / UI | Persist per existing dungeon entry setup |
| `DungeonFloorInstanceManager` | Destroy floor instances; manager may persist idle until next dungeon entry |
| `DungeonRunState` | Clear `activeFloorId`; reset or archive `DungeonTimeService` clock for **next** dungeon entry |
| Inventories on living members | Persist with actors |

**Next dungeon entry:** Clock **resets** to day 0 / cycle 0 with `maximumCycles = floor1.base` only (additional budgets re-applied as floors are visited again).

---

## 8. Town arrival (minimal v0)

**G1 — Spawn living party** at town **dungeon return** anchor (formation layout applies per [Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md) §6.5).

**G2 — Camera / turn** — `TurnManager` returns to `PLAYER_TURN`; town may use non-combat turn rules later (TBD).

**G3 — No enemies** from dungeon follow into town.

---

## 9. Data & services (implementation sketch)

### 9.1 — `DungeonTimeProfile` (optional ScriptableObject)

If floor defs become crowded, extract:

- `playerTurnsPerDay`, `playerTurnsPerNight`
- `baseDayNightCycles`, `additionalDayNightCycles`
- optional `useIndependentLightingCycle`

Referenced by `DungeonFloorDefinition`.

### 9.2 — `DungeonTimeService`

| Responsibility |
|----------------|
| Own run calendar state (§5.2) |
| `OnPlayerPhaseCompleted()` / `OnRestStep()` |
| Raise `PhaseChanged`, `CycleCompleted`, `DeadlineExtended`, `TimeExpired` |
| Call `ForcedDungeonExit()` |

Lifetime: **DDOL** on run layer; created with `DungeonRunState` / `DungeonRunBootstrap`.

### 9.3 — `DungeonExitService` (or extend `DungeonFloorInstanceManager`)

| Responsibility |
|----------------|
| `ForcedDungeonExit()` orchestration §7 |
| `ApplySurvivorRules()` §7.3 |
| `LoadTownScene()` |

---

## 10. UI & feedback (v0 minimal)

| Item | v0 |
|------|-----|
| Debug overlay | Optional: `Day 3/Night, cycle 2/7, turns 12/50` |
| Player-facing HUD | **Future** |
| Expiry modal | **One-button modal** on time expiry ([Floor 1 §9.11.1](Dungeon-Floor-1-Production-Requirements.md)) — show highest floor reached; on dismiss → **immediate** forced exit (§7) |
| Log on phase change | `[DungeonTime] Phase→Night (floor dungeon_floor_01, 0/30 turns)` |

---

## 11. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC-T1** | Floor 1 authored with `baseDayNightCycles = 7`, `additional = 0`, configurable day/night turn lengths. |
| **AC-T2** | Floor 2 adds `additional = 3` on first visit; deadline becomes **10** cycles. |
| **AC-T3** | Floor 3 adds `additional = 4` on first visit; deadline becomes **14** cycles. |
| **AC-T4** | Clock uses **PerPlayerPhase** ticks; rest steps advance the calendar. |
| **AC-T5** | Active floor’s `playerTurnsPerDay` / `playerTurnsPerNight` apply to the current phase. |
| **AC-T6** | When `elapsedCycles >= maximumCycles` after a night ends, party loads **Town** scene. |
| **AC-T7** | Living survivors: full inventory, full HP, full Soul Power, **no** statuses. |
| **AC-T8** | Dead members do not appear in town. |
| **AC-T9** | Dungeon floor instances destroyed on exit; revisiting dungeon later starts a **fresh** clock. |
| **AC-T10** | `[DungeonTime]` logs for phase change, extension, and forced exit. |

---

## 12. Open questions / playtest knobs

| # | Question | Default |
|---|----------|---------|
| Q1 | Expire at **end of last night** vs **start of last day**? | End of last night |
| Q2 | Show **remaining cycles** in HUD before UI milestone? | Debug overlay only |
| Q3 | Pause clock during blocking modals (trap confirm, death dialog)? | **No** (v0) |
| Q4 | `PerMemberAction` calendar mode needed? | Defer unless requested |
| Q5 | Town scene production name vs `TownTest` | Use `TownTest` until `Town.unity` exists |

---

## 13. Traceability

| Request | Section |
|---------|---------|
| StGaaB inspiration | §3 |
| Configurable turns per day/night | §4.2, §5.1 |
| Per-floor day vs night lengths | §4.2 |
| Floor 1 = 7 cycles | §5.1 |
| Cumulative additional per floor | §5.1, §5.3 |
| Forced exit → Town | §7 |
| Keep inventory | §7.3 |
| Full HP + Soul Power + clear statuses | §7.3 |
| Dead stay dead | §7.4 |
| Party persists across scenes | §7.5, §1.2 DDOL |

---

## 14. Related future work

- Manual **leave dungeon** with different rules (keep HP, no free heal).
- Save/load `DungeonTimeService` snapshot.
- Town gameplay (shops, quests) on arrival.
- Sync calendar to visible skybox / global post-processing.
- “Time pressure” modifiers (items that add days, curses that drain days).
