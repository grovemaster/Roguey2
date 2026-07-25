# Lord of the Floor (LotF) — Requirements (draft)

**Status:** **Implemented (v0)** — LotF framework + Giant Skeleton King + Mist of the Abyss. Run **JRogue → World → Create Lord of the Floor v0 Assets (Giant Skeleton King)** in Unity to author/wire content assets.

**Purpose:** Define **Lords of the Floor** — special dungeon monsters with **unique names, titles, and summoning conditions**. Each LotF may appear **at most once per dungeon run**. This document specifies the **shared LotF framework** and the **first LotF**: **Giant Skeleton King**, *Lord of Giant Skeletons*.

**Depends on:** [Dungeon time](Dungeon-Time-Requirements.md) (dungeon day index / day-start boundary), [Dungeon Floor 1 production](Dungeon-Floor-1-Production-Requirements.md), [Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md) (multi-floor park/persist, floor identity), [Multi-tile enemies](../Combat/Multi-Tile-Enemy-Requirements.md) (2×2 Giant Skeleton baseline), [Conditional enemy spawn](../Combat/Conditional-Enemy-Spawn-Requirements.md) (`EnemySpawnService` / placement), [Monster map presence](Monster-Map-Presence-Requirements.md) (floor-wide while-alive effects), [Enemy death loot & mana stones](../Combat/Enemy-Death-Loot-And-Mana-Stones-Requirements.md), [Mist of the Abyss](../Essence/Mist-Of-The-Abyss-Essence-Requirements.md) (reusable LotF ability; first host = Giant Skeleton King).

**Related:** [Dungeon monster spawn schedules](Dungeon-Monster-Spawn-Schedule-Requirements.md) (day-driven **groups** — **orthogonal**; LotF is **not** a schedule refill row), existing **Giant Skeleton** species / prefab (`giant_skeleton`, `GiantSkeletonEnemy`).

**Explicitly out of scope (v0):** Additional Lords beyond the first; LotF despawn-without-death behaviors (framework must **reserve** the once-per-run slot if despawn is added later); save/load of LotF ledger across sessions; LotF AI beyond Giant Skeleton baseline; custom boss UI / health bar; cinematic intro; town / overworld LotFs.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Named lords** — Every LotF has a **name** and a **title** (player-facing identity distinct from ordinary species display names). |
| **G2** | **Unique summon gates** — Each LotF has its **own** summoning condition; the framework evaluates conditions at defined run moments (v0: **dungeon day start**). |
| **G3** | **Once per dungeon run** — A given LotF may be **summoned at most once** in a run. Death **or** despawn (any reason) **consumes** that LotF’s run slot permanently. |
| **G4** | **First LotF content** — Ship **Giant Skeleton King** (*Lord of Giant Skeletons*) on production Floor 1 with locked gates, spawn placement, loot, and **Mist of the Abyss**. |
| **G5** | **Reuse Giant Skeleton combat shell** — Footprint / attack profiles start from the existing **2×2 Giant Skeleton**; LotF is a **distinct species** with its own id, name, title, loot, presence effects, and a **v0 red tint** on the shared sprite (unique art later). |
| **G6** | **Data-driven** — LotF identity, summon rules, spawn definition, and presence/essence hooks are authored assets — not hard-coded one-off scene scripts. |
| **G7** | **Debuggable** — Logs use prefix **`[LotF]`** with lord id, day index, gate pass/fail reasons, spawn cell, and run-slot state. |
| **G8** | **Announce appearance** — On successful summon, write a **combat log** line that a Lord of the Floor has appeared (name + title). |

---

## 2. Glossary

| Term | Meaning |
|------|---------|
| **Lord of the Floor (LotF)** | A special enemy unique to a dungeon context, with a **name**, **title**, **summoning condition**, and usually a **unique ability** / map presence. |
| **Name** | Primary identity string (e.g. **Giant Skeleton King**). |
| **Title** | Honorific / epithet (e.g. **Lord of Giant Skeletons**). Presentation may show as `Name, Title` or name with title subtitle — pick one UX and keep it consistent. |
| **LotF id** | Stable machine id (locked for first: `lotf_giant_skeleton_king`). |
| **Dungeon day** | 1-based day index from [dungeon time](Dungeon-Time-Requirements.md) / [spawn schedules glossary](Dungeon-Monster-Spawn-Schedule-Requirements.md): **Day 1** = run start; later days after each completed day–night cycle. |
| **Day start** | The moment the run enters a **new dungeon day phase** (same boundary used for day-schedule spawn passes). Day 1 start = dungeon run begin. |
| **Run slot (LotF)** | Per-run flag for one LotF: **`Available`** → **`Summoned`** (alive or pending) → **`Consumed`** (slain **or** despawned). **`Consumed`** never returns to **`Available`** in that run. |
| **Summon gate** | Boolean condition evaluated at an evaluation moment; if true **and** run slot is **`Available`**, the LotF may spawn. |
| **Host floor** | Floor on which the LotF is allowed to exist / apply its floor-wide effects (v0 first LotF: **Floor 1** / `dungeon_floor_01`). |
| **Party size (gate)** | Count of **living** party members in the roster at gate check time (`HP > 0`). Dead members are ignored; party splitting across floors is **out of scope** for v0 (see §5.2). |

