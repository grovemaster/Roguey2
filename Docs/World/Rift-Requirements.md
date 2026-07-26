# Rifts — Requirements

**Status:** **Implemented (v0)** — framework + Rift Test content pack. Run `JRogue/Dungeon/Create Rift Test Pack` in the Editor to generate/refresh assets and wire Floor 1 policy.

**Purpose:** Define **Rifts** as special, **hand-authored** dungeon spaces that exist **outside** the normal floor chain. A rift is entered through a **rift portal** on a host dungeon floor (player-triggered via offering, or **wandering**). Rifts use fixed layouts and enemy placements, keep the **dungeon timer running** without forcing a town return while inside, and return the party to their entry tiles (or town if the dungeon has already ended).

**Depends on:** [Altar & map interact](Altar-And-Map-Interact-Requirements.md) (offering slots / mana stones — extend for species filters), [Interactable tiles](../Combat/Interactable-Tiles-Requirements.md) (Northern Dark pedestal bump), [Dungeon Floor 1 production](Dungeon-Floor-1-Production-Requirements.md) (`northern_dark`, `vault_altar_3x3`), [Dungeon Floor 2 / descent plinth](Dungeon-Floor-2-Production-Requirements.md) (bump → portal precedent), [Dungeon time](Dungeon-Time-Requirements.md) / [Floor time limits](Dungeon-Floor-Time-Limits-Requirements.md) (day/cycle clock, forced exit), portal stack (`PortalInteractable`, `PortalEntryService`, `DungeonFloorInstanceManager`), [Enemy death loot & mana stones](../Combat/Enemy-Death-Loot-And-Mana-Stones-Requirements.md) (`goblin` / `ghoul` / `dire_wolf` species-tagged stones), [Dungeon log](../UI/Dungeon-Log-Requirements.md) (`GameLogService`), [Lord of the Floor](Lord-Of-The-Floor-Requirements.md) (orthogonal: day-start unique monsters; rifts are **not** LotFs).

**Related:** Floor portals (Floor 1↔2), town↔dungeon portals, vault stamps / layout stamps, monster spawn schedules (rifts **do not** use schedules), safe zones (orthogonal).

**Explicitly out of scope (v0):** Floor 2+ host rifts (must not break when a floor has **zero** rifts); multi-rift selection UI; party-splitting across rift vs host floor; capturable rift abilities; randomly generated rift layouts; monster schedules inside rifts; recovery of offered mana stones after portal opens; additional production rifts beyond **Rift Test**.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Rifts are orthogonal floors** — Hand-authored instances outside the normal Floor 1→2 chain; entered/exited only via rift portals. |
| **G2** | **Hard-coded content** — Layout, doors, initial enemies, special-room summons, boss, and exit portal cell are **authored**, not procedurally generated. |
| **G3** | **No monster schedule** — Rifts do not run `MonsterSpawnScheduleService`. Spawns are initial placement + **condition-gated** summons only. |
| **G4** | **Dungeon time continues** — Day/night cycles and the dungeon clock keep advancing while the party is in a rift. |
| **G5** | **No forced town from inside a rift** — If dungeon time expires while the party is in a rift, **do not** run the forced town exit. Resolve town return only when the party **exits the rift** (if the dungeon has already ended) or when time expires **outside** a rift (existing behavior). |
| **G6** | **Extensible catalog** — Multiple rifts per floor (future) via data; Floor 1 ships one (**Rift Test**); floors with **no** rifts are valid. |
| **G7** | **Portal discipline** — At most **one** rift portal open per host floor per dungeon run; configurable run cooldown, min day, and wandering rules per floor. |
| **G8** | **Monsters never use portals** — Monsters never path onto or enter tiles that contain a **floor portal** or a **rift portal**. |
| **G9** | **Whole-party entry (v0)** — If any living party member enters a rift portal, the **entire party** enters the rift. |
| **G10** | **Logs use prefix `[Rift]`** — Designer/debug logs; player-facing enter line via `GameLogService`. |

---

## 2. Glossary

