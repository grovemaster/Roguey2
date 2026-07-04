# Barbarian — Holy Land (requirements)

**Purpose:** Specify the **Barbarian-only Holy Land** town district — a racial sanctuary inspired by *Surviving the Game as a Barbarian* (StGaAB) where only Barbarian party members may enter while the rest of the party waits behind. Covers **party split / reunify**, **new hub floors** south of `DimensionSquareTest`, **decagon nexus layout**, **dirt ground tiles**, and **chief + shaman tent** content for the first Barbarian town vertical slice.

**Status:** Implemented (v0).

**Depends on:** [Town hub multi-floor](../World/Town-Hub-Multi-Floor-Requirements.md), [Town building entry & exit](../World/Town-Building-Entry-And-Exit-Requirements.md), [Barbarian Spirit Imprint — Shaman NPC](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md), [NPC dialog](../World/NPC-Dialog-Requirements.md), [Safe zones](../World/Safe-Zone-Requirements.md), [Party control HUD](../UI/Party-Control-HUD-Requirements.md), [Subspace inventory & encumbrance](../Inventory/Subspace-Inventory-And-Encumbrance-Requirements.md), [Ability hotbar — party transfer §11](../UI/Ability-Hotbar-Requirements.md), `DungeonFloorInstanceManager`, `PartyManager`, `FormationRushService`, `PlayerCommandProcessor.ProcessFollowerRush`, `DimensionSquareLayout`, `Race.Barbarian`.

**Related:** [Phase 3 — Barbarian Spirit Imprint](Phase3-Requirements.md), [Dwarf — Hall of Ancestors](Dwarf-Clan-And-Hall-Of-Ancestors-Requirements.md) (racial-only town district pattern).

**Explicitly out of scope (v0):** Barbarian **quest board**, **training dummies**, **clan politics**, **PvP**, **combat** inside the Holy Land; **respec** imprint at the shaman; **chief** progression services (static flavor only); custom **building facade art** (reuse town building tiles — art pass later); **Fairy** or other races’ holy districts; save/load edge cases beyond existing party persistence; **fade curtain** polish beyond existing building transition pattern (optional v0).

---

## Locked decisions (v0)

