# Map interact & offering altars — Requirements

**Adjacent map interact** lets the player trigger features **without bumping into them** (DCSS-style shrine worship), using a dedicated **Interact** control. **Offering altars** are the first feature on this pipeline: they hold **slot-based “altar inventory”**, open **place/remove dialogs** with **data-driven item filters**, and fire **completion events** when the correct offerings are present. v0 ships one concrete altar: **tier 9 + tier 8 mana stones** → spawn **Skeleton**.

**Depends on:** `MapManager`, `GridManager`, `PartyManager`, `PlayerCommandProcessor`, `PlayerCommand` / `InputHandler`, `TurnManager`, `PartyManaStoneLedger` ([mana stones](../Combat/Enemy-Death-Loot-And-Mana-Stones-Requirements.md)), `EnemySpawnService` / `EnemySpawnPlacementResolver` ([conditional spawn](../Combat/Conditional-Enemy-Spawn-Requirements.md)), [Interactable tiles](../Combat/Interactable-Tiles-Requirements.md) (registry/overlay pattern; altars are **not** bump levers), [Inventory UI](../Inventory/Inventory-UI-Redesign-Requirements.md) (modal chrome), [Auto-pickup confirmation](../Inventory/Auto-Pickup-Confirmation-Requirements.md) (escape = no turn).

**Related:** [Door requirements](Door-Requirements.md) (`OpenDoor` / `CloseDoor` keys — separate from Interact).

**Explicitly out of scope (v0):** Save/load altar slot state; multiplayer; altar bump activation; generic carried-item piles on altars (non–mana-stone items use future `AltarItemSlotFilter` types); crafting UI; worship/reputation systems.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Dedicated Interact control** — player can interact with adjacent features without moving into their cell. |
| **G2** | **Multi-target picker** — when several adjacent interactables exist, show a list; pick one or cancel. |
| **G3** | **Extensible altar framework** — slots, filters, altar inventory, multiple completion rules / events for future content. |
| **G4** | **Mana stone pair altar (v0)** — tier 9 + tier 8 mana stones (any source species), place/remove UI, skeleton spawn on completion. |
| **G5** | **Turn discipline** — open/cancel = no turn; place/remove = turn + `ProcessFollowerRush` when formation active. |
| **G6** | **Shipping art** — CC0 stone shrine sprite imported under `Assets/Art/Altars/` (§12). |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Map interactable** | Any cell-anchored feature that responds to the **Interact** command when the player is **orthogonally adjacent** (v0). |
| **Adjacent interact** | Interaction mode: **no bump**, **no occupancy** of the feature cell required. |
| **Offering altar** | Map interactable with **N fixed slots** (altar inventory) and **completion rules**. |
| **Altar slot** | One required offering position with its own **accept filter** and at most **one** stored offering at a time (v0). |
| **Accept filter** | Data rule for which party-held items may be placed in a slot (tier, category, item id, etc.). |
| **Place filter (UI)** | Runtime subset of party holdings shown in the place list; **excludes tiers already filled** on the altar (v0 mana altar). |
| **Completion rule** | Predicate over filled slots; when true, runs **completion effects** once (latched). |
| **Party holdings (mana)** | `PartyManaStoneLedger` stacks `(tier, sourceSpeciesId) → count` — **not** carried `ItemInstance` slots. |

---

## 3. Map interact — framework

### R3.1 — Interaction modes (locked)

| Mode | Examples | How triggered |
|------|----------|----------------|
| **Bump** | Lever switches, some traps | Move into blocked cell |
| **Dedicated key** | Doors | `OpenDoor` / `CloseDoor` |
| **Adjacent interact (new)** | Altars, shrines, NPCs, chests (future) | **`Interact`** action |

Altars **must not** require bump. **`blocksOccupancy = true` (v0)** — actors cannot enter the altar cell (overlay on walkable floor underneath).

### R3.2 — `IAdjacentMapInteractable`

