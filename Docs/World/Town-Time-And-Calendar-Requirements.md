# Town time & calendar — Requirements

**Status:** v0 implemented — `TownTimeService`, town time levers, portal window, dungeon return phase.

**Purpose:** Model **town-only** passage of time: **morning → day → night** within a **calendar day**, separate from the **dungeon** day/night clock. v0 uses **two adjacent mutual-exclusive lever switches** to advance phases (Persona-style “spend time” stand-in). v0 also gates the **town → dungeon portal** to specific **morning** windows on a repeating **3-day** cadence. Returning from the dungeon always lands the party in town during the **day** phase.

**Design north star (post-v0):** Like *Persona 5*, certain **actions** (shops, dialogue, training, dungeons, etc.) advance the town phase and eventually the calendar. v0 replaces that action catalog with **lever bumps** until those systems exist.

**Depends on:** `TownPortalSetupPhase`, `TownToDungeonPortalInteractable`, `PortalEntryService`, `DungeonEntryService`, `DungeonExitService`, `InteractableTileService`, [Interactable tiles (levers)](../Combat/Interactable-Tiles-Requirements.md), [Dungeon time](Dungeon-Time-Requirements.md) (separate clock), [Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md) (`town_main` floor instance), [Shop NPCs](Shop-NPC-Requirements.md) (future time-cost hooks), [NPC dialog](NPC-Dialog-Requirements.md), [Lighting](Lighting-Requirements.md) (optional town presentation sync).

**Related scenes:** `TownTest.unity` / production **Town** scene; `town_main` floor via `TownCatalog`.

**Explicitly out of scope (v0):** Full Persona-style activity menu; NPC schedules keyed to phase; shops closing at night; save/load of town calendar mid-run across game sessions; month/season/year calendar; UI calendar widget (debug overlay OK); town lighting tied to phase (recommended follow-up); dungeon time affected by town levers; advancing town time while party is **inside** the dungeon.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | Town has its own **phase clock**: **Morning → Day → Night →** next calendar day **Morning**. |
| **G2** | Town phase advances are **independent** of dungeon player-turn calendar ([Dungeon time](Dungeon-Time-Requirements.md)). |
| **G3** | **v0 trigger:** Two **adjacent lever switches**; flipping either **on** advances one town phase and enforces **exactly one lever on**. |
| **G4** | **v0 portal gate:** On **days 1, 4, 7, …** (every third day from run start), during **Morning** only, the **town → dungeon portal** is enterable; portal **closes** when that day advances to **Day**. |
| **G5** | **Dungeon return:** Any exit from dungeon to town sets town phase to **Day** (same calendar day as when the party left town — does not skip days). |
| **G6** | **Future-ready:** `TownTimeService` accepts pluggable **phase advance triggers** beyond levers (NPC talk, shop, rest, story flags). |
| **G7** | **Calendar counter** exists in data model for v0 (integer **day index**); richer calendar (week/month) deferred. |
| **G8** | Town time state **persists** for the run while traveling town ↔ dungeon (DDOL service, not town floor `GameObject`s). |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Town phase** | One segment of a town day: **Morning**, **Day**, or **Night**. |
| **Town day** | One full sequence Morning → Day → Night. Identified by **`calendarDayIndex`** (integer ≥ 1). |
| **Phase advance** | Move to the next town phase per §4.2 (may roll calendar day). |
| **Calendar day index** | Run-scoped counter; increments when Night → Morning (§4.2). v0 has **no** month names. |
| **Portal window** | Morning phase on selected calendar days when `TownToDungeonPortalInteractable` accepts entry (§6). |
| **Time lever (v0)** | One of two mutual-exclusive town levers that calls `TownTimeService.AdvancePhase()`. |
| **Dungeon calendar** | Separate system — player-turn day/night cycles and run deadline ([Dungeon time](Dungeon-Time-Requirements.md)). |
| **Action trigger (future)** | Gameplay event (shop, dialog, etc.) that advances town phase without levers. |