| # | Decision |
|---|----------|
| **L1** | **Barbarian-only admission.** Crossing the Holy Land portal requires `CharacterStats.race == Race.Barbarian`. Non-Barbarians **cannot** enter the Holy Land proper floor. |
| **L2** | **Party does not enter as a unit.** When one or more Barbarians enter, **all non-Barbarian members stay behind** at the **nexus** (parked / de-loaded from the active floor). They **rejoin** when every Barbarian on the Holy Land floor has exited back to the nexus. |
| **L3** | **Multiple Barbarians.** If the party has **two or more** Barbarians, **all Barbarians** may enter together on the same transition. Non-Barbarians still stay behind. |
| **L4** | **Reunify on return to town proper.** Exiting the nexus back to `dimension_square` (Dimension Square) **respawns the full party together** at the south arrival anchor — same visual as today’s whole-party portal transitions. |
| **L5** | **Inventory QoL across split.** While Barbarian(s) are in the Holy Land, the player may still **swap / transfer inventory** between **any party members** (including parked members) via the Inventory UI — out of combat, same rules as [Ability hotbar §11](../UI/Ability-Hotbar-Requirements.md). |
| **L6** | **Three floors, one scene.** `HolyLandNexusTest` and `barbarian_holy_land` are **additional floor instances** in the existing **`DimensionSquareTest`** scene catalog (same pattern as market / building interiors). |
| **L7** | **South link from Dimension Square.** The **south arm** of the dimension plus opens to the nexus (mirrors the **north** strip to market). |
| **L8** | **Nexus shape.** `HolyLandNexusTest` is a **decagon** walkable district inside a **40×40** bounding box. |
| **L9** | **Nexus exits.** **North** (center of top edge) → `dimension_square`. **West of north** (portal cell immediately **west** of the north exit on the decagon rim) → `barbarian_holy_land`. |
| **L10** | **Holy Land ground.** Holy Land proper uses **DCSS grey dirt** floor tiles — **first town floor that is not stone**. **Walls** reuse the **same stone wall tiles** as Dimension Square / town hub (v0). |
| **L11** | **Building tiles (temporary).** Shaman **tent exterior and interior** reuse **existing town building floor/wall tiles** until a dedicated art pass. |
| **L12** | **Shaman tent.** **8×8** interior, **one south-facing entrance** on the exterior shell, **south-facing exit** inside linking back to Holy Land proper. Shaman NPC is **`shaman_barbarian`** per [Shaman NPC doc](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md). |
| **L13** | **Chief NPC (v0).** One **chief** NPC in the open Holy Land with **static dialog only** — no shop, quest, or progression services. |
| **L14** | **Safe zone.** Nexus + Holy Land proper + tent interior are **town safe zones** (no combat, no turn cost for talk / inventory / portals). |
| **L15** | **Party Control HUD (top rail).** While the active floor is **`barbarian_holy_land`** or **`barbarian_shaman_tent_interior`**, the [Party control HUD](../UI/Party-Control-HUD-Requirements.md) portrait rail lists **only Barbarian** party members as **selectable** control targets — no other party members appear as selectable (prefer **omit** non-Barbarians from the rail entirely). If the party has **multiple Barbarians**, **each** Barbarian remains selectable; the player cycles among Barbarians only. **Does not** apply to the **Inventory** party strip (§8.3) or to nexus / Dimension Square. |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **StGaAB fidelity** — Barbarians get a **private racial sanctuary**; other races wait outside. |
| **G2** | **Clear geography** — Player discovers the Holy Land by walking **south** from Dimension Square into a **decagon nexus**, then through a **west-of-north** gate into the proper. |
| **G3** | **Party split without data loss** — Parked members keep inventory, XP, imprint state; only **presence on the active floor** changes. |
| **G4** | **Formation correctness** — `ProcessFollowerRush()` / `FormationRushService` operate only on **present** members; parked members do not break breadcrumbs or grid occupancy. |
| **G5** | **QoL inventory** — Barbarian can re-equip / swap with parked allies without leaving the Holy Land. |
| **G6** | **Reunify clarity** — Leaving nexus → Dimension Square shows **everyone together** again. |
| **G7** | **Shaman home** — Spirit Imprint upgrades happen **inside the tent** (relocate or duplicate authored shaman per implementation — see §8.3). |
| **G8** | **Visual milestone** — First hub district with **dirt** flooring; establishes pattern for future racial grounds (elf groves, etc.). |
| **G9** | **Data-driven floors** — Layout constants + catalog assets; editor menu to stamp / rebuild test scene (parity with `DimensionSquareSceneCreator`). |
| **G10** | **Focused control HUD** — While inside the Holy Land proper (outdoor + tent), the top rail offers **Barbarian-only** member selection so the player cannot attempt to control parked allies. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Holy Land (proper)** | Barbarian-only outdoor floor: chief, tent exterior, dirt ground. Floor id **`barbarian_holy_land`**. |
| **Holy Land nexus** | Neutral **decagon** foyer south of Dimension Square. All races may stand here; gates the racial portal. Floor id **`holy_land_nexus`**. |
| **Dimension Square** | Existing **`dimension_square`** hub floor (`DimensionSquareTest` scene). |
| **Present member** | Party member **spawned and active** on the current floor (`GameObject` active, registered on grid when applicable). |
| **Parked member** | Party member **not on the active floor** — hidden / de-registered while allies visit the Holy Land. Still in `PartyManager.partyMembers`. |
| **Admission gate** | Step-on or interact portal from nexus → Holy Land proper; filters by `Race.Barbarian`. |
| **Rejoin** | Transition that **respawns parked members** next to returning Barbarian(s) on the nexus (or full party on Dimension Square). |
| **Chief** | Flavor NPC (`chief_barbarian` proposed id) — static lines only in v0. |

---

## 3. Reference — StGaAB (behavioral intent)

| StGaAB behavior | JRogue v0 mapping |
|-----------------|-------------------|
| Only Barbarians enter the Holy Land | **L1**, admission gate on nexus → proper |
| Party waits outside | **L2** — non-Barbarians **parked** at nexus |
| Multiple Barbarians in party | **L3** — all enter together |
| Manage gear while split | **L5** — Inventory UI **Give** / transfers reach parked members |
| Return to town together | **L4** — nexus → `dimension_square` uses **whole-party** spawn |