```csharp
public interface IAdjacentMapInteractable
{
    Vector3Int Cell { get; }
    string ListLabel { get; }           // picker row, e.g. "Stone altar"
    int SortOrder { get; }              // stable ordering when multiple
    bool CanInteract(PartyMember actor);
    void OpenInteractUI(PartyMember actor); // or enqueue modal
}
```

| Rule | Detail |
|------|--------|
| **Registry** | `AdjacentMapInteractableService` maps `Vector3Int` → interactable (parallel discipline to `InteractableTileService`). |
| **Registration** | Scene bootstrap / `MapFeatureHost` registers instances at authoring cells. |
| **Query** | `GetOrthogonalAdjacent(playerCell)` returns all interactables on cells **sharing an edge** with `playerCell` where `CanInteract(activeMember)`. |
| **Diagonal** | **No** (v0); 8-way optional later. |

### R3.3 — `PlayerCommandKind.Interact`

| Field | Requirement |
|-------|-------------|
| **Command** | `PlayerCommand.Interact()` → `PlayerCommandKind.Interact`. |
| **Input** | `GameControls` action **`Interact`**, default **`<Keyboard>/e`** (rebindable). Gamepad: **`buttonNorth`** (Y / △) secondary binding optional. |
| **Processor** | `PlayerCommandProcessor.ApplyInteract()` — only when `InputState.Normal` and no blocking modal. |
| **Log tag** | `[MapInteract]` |

**DCSS note:** Stone Soup uses context-specific commands for shrines; this project standardizes on **`E` = Interact** for all adjacent features (altars first).

### R3.4 — Interact flow

```
Player presses Interact
  → activeMemberCell = PartyManager.GetActiveMember().GridPosition
  → candidates = AdjacentMapInteractableService.GetOrthogonalAdjacent(activeMemberCell)
  → filter CanInteract(activeMember)
  → count == 0: optional log "[MapInteract] nothing nearby" ; return false (no turn)
  → count == 1: OpenInteractUI(activeMember, candidate)
  → count >= 2: open AdjacentInteractPickerModal (§4)
```

| Outcome | Turn cost |
|---------|-----------|
| **0 candidates** | **No** turn |
| **Open picker → Escape** | **No** turn |
| **Open picker → choose target** | **No** turn (only opens feature UI) |
| **Open altar UI → Escape** | **No** turn |
| **Place/remove offering** | **Yes** — §7 |

### R3.5 — Coexistence with levers / doors

| Case | Behavior |
|------|----------|
| Cell has **lever** (`blocksOccupancy`) | Bump only; **not** in adjacent-interact list unless also registered as `IAdjacentMapInteractable` (altars: **no**). |
| Cell has **altar overlay** | Adjacent interact only. |
| **Door** on adjacent wall | Still uses **`O`** / **`C`**; may also appear in interact picker if registered (future). |

---

## 4. Multi-target picker — UI mock

When **≥ 2** adjacent interactables, show a **blocking modal** (same overlay family as inventory confirm).

### 4.1 — Mock

```
┌──────────────────────────────────────────────┐
│  INTERACT                                    │
├──────────────────────────────────────────────┤
│  Choose what to interact with:               │
│                                              │
│  ▶ Stone altar                               │
│    Wall torch                                │
│    NPC: Mysterious hermit                    │
│                                              │
│  [ Esc ] Cancel                              │
└──────────────────────────────────────────────┘
```

| Key | Action |
|-----|--------|
| **↑ / ↓** or **number keys** | Change selection |
| **Enter / Space** | Open selected interactable UI |
| **Escape** | Close picker; **no turn** |

### 4.2 — Requirements

| ID | Rule |
|----|------|
| **P1** | List uses each interactable’s `ListLabel`. |
| **P2** | Sort by `SortOrder`, then label. |
| **P3** | Only one modal at a time; picker closes before altar UI opens. |

---

