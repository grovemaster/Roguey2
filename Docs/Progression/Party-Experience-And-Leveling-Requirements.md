# Party experience & leveling — Requirements

Party members gain **experience (XP)** and **levels** using a model inspired by *Surviving the Game as a Barbarian* (**STBGB**): XP is awarded for the **first kill of each enemy species** only; repeat kills of the same species grant **no** XP. When one party member earns a kill reward, **every** party member receives the **same** XP amount. Level-ups increase combat stats (including **HP**, **Maximum Soul Power**, and **Constitution**). Some items (e.g. **Potion of Experience**) grant XP to the whole party when consumed.

**Depends on:** `PartyManager`, `CharacterStats`, `HealthComponent`, enemy defeat/death pipeline (`EnemyController` / `BaseActor.Die`), `InventoryManager`, `InventoryCollector`, `WorldItem`, `ItemData`, `InventoryConsumePolicy`, `InventoryUsability`.

**Related:** [Multi-tile enemies](../Combat/Multi-Tile-Enemy-Requirements.md) (`GiantSkeletonEnemy` vs `Skeleton` must be **distinct species** for XP). [Dwarf — Ancestor & common abilities](../RacialSystem/Dwarf-Ancestor-And-Common-Abilities-Requirements.md) (level-gated common slots — future hook). [Undead race](../RacialSystem/Undead-Race-Requirements.md) (potion ban — Undead cannot drink potions including Experience unless design overrides).

---

## 1. Goals

**G1 — Vertical slice (v0)**  
On first kill of an enemy **species**, all party members gain that species’ configured XP. Repeat kills grant zero XP from that species. Party members can level up to **50** with stat growth. A **Potion of Experience** exists as pick-uppable, inventory-only consumable that awards XP to the **whole party**.

**G2 — STBGB-first-kill loop**  
Progression rewards exploration and variety, not grinding the same encounter repeatedly.

**G3 — Party fairness**  
Kill XP is **shared equally** (same integer amount per member per award event). Item-granted XP (Potion of Experience) also applies to **all** members.

**G4 — Data-driven species & items**  
Designers set per-species first-kill XP and per-potion XP in assets without new `MonoBehaviour` types per monster or potion.

**G5 — Potion pickup gate (all potions)**  
A potion (any effect) must be **picked up into a party member’s inventory** before it can be consumed; ground/world potions cannot be “used” in place.

**G6 — Physical deliverables**  
Shipping v0 includes `ItemData` for Potion of Experience, a placeable world pickup, and at least one enemy species wired with a species id + first-kill XP (see §8).

**G7 — Future level hooks**  
Level and XP APIs are stable enough for Dwarf common-ability unlocks, Undead skill points, and UI (character sheet) in later phases.

---

## 2. Reference — STBGB (design intent)

| STBGB idea | This project |
|------------|----------------|
| Personal “monster book”; first kill of a species grants XP | **Party species journal** (§4): first time the **party** defeats a species, award XP once |
| Same species again → no XP | Repeat kills of `Skeleton` after the first party kill → **0** XP |
| Different variants are different entries | `Skeleton` vs `GiantSkeleton` are **different** species ids |
| Level ups improve the survivor | Level up increases **Constitution**, **Max HP**, **Max Soul Power** (§6) |
| Consumables can grant power | **Potion of Experience** grants party XP (§7) |

---

## 3. Glossary

| Term | Definition |
|------|------------|
| **Species** | Stable content id for an enemy type (e.g. `skeleton`, `giant_skeleton`). Not the same as a single prefab instance. |
| **First kill** | First time the **party** records a defeating blow against that species (§4). |
| **Kill credit** | Party member (or game rules) designated as the killer for the defeat event (§5). |
| **Award event** | One grant of XP to all party members (from kill or item). |
| **Party level** | Per-member integer **character level** `1 … maxLevel` (§6). |
| **Max level** | **50** for v0 (tunable constant; design may change later). |

---

## 4. Species journal (first-kill tracking)

### 4.1 — Party-wide journal (v0)

- Persist a set (or dictionary) of species ids the party has **already** received first-kill XP for: `PartySpeciesJournal`.
- On enemy defeat, if `speciesId` is **not** in the journal:
  - Add `speciesId` to the journal.
  - Grant XP (§5).
- If already in the journal: **no** XP from that kill.

**Rationale:** Matches STBGB variety incentive while fitting a JRPG party that shares kill rewards. **Per-member** journals are **out of v0** (future optional mode).