---

## 4. Current baseline (gap)

| Area | Today | Gap |
|------|-------|-----|
| **Portal transitions** | `TryTransitionPortalForWholeParty` only — always moves **entire** party | Need **partial-party** transition + **park / unpark** |
| **Party list** | `PartyManager.partyMembers` is a flat list; all members assumed co-located | Need **presence filter** for rush, grid; **Party Control HUD** Barbarian-only on Holy Land floors (§8.4); Inventory strip still shows all members (§8.3) |
| **Formation rush** | `FormationRushService.Rush` iterates **all** `partyMembers` | Must **skip parked** members; trim / realign `positionHistory` to present count |
| **Dimension Square south** | South border is **wall** (no exit) | Add south strip portal → nexus |
| **Inventory Give** | [Hotbar §11](../UI/Ability-Hotbar-Requirements.md) — **stub** | Holy Land QoL **depends** on Give (or equivalent) working **cross-presence** |
| **Shaman NPC** | Implemented elsewhere in town (test placement) | **Relocate** to Holy Land tent interior for this slice |
| **Town floor art** | Stone floor variants only | Introduce **DCSS grey dirt** tile set for Holy Land proper |

---

## 5. Floor topology

```text
                    [ town_market ]
                          ↑ north strip
              ┌───────────────────────┐
              │   dimension_square    │  (40×40 plus)
              │         ⊕             │
              └───────────┬───────────┘
                          ↓ south strip (NEW)
              ┌───────────────────────┐
              │  holy_land_nexus      │  (40×40 decagon)
              │    ╱ north → square   │
              │   ╱  NW → holy land   │
              └───────────────────────┘
                          ↓ NW gate (Barbarians only)
              ┌───────────────────────┐
              │ barbarian_holy_land   │  (dirt outdoor)
              │  Chief · Tent (8×8)   │
              └───────────────────────┘
```

All three hub floors live under **`DimensionSquareTest`** (or successor hub scene) via `DungeonFloorInstanceManager.floorDefinitions` + `DimensionSquareCatalog` (or extended catalog asset).

---

## 6. Layout specifications

### 6.1 — Dimension Square → Nexus (south strip)

Mirror [DistrictSquareMarketTransition](../Assets/Scripts/World/Town/DistrictSquareMarketTransition.cs) pattern:

| Constant | Proposed value |
|----------|----------------|
| `SquareSouthEdgeY` | `0` |
| `StripMinX` / `StripMaxX` | `DimensionSquareLayout.ArmMin` … `ArmMax` (10 … 29) |
| `SquareArrivalCell` (from nexus north) | `(20, 1, 0)` |
| `NexusArrivalCell` (from square south) | `(20, 38, 0)` |
| Link ids | `district_square_to_holy_nexus` / `district_holy_nexus_to_square` |

**Layout change:** `DimensionSquareLayout.Paint` must treat south-edge strip cells like north market cells — **no wall** on the transition row.

### 6.2 — Holy Land Nexus (`holy_land_nexus`) — decagon, 40×40

| Property | Value |
|----------|-------|
| **Bounding size** | 40 × 40 (`MapSize = 40`) |
| **Walkable shape** | **Regular decagon** inscribed in the map (10-sided polygon). Non-walkable exterior = stone walls. |
| **Floor tiles** | Same **stone floor** variants as Dimension Square (nexus is neutral stone — not dirt). |
| **Wall tiles** | Same as Dimension Square hub. |

**Decagon construction (implementer note):** Center at `(20, 20)`. For each cell `(x, y)`, classify inside decagon using polar angle / edge half-planes from center (or authored bitmask). Border cells outside decagon → wall. Keep **north** and **north-west** rim openings for portals (see below).

**Exits on decagon rim:**

| Exit | Direction | Target floor | Portal link id (proposed) | Arrival anchor (proposed) |
|------|-----------|--------------|---------------------------|---------------------------|
| **North** | Center of top edge | `dimension_square` | `district_holy_nexus_to_square` | Square south strip (§6.1) |
| **Holy Land gate** | **West of north exit** — rim cell immediately **west** of north portal (e.g. `(19, 39)` if north portal is `(20, 39)`) | `barbarian_holy_land` | `holy_nexus_to_barbarian_holy_land` | Holy Land south-east approach (§6.3) |
| **Return from Holy Land** | Paired exit on Holy Land floor | `holy_land_nexus` | `barbarian_holy_land_to_nexus` | Nexus gate cell |