## 5. Offering altar — framework

### R5.1 — Architecture

```
AltarDefinition (ScriptableObject)
  → AltarInstance (runtime: cell, slot contents, completion latch)
  → AltarInteractable : IAdjacentMapInteractable
  → AltarOfferingModal (UI)
  → AltarOfferingService (place/remove, ledger/inventory transfer)
  → AltarCompletionEvaluator
  → AltarCompletionEffect[] (spawn, XP, flags, …)
```

Menu: **`JRogue/World/Altar Definition`**.

### R5.2 — `AltarDefinition`

| Field | Purpose |
|-------|---------|
| `altarId` | Stable string / enum for saves & tests |
| `displayName` | Picker + logs |
| `descriptionTemplate` | Flavor shown in offering UI (supports `{slotHints}`) |
| `slots` | Ordered `AltarSlotDefinition[]` |
| `completionRules` | Ordered rules; **first match wins** (v0: one rule) |
| `sprite` | Overlay sprite (`Altar_StoneShrine` v0) |
| `blocksOccupancy` | **true** (v0) |
| `usedDescriptionTemplate` | Shown after completion; no place/remove (v0) |

### R5.3 — `AltarSlotDefinition`

| Field | Purpose |
|-------|---------|
| `slotId` | Stable id within altar |
| `label` | UI, e.g. `"Tier 9 mana stone"` |
| `acceptFilter` | `AltarSlotAcceptFilter` asset (AND of predicates) |
| `maxCount` | **1** (v0) |

### R5.4 — `AltarSlotAcceptFilter` (extensible)

Abstract ScriptableObject; **all** predicates in list must pass.

| Implementation | Parameters | v0 use |
|----------------|------------|--------|
| **`ManaStoneTierAcceptFilter`** | `tier` (1–9) | Tier 9 slot, tier 8 slot |
| **`AnySourceSpeciesManaStoneFilter`** | — | Any `sourceSpeciesId` (default for v0 slots) |
| **`ManaStoneSourceSpeciesAcceptFilter`** | `speciesId` | Future: “skeleton stones only” |
| **`ItemCategoryAcceptFilter`** | `ItemCategory` | Future: weapons, potions |
| **`ItemDataAcceptFilter`** | `ItemData` reference | Future: exact item |
| **`CompositeAcceptFilter`** | child filters | Designer convenience |

**Restriction enforcement:** Placement API **rejects** items that fail `acceptFilter` even if UI misconfigured.

### R5.5 — Runtime altar inventory

| Type | Role |
|------|------|
| **`AltarSlotState`** | Per slot: empty **or** stored payload |
| **Mana stone payload (v0)** | `tier`, `sourceSpeciesId` (one stone = one stack unit removed from ledger) |
| **Future payload** | `ItemInstance` reference / serialized item id |

**Invariant (v0):** At most **one** mana stone per slot. Placing spends **1** from `PartyManaStoneLedger.TrySpend(tier, species, 1)`. Removing calls `Add(tier, species, 1)`.

### R5.6 — `AltarCompletionRule`

| Field | Purpose |
|-------|---------|
| `ruleId` | Debug / telemetry |
| `requiredSlots` | Each entry: `slotId` + optional exact payload match |
| `effects` | `AltarCompletionEffect[]` run **in order** when rule satisfied |

| Implementation | v0 |
|----------------|-----|
| **`SpawnEnemyAltarCompletionEffect`** | `EnemySpawnDefinition` + origin = altar cell |
| **`GrantPartyExperienceAltarCompletionEffect`** | Future |
| **`SetMapFlagAltarCompletionEffect`** | Future |

**Latch:** After effects run for a rule, set `completionFired = true` for that rule; **repeat offerings do not re-fire** (v0).

**Multiple rules:** Evaluate **top to bottom**; fire **first** unsatisfied→satisfied transition only (future altars may chain different rewards).

### R5.7 — Placement on map