### 4.2 — Species identity on enemies

- Every enemy that can award XP references an **`EnemySpeciesDefinition`** asset (or embedded id on prefab) with:
  - `speciesId` (string or hash, stable across scenes/saves)
  - `displayName` (UI / log)
  - `firstKillExperience` (int ≥ 0)
- **Giant Skeleton** and **Skeleton** use **different** `speciesId` values.
- Enemies without a species definition do not award kill XP (boss placeholders, summons, etc.).

### 4.3 — Saves

- Journal and each member’s `level` / `currentExperience` serialize with the run/save blob (exact save system TBD; v0 minimum: in-memory + test hooks).

---

## 5. Kill XP — rules

### 5.1 — When XP is evaluated

- Trigger on **enemy death** (hostile `EnemyController` or generic enemy defeat hook), after kill is confirmed.

### 5.2 — Kill credit

- **Killing blow** attribution: the party member who dealt the **last damaging hit** that reduced the enemy to 0 HP (or who triggered `Die()` via bump attack).
- If attribution is ambiguous (trap, environmental): default to **active party leader** at time of death; log in debug.
- **Future:** assists, overkill, party-wide “any member in combat” — not v0.

### 5.3 — Distribution

- On a **first kill** for `speciesId` with `firstKillExperience = X`:
  - Every **living** party member in `PartyManager.partyMembers` receives **`+X`** XP (same integer).
  - Dead members: **v0** — document choice; default **still receive** XP (party wipe recovery fairness).
- **No** split or bonus for killer; amounts are identical.

### 5.4 — Repeat kills

- Second and subsequent kills of the same `speciesId`: **0** kill XP, journal unchanged.

### 5.5 — Logging / UX (v0 minimum)

- Debug log: `"First kill: {displayName} (+{X} XP, party)"` or `"Repeat kill: {displayName} (0 XP)"`.
- **Future:** toast, monster codex UI, XP float text.

---

## 6. Leveling & stat growth

### 6.1 — Per-member progression

Each party member has:

| Field | Type | Notes |
|-------|------|--------|
| `level` | int | `1` at new game; cap **`maxLevel = 50`** (v0 constant). |
| `experience` | int | Current XP toward next level. |

### 6.2 — Level-up threshold

- **`ExperienceCurve`** asset or static table: XP required to advance `level → level + 1`.
- Curve is **data-driven** (designer tunable); v0 may use a simple formula (e.g. linear or polynomial placeholder) with the requirement that **level 50 is reachable** and documented.
- On `experience >= thresholdForNextLevel`:
  - Increment `level` (stop at 50).
  - Subtract threshold (or carry overflow — document in implementation; **carry overflow** recommended).
  - Apply **level-up stat grants** (§6.3).
  - Refresh **current HP** / **current Soul Power** policy (§6.4).

### 6.3 — Stat increases on level-up (v0)

Each level gained applies **permanent** growth to the member (source key e.g. `CharacterLevel:{level}` or batch `CharacterLevel`):

| Stat | v0 rule |
|------|---------|
| **Constitution** | `+constitutionPerLevel` from curve/table (default placeholder: **+1** per level unless content overrides). |
| **Max HP** | Derived from `Constitution` (`MaxHP = Constitution × 10` today); leveling Constitution raises Max HP automatically. |
| **Max Soul Power** | Increase via **`+maxSoulPowerPerLevel`** flat bonus **and/or** increases to Intelligence/Wisdom if design ties soul to attributes — v0: **flat +N Max Soul Power per level** applied as modifier or direct pool resize (pick one in code; document in implementation notes). |

**Minimum requirement:** A level-up **measurably** increases Constitution, effective Max HP, and effective Max Soul Power.

**Other attributes** (Strength, Dexterity, etc.): **out of v0** unless added to `LevelRewardTable` later.

### 6.4 — Current pools on level-up

- **HP:** Increase `currentHP` by the same delta as `MaxHP` (heal the gain), or full heal — **v0 default: heal by MaxHP delta**; do not reduce current HP.
- **Soul Power:** Increase `currentSoulPower` by Max Soul Power delta (same policy).

### 6.5 — At max level (50)

- Further XP awards can be ignored or banked for future paragon — **v0:** grant XP but no level-ups past 50; optional debug log.

---

## 7. Item-granted experience (Potion of Experience)

### 7.1 — Pickup-before-consume (all potions)

**G5 — Global potion rule**