| Term | Meaning |
|------|---------|
| **Rift** | A hand-authored dungeon instance (`RiftDefinition`) with its own layout, spawns, and exit rules. Not part of the normal floor chain. |
| **Host floor** | The dungeon floor that may open a rift portal (e.g. `dungeon_floor_01`). |
| **Rift portal** | Temporary portal tile on a host floor that teleports the party into a rift. |
| **Exit portal** | Portal inside a rift that appears after the **rift boss** dies; returns the party to the host floor entry spot. |
| **Player-triggered portal** | Rift portal created by satisfying authored trigger conditions (v0: Northern Dark pedestal offerings). |
| **Wandering portal** | Rift portal that spawns on a random unoccupied host-floor tile under wandering rules. |
| **Dungeon run** | One expedition: town → enter dungeon → exit/force-exit to town. The next town→dungeon entry is the next run. Maintain a **run index** (1, 2, 3, …) across expeditions in the same Play session (**R14 / O-B**; not only `runSeed`). Resets when Play Mode stops. |
| **Rift boss** | Designated enemy whose defeat unlocks the exit portal at a fixed cell. |
| **Special summon** | Enemy spawn gated by a condition (e.g. first entry into a room), not by the monster schedule. |
| **Portal open duration** | Turns a rift portal remains on the host floor before closing (**30** for v0). |
| **Wandering respawn delay** | Turns after a wandering portal despawns before it may spawn again elsewhere (**20** for v0). |

---

## 3. Framework

### 3.1 — Identity contract (`RiftDefinition`)

| Field | Required | Notes |
|-------|----------|-------|
| `riftId` | Yes | Stable id (e.g. `rift_test`) |
| `displayName` | Yes | Player-facing name (e.g. `Rift Test`) |
| `hostFloorIds` | Yes | Floors that may open this rift; Floor 1 only for v0 |
| `layout` | Yes | Hand-authored stamp / scene / vault composite — **not** ZoneComposite random gen |
| `entryAnchor` | Yes | Fixed party start cell(s) inside the rift |
| `exitPortalCell` | Yes | Fixed cell for exit portal after boss death |
| `riftBoss` | Yes | Species/spawn + identity so death can be detected |
| `initialSpawns` | Yes | Enemies present at rift load |
| `conditionalSummons` | Optional | Room/trigger → spawn list (once per condition) |
| `enterCombatLogLine` | Yes | Default: `You have entered {displayName}` |
| `trigger` | Optional | Player-trigger definition (pedestal offerings, etc.) |

### 3.2 — Host floor rift policy (`DungeonFloorRiftPolicy` or fields on `DungeonFloorDefinition`)

Configurable **per host floor** (floors with no rifts omit / leave empty):

| Field | Floor 1 (locked v0) | Meaning |
|-------|---------------------|---------|
| `rifts` | `[Rift Test]` | Catalog of rifts that may open on this floor |
| `maxRiftPortalsPerRun` | **1** | At most one rift portal (player-triggered **or** wandering) open per run on this floor |
| `minDungeonRunsBetweenPortals` | **3** | After a portal is triggered/opened on this floor, the next player-triggered open is eligible on run `lastPortalRun + 1 + 3` (i.e. **4th next** run counting the trigger run as current — see §5.2) |
| `minDungeonDayToOpenPortal` | **2** | Portal open only from dungeon day **2** onward (`dungeonDay = ElapsedCycles + 1`) |
| `minDungeonRunsBeforeWandering` | **5** | If no party has **entered** a rift belonging to this floor for **5** dungeon runs, wandering may begin on the **6th** such run (when day ≥ min day) |
| `riftPortalOpenTurns` | **30** | Lifetime of an open portal tile |
| `wanderingRespawnDelayTurns` | **20** | After wandering despawn, wait this many turns before spawning again elsewhere |

**Locked:** Floor 2 (and any floor without a rift policy / empty `rifts`) must **never** throw; rift services no-op when the active floor has no rifts.

### 3.3 — Portal state machine (per host floor, per run)