---

## 3. Two clocks — town vs dungeon

| Aspect | **Town clock** (this doc) | **Dungeon clock** ([Dungeon time](Dungeon-Time-Requirements.md)) |
|--------|---------------------------|-------------------------------------------------------------------|
| **Phases** | Morning, Day, Night | Day, Night only |
| **Advance unit (v0)** | Lever activation (future: authored actions) | Completed **player phase** (turns) |
| **Purpose** | Hub pacing, portal windows, future NPC schedules | Run time pressure, forced exit |
| **Runs while in dungeon?** | **Frozen** — no phase ticks in dungeon (v0) | **Active** |
| **On dungeon → town** | Phase forced to **Day** (§7); day index unchanged | Clock reset on next dungeon **entry** |
| **Service** | `TownTimeService` (DDOL) | `DungeonTimeService` (DDOL run layer) |

**Locked:** Never conflate `DungeonTimePhase` with `TownTimePhase`. Presentation may reuse lighting enums internally but **state is separate**.

---

## 4. Town calendar model

### 4.1 — Phase enum

```csharp
public enum TownTimePhase
{
    Morning = 0,
    Day = 1,
    Night = 2,
}
```

### 4.2 — Phase sequence (locked)

```text
Run start (town) → Morning (day 1)
  → [advance] → Day (day 1)
  → [advance] → Night (day 1)
  → [advance] → Morning (day 2)
  → …
```

| Current phase | After `AdvancePhase()` |
|---------------|------------------------|
| **Morning** | **Day** (same `calendarDayIndex`) |
| **Day** | **Night** (same `calendarDayIndex`) |
| **Night** | **Morning** (`calendarDayIndex++`) |

### 4.3 — Run state (v0 fields)

Held by **`TownTimeService`** on the DDOL run layer (mirror pattern: `TownShopStateService`, `DungeonTimeService`):

| Field | Type | Initial (new run) |
|-------|------|-------------------|
| `calendarDayIndex` | int ≥ 1 | **1** |
| `currentPhase` | `TownTimePhase` | **Morning** |
| `activeTimeLeverId` | `InteractableTileId` or int | **None** — which of the two levers is visually **on** |

Optional debug:

| Field | Purpose |
|-------|---------|
| `totalPhaseAdvances` | Lifetime counter for QA |

**Not v0:** `dayOfWeek`, `season`, `year`, story `flags[]` on calendar (stub hooks OK in service API).

### 4.4 — Relationship to future Persona-style actions

Post-v0, multiple systems call the same API:

```text
TownTimeService.TryAdvancePhase(TownPhaseAdvanceSource source)
```

| `TownPhaseAdvanceSource` (illustrative) | v0 |
|----------------------------------------|-----|
| `TimeLever` | **Yes** |
| `ShopTransaction` | Future |
| `NpcDialog` | Future |
| `RestAtInn` | Future |
| `StoryScript` | Future |
| `DebugCheat` | Optional |

**Rule (future):** One successful call = **one** phase advance unless a scripted exception is explicitly authored.

---

## 5. v0 — Time advance levers

### 5.1 — Placement

Two lever instances on **`town_main`**, **orthogonally adjacent** (share an edge), placed via stamp markers or a **`TownTimeLeverSetupPhase`** (similar to `TownNpcSetupPhase`).

| Lever | Marker id (proposed) | Notes |
|-------|----------------------|-------|
| **Time lever A** | `town_time_lever_a` | Mutual pair |
| **Time lever B** | `town_time_lever_b` | Adjacent to A |

Suggested plaza cells (authoring TBD in stamp): e.g. `(8, 6)` and `(9, 6)` — **not** on portal/NPC cells.

**Sprites:** Reuse **`LeverSwitch_Off.png`** / **`LeverSwitch_On.png`** (handle right / left) from [Interactable tiles §12](../Combat/Interactable-Tiles-Requirements.md) — CC0 wall-mount levers at 32×32.