Same discipline as [interactable overlay](../Combat/Interactable-Tiles-Requirements.md):

| Layer | Role |
|-------|------|
| **Floor** | Walkable `floorMap` tile |
| **Registry** | `AdjacentMapInteractableService` + `AltarInstance` |
| **Overlay** | `Altar_Overlay` tilemap or `AltarView` sprite at cell |

---

## 6. Altar offering dialog — UI mock (authoritative)

Opened from **Interact** on the v0 altar (or single candidate). Uses dim overlay + panel (inventory modal family).

### 6.1 — Mock — both slots empty, player has stones

```
┌────────────────────────────────────────────────────────────────────────┐
│  STONE ALTAR                                                    [ Esc ] │
├────────────────────────────────────────────────────────────────────────┤
│  This altar has places for a tier 9 mana stone and a tier 8 mana      │
│  stone.                                                                │
│                                                                        │
│  ON ALTAR                                                              │
│    (empty)                                                             │
│    (empty)                                                             │
│                                                                        │
│  YOUR MANA STONES (tier 9 and tier 8)                                  │
│    ▶ Tier 9 · skeleton × 2                                             │
│      Tier 9 · giant_skeleton × 1                                     │
│      Tier 8 · skeleton × 3                                             │
│      Tier 8 · orc × 1                                                  │
│                                                                        │
│  [ Enter ] Place selected stone on altar    [ R ] Remove from altar   │
│  (Remove disabled when altar empty)                                    │
└────────────────────────────────────────────────────────────────────────┘
```

### 6.2 — Mock — tier 9 already on altar (tier 9 hidden from list)

```
┌────────────────────────────────────────────────────────────────────────┐
│  STONE ALTAR                                                    [ Esc ] │
├────────────────────────────────────────────────────────────────────────┤
│  This altar has places for a tier 9 mana stone and a tier 8 mana      │
│  stone.                                                                │
│                                                                        │
│  ON ALTAR                                                              │
│    Tier 9 mana stone · skeleton                                        │
│    (empty — tier 8 slot)                                               │
│                                                                        │
│  YOUR MANA STONES (tier 8 only)                                        │
│    ▶ Tier 8 · skeleton × 3                                             │
│      Tier 8 · orc × 1                                                  │
│                                                                        │
│  [ Enter ] Place selected stone on altar    [ R ] Remove from altar   │
└────────────────────────────────────────────────────────────────────────┘
```

### 6.3 — Mock — player lacks eligible stones

```
┌────────────────────────────────────────────────────────────────────────┐
│  STONE ALTAR                                                    [ Esc ] │
├────────────────────────────────────────────────────────────────────────┤
│  This altar has places for a tier 9 mana stone and a tier 8 mana      │
│  stone.                                                                │
│                                                                        │
│  ON ALTAR                                                              │
│    (empty)                                                             │
│    (empty)                                                             │
│                                                                        │
│  YOUR MANA STONES                                                      │
│    You have no tier 9 or tier 8 mana stones to place.                  │
│                                                                        │
│  [ R ] Remove from altar (disabled)                                    │
└────────────────────────────────────────────────────────────────────────┘
```

### 6.4 — Mock — remove flow

When **R** pressed and altar has ≥1 stone:

1. Highlight **on altar** rows (tier 9 slot, tier 8 slot).
2. **Enter** on a filled slot → stone returns to ledger, **consumes turn**, closes modal (§7).

### 6.5 — Dialog rules