```text
None ──(player trigger OR wandering spawn)──► Open
Open ──(party enters)──► ConsumedForRun (portal gone; party in rift)
Open ──(30 turns elapsed)──► Closed
Closed ──(wandering only: +20 turns)──► Open (new random cell)   // until enter or dungeon ends
Closed ──(player-triggered)──► stays floor tile for rest of run (no auto re-open)
```

| Event | Result |
|-------|--------|
| Player trigger succeeds | Pedestal/interactable replaced by rift portal; offerings **consumed** (not recoverable); any wandering portal on this floor is **overwritten**; portal Open **30** turns; cooldown starts |
| Wandering spawn | Portal on random unoccupied, non-trap, non-hazard, non-blocked floor tile; Open **30** turns; **cooldown starts** (same as player trigger) |
| Party enters portal | Whole party → rift at `entryAnchor` (one member stepping on is enough); host portal removed; mark floor “rift entered”; combat log enter line |
| Portal timer hits 0 | Tile becomes normal floor; if wandering and no entry yet this run, schedule respawn after **20** turns |
| Rift boss dies | Spawn **exit portal** at fixed `exitPortalCell` inside rift |
| Party uses exit portal | Return to host entry cells (or nearest valid); if dungeon already ended → town dialog + town |

### 3.4 — Evaluation moments

| Moment | Behavior |
|--------|----------|
| Dungeon run begins (`BeginRun`) | Increment **dungeon run index** (O-B session meta); reset per-run portal open flag; evaluate wandering eligibility counters |
| Day start / during day | Allow portal open only if `dungeonDay >= minDungeonDayToOpenPortal` |
| Player completes pedestal offerings | Try player-triggered open if gates pass (§5) |
| Each player turn on host floor | Tick open portal lifetime; tick wandering respawn delay |
| First party member steps on rift portal | Whole-party enter (§6) |
| Rift boss death | Unlock exit portal |
| Dungeon time expiry | If party **in rift** → suppress forced town; flag `dungeonEndedWhileInRift`. If party **on host floor** → existing forced town |
| Exit rift | If dungeon ended (or flag set) → show dungeon-ended dialog → town; else arrive on host floor |

### 3.5 — Orthogonal systems

| System | Relationship |
|--------|----------------|
| Monster spawn schedule | **Off** inside rifts |
| Lord of the Floor | Unrelated; LotF still evaluates on host floors only |
| Mist of the Abyss | Host-floor map presence; does not apply inside rift unless a future rift opts in |
| Floor 1↔2 portals | Separate; monster ban applies to **both** floor and rift portals (**G8**) |
| Altar offering framework | Reuse/extend for Northern Dark pedestal trigger |

---

## 4. Implementation phases

| Phase | Name | Summary | Depends on |
|-------|------|---------|------------|
| **0** | Requirements capture | This document | — |
| **1** | Run index + floor rift policy | Persist dungeon run counter; attach per-floor rift policy data; no-op on floors without rifts | 0 |
| **2** | Rift instance + hand layout | Load authored **Rift Test** layout; entry anchor; initial spawns; conditional room summons; boss; exit portal on boss death | 0 |
| **3** | Portal enter/exit | Whole-party enter/exit; combat log; return to entry tiles / nearest valid; dungeon-ended-in-rift handling (**G5**) | 1–2 |
| **4** | Player-triggered portal | Convert Northern Dark pedestal to offering altar (species mana stones); replace with portal; 30-turn lifetime; consume stones | 1, 3, altar |
| **5** | Wandering portals | Counters, random tile spawn, 30/20 cycle, random rift from floor catalog | 1, 3 |
| **6** | Monster portal ban | Pathfinding / occupancy: monsters never enter floor or rift portal tiles | 3 |
| **7** | Content pack + tests | Editor pack for Rift Test; unit tests for gates, timers, exit fallback, Floor 2 no-rift safety | 1–6 |

---

## 5. Player-triggered portal (Floor 1 Northern Dark)

### 5.1 — Pedestal baseline (as-is)