| State | Can consume? |
|-------|----------------|
| On ground (`WorldItem`, not in inventory) | **No** |
| In a party member’s **carried** inventory (`ItemStorageLocation` in bag) | **Yes**, if other policies pass |
| Equipped in a slot | **No** for potions (matches `InventoryUsability`) |

**Implementation:** `Use` / `OnActivate` path rejects potions unless `ItemInstance` is owned by the consuming member’s `InventoryManager` and not on ground. `InventoryUsability.AppearsUsableNow` remains false for ground items (implicit: not in inventory UI).

### 7.2 — Potion of Experience

- **Category:** `ItemCategory.Potion`.
- **Effect:** On successful consume by **one** party member, grant **`experienceAmount`** (int) to **every** party member (same number each), independent of species journal.
- Does **not** add entries to `PartySpeciesJournal`.
- Respects **`InventoryConsumePolicy`** (Undead potion ban applies unless a future racial exception exists).

### 7.3 — Other XP items (future)

- Scrolls, food, quest rewards may call the same **`PartyExperienceService.AwardPartyExperience(int, source)`** API.

---

## 8. Physical deliverables & prefab strategy

### 8.1 — Enemy species (data, not prefab per species)

| Deliverable | Path (suggested) | Notes |
|-------------|------------------|--------|
| `EnemySpeciesDefinition` | `Assets/Data/Enemy/` | ScriptableObject: `speciesId`, `displayName`, `firstKillExperience` |
| Sample: Skeleton | `SkeletonSpecies.asset` | e.g. 50 XP |
| Sample: Giant Skeleton | `GiantSkeletonSpecies.asset` | e.g. 150 XP; distinct id from Skeleton |
| Wire prefabs | `Enemy.prefab`, `GiantSkeletonEnemy.prefab`, etc. | Reference species asset on `EnemyController` or footprint variant |

No requirement for a **separate enemy prefab per species** beyond existing prefab variants; species is **data** on the prefab.

### 8.2 — Potions: ItemData vs prefab (resolved recommendation)

**All potions on the ground occupy exactly one grid tile** (1×1), same as other small world drops. Size does not vary by potion type in v0.

**Do not require a separate Unity prefab per potion type** when tile size, collider, and pickup flow are identical.  
Potions are defined primarily as **`ItemData` ScriptableObjects** (same pattern as `RustySword`, `Heal_Standard` under `Assets/Resources/Item/`). The world object only needs a reference to that data plus the shared `WorldItem` behavior.

| Layer | Required? | Purpose |
|-------|-----------|---------|
| **`ItemData` per potion** | **Yes** | One asset per distinct effect: `PotionOfExperience.asset`, future `PotionOfHealing.asset`, etc. Holds category `Potion`, icon, weight, `ItemEffect` / activate payload. |
| **World pickup prefab** | **Yes (at least one)** | Scene-placed pickup using `WorldItem` + `SpriteRenderer`. |
| **Prefab per potion type** | **No** (v0) | Not needed while every potion is **1×1** on the ground with the same pickup component setup. **Optional later** only if a potion needs a unique world mesh, multi-tile pickup, or special interaction VFX on the drop itself. |

**v0 physical goals**

| Asset | Requirement |
|-------|-------------|
| `PotionOfExperience.asset` | `ItemData`, category Potion, XP amount, icon |
| `GrantExperienceEffect.asset` (or inline) | `ItemEffect` subclass or `ItemData` activate hook calling party XP API |
| `WorldItem_Potion.prefab` | **One** generic world pickup prefab under `Assets/Prefabs/Item/` (or `World/`) with `WorldItem`; designer assigns `PotionOfExperience` (or any potion `ItemData`) in the Inspector |
| Placeholder in test scene | At least one `WorldItem_Potion` instance referencing Potion of Experience |

**Future potions (healing, stat buffs, cure):** Add **`ItemData` only**; reuse `WorldItem_Potion.prefab` or create variants **only** if visuals differ.

```
ItemData (required per potion type)
  PotionOfExperience.asset
  PotionOfHealing.asset      ← future
  PotionOfCurePoison.asset   ← future

WorldItem_Potion.prefab      ← shared pickup shell (v0)
  └── Inspector: data → PotionOfExperience.asset

Optional (only if a future potion breaks 1×1 or needs unique world behavior):
  WorldItem_<Special>.prefab  ← rare exception, not the default pattern
```

### 8.3 — Code / service deliverables