### 5.2 — Mutual-exclusive behavior (exception to latching levers)

Standard levers ([Interactable tiles §5](Interactable-Tiles-Requirements.md)) are **latching off→on only**. Town time levers add **switch** semantics:

| Rule | Detail |
|------|--------|
| **Pair** | Exactly **two** levers registered as `TownTimeLeverPair`. |
| **Exclusive on** | At most **one** lever visually **on** at a time. |
| **Bump off lever** | If lever is **off**, bump **turns it on**, **turns the other off**, then **advances town phase** (§4.2). |
| **Bump on lever** | If lever is **already on**, **no** phase advance, **no** turn cost (same as standard “already activated”). |
| **Initial state** | Both **off**; `activeTimeLeverId = None`. **First** bump on either lever: that lever **on**, phase advances **without** needing the other off first. |
| **Turn cost** | Successful phase-advancing bump consumes **one player action** (same as standard lever activation). |

```text
Both off, Morning
  → bump lever A → A on, B off, advance → Day
  → bump lever B → B on, A off, advance → Night
  → bump lever A → A on, B off, advance → Morning (day 2)
```

### 5.3 — Implementation sketch

| Piece | Responsibility |
|-------|----------------|
| **`TownTimeLeverEffect`** | `InteractableEffect` asset: call `TownTimeService.AdvancePhase(TimeLever)`, update pair visuals via service |
| **`TownTimeLeverPairDefinition`** | ScriptableObject or stamp metadata: lever A/B ids + cells |
| **`TownTimeLeverSetupPhase`** | Spawn/register pair on `town_main` generate |
| **`InteractableTileService`** | Extension or side channel to force **off** state on sibling lever when one activates |

**Do not** reuse SampleScene QA lever chain assets verbatim — town levers need the mutual-exclusive + time advance effect.

### 5.4 — Preconditions (v0)

| Precondition | v0 |
|--------------|-----|
| Block advance during blocking UI | **Future** — levers respect `InputHandler.BlocksFloorGameplay()` failures like other bumps |
| Block advance while in dungeon | N/A — levers exist only on `town_main` |
| Require portal closed | **No** |

---

## 6. v0 — Dungeon portal window

### 6.1 — Portal interactable

Existing **`TownToDungeonPortalInteractable`** at stamp marker `town_dungeon_portal` (default cell `(10, 10)` per town pack). v0 adds **eligibility** check before `DungeonEntryService.RequestEnterDungeonFromTown()`.

### 6.2 — Open rule (locked)

Portal entry is allowed **only when all** of the following hold:

| Condition | Rule |
|-----------|------|
| **Phase** | `currentPhase == Morning` |
| **Calendar cadence** | `calendarDayIndex % 3 == 1` |

**Examples:** Portal window on **morning of days 1, 4, 7, …** only.

**First run morning (day 1):** Portal **open** (qualifying window day).

### 6.3 — Close rule (locked)

When town phase advances **Morning → Day** on a portal-window day, portal **immediately closes** for that calendar day.

| Phase on day 1 | Portal enterable? |
|----------------|-------------------|
| Morning | **Yes** |
| Day | **No** |
| Night | **No** |

Portal does **not** reopen until the **next** qualifying morning (day 4, 7, …).

### 6.4 — Player feedback (v0 minimum)

| Event | Feedback |
|-------|----------|
| Step on portal while **closed** | Log `[TownTime] Portal closed — …` + optional one-line toast (future UI) |
| Step on portal while **open** | Existing enter-dungeon dialog / flow |
| Phase advance closes window | Log when day 1 (or 4, 7…) morning → day |

Suggested closed reasons in message:

- Wrong phase: *“The portal is dormant until morning.”*
- Wrong day: *“The portal opens on every third dawn (days 1, 4, 7…). Today is day {N}, {phase}.”*