- Vault: `vault_altar_3x3` in `northern_dark` (`INTERACTABLE bump_altar_indentations`).
- Today: flavor-only bump — *“There are 3 small indentations and 1 larger indentation.”*
- Offering altar framework exists (`AltarDefinition`, slots, `AltarOfferingService`) but is **not** wired to this vault yet.

### 5.2 — Locked trigger (v0)

**Offerings (consumed on success):** exactly one mana stone from each:

| Slot | Accepts |
|------|---------|
| 1 | Mana stone with `sourceSpeciesId == goblin` |
| 2 | Mana stone with `sourceSpeciesId == ghoul` |
| 3 | Mana stone with `sourceSpeciesId == dire_wolf` |

**Locked:** Tier may stay unrestricted or require the Floor 1 drop tier (tier **9** today); species is the required filter. Add `ManaStoneSpeciesAcceptFilter` (or tier+species).

**Dialog / UX:** Replace flavor-only bump with at least **one** offering dialog flow (reuse `AltarOfferingModalUI` or equivalent) so the player can place/remove stones before completion. Keep the existing “3 small indentations and 1 larger indentation” examine/flavor copy — the **larger indentation is reserved for a 4th offering in a later version** and is **inactive in v0** (not placeable / not required).

**On completion:**

1. Consume the three stones (not recoverable).
2. Unregister pedestal interactable; replace tile with **rift portal** (descent-plinth → portal pattern).
3. Bind portal to the chosen rift (**Rift Test** in v0). If a **wandering** portal is already open on this floor, **overwrite/remove** it — only one rift portal may exist (§3.2 / §8.3).
4. Portal remains **30** turns, then becomes normal floor.
5. Mark host floor: rift portal opened this run; record `lastPortalOpenedRunIndex` for cooldown.

### 5.3 — Gates (all must pass)

| Gate | Floor 1 rule |
|------|----------------|
| Host floor has ≥1 rift | Yes |
| `dungeonDay >= minDungeonDayToOpenPortal` | Day **≥ 2** |
| No rift portal already open / already used this run | `maxRiftPortalsPerRun = 1` |
| Run cooldown | `currentRunIndex >= lastPortalOpenedRunIndex + minDungeonRunsBetweenPortals + 1` with `minDungeonRunsBetweenPortals = 3` |

**Worked cooldown examples** (`minBetween = 3`):

| Last portal opened on run | Next eligible player trigger |
|---------------------------|------------------------------|
| 1 | 5 |
| 4 | 8 |
| Never | Any run with day ≥ 2 (subject to max-per-run) |

### 5.4 — Failure feedback

If the player completes offerings but a gate fails, show a clear dialog/combat-log reason (too early in the day, already used this run, cooldown, etc.) and **do not** consume stones (or refund if consumption was speculative — prefer check gates **before** consume).

---

## 6. Entering a rift

| Rule | Detail |
|------|--------|
| Trigger | Any living party member steps onto the rift portal tile (same whole-party policy as dungeon floor portals) |
| Party | **Entire party** enters (**G9**) |
| Arrival | Fixed `entryAnchor` every time |
| Combat log | `You have entered Rift Test` (or `{displayName}`) via `GameLogService` |
| Host portal | Removed/closed on entry |
| Monsters | Cannot enter the portal tile (**G8**) |

---

## 7. Inside a rift

### 7.1 — Rules of the space

| Topic | Rule |
|-------|------|
| Layout | Hand-authored only |
| Monster schedule | **None** |
| Conditional summons | Supported (once per condition) |
| Dungeon timer | Continues (**G4**) |
| Time expiry | **No** forced town while inside (**G5**) |
| Exit | Only via exit portal after boss death (v0); no second host-floor portal required |

### 7.2 — Exit portal

- Appears only after **rift boss** is defeated.
- Always at the same authored cell (`exitPortalCell`).
- **Combat log (locked):** when the exit portal appears, append `An exit portal opens.` via `GameLogService`.
- **No abandon (locked):** the party **cannot** leave the rift until the boss is dead and the exit portal is used. This is intentional **high risk, high reward**.