---

## 3. Framework (all LotFs)

### 3.1 — Identity contract

Every LotF definition **must** author:

| Field | Required | Notes |
|-------|----------|-------|
| **`lotfId`** | Yes | Stable string id |
| **`displayName`** | Yes | Name |
| **`title`** | Yes | Title / epithet |
| **`hostFloorId`** | Yes | e.g. `dungeon_floor_01` |
| **`species` / prefab** | Yes | Enemy spawn target |
| **`summonEvaluator`** | Yes | Gate logic (asset or typed rule set) |
| **`spawnPlacement`** | Yes | How to pick cells on host floor |
| **`oncePerRunPolicy`** | Yes | Locked default: **consume on summon success**; remain consumed on death **or** despawn |
| **`uniqueAbility` / presence** | Optional | Link to essence and/or [map presence](Monster-Map-Presence-Requirements.md) profile |

### 3.2 — Once-per-run rule (locked)

| Event | Run slot transition |
|-------|---------------------|
| Run begins | **`Available`** for each LotF that can appear in this dungeon type |
| Summon succeeds (enemy instantiated) | **`Summoned`** |
| LotF slain | **`Consumed`** — **cannot** summon again |
| LotF despawns for any reason (future behaviors, floor tear-down mid-fight, etc.) | **`Consumed`** — **cannot** summon again |
| Gate fails | No change |
| Gate would pass but slot is **`Summoned`** or **`Consumed`** | **No spawn** |

**Locked:** “Once per dungeon run” means **one summon opportunity consumed forever** after the first successful summon — **not** “one living instance at a time with respawn after leave.”

### 3.3 — Evaluation moments (v0)

| Moment | Behavior |
|--------|----------|
| **Dungeon day start** | Evaluate all LotFs whose gates are day-start based |
| Other triggers (lever, quest, item) | **Out of scope** for v0; framework should allow future evaluators |

**Ordering vs day schedule:** LotF evaluation runs in a **deterministic** order relative to [monster spawn schedule](Dungeon-Monster-Spawn-Schedule-Requirements.md) day pass (recommend: **after** schedule refill, or **before** — pick one, log both). LotF enemies are **not** members of spawn groups and **do not** refill.

### 3.4 — Orthogonal systems

| System | Relationship |
|--------|--------------|
| **Scheduled groups** | LotF is **not** a `OncePerDungeonIfAbsent` schedule row |
| **Conditional lever spawns** | Different trigger family; may share `EnemySpawnService` placement |
| **Ordinary Giant Skeleton** | Separate species / content; killing a normal giant does **not** consume LotF slot |
| **Monster map presence** | Preferred host for floor-wide while-alive effects (Mist of the Abyss) |

---

## 4. Implementation phases (overview)

| Phase | Name | Summary | Depends on |
|-------|------|---------|------------|
| **0** | **Requirements capture** | This document + [Mist of the Abyss](../Essence/Mist-Of-The-Abyss-Essence-Requirements.md) | — |
| **1** | **LotF runtime & ledger** | Run-slot state, day-start evaluation hook, `[LotF]` logging | Dungeon time |
| **2** | **Giant Skeleton King species / prefab** | Distinct LotF enemy based on 2×2 Giant Skeleton; name + title; **red tint** on shared sprite | Multi-tile enemy baseline |
| **3** | **Summon gates + placement + announce** | Day ≥ 3, ≥ 4 living members, party on Floor 1; random 2×2 fit; once-per-run; combat-log appearance line | Phases 1–2, Floor 1 |
| **4** | **Loot** | Dedicated loot table: **1×** tier-**8** mana stone at **100%** | Enemy loot pipeline |
| **5** | **Mist of the Abyss** | Floor-wide disable of essence **actives**; see essence doc | Map presence + essence execute gates |
| **6** | **Playtest & QA** | Acceptance checklist §9 | Phases 1–5 |