### 6.5 — Visual (recommended, not blocking v0)

| State | Presentation |
|-------|----------------|
| **Open window** | Portal visual **active** (existing `PlacePortalVisual`) |
| **Closed** | Portal visual **inactive** / sealed overlay |

If art hook is deferred, **logic gate alone** satisfies AC.

---

## 7. Dungeon exit → town phase

When the party returns to town from the dungeon — **forced time exit** ([Dungeon time §7](Dungeon-Time-Requirements.md)), voluntary retreat (future), or floor chain back to hub:

| Rule | Behavior |
|------|----------|
| **Phase on arrival** | Set `currentPhase = Day` |
| **Calendar day** | **Do not** increment; preserve `calendarDayIndex` from when the party entered the dungeon |
| **Time frozen in dungeon** | Phases did not tick while underground (§3) |
| **Portal window** | Evaluate §6 on arrival — if arrival day is `{1,4,7…}` and phase is now **Day**, portal is **closed** until next qualifying morning |

**Example:** Party leaves town on **day 1 morning** (portal open), clears dungeon, returns same session → town **day 1, Day phase**, portal **closed** until day 4 morning.

**Contrast — forced dungeon exit survivor rules** (HP, statuses) remain in [Dungeon time §7.3](Dungeon-Time-Requirements.md); this section **only** sets town phase.

---

## 8. Town bootstrap & persistence

### 8.1 — New run / first town visit

| Field | Value |
|-------|-------|
| `calendarDayIndex` | **1** |
| `currentPhase` | **Morning** |
| Time levers | Both **off** |

### 8.2 — Town floor regen

`town_main` is regenerated when the town scene loads; **`TownTimeService` state is not reset** by floor regen (same pattern as shop snapshots).

### 8.3 — Dungeon entry / exit

| Transition | Town clock |
|------------|------------|
| Town → dungeon | **Freeze** (no advances in dungeon) |
| Dungeon → town | Apply §7 (**Day** phase) |
| Town lever bump | **Advance** per §4.2 |

### 8.4 — Next dungeon run

Entering dungeon starts a **fresh** [dungeon calendar](Dungeon-Time-Requirements.md). **Town calendar continues** across multiple dungeon dives in the same run (v0).

---

## 9. Lighting & presentation (optional v0)

| Approach | Recommendation |
|----------|----------------|
| **A — Town phase drives ambient** | `TownTimeService.PhaseChanged` → town `LightingService` / global tint per phase |
| **B — Presentation only on demand** | Defer to post-v0 |

**Default for v0:** **B** — logic and portal gate ship first; hook event for lighting backlog.

Future mapping (illustrative):

| Town phase | Ambient feel |
|------------|--------------|
| Morning | Warm, low sun |
| Day | Bright |
| Night | Cool, dim |

---

## 10. Data & services (implementation sketch)

### 10.1 — `TownTimeService`

| Responsibility |
|----------------|
| Own §4.3 state (DDOL) |
| `AdvancePhase(TownPhaseAdvanceSource)` / `TryAdvancePhase` |
| `bool IsDungeonPortalOpen()` — §6 |
| `ApplyDungeonReturnPhase()` — §7 |
| Events: `PhaseChanged`, `CalendarDayChanged`, `PortalWindowOpened`, `PortalWindowClosed` |
| Log prefix: **`[TownTime]`** |

### 10.2 — `TownToDungeonPortalInteractable` change

Before `DungeonEntryService.RequestEnterDungeonFromTown()`:

```text
if !TownTimeService.Instance.IsDungeonPortalOpen():
  show closed feedback
  return false
else:
  existing enter flow
```

### 10.3 — `DungeonExitService` / town load hook

After town scene load, before player control:

```text
TownTimeService.ApplyDungeonReturnPhase()
refresh portal visual from IsDungeonPortalOpen()
```

### 10.4 — Debug overlay (optional v0)