### 7.3 — Exiting to host floor

1. Party returns to the **exact host-floor cells** where they entered (formation-aware: entry footprint / leader cell + formation offsets as used elsewhere).
2. If those cells are occupied, trapped, hazardous, or otherwise illegal: place on the **nearest** unoccupied, non-trap, non-hazard, walkable tiles to the entry spot.
3. If the dungeon has already ended (timer expired while in rift, or ended upon exit): show the existing **dungeon has ended** dialog and transport the party to town (`DungeonExitService` path).

---

## 8. Wandering rift portals

### 8.1 — Eligibility

For host floor F:

- `rifts` non-empty.
- Runs since last **rift entry** on F ≥ `minDungeonRunsBeforeWandering` (**5** on Floor 1) → wandering may occur starting the **next** run (the “6th” run in the user’s framing).
- `dungeonDay >= minDungeonDayToOpenPortal` (**2**).
- No rift portal already open / consumed this run on F (`maxRiftPortalsPerRun`).

**Locked:** If the party never enters the wandering portal, wandering may run again on the **next** dungeon run (still subject to day + max-per-run).

**Locked — cooldown:** When a wandering portal **opens** (first spawn in that run’s wandering cycle), set `lastPortalOpenedRunIndex` the same as a player-triggered open — wandering starts the player-trigger run cooldown.

### 8.2 — Behavior while open

```text
Spawn at random valid tile → live 30 turns → despawn to floor
→ wait 20 turns → spawn at a (new) random valid tile → …
until party enters OR dungeon run ends OR overwritten by player trigger
```

| Constraint | Detail |
|------------|--------|
| Tile | Unoccupied, walkable floor; not trap; not hazard; not blocked interactable; not existing portal |
| Which rift | Uniform random among `rifts` on that floor (v0: only Rift Test) |
| Monsters | Never path onto the portal tile (**G8**) |
| Entry | Any one living party member stepping on the portal = **whole party** enters that rift (**G9**) |

### 8.3 — Overwrite when multiple rifts exist

**Locked:** A host floor may eventually list several rifts. At most one rift portal tile may exist on that floor at a time.

- If a **player-triggered** portal opens while a **wandering** portal is present, the wandering portal is **removed/overwritten** and replaced by the player-triggered portal (bound to the triggered rift).
- The overwritten wandering portal does not continue its 30/20 cycle.
- Entering whatever portal remains still moves the **whole party** into that portal’s bound rift.

---

## 9. Monster ban on portal tiles (**G8**)

**Locked for all portal kinds:**

- **Floor portals** (e.g. Floor 1↔2, descent plinth portal).
- **Rift portals** (player-triggered and wandering).
- **Rift exit portals** (monsters inside the rift must not stand on / path onto the exit portal tile).

Implementation intent: extend pathfinding / occupancy (precedent: `TrapService.IsPathingAvoidCell`, `MapCellOccupancy.BlocksActorEntry`) so **enemy** seekers treat portal cells as non-enterable. Players/party may still use portals.

---

## 10. First content — Rift Test

### 10.1 — Identity (locked)

| Field | Value |
|-------|-------|
| `riftId` | `rift_test` |
| `displayName` | `Rift Test` |
| Host floor | `dungeon_floor_01` only |
| Enter log | `You have entered Rift Test` |

### 10.2 — Layout (locked topology)

North is “deeper.” **Room A (start) → north → Room B (special) → north → Room C (boss).**

Coordinates below are **rift-local** (authoring space). Exact world origin is an implementation detail; relative topology is locked.

```text
[Room A — starting room] 10×20
  - Party entry anchor in this room (fixed)
  - 1 ghoul at local (3, 7) — initial spawn
  - North end: 1-tile-wide hallway, length 5 → Room B

[Room B — special room] 15×15   (north of Room A)
  - On first party entry into Room B: spawn 2 goblins at bottom-left and bottom-right corners (once)
  - North end: door → 1-tile-wide hallway, length 10 → door → Room C

[Room C — boss room] 20×20   (north of Room B)
  - Rift boss already present: v0 = regular Goblin (unique rift bosses later)
  - Exit portal cell: northern end of Room C (fixed); appears only after boss death
```