| ID | Rule |
|----|------|
| **U1** | Header/flavor text from `AltarDefinition.descriptionTemplate`. v0 exact copy: *“This altar has places for a tier 9 mana stone and a tier 8 mana stone.”* |
| **U2** | **ON ALTAR** lists each slot label + contents or `(empty)`. |
| **U3** | **YOUR …** lists ledger stacks matching **place filter**: union of tiers for **empty** slots only (v0: if tier 9 filled, **no** tier 9 lines). |
| **U4** | If no matching stacks, show **U3 empty copy** (tier-specific message when only one tier missing optional). |
| **U5** | **Place:** select stack row → **Enter** → validate accept filter + empty target slot → spend ledger → fill slot → evaluate completion → **close** → **turn** (§7). |
| **U6** | **Remove:** **R** then select on-altar row → return to ledger → **close** → **turn** (§7). |
| **U7** | **Escape** anytime → close without mutation → **no turn**. |
| **U8** | While modal open, **movement / abilities** blocked (same as inventory overlay). |
| **U9** | Species label uses friendly name from `EnemySpeciesDefinition` when known; else raw `speciesId`. |
| **U10** | Tier display uses project convention: **9 = lowest band** (see mana stone doc). |

### 6.6 — Place target slot resolution (v0)

When placing a tier **T** stone:

1. If slot with `ManaStoneTierAcceptFilter(T)` is **empty**, place there.
2. If multiple empty slots accept **T** (future), prompt slot choice — **not** v0.

---

## 7. Turn consumption & formation rush

| Action | Turn? | `ProcessFollowerRush`? |
|--------|-------|-------------------------|
| Press **Interact** (open picker or altar UI) | **No** | **No** |
| **Escape** from picker or altar UI | **No** | **No** |
| **Place** mana stone on altar | **Yes** | **Yes** if `PartyManager.IsFormationActive` |
| **Remove** mana stone from altar | **Yes** | **Yes** if formation active |

**Implementation:** After successful place/remove, `PlayerCommandProcessor` calls the same path as `Wait` / door open: end active member’s action, advance turn pipeline, then `ProcessFollowerRush()` when formation active.

**Completion spawn:** Runs **during** the place action that satisfied the last slot, **before** turn ends (enemy acts same turn cycle as placement) unless playtest dictates otherwise — **locked v0:** spawn synchronously on place confirm, then consume turn.

---

## 8. v0 content — Mana stone pair altar

### R8.1 — `AltarDefinition` asset

| Field | Value |
|-------|-------|
| `altarId` | `altar_mana_stone_pair_v0` |
| `displayName` | `Stone altar` |
| `descriptionTemplate` | See §6.5 **U1** |
| `sprite` | `Assets/Art/Altars/Sprites/Altar_StoneShrine.png` |
| **Slot 0** | `ManaStoneTierAcceptFilter` tier **9**, any species |
| **Slot 1** | `ManaStoneTierAcceptFilter` tier **8**, any species |
| **Completion rule** | Both slots filled (any species per slot) → `SpawnEnemyAltarCompletionEffect` |

### R8.2 — Spawn effect

| Field | Value |
|-------|-------|
| **Prefab** | `Assets/Prefabs/Actor/Enemy/Enemy.prefab` + `SkeletonSpecies` |
| **Policy** | `NorthOfOriginThenNearestUnoccupiedFloor` |
| **Origin** | Altar cell |
| **Primary offset** | `(0, 1, 0)` (north) |
| **Service** | Reuse `EnemySpawnService.TrySpawn` ([conditional spawn](../Combat/Conditional-Enemy-Spawn-Requirements.md)) |
| **Failure** | Log `[Altar:Spawn] failed`; altar stays complete; placement turn still consumed |

### R8.3 — SampleScene placement

| Requirement | Detail |
|-------------|--------|
| **Scene** | `SampleScene` — at least one authored `altar_mana_stone_pair_v0` on walkable floor |
| **QA** | Party can stand adjacent, press **E**, place tier 9 + tier 8 stones from ledger, skeleton spawns north or nearest |
| **Ledger setup** | Debug menu or test loot so player has tier 8/9 stones (skeleton drops per loot doc) |

---

## 9. Future altars & interactables (not v0)