---

## 5. First LotF — Giant Skeleton King

### 5.1 — Identity (locked)

| Field | Value |
|-------|--------|
| **`lotfId`** | `lotf_giant_skeleton_king` |
| **Name** | **Giant Skeleton King** |
| **Title** | **Lord of Giant Skeletons** |
| **Host floor** | Production **Floor 1** (`dungeon_floor_01`) |
| **Visual / combat base** | Same **Giant Skeleton** **2×2** sprite and `GiantSkeletonEnemy` attack profiles, with a **red tint** so the King is visually distinct from scheduled / ordinary Giant Skeletons (§5.4.1) |
| **Species id (proposed)** | `giant_skeleton_king` (must **not** collide with `giant_skeleton`) |
| **First-kill XP** | **100** (editable later on the species asset) |
| **Unique ability** | **Mist of the Abyss** — [full requirements](../Essence/Mist-Of-The-Abyss-Essence-Requirements.md) |

**Presentation (locked):** Combat log appearance line, examine, and related UI communicate both name and title (e.g. *Giant Skeleton King, Lord of Giant Skeletons*). Strings must be data-driven.

### 5.2 — Summoning condition (locked)

Evaluate at **every dungeon day start** beginning with **day 3**:

| Gate | Rule |
|------|------|
| **Day index** | `dungeonDay >= 3` (valid on start of day **3**, **4**, **5**, …) |
| **Floor presence** | The **player’s party** is on **Floor 1** at that day-start evaluation (v0 assumes the party is co-located; party splitting is a later concern) |
| **Floor availability** | Floor 1 is still a live / available floor of the current dungeon run (not ended / torn down) |
| **Party size** | **≥ 4 living** party members in the roster (`HP > 0`) |
| **Run slot** | `lotf_giant_skeleton_king` is still **`Available`** |

**All gates must pass.** If any fail, do not summon; leave slot unchanged so a **later** day start may still succeed.

**Examples:**

| Situation at day start | Result |
|------------------------|--------|
| Day 2, 5 living members, party on Floor 1 | **No** (day too early) |
| Day 3, 3 living members (e.g. 5 roster slots but 2 dead), party on Floor 1 | **No** (party size) |
| Day 3, 4 living + 1 dead, party on Floor 1 | **Yes** — dead members do **not** block if living count ≥ 4 |
| Day 3, 4 living members, party on Floor **2** | **No** (wrong floor) |
| Day 3, 4 living members, party on Floor 1, never summoned | **Yes** — spawn |
| Day 4, 4 living members, party on Floor 1, King already slain | **No** (slot **Consumed**) |
| Day 5, 4 living members, party on Floor 1, never summoned (gates failed days 3–4) | **Yes** — spawn |

**Party size detail (locked):** Count **living** members (`HP > 0`) in `PartyManager`. Dead / destroyed members are **ignored** — they neither help nor hurt the gate. Recruits count if living. **v0 does not** require counting only members “present on Floor 1” separately from the floor-presence gate; if party splitting ships later, revisit whether living members on other floors still count.

**“Start of day onwards”:** There is **no** upper day cap. As long as Floor 1 remains available, the party is on Floor 1, ≥ 4 living members exist, and the run slot is still **`Available`**, summon may occur on day 3+.

### 5.3 — Spawn placement (locked)

| Rule | Detail |
|------|--------|
| **When** | Immediately when summon gates pass |
| **Where** | **Random** valid anchor on **Floor 1** that can accommodate a **2×2** footprint |
| **Validity** | Same footprint rules as [conditional spawn](../Combat/Conditional-Enemy-Spawn-Requirements.md) / multi-tile placement: all four cells walkable, not blocked by interactables that block occupancy, not occupied by party/enemies |
| **Failure** | If **no** valid 2×2 cell exists, **do not** consume the run slot; log `[LotF]` failure; retry on a **later** day-start if gates still pass |
| **Success** | Instantiate LotF; set run slot to **`Summoned`**; emit **combat log appearance** line (§5.3.1) |

**Locked:** Placement is **floor-wide random among valid 2×2 anchors**, not restricted to a single zone unless playtest proves needed (then amend this section).

### 5.3.1 — Combat log on appearance (locked)

On **successful** summon (enemy instantiated on the grid):