**Admission:** Holy Land gate uses **partial-party transition** (§7) — Barbarians only.

### 6.3 — Barbarian Holy Land proper (`barbarian_holy_land`)

| Property | Value |
|----------|-------|
| **Size** | ≥ 32×32 walkable outdoor (author in 40×40 instance if simpler) |
| **Ground** | **DCSS grey dirt** — use existing cavern tile assets under `Assets/TileMaps/Dcss/Cavern/grey_dirt_*` (sprites: `Assets/Sprites/DCSS/.../dungeon/floor/grey_dirt_*`). **First non-stone town floor.** |
| **Walls** | Perimeter + tent shell — **same stone wall tile** as hub. |
| **Return portal** | South or south-east rim → nexus (paired link above). |

**Placed content (v0):**

| Entity | Placement | Notes |
|--------|-----------|-------|
| **Chief** | Open dirt area, near camp center | `NpcController` id **`chief_barbarian`**. **Static dialog** — 1–3 flavor lines, no choices, no services. |
| **Shaman tent (exterior)** | Building footprint on dirt | South wall has **door / entrance** (1 tile wide). Reuse **town building** wall/floor tiles (temporary). |
| **Shaman tent (interior)** | Separate floor instance **`barbarian_shaman_tent_interior`** | **8×8** inner floor. **Enter** via south door. **Exit** tile on **south** inner edge → back to Holy Land proper. |

**Tent interior layout (8×8):**

```text
  N
  ┌────────┐
W │        │ E
  │ Shaman │
  │   @    │
  └───▓────┘  ← exit (south center)
  S   door
```

| Interior constant | Value |
|-------------------|-------|
| Inner size | 8 × 8 walkable |
| Entrance (exterior) | South face, centered |
| Exit (interior) | South row, center cell — `PortalInteractable` → `barbarian_holy_land` |
| Shaman spawn | Interior center or north half — faces south toward entrance |

### 6.4 — Portal link summary

| Link id | Source floor | Target floor | Transition type |
|---------|--------------|--------------|-----------------|
| `district_square_to_holy_nexus` | `dimension_square` | `holy_land_nexus` | Whole party |
| `district_holy_nexus_to_square` | `holy_land_nexus` | `dimension_square` | Whole party (**reunify**) |
| `holy_nexus_to_barbarian_holy_land` | `holy_land_nexus` | `barbarian_holy_land` | **Barbarians only** (partial) |
| `barbarian_holy_land_to_nexus` | `barbarian_holy_land` | `holy_land_nexus` | **Barbarians only** (partial; **rejoin** parked) |
| `barbarian_tent_enter` | `barbarian_holy_land` | `barbarian_shaman_tent_interior` | Whole **present** party on Holy Land (Barbarians only on floor anyway) |
| `barbarian_tent_exit` | `barbarian_shaman_tent_interior` | `barbarian_holy_land` | Whole **present** party |

---

## 7. Party admission, split, and reunify

### 7.1 — Whole-party vs partial-party transitions

| Transition | Members moved | Members parked |
|------------|---------------|----------------|
| Square ↔ Nexus | **All** | None |
| Nexus → Holy Land | **All Barbarians** in `partyMembers` | **All non-Barbarians** |
| Holy Land → Nexus | **All Barbarians** on Holy Land floor | Unpark non-Barbarians at nexus anchor |
| Nexus → Square | **All** (reunify) | None |

### 7.2 — Park / unpark rules

| Rule | Detail |
|------|--------|
| **Park** | On partial transition **into** Holy Land: for each non-Barbarian — `grid.UnregisterActor`, disable `GameObject` (or move to hidden `PartyParkRoot`), exclude from `FormationRushService` and turn presentation. |
| **Persist** | Parked members remain in `partyMembers`; inventory, stats, imprint, quest flags **unchanged**. |
| **Unpark** | When **last** Barbarian leaves Holy Land → nexus: respawn parked members at **formation offsets** from nexus gate anchor (reuse `PartySpawnService` / formation profile). |
| **Reunify** | Nexus → Square: **always** whole-party spawn at square south arrival — parked list empty; all members active. |
| **Active member** | If active leader is **parked**, auto-switch control to a **present** Barbarian before Holy Land transition. If no Barbarian in party, Holy Land gate shows rejection (§7.4). |