| Entity | When |
|--------|------|
| Ghoul @ (3,7) in Room A | Initial |
| 2 Goblins in Room B corners | First entry into Room B |
| Goblin boss in Room C | Initial |
| Exit portal (+ combat log) | Boss death |

### 10.3 — Combat baseline (v0)

- Boss is a **normal Goblin** (same HP/damage/loot/XP as a regular goblin) until redesigned.
- **Future:** rift bosses are intended to become **unique monsters**; the framework must identify the boss by rift definition, not by hard-coding “goblin.”

---

## 11. Related assets (target)

| Asset | Location (suggested) | Notes |
|-------|----------------------|-------|
| `Rift_Test.asset` | `Assets/Data/Rifts/` | Definition + layout ref |
| `RiftLayout_Test` stamp/scene | `Assets/Data/Rifts/Layouts/` | Hand-authored map |
| Floor 1 rift policy | On `Floor_prod_dungeon_floor_01` | §3.2 values |
| Pedestal altar def | `Assets/Data/Altar/` or Interactables | 3 active species slots + inactive 4th (larger) indentation reserved |
| Species accept filter | Code + asset | `goblin` / `ghoul` / `dire_wolf` |

| Component | Responsibility |
|-----------|----------------|
| `RiftDefinition` | Data contract |
| `RiftService` | Enter/exit, instance lifecycle, boss → exit portal |
| `RiftPortalService` | Player trigger, wandering, timers, per-floor run gates |
| `DungeonRunIndex` / extend `DungeonRunState` | Persistent run counter across expeditions |
| `RiftConditionalSummon` | Room-entry once summons |
| Pathfinding portal ban | Enemies never enter portal cells |
| Editor pack | Create/wire Rift Test + pedestal altar |

---

## 12. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC1** | Rift Test layout is northward: Room A → Room B → Room C; ghoul at Room A (3,7); Room B first entry spawns 2 corner goblins once; boss goblin in Room C. |
| **AC2** | Entering the rift logs `You have entered Rift Test` and places the party at the fixed entry anchor every time. |
| **AC3** | No monster spawn schedule runs inside the rift. |
| **AC4** | Dungeon day/night continues in the rift; time expiry **inside** does not force town; exiting after expiry shows dungeon-ended → town. |
| **AC5** | Boss death spawns exit portal at the fixed northern boss-room cell and logs `An exit portal opens.` |
| **AC6** | Exit returns party to host entry tiles, or nearest valid tiles if blocked. Party cannot leave without beating the boss. |
| **AC7** | Northern Dark pedestal keeps 3 small + 1 larger indentation flavor; v0 accepts goblin + ghoul + dire wolf stones on the three small slots; larger slot inactive; on success consumes stones, becomes a rift portal for 30 turns (overwriting any wandering portal). |
| **AC8** | Floor 1: max 1 rift portal per run; min day 2; min runs between portals = 3 (next eligible = last + 4). Wandering open also starts that cooldown. |
| **AC9** | Floor 1 wandering: after 5 runs without rift entry, from run 6 (day ≥ 2) portal may wander with 30 open / 20 delay cycle until entry, overwrite, or run end. |
| **AC10** | Monsters never path onto floor portals or rift portals. |
| **AC11** | One party member entering a rift portal moves the **whole** party. |
| **AC12** | Floor 2 (no rifts) loads and runs without rift-related errors. |
| **AC13** | Framework supports additional `RiftDefinition`s without rewriting enter/exit/portal services. |

---

## 13. Resolved decisions