| Rule | Detail |
|------|--------|
| **Channel** | Player-facing **combat log** (same channel as other encounter / kill messages) |
| **Content** | Must state that a **Lord of the Floor** has appeared, and identify this lord by **name** and **title** |
| **Must not include** | Spawn **location**, grid coordinates, zone name, or any other placement hint that reveals where the LotF appeared |
| **Example copy** | *The Giant Skeleton King, Lord of Giant Skeletons, has appeared!* (exact wording may be tuned; keep name + title + “appeared”) |
| **Timing** | Once per successful summon (which is at most once per run) |
| **Debug** | Still emit `[LotF]` engineer log with cell / day / id (debug/dev only — **not** mirrored into the combat log) |

### 5.4 — Combat baseline

| Property | Value |
|----------|--------|
| **Footprint** | **2×2**, bottom-left anchor (same as Giant Skeleton) |
| **Attack profiles** | Reuse Giant Skeleton **adjacent single-target** + **side sweep** selection ([Multi-tile enemy §4.3](../Combat/Multi-Tile-Enemy-Requirements.md)) |
| **XP / journal** | Independent species from `giant_skeleton`; **`firstKillExperience = 100`** (designer may change later) |

### 5.4.1 — Visual distinction from ordinary Giant Skeleton (locked)

Ordinary / scheduled Giant Skeletons (`giant_skeleton`) and the King must be distinguishable at a glance on Floor 1.

| Property | Value |
|----------|--------|
| **Sprite** | **Same** Giant Skeleton **2×2** sprite as the baseline monster |
| **Tint (v0)** | **Red tint** on the King’s renderer / sprite color (ordinary giants remain untinted / baseline color) |
| **Future** | Replace with an **entirely unique sprite**; remove or keep tint as art direction then decides |
| **Must not** | Share an indistinguishable look with schedule-spawned giants so players confuse LotF with trash |

### 5.5 — Loot on defeat (locked)

| Payload | Tier | Drop chance |
|---------|------|-------------|
| Mana stone | **8** | **100%** (**exactly one** guaranteed stone) |

**Notes:**

- This **overrides** ordinary `EnemyLootTable_GiantSkeleton` (3× guaranteed + 30% fourth). The King uses a **dedicated** loot table.
- `sourceSpeciesId` on the stone should identify the King species (e.g. `giant_skeleton_king`).
- Mist of the Abyss is **never** a loot drop and is **not capturable** — see [Mist of the Abyss](../Essence/Mist-Of-The-Abyss-Essence-Requirements.md).

### 5.6 — Unique ability summary

While the Giant Skeleton King is **alive**, **Mist of the Abyss** applies to the **entire Floor 1** (suppression, combat logs, HUD badge + edge vignette). Full rules: [Mist of the Abyss — Requirements](../Essence/Mist-Of-The-Abyss-Essence-Requirements.md). Mist is implemented as a **reusable** presence effect so future LotFs can attach the same ability.

**Ends / clears when any of:**

1. Party leaves Floor 1 (e.g. descends to Floor 2) — mist **does not** apply on other floors; visuals hide  
2. Dungeon run ends  
3. Giant Skeleton King dies or despawns (presence reverts; visuals hide)

**Reapplies** when the party re-enters Floor 1 while the King is still alive.

---

## 6. Related assets (target)

| Asset | Location (suggested) | Notes |
|-------|----------------------|-------|
| `LordOfTheFloorDefinition` (or equiv.) | `Assets/Data/Enemy/LotF/` | Framework SO: id, name, title, gates, spawn, presence |
| `Lotf_GiantSkeletonKing.asset` | same | First LotF authoring |
| `GiantSkeletonKingSpecies.asset` | `Assets/Data/Enemy/` | `speciesId: giant_skeleton_king` |
| `GiantSkeletonKingEnemy.prefab` | `Assets/Prefabs/Actor/Enemy/` | Clone/variant of `GiantSkeletonEnemy`; **red tint** |
| `EnemyLootTable_GiantSkeletonKing.asset` | `Assets/Data/Enemy/Loot/` | §5.5 |
| `EnemySpawnDefinition` (LotF) | `Assets/Data/Spawn/` | Random 2×2 Floor 1 placement |
| Mist of the Abyss presence effect | See essence doc | Reusable floor-wide active-essence suppress + visuals (**not** droppable) |
| Map presence profile | `Assets/Data/Enemy/MapPresence/` | v0: King hosts Mist; future LotFs may reuse same effect |

**Code (suggested):**

| Component | Responsibility |
|-----------|----------------|
| `LordOfTheFloorService` | Day-start evaluate, ledger, spawn orchestration |
| `LordOfTheFloorRunLedger` | Per-run slot state (`Available` / `Summoned` / `Consumed`) |
| `ILotfSummonGate` / gate assets | Composable conditions (day, floor, party size) |