### 7.3 — Multiple Barbarians

When **two Barbarians** enter:

- Both transition to Holy Land anchor cells (formation spawn).
- Non-Barbarians park once (idempotent).
- Either Barbarian may exit; **unpark** triggers when **no** Barbarian remains on `barbarian_holy_land` (count present Barbarians on floor == 0).

**Edge case:** One Barbarian dead / removed from party — only living Barbarians count for admission.

### 7.4 — Rejection UX (non-Barbarian at Holy Land gate)

| Condition | Feedback |
|-----------|----------|
| No Barbarian in party | Message log: **“Only Barbarians may enter the Holy Land.”** No transition. |
| Barbarian present but **active leader** is non-Barbarian | Auto-switch to nearest Barbarian **or** prompt once (implementer choice — prefer auto-switch for frictionless play). |
| Non-Barbarian tries to follow through gate | Blocked; leader must be Barbarian at gate tile. |

### 7.5 — Proposed service surface

New **`PartyFloorPresenceService`** (name tentative):

```csharp
// Pseudocode — not implemented
void ParkMembers(IReadOnlyList<BaseActor> members, string waitFloorId, Vector3Int waitAnchor);
void UnparkMembers(IReadOnlyList<BaseActor> members, Vector3Int spawnAnchor, FormationProfile profile);
IReadOnlyList<BaseActor> GetPresentMembers();
IReadOnlyList<BaseActor> GetParkedMembers();
bool IsParked(BaseActor member);
```

`DungeonFloorInstanceManager` gains:

- `TryTransitionPortalForPresentParty(linkId, targetFloorId)` — whole **present** subset.
- `TryTransitionHolyLandAdmission(linkId, targetFloorId)` — Barbarian filter + park/unpark orchestration.

---

## 8. NPCs & services

### 8.1 — Chief (`chief_barbarian`)

| Field | v0 |
|-------|-----|
| **Id** | `chief_barbarian` |
| **Display name** | **Chief** (placeholder) |
| **Dialog** | Static opener only — e.g. **“Welcome, child of the wild. The camp honors your strength.”** No choices. |
| **Race gate** | None (any **present** member may talk; non-Barbarians should not reach this floor in v0). |
| **Services** | None |

### 8.2 — Shaman (`shaman_barbarian`)

Relocate (or re-author) per [Barbarian Spirit Imprint — Shaman NPC](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md):

| Field | v0 |
|-------|-----|
| **Location** | **Inside** `barbarian_shaman_tent_interior` |
| **Mechanics** | Unchanged — Barbarian speaker, imprint upgrade dialog, costs, forward-only path |
| **Placement note** | Remove duplicate shaman from old town test slot when Holy Land ships |

### 8.3 — Inventory & encumbrance (QoL)

While Barbarian(s) are in Holy Land and allies are **parked** at nexus:

| ID | Requirement |
|----|-------------|
| **I1** | Inventory UI **party strip** lists **all** members (parked shown with **“Waiting at nexus”** or dimmed portrait). |
| **I2** | **Give** / transfer ([Hotbar §11 T1–T8](../UI/Ability-Hotbar-Requirements.md)) works between **any** pair of party members regardless of floor presence. |
| **I3** | Out of combat only; encumbrance rules unchanged. |
| **I4** | No turn cost for transfers (consistent with [Subspace §G7](../Inventory/Subspace-Inventory-And-Encumbrance-Requirements.md)). |

**Dependency:** Implement **Give** (or minimal cross-member transfer) **before or with** Holy Land v0 — otherwise **L5** is unmet.

### 8.4 — Party Control HUD (top rail)

Distinct from §8.3 Inventory UI: the **top playfield party rail** (`PartyControlHudUI`) governs **which member the player controls** on the map.