| Feature | Notes |
|---------|--------|
| **Multiple completion outcomes** | Several `AltarCompletionRule` rows: e.g. tier 9 only → curse; tier 9+8 → skeleton |
| **Non–mana-stone slots** | `ItemInstance` from party inventory; encumbrance / equip rules |
| **Partial offerings** | Ritual progress meter |
| **Shrines / NPCs / chests** | Implement `IAdjacentMapInteractable` with different modals |
| **Bump + interact** | Some features may register both (document per feature) |
| **Save/load** | Serialize `AltarSlotState` + completion latch |

---

## 10. Data & services summary

| Type | Namespace / menu |
|------|------------------|
| `AdjacentMapInteractableService` | `JRogue.World` |
| `AltarDefinition`, `AltarSlotDefinition`, filters, effects | `JRogue/World/...` |
| `AltarOfferingService` | Ledger transfer + validation |
| `AltarOfferingModal`, `AdjacentInteractPickerModal` | `JRogue.UI` |
| `AltarInteractable` | Scene component or map feature host |

### 10.1 — Unit tests (v0)

| Test | Assert |
|------|--------|
| `AltarCompletionEvaluator` | Empty → false; tier 9 only → false; tier 9+8 → true |
| `AltarPlaceFilter` | Tier 9 on altar → place list excludes tier 9 |
| `AltarOfferingService_Place` | Ledger −1, slot +1 |
| `AltarOfferingService_Remove` | Ledger +1, slot empty |
| `AdjacentInteractableQuery` | Returns only orthogonal, `CanInteract` |
| `EnemySpawnPlacementResolver` | Altar origin → north then nearest (existing tests extended) |

---

## 11. Implementation checklist

- [x] `PlayerCommandKind.Interact` + `GameControls.Interact` (`E`)
- [x] `AdjacentMapInteractableService` + `IAdjacentMapInteractable`
- [x] `AdjacentInteractPickerModal`
- [x] `AltarDefinition` / slots / filters / completion effects
- [x] `AltarOfferingService` + `PartyManaStoneLedger` integration
- [x] `AltarOfferingModal` per §6 mocks
- [x] Turn + `ProcessFollowerRush` on place/remove only
- [x] `SpawnEnemyAltarCompletionEffect` + v0 assets (editor menu)
- [ ] SampleScene altar + QA steps (run **JRogue/World/Wire SampleScene Mana Stone Altar** in Unity)
- [x] Unit tests §10.1

---

## 12. Art assets (delivered)

| Asset | Path | License |
|-------|------|---------|
| Source | `Assets/Art/Altars/ThirdParty/ElementalShrines/originals/stone1.png` | CC0 — [OpenGameArt](https://opengameart.org/content/elemental-stonesshrines-pixel-art) |
| Shipped sprite | `Assets/Art/Altars/Sprites/Altar_StoneShrine.png` | Copy of `stone1.png`, 32×32 |
| Attribution | `Assets/Art/Altars/ThirdParty/ElementalShrines/README.md`, `LICENSE.txt` | |

**Import settings:** PPU **32**, **Point** filter, pivot **(0.5, 0.25)** so base sits on floor cell.

---

## 13. Acceptance criteria (v0)

| # | Criterion |
|---|-----------|
| **AC1** | Press **E** adjacent to altar (not bumping) opens offering dialog with §6.5 **U1** text. |
| **AC2** | Dialog shows on-altar slots and ledger stacks per §6.5 **U2–U4**; tier already placed hidden from place list. |
| **AC3** | Place/remove updates ledger and altar slots; **Escape** never costs a turn. |
| **AC4** | Place/remove costs a turn and triggers `ProcessFollowerRush` when formation active; dialog closes. |
| **AC5** | With tier 9 + tier 8 placed, skeleton spawns north of altar or nearest valid floor per spawn doc. |
| **AC6** | With ≥2 adjacent interactables, picker mock §4.1 appears; cancel = no turn. |
| **AC7** | Overlay sprite visible at altar cell using `Altar_StoneShrine`. |