| Component | Role |
|-----------|------|
| `PartyExperienceService` (or `PartyProgressionManager`) | Journal, award XP, level-up, hooks death + items |
| `ExperienceCurve.asset` | Level thresholds 1→50 |
| `LevelRewardTable.asset` | Constitution / soul per level (optional split from curve) |

---

## 9. Integration points

| System | Behavior |
|--------|----------|
| `EnemyController.Die` / damage pipeline | Notify progression with killer + `EnemySpeciesDefinition` |
| `PartyManager` | Enumerate members for awards |
| `CharacterStats` | Store level/XP; apply Constitution modifiers on level-up |
| `HealthComponent` / pools | Apply Max HP changes |
| Inventory **Use** | Potions only from bag; Potion of Experience calls party award |
| `InventoryConsumePolicy` | Undead potion ban blocks Experience potion for Undead |
| UI | v0: logs only; level/XP bar **future** |

---

## 10. Authoring tables (v0 placeholders)

Designers fill real numbers in assets; placeholders prove wiring.

| Species | `speciesId` | First-kill XP (placeholder) |
|---------|-------------|----------------------------|
| Skeleton | `skeleton` | 25 |
| Giant Skeleton | `giant_skeleton` | 100 |

| Item | XP per drink (placeholder) |
|------|----------------------------|
| Potion of Experience | 50 |

| Constant | Value |
|----------|--------|
| `maxLevel` | **50** |
| `constitutionPerLevel` | 1 (tunable) |

---

## 11. Phased delivery

| Phase | Scope |
|-------|--------|
| **v0** | Party journal, kill + potion awards, level 1–50, Constitution/HP/Soul growth, `PotionOfExperience` + `WorldItem_Potion`, sample species on enemies |
| **v1** | Codex UI, XP bar, level-up fanfare, per-level skill points (Undead/Dwarf hooks) |
| **v2** | Per-member journals, paragon past 50, assist XP |

---

## 12. Acceptance criteria

- Given party has **never** killed a Skeleton, first Skeleton death awards **X** XP to **each** member.
- Given party **has** killed a Skeleton, second Skeleton death awards **0** kill XP.
- Given **Giant Skeleton** first kill, awards XP; subsequent Giant Skeletons award **0** (independent of Skeleton journal).
- Given member A gets killing blow, member B (alive in party) receives the **same** XP as A.
- Given member at level 49 with enough XP, level becomes **50** and does not exceed 50.
- Given Potion of Experience **on ground**, **Use** is unavailable / not offered.
- Given potion in member inventory, **Use** grants XP to **all** party members.
- Given **Undead** party member, Potion of Experience **cannot** be consumed (`InventoryConsumePolicy`).
- Given level-up, **Constitution**, **MaxHP**, and **MaxSoulPower** increase vs pre-level snapshot.
- Given `WorldItem_Potion.prefab` + `PotionOfExperience.asset`, designer can place pickup without new prefab type.

---

## 13. Code touchpoints (implementation checklist)

| Area | Action |
|------|--------|
| `EnemySpeciesDefinition` | ScriptableObject + reference on enemies |
| `PartySpeciesJournal` | Serialize species ids |
| `PartyExperienceService` | Award, level-up, death hook |
| `ExperienceCurve` / rewards | Data for thresholds and stat grants |
| `CharacterStats` | `level`, `experience` fields |
| Kill pipeline | Killer attribution on `Die` |
| `ItemEffect` / potion activate | `GrantPartyExperienceEffect` |
| `InventoryUsability` / Use pipeline | Reject ground potions explicitly |
| Assets | `PotionOfExperience.asset`, species assets, `WorldItem_Potion.prefab` |
| Tests | First/repeat kill, party split, potion pickup gate, max level 50 |

---

## 14. Open decisions (non-blocking v0)

| Topic | Default for v0 |
|-------|----------------|
| Dead members receive kill XP? | **Yes** |
| XP curve formula | Simple increasing threshold table in asset |
| Max Soul Power per level | Flat +2 per level (placeholder) until balance pass |
| Bank XP at level 50 | Yes, no level-up |

---

## 15. Related documents

- [Multi-tile enemy requirements](../Combat/Multi-Tile-Enemy-Requirements.md)
- [Undead race requirements](../RacialSystem/Undead-Race-Requirements.md) (potion ban)
- [Dwarf — Ancestor & common abilities](../RacialSystem/Dwarf-Ancestor-And-Common-Abilities-Requirements.md) (future level unlocks)
- [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md)