| ID | Requirement |
|----|-------------|
| **H1** | When active floor is **`barbarian_holy_land`** or **`barbarian_shaman_tent_interior`**, the rail shows **only** party members with `Race.Barbarian`. |
| **H2** | Non-Barbarian members are **not selectable** — implement by **omitting** their portraits from the rail (preferred) or rendering them **disabled** with no click / hotkey response. |
| **H3** | **Multiple Barbarians:** every Barbarian in the party appears on the rail and is selectable; `CycleActiveMember` and portrait clicks rotate **among Barbarians only**. |
| **H4** | On transition **into** Holy Land proper, if the active member is non-Barbarian, **auto-switch** to a Barbarian before the floor activates (same rule as §7.2). |
| **H5** | On transition **out** to nexus (or any non–Holy Land floor), restore the **full** party rail (all members selectable per normal [Party control HUD](../UI/Party-Control-HUD-Requirements.md) rules). |
| **H6** | Map highlight ring (`PartyMemberMapHighlight`) follows the active Barbarian only — never a parked ally. |

**Explicit non-goal:** Restricting Inventory UI (§8.3) — parked allies remain reachable there for gear swaps.

---

## 9. Formation rush & `ProcessFollowerRush()` implications

Today `PlayerCommandProcessor` calls `FormationRushService.Rush(party, …)` which loops **every** entry in `partyMembers`. With parked members this **must** change.

### 9.1 — Required behavior

| ID | Requirement |
|----|-------------|
| **F1** | `FormationRushService.Rush` iterates **present members only** (`PartyFloorPresenceService.GetPresentMembers()`). |
| **F2** | Parked members are **not** unregistered/re-registered during rush on another floor. |
| **F3** | `PartyManager.positionHistory` length matches **present** member count during split; on reunify, **rebuild** history from actual positions for full party. |
| **F4** | `PartyManager.GetActiveMember()` must never return a **parked** actor. On Holy Land floors, `CycleActiveMember` / portrait swap considers **Barbarians only** (§8.4 **H1–H3**). |
| **F5** | `TurnManager` player turn / `CanActorTakeAction` — only **present** members consume turns. |
| **F6** | Single Barbarian alone on Holy Land: rush becomes **no-op** (no followers) — leader move completes turn normally. |

### 9.2 — Control flow (split party)

```text
Leader (Barbarian) moves on holy_land
  → RecordMoveHistory (present indices only)
  → ProcessFollowerRush
       → Rush skips parked members
       → ReconcilePartyOnGrid (present only)
  → ForceEndPlayerTurn (present only)
```

### 9.3 — Tests

| Test | Assert |
|------|--------|
| Park on enter | Non-Barbarian inactive, not on grid, still in party list |
| Rush during split | No null refs; parked positions unchanged |
| Unpark on exit | All members at nexus; grid registered |
| Square reunify | Full formation at south anchor |
| Two Barbarians | Both enter; one exits — unpark only when both out |

---

## 10. Art & tiles

### 10.1 — Holy Land proper (dirt)

| Asset | Path (existing) |
|-------|-----------------|
| Grey dirt floor variants | `Assets/TileMaps/Dcss/Cavern/grey_dirt_*_new.asset`, `grey_dirt_b_*.asset` |
| Source sprites | `Assets/Sprites/DCSS/Dungeon Crawl Stone Soup Full/dungeon/floor/grey_dirt_*.png` |

**New catalog entry:** `HolyLandFloorTiles` (or extend town catalog) referencing 8–16 dirt variants for visual variety (hash pick like `DimensionSquareLayout.PickFloorTile`).

### 10.2 — Walls & buildings (v0 temporary)

| Surface | Tile source |
|---------|-------------|
| Hub / nexus / Holy Land walls | Same `TileBase` as `DimensionSquareCatalog` stone walls |
| Nexus / square floors | Existing stone floor pool |
| Tent exterior / interior | **Same building tiles** as current town interiors (e.g. Mira shop pattern) — **replace in later art pass** |

### 10.3 — NPC sprites

| NPC | Art note |
|-----|----------|
| Chief | New placeholder sprite or reuse Barbarian NPC doll — **must not** duplicate shaman portrait |
| Shaman | Existing `NPC_ShamanBarbarian` / `Portrait_ShamanBarbarian` per shaman doc |

---

## 11. Scene & asset checklist