| ID | Decision |
|----|----------|
| **R1** | Rifts are hand-authored; not randomly generated. |
| **R2** | No monster schedule inside rifts; conditional summons allowed. |
| **R3** | Dungeon timer runs in rifts; no forced town while inside. |
| **R4** | Exit portal appears only after rift boss death, at a fixed cell; combat log `An exit portal opens.` |
| **R5** | Player trigger v0: goblin + ghoul + dire wolf mana stones on Northern Dark pedestal; stones consumed; portal lasts 30 turns. Larger indentation reserved for a **4th offering later** (inactive in v0). |
| **R6** | Floor 1 policy: max 1 portal/run; min day 2; min runs between portals 3; wandering after 5 runs without entry (active from 6th); wandering 30/20. |
| **R7** | Whole party enters when any member steps on the rift portal (v0). |
| **R8** | Monsters never enter floor or rift portal tiles. |
| **R9** | Floor 2 has no rifts; systems must no-op safely. |
| **R10** | First rift is **Rift Test**; layout northward A→B→C (§10.2); boss is a normal Goblin for now (unique bosses later). |
| **R11** | Opening a **wandering** portal starts the same player-trigger run cooldown. |
| **R12** | Player-triggered portal **overwrites** any pre-existing wandering portal on that floor. |
| **R13** | No exit without beating the boss — intentional high risk / high reward. |
| **R14** | Run index / portal cooldown / rift-entry counters use **O-B**: `DontDestroyOnLoad` runtime meta that survives town↔dungeon scene loads for the play session; **reset when Play Mode stops** (or the built process quits). Full disk save (**O-C**) is future. |

---

## 14. Open questions

| ID | Question | Default if unresolved |
|----|----------|------------------------|
| **Q1** | Exact local coordinate origin / facing of Room A–C when stamped? | Author in editor pack; lock cells in playtest |

### 14.1 — Run-index persistence (**R14**)

**Locked: O-B — Runtime meta.**

Counters (`dungeonRunIndex`, `lastPortalOpenedRunIndex`, `lastRiftEnteredRunIndex` per host floor) live on a `DontDestroyOnLoad` service (or extended `DungeonRunState`) and survive **town ↔ dungeon** scene loads for the whole Play session / built-game process.

| Event | Counters |
|-------|----------|
| Town → dungeon → town → dungeon (same Play session) | **Persist** / increment across expeditions |
| Unity **Stop** (square button) / quit built game | **Reset** |
| Future campaign disk save (**O-C**) | Out of scope for v0; migrate later if a save format exists |

---

## 15. Playtest checklist

1. Floor 1 day 1: pedestal offerings cannot open a portal (min day 2).
2. Day 2+: place goblin + ghoul + dire wolf stones → portal replaces pedestal; stones gone; enter → log + fixed start in Room A.
3. Progress north: Room B summons once; boss death opens northern exit portal + log; cannot leave before boss; exit returns to entry tiles.
4. Let dungeon time expire inside rift → no town yet; exit → dungeon-ended dialog → town.
5. Trigger portal, leave it 30 turns → floor tile; cannot open second portal same run.
6. Let wandering open, then complete pedestal → wandering overwritten; entering moves whole party.
7. Across runs **in one Play session**: verify wandering open starts cooldown; 3-run cooldown; wandering after 5 runs without entry (30/20 cycle). Stop Play → counters reset.
8. Confirm enemies never step on Floor 1↔2 portal or rift portal tiles.
9. Enter Floor 2 / run with no rift policy — no errors.
10. One member walks onto portal — whole party is in the rift.

---

## 16. Revision history

| Date | Change |
|------|--------|
| 2026-07-25 | Initial draft — rift framework, Floor 1 pedestal trigger, wandering portals, Rift Test layout, timer/exit rules, monster portal ban, extensibility |
| 2026-07-25 | Resolve Q2–Q5, Q7; clarify northward A→B→C; wandering cooldown + overwrite; exit log; 4th indentation reserved; no abandon; elaborate Q6 run-index persistence options |
| 2026-07-25 | Lock **R14 / O-B** — runtime meta survives town↔dungeon; resets on Play Mode stop |
| 2026-07-25 | **Implemented v0** — `RiftService` / `RiftPortalService` / session meta, Floor 1 pedestal→portal, wandering, portal path ban, Rift Test pack + gate unit tests |