Example: `Town D1 · Morning · Portal OPEN` or `Town D2 · Morning · Portal CLOSED`.

---

## 11. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC-TT1** | New run starts at **day 1, Morning**; both time levers **off**. |
| **AC-TT2** | Bumping an **off** time lever turns it **on**, sibling **off**, consumes **one** player action, advances phase Morning→Day→Night→next Morning. |
| **AC-TT3** | Bumping the **already-on** lever does **not** advance phase or spend a turn. |
| **AC-TT4** | `calendarDayIndex` increments only on **Night → Morning** transition. |
| **AC-TT5** | Portal enterable **only** when `calendarDayIndex % 3 == 1` **and** phase is **Morning**. |
| **AC-TT6** | Advancing **Morning → Day** on a window day **closes** portal until next window morning. |
| **AC-TT7** | Day **1** morning: portal **open**. Days **2–3** morning: portal **closed**. Day **4** morning: portal **open**. |
| **AC-TT8** | Dungeon → town return sets phase to **Day** without changing `calendarDayIndex`. |
| **AC-TT9** | Dungeon calendar ticks **do not** advance town phase while in dungeon. |
| **AC-TT10** | Town phase state survives town ↔ dungeon travel within a run. |
| **AC-TT11** | `[TownTime]` logs phase changes, day rollovers, portal open/close, and rejected portal attempts. |

---

## 12. Future work (post-v0)

| Item | Notes |
|------|-------|
| **Persona-style action costs** | Map activities → `TryAdvancePhase` with optional “free actions” |
| **NPC schedules** | `NpcDefinition.availablePhases[]`, shop hours |
| **Rich calendar** | Weekday names, festivals, deadlines |
| **Multiple portal rules** | Quest-gated portals, weekend-only |
| **Save/load** | Persist `TownTimeService` in run save |
| **Manual day skip** | Inn rest advances to Morning (+ heal) |
| **UI** | Calendar widget, phase icon, portal countdown |
| **Lever removal** | Hide debug levers when action system replaces them |

---

## 13. Open questions

| # | Question | v0 default |
|---|----------|------------|
| **Q1** | Portal cadence: days **1,4,7** (`% 3 == 1`) vs **3,6,9** (`% 3 == 0`)? | **1,4,7** (`calendarDayIndex % 3 == 1`) |
| **Q2** | Starting phase **Morning** vs **Day** on brand-new run? | **Morning** |
| **Q3** | Should dungeon return ever land on **Morning** (e.g. “early exit” story)? | **No** — always **Day** (§7) |
| **Q4** | Time lever cells — final stamp coordinates? | **(8, 6)** and **(9, 6)** |
| **Q5** | Advance town phase when using shop / talk (no lever)? | **No** in v0 — levers only |
| **Q6** | Portal `% 3` uses 1-based day index? | **Yes** — day **1** is first open morning |

---

## 14. Traceability

| Request | Section |
|---------|---------|
| Persona-style action-driven time (future) | §1, §4.4, §12 |
| Town morning / day / night | §2, §4 |
| Town calendar (minimal v0) | §4.3, §12 |
| Two adjacent mutual-exclusive levers | §5 |
| Lever flip advances phase | §4.2, §5.2 |
| Portal on days 1, 4, 7 morning until day phase | §6 |
| Dungeon exit → town **day** phase | §7 |
| Town time ≠ dungeon time | §3 |
| Complex triggers later | §4.4, §12 |

---

## 15. Related docs

- [Dungeon time](Dungeon-Time-Requirements.md) — dungeon day/night **turn** clock, forced exit, survivor rules.
- [Interactable tiles (levers)](../Combat/Interactable-Tiles-Requirements.md) — base lever framework; town time levers extend with mutual-exclusive switch behavior (§5.2).
- [Shop NPCs](Shop-NPC-Requirements.md) — future candidate for phase-advance triggers.
- [Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md) — `town_main` floor id, portal markers, DDOL run layer.