---

## 7. Acceptance criteria — Giant Skeleton King

| ID | Criterion |
|----|-----------|
| **AC1** | On **day 3+** start, with **≥ 4 living** members, party on **Floor 1**, and unused run slot, King **spawns once** at a valid **2×2** cell. |
| **AC2** | On **day 2** start with otherwise valid party/floor, King **does not** spawn. |
| **AC3** | With **3** living members (even if more are dead on the roster) at day 3+ start on Floor 1, King **does not** spawn; later day with **4+ living** still can. |
| **AC3b** | With **4 living + N dead** on the roster at day 3+ start on Floor 1, King **does** spawn. |
| **AC4** | If party is on Floor 2 at day 3+ start, King **does not** spawn on Floor 1 (or anywhere). |
| **AC5** | After King is **slain**, further day starts **never** summon him again in that run. |
| **AC6** | If King is **despawned** without a kill (test harness / future rule), further day starts **never** summon him again. |
| **AC7** | Defeat drops **exactly one** tier-**8** mana stone at **100%** (no ordinary giant 3+1 table); first kill awards **100 XP**. |
| **AC8** | While King lives on Floor 1, Mist of the Abyss blocks **essence actives** per essence doc; item actives, essence passives/stats, mage magic, and priest divine abilities still function. |
| **AC9** | Leaving Floor 1 clears mist for the party; returning to Floor 1 while King still lives **re-applies** mist. |
| **AC10** | Logs include `[LotF]` gate failures with reason (day / party / floor / slot). |
| **AC11** | On successful summon, combat log announces the Lord of the Floor has appeared (name + title) and **does not** include spawn location or coordinates. |
| **AC12** | King uses the Giant Skeleton sprite with a **visible red tint**; ordinary Giant Skeleton does not. |

---

## 8. Resolved decisions

| ID | Decision |
|----|----------|
| **R1** | First-kill XP for `giant_skeleton_king` is **100** (may change later on the asset). |
| **R2** | Successful summon writes a **combat log** appearance line with name + title; **no** spawn location in that message. |
| **R3** | Party-size gate = **≥ 4 living** roster members. Dead members are ignored. Party splitting across floors is **deferred**. |
| **R4** | Mist of the Abyss is **not** droppable or capturable; implemented as a **reusable** LotF presence effect (first host: King). |
| **R5** | King shares the Giant Skeleton sprite with a **red tint** in v0; unique sprite later. Ordinary / schedule giants stay visually baseline. |

## 9. Open questions

| ID | Question | Default if unresolved |
|----|----------|------------------------|
| **Q1** | Exact combat-log sentence template (punctuation / “Lord of the Floor” vs title only)? | Example in §5.3.1 |
| **Q2** | Exact red tint color / multiplier on the sprite renderer? | Clearly readable red vs untinted giant; tune in playtest |
| **Q3** | Show title in examine / journal beyond the appearance log? | Examine minimum; journal optional |

---

## 10. Playtest checklist

1. Enter production dungeon with **4+ living** members; advance to **day 3** while remaining on Floor 1 → King appears once; combat log announces appearance; King is **red-tinted**.  
2. Roster with dead members but still **≥ 4 living** → summon still allowed when other gates pass.  
3. Same, but leave to Floor 2 before day 3 start → no King; return to Floor 1 and hit a later day start with 4+ living → King appears.  
4. Kill King → confirm **one** tier-8 stone and **100** first-kill XP; advance days → no second King.  
5. With King alive: attempt Sudden Strength / Telekinesis (or any essence active) on Floor 1 → blocked; Helmet of Light active → works; Mage spell / Priest divine → works.  
6. Descend to Floor 2 with King still alive → essence actives work again; ascend → blocked again.  
7. Kill King → essence actives work on Floor 1 again.

---

## 11. Revision history

| Date | Change |
|------|--------|
| 2026-07-24 | Initial draft — LotF framework + Giant Skeleton King (Lord of Giant Skeletons); summon gates; once-per-run; loot; link to Mist of the Abyss |
| 2026-07-24 | Lock XP **100**; combat-log appearance; living-only party size (≥4); Mist not capturable/droppable; **red tint** on shared Giant Skeleton sprite |
| 2026-07-24 | Mist visuals (badge + vignette) + reapply rules; Mist marked reusable for future LotFs |
| 2026-07-24 | Appearance combat log must **not** reveal spawn location |