| Asset / type | Proposed id / path |
|--------------|-------------------|
| Scene | `Assets/Scenes/Town/DimensionSquareTest.unity` (extend) |
| Floor definition | `Assets/Resources/Town/Floors/holy_land_nexus.asset` |
| Floor definition | `Assets/Resources/Town/Floors/barbarian_holy_land.asset` |
| Floor definition | `Assets/Resources/Town/Floors/barbarian_shaman_tent_interior.asset` |
| Layout helper | `Assets/Scripts/World/Town/HolyLandNexusLayout.cs` |
| Layout helper | `Assets/Scripts/World/Town/BarbarianHolyLandLayout.cs` |
| Transition constants | `Assets/Scripts/World/Town/DistrictSquareHolyNexusTransition.cs` |
| Editor menu | `Assets/Editor/World/HolyLandSceneCreator.cs` — **JRogue → Town → Create / Update Holy Land Floors** |
| Catalog | Extend `DimensionSquareCatalog` or `HolyLandCatalog.asset` |
| NPC profiles | `Assets/Resources/NPC/chief_barbarian.asset` (new) |

---

## 12. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **A1** | Walk **south** from Dimension Square into **decagon nexus** — whole party. |
| **A2** | Walk **north** from nexus — whole party returns to square **together**. |
| **A3** | Barbarian walks **Holy Land gate** (west of north exit) — **only Barbarians** transition; others **parked**. |
| **A4** | Two-Barbarian party — **both** enter; one exits — allies still parked until **both** out. |
| **A5** | Exit Holy Land to nexus — **parked members visible** again in formation. |
| **A6** | Nexus → square — **full party** visible at south strip. |
| **A7** | Holy Land ground renders **dirt**; walls remain **stone**. |
| **A8** | Chief shows **static** dialog. |
| **A9** | Tent **8×8** interior; south entrance / south exit; shaman imprint dialog **works**. |
| **A10** | Inventory **Give** works between Barbarian (in Holy Land) and parked ally (nexus). |
| **A11** | Formation move + rush on Holy Land with parked allies — **no errors**, no ghost followers. |
| **A12** | Non-Barbarian party cannot enter Holy Land gate (message + block). |
| **A13** | On Holy Land proper + tent: top HUD shows **Barbarian(s) only**; clicks / cycle cannot select parked or non-Barbarian members. |

---

## 13. Implementation phases (suggested)

| Phase | Deliverable |
|-------|-------------|
| **P0 — Layout** | `HolyLandNexusLayout` decagon paint + south strip on Dimension Square + portal markers |
| **P1 — Presence** | `PartyFloorPresenceService` + partial portal API on `DungeonFloorInstanceManager` |
| **P2 — Holy Land proper** | Dirt tiles, chief, tent exterior/interior floors, shaman relocate |
| **P3 — Formation** | Rush / turn / history fixes + unit tests (§9.3) |
| **P4 — Inventory QoL** | Give / transfer with parked members (if not already shipped) |
| **P5 — Polish** | Party Control HUD Barbarian-only rail (§8.4), optional building fade, editor rebuild menu |

---

## 14. Open questions

| # | Question | Default if unresolved |
|---|----------|------------------------|
| **Q1** | Exact decagon rim cells for north vs NW portals — hand-tune in editor? | Yes — constants in `HolyLandNexusLayout` with gizmo debug |
| **Q2** | Auto-switch active member to Barbarian at gate vs modal prompt? | Auto-switch |
| **Q3** | Chief dialog lines — final copy? | Placeholder §8.1 |
| **Q4** | Tent building art pass timeline? | Reuse town tiles until dedicated pass |
| **Q5** | Should nexus show a visible **“Holy Land”** sign / arch prop? | Optional v0 — portal tile sufficient |

---

## 15. Cross-references (implementation)

| System | File / doc |
|--------|------------|
| Whole-party portal | `DungeonFloorInstanceManager.TryTransitionPortalForWholeParty` |
| Formation rush | `FormationRushService.Rush`, `PlayerCommandProcessor.ProcessFollowerRush` |
| Square layout | `DimensionSquareLayout`, `DistrictSquareMarketTransition` |
| Shaman upgrade | [Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) |
| Building enter/exit | [Town-Building-Entry-And-Exit-Requirements.md](../World/Town-Building-Entry-And-Exit-Requirements.md) |
| Party transfer | [Ability-Hotbar-Requirements.md §11](../UI/Ability-Hotbar-Requirements.md) |
