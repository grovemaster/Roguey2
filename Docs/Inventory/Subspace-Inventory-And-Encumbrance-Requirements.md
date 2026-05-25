# Subspace inventory & encumbrance — Requirements

Party inventory supports **nested storage** (containers) with **encumbrance policies** inspired by **Baldur’s Gate 3** (carry limits while managing inventory), **Surviving the Game as a Barbarian** (expandable backpack weight reduction, subspace ring zero-weight stash, essence subspace inventories), integrated with the existing **Inventory UI** (full-screen CRPG layout).

**Depends on:** `ItemInstance`, `ItemData`, `ItemStorageLocation`, `InventoryManager`, `EquipmentManager`, `CharacterStats` (`EncumbranceLimit`, per-stat `sight` unrelated), `InventoryUI`, `InventoryViewModel`, `InventoryPresentationModel`, `InventoryDetailFormatter`, `InventoryPolicy`, `PartyManager`, `FloorItemPileService` / pickup flows, `EssenceSlotManager`, `EssenceData`, [Inventory UI redesign](Inventory-UI-Redesign-Requirements.md), [Floor item piles](Floor-Item-Pile-Requirements.md).

**Related:** [Enemy death loot & mana stones](../Combat/Enemy-Death-Loot-And-Mana-Stones-Requirements.md) (weightless party currency). [Party experience & leveling](../Progression/Party-Experience-And-Leveling-Requirements.md) (future potions in bag). [Phase 0 stacking glossary](../RacialSystem/Phase0-Glossary-And-Data-Contracts.md) (stat modifiers stack by source).

**Explicitly separate (later):** BG3-style **encumbrance tiers** (movement penalties). **Character screen** paper doll. Drag-and-drop between arbitrary UI cells (v1 uses actions + scope navigation).

---

## 1. Goals

**G1 — Accurate encumbrance**  
Actor carry limit reflects **effective encumbrance weight**, not a flat sum of every `ItemInstance` in a list. Items inside exempt containers contribute **0** toward limit; discounted containers contribute **reduced** weight.

**G2 — STBGB-style containers**  
Support at minimum:

| Container type | Encumbrance rule |
|----------------|------------------|
| **Loose carried** | Full `ItemData.weight × quantity` |
| **Equipped gear** | Full weight (existing behavior) |
| **Expandable backpack** | Contents use **multiplier** (e.g. 50% of catalog weight) |
| **Subspace ring** | Contents **exempt** (0 encumbrance) |
| **Essence subspace** | Stash contents **exempt**; separate from physical bag |

**G3 — Inventory UI parity**  
The existing Inventory menu remains the primary surface: party strip, encumbrance bar, category tabs, 50/50 list + inspect, actions bar. Containers add **storage scope navigation** (root vs inside container vs essence stash), not a second inventory screen.

**G4 — Fair transfers**  
Moving items into/out of containers re-runs **`CanCarry` / encumbrance checks** on the owning actor. Pickup, drop, equip, unequip, and floor pile flows remain consistent.

**G5 — Party clarity**  
**Focused Member** mode is the main place to open containers and move items. **Party Aggregate** mode stays scannable (container shells visible; contents not fully flattened by default).

**G6 — Data-first**  
One `ItemInstance` graph (parent container + location); encumbrance policy on **container definition**, not hard-coded per item name.

**G7 — Subspace transfers are free actions**  
Moving items **between** loose carried, expandable backpack, subspace ring, and essence subspace (same owner) does **not** consume a player turn — including while the Inventory menu is open and during out-of-combat management.

**G8 — Auto-route into subspace**  
When an actor gains a new item (pickup, loot, receive) and owns **at least one** subspace container with free capacity, the game **automatically stores** the item in the best eligible subspace instead of loose carried (§6.4). Manual **Take** to loose bag remains available.

**G9 — Distinct-type capacity per subspace**  
Each subspace inventory caps **how many different item types** it may hold; limits are **per container definition** (ring vs backpack vs essence). Same-type stacking does not consume an extra type slot (§5.2).

**G10 — Multiple subspaces & UI traceability**  
An actor may own **several** subspace inventories at once. Routing picks the subspace that **maximizes encumbrance reduction** among non-full targets (§6.4). The Inventory menu always shows **which subspace holds each item** (§7.10).

---

## 2. Reference — external games (summary)

| Source | Behavior | This spec |
|--------|----------|-----------|
| **BG3** | Encumbrance while managing inventory; per-character limits; over-limit blocks pickup | Encumbrance strip + `CanCarry`; optional future tiers (§12) |
| **STBGB — expandable backpack** | Items **inside** backpack weigh less; items outside or on other members weigh normally | `EncumbrancePolicy.Multiplier` on container |
| **STBGB — subspace ring** | Separate inventory inside ring; **zero** encumbrance for contents | `EncumbrancePolicy.Exempt` + drill-down UI |
| **STBGB — essence** | Essence grants subspace stash; contents zero encumbrance | Essence-linked stash + **Essence** UI scope (§7) |
| **Existing project** | Mana stones / currency: weight 0, party ledger | Unchanged; same exempt pattern |

---

## 3. Current baseline (as-is)

Documented so implementers know what changes.

| Area | Today |
|------|--------|
| **Storage** | Per-actor `InventoryManager.carriedItems` (flat list) + `EquipmentManager` equipped instances |
| **`ItemStorageLocation`** | `Unknown`, `OnGround`, `Carried`, `Equipped` — no “inside container” |
| **Weight** | `ItemInstance.TotalWeight` = `definition.weight × quantity` |
| **Limit** | `CharacterStats.EncumbranceLimit` = `Constitution × 5` |
| **`CanCarry`** | `GetTotalWeight() + new item ≤ EncumbranceLimit` (carried + equipped) |
| **UI encumbrance** | `BuildWeightAndCurrencyLine`: **sum** all members’ `GetTotalWeight()` vs **sum** all `EncumbranceLimit` |
| **Pickup** | Per **picker** encumbrance (`FloorPickupMenuUI`, `FloorPickupCoordinator`) |
| **Essence** | `EssenceData` in slots (abilities/passives); **no** physical item stash |
| **Inventory UI rows** | `InventoryViewModel`: equipped slots, then flat `carriedItems`; no nesting |

---

## 4. Encumbrance model

### 4.1 — Definitions

| Term | Meaning |
|------|---------|
| **Catalog weight** | `ItemData.weight × quantity` — intrinsic mass of the stack |
| **Encumbrance weight** | What counts toward `EncumbranceLimit` for the owning actor |
| **Container shell** | The equipped or carried item that **owns** a child list (ring, backpack) |
| **Storage node** | Actor root, loose list, equipped slot, or container instance |
| **Subspace inventory** | Any container or essence stash using `Multiplier` or `Exempt` policy (backpack, ring, essence) |
| **Distinct item type** | Unique `ItemData` identity in a subspace; multiple stacks / quantities of the **same** `ItemData` count as **one** type (§5.2.3) |
| **Type capacity** | `maxDistinctItemTypes` on a subspace — max distinct types allowed at once |

### 4.2 — `EncumbrancePolicy` (container definition)

```csharp
enum EncumbrancePolicy
{
    Full,        // 100% of catalog weight (default loose / equipped)
    Multiplier,  // catalog × weightMultiplier (e.g. 0.5 backpack)
    Exempt       // 0 encumbrance (subspace ring, essence stash)
}
```

| Policy | `EncumbranceWeight` |
|--------|---------------------|
| `Full` | `TotalWeight` |
| `Multiplier` | `TotalWeight × weightMultiplier` (clamped ≥ 0) |
| `Exempt` | `0` |

**Container shell** itself uses **`Full`** unless a future item defines otherwise (empty ring still has ring mass).

### 4.3 — Actor total encumbrance

Replace raw `GetCarriedWeight()` summation for limit checks with:

```
GetEncumbranceWeight(actor) =
    Sum(loose carried, Full)
  + Sum(equipped shells, Full)
  + Sum(each container's contents, recursive, parent's policy)
```

**Mana stones / currency:** remain **exempt** via ledger (not in bag encumbrance sum).

### 4.4 — `CanCarry` and transfers

**R4.4.1** `CanCarry(instance, StorageTarget target)` evaluates **delta encumbrance** for the owning actor when the instance would live at `target` (loose vs inside container X).

**R4.4.2** Moving item **into** exempt storage decreases actor encumbrance immediately after success.

**R4.4.3** Moving item **out** to loose bag may **fail** if `GetEncumbranceWeight() + item.Full > EncumbranceLimit` (same UX as today: warning / blocked pickup).

**R4.4.4** Equip/unequip: shell weight always applies; moving equipped container does not duplicate child encumbrance at actor root (children counted only under container policy).

### 4.5 — Party vs per-member encumbrance (locked for v1)

| Rule | Choice |
|------|--------|
| **Enforcement** | **Per actor** (matches floor pickup picker and `InventoryManager` on each `BaseActor`) |
| **Inventory UI strip** | **Party aggregate** sum of encumbrance weights vs sum of limits (existing pattern); optional subtitle **focused member** `42 / 90` (nice-to-have) |

Document in UI when aggregate exceeds sum of limits because one member is overloaded.

---

## 5. Data model

### 5.1 — Parent / child on `ItemInstance`

**R5.1.1** Add persistent parent reference (choose one in implementation):

- `string parentContainerInstanceId` (null = loose on actor root), or  
- `ItemStorageLocation.InsideContainer` + parent id field.

**R5.1.2** Extend `ItemStorageLocation`:

| Value | Meaning |
|-------|---------|
| `InsideContainer` | Child of a container `ItemInstance` |
| `EssenceStash` | Child of essence subspace (not inside a physical ring item) |

**R5.1.3** Each `ItemInstance` remains a distinct id (no merging unrelated drops).

### 5.2 — `ContainerDefinition` (on `ItemData` or sub-asset)

| Field | Purpose |
|-------|---------|
| `bool isContainer` | Shell opens a child inventory |
| `bool participatesInSubspaceRouting` | When true, eligible for auto-store and multi-container priority (§6.4) |
| `EncumbrancePolicy contentPolicy` | How **contents** count toward actor |
| `float weightMultiplier` | Used when policy = `Multiplier` |
| `int maxDistinctItemTypes` | Cap on **different item types** in this subspace (not total stack count) |
| `string subspaceDisplayName` | UI label, e.g. `Subspace Ring II`, `Expandable Backpack`, `Fire Spirit Stash` |

**R5.2.1** Subspace ring item: `isContainer = true`, `contentPolicy = Exempt`, `participatesInSubspaceRouting = true`, designer-set `maxDistinctItemTypes` (e.g. 8).

**R5.2.2** Expandable backpack: `isContainer = true`, `contentPolicy = Multiplier`, `weightMultiplier = 0.5` (designer-tunable), `participatesInSubspaceRouting = true`, its own `maxDistinctItemTypes` (e.g. 12 — may differ from ring).

**R5.2.3** **Distinct type counting**

- Count = number of unique `ItemData` assets with at least one `ItemInstance` in that subspace.
- Adding quantity to an **existing** type (same `ItemData`, merge allowed) does **not** increase the count.
- Adding a **new** `ItemData` when `currentDistinctTypes >= maxDistinctItemTypes` ⇒ subspace is **full** for routing (§6.4).
- v1: no partial-type slots; future: “overflow slot” essences optional.

**R5.2.4** Essence subspace (`EssenceData`): `hasSubspaceInventory`, `maxDistinctItemTypes`, `subspaceDisplayName` — independent per essence asset (two essences ⇒ two stashes, two limits).

### 5.3 — `ContainerInventory` service (or methods on `InventoryManager`)

| API | Behavior |
|-----|----------|
| `GetContents(containerInstanceId)` | Read-only child list |
| `TryAddToContainer(actor, containerId, instance)` | Policy + capacity + encumbrance checks |
| `TryRemoveToLoose(actor, containerId, instance)` | `CanCarry` to loose |
| `TryMove(instance, from, to)` | Validates same owner actor |

**R5.3.1** Container must be **on that actor** (equipped or loose carried) to add/remove.

**R5.3.2** Dropping a container shell to the floor: **v1** — drop shell only if empty, or drop shell + serialize children in pile entry (implementation choice; document in PR). **Preferred v1:** cannot drop non-empty container without emptying first.

### 5.4 — Essence subspace (non-item container)

**R5.4.1** `EssenceData` may define:

- `bool hasSubspaceInventory`
- `int maxDistinctItemTypes` (per essence definition — not shared with ring/backpack)
- `string subspaceDisplayName`
- Runtime stash: `List<ItemInstance>` per equipped essence **slot index** on `EssenceSlotManager`

**R5.4.2** Stash items use `ItemStorageLocation.EssenceStash` and **always** `EncumbrancePolicy.Exempt`.

**R5.4.3** Essence stash is **not** in `carriedItems`; UI uses parallel data source (§7.4).

### 5.5 — Central calculator

**R5.5.1** `EncumbranceCalculator` (static or service) is the **only** place that maps `(instance, parent chain) → encumbrance weight`.

**R5.5.2** `InventoryManager.GetTotalWeight()` may remain as catalog sum for debug; **`GetEncumbranceWeight()`** is used for `CanCarry` and UI bar.

---

## 6. Gameplay integration

### 6.1 — Turn cost policy (locked)

| Action | Consumes player turn? |
|--------|------------------------|
| Move item **loose ↔ subspace** (ring, backpack, essence) **same actor** | **No** |
| Move item **between two subspaces** on **same actor** | **No** |
| **Open** Inventory menu / browse / Store / Take / Open container | **No** |
| Floor **pickup** (confirm pile / walk-over) | **Yes** (existing — one party action per pickup batch) |
| **Drop** item to ground | **Yes** (existing) |
| **Equip / unequip** shell (ring on finger, backpack on back) | **Yes** (existing equipment flow) |
| **Use** consumable / ability from item or essence | **Yes** (existing) |
| **Give** to ally (future) | **Yes** (when implemented) |

**R6.1.1** `InventoryManager` / `ContainerInventory` subspace transfer APIs must **not** call `TurnManager.OnPlayerActionComplete`.

**R6.1.2** `InventoryPolicy` documents: subspace reorganize is **inventory management**, exempt from combat “one item action” only for **Use/Drop/pickup** — subspace Store/Take remains allowed in combat **without** spending turn (owner only).

### 6.2 — Floor pickup

**R6.2.1** On successful pickup to picker: run **`SubspaceRouting.TryAutoStore(picker, instance)`** before defaulting to loose `carriedItems` (§6.4).

**R6.2.2** If no subspace accepts the item, fall back to **loose carried** (existing `CanCarry` on loose target).

**R6.2.3** Encumbrance preview in pickup header uses **`GetEncumbranceWeight()`** on picker **after** simulated auto-store when possible.

**R6.2.4** Pickup UI may show resolved destination: `→ Subspace Ring II` in confirm row subtitle (optional v1; required in Inventory inspect).

### 6.3 — Drop / give / combat

**R6.3.1** Drop from container: remove from child list, create floor pile entry (existing drop flow); **consumes turn**.

**R6.3.2** `InventoryPolicy`: in combat, only **owner** may Use/Store/Take from own subspaces; subspace transfers **do not** cost turn (§6.1).

**R6.3.3** Give to ally: **future**; costs initiator turn when implemented — **not** the same code path as subspace moves.

### 6.4 — Subspace auto-routing (priority & multiple containers)

**R6.4.1** **`SubspaceRouting`** (static service or `InventoryManager` method) runs whenever an item would be added to an actor’s inventory:

- Floor pickup (post-confirm)
- Loot grant
- Unequip-to-bag (optional: try subspace before loose — **yes**, same routing)
- Manual **Store** uses same “pick best target” if user does not specify container

**R6.4.2** **Eligibility:** collect all subspaces on actor where `participatesInSubspaceRouting` and container is accessible (equipped or loose shell for items; essence stash if essence equipped):

| Source | Id for routing |
|--------|----------------|
| Expandable backpack instance | `containerInstanceId` |
| Subspace ring instance | `containerInstanceId` |
| Essence stash | `(essenceSlotIndex, EssenceData asset id)` |

**R6.4.3** **Full check:** skip subspace if item is a **new distinct type** and `distinctTypeCount >= maxDistinctItemTypes`. If item’s `ItemData` already present in that subspace, not full (may stack per merge rules).

**R6.4.4** **Priority score** among non-full subspaces — maximize **encumbrance reduction** for this item:

```
encumbranceIfLoose = EncumbranceCalculator.Weight(item, Full)
encumbranceIfInTarget = EncumbranceCalculator.Weight(item, targetPolicy, targetMultiplier)
savings = encumbranceIfLoose - encumbranceIfInTarget
```

Pick subspace with **highest `savings`**; tie-break order (documented): **Exempt** &gt; higher multiplier reduction &gt; **larger remaining type capacity** &gt; stable sort by `subspaceDisplayName`.

**R6.4.5** **Multiple containers example:** actor has **Exempt ring** (8/8 types full) and **50% backpack** (3/12 types). New iron ingot (`ItemData` not in either): backpack wins if not full; if backpack full, try ring; if all full ⇒ **loose** (or pickup fails if loose over limit).

**R6.4.6** **No subspace owned:** behavior identical to today — loose carried only.

**R6.4.7** Player setting (future): “Auto-subspace on pickup” toggle default **on**; v1 always on.

### 6.5 — Equip container shell

**R6.5.1** Equipping subspace ring / backpack follows `EquipmentManager` (item must be in loose bag first); **consumes turn**.

**R6.5.2** Unequip non-empty container: allowed if loose bag has encumbrance room for **shell only**; children stay inside shell instance; **consumes turn**.

**R6.5.3** Actor may equip **multiple** subspace shells simultaneously (e.g. ring + backpack + several essence slots) — each contributes a separate routing target (§6.4).

---

## 7. Inventory UI requirements

Layout unchanged from [Inventory UI redesign](Inventory-UI-Redesign-Requirements.md): party strip → encumbrance → search → category tabs → 50/50 list | inspect → actions → footer.

### 7.1 — Storage scope (new)

**R7.1.1** Add **storage scope** orthogonal to `BrowseMode`:

| Scope | List contents |
|-------|----------------|
| **Root** | Equipped + loose on focused member (container **shells** only; not flattened children) |
| **InsideContainer** | Children of one selected container instance |
| **EssenceStash** | Items in one essence slot’s subspace (when tab/scope active) |

**R7.1.2** Breadcrumb when not at root:

```
[ ← Back to bag ]    Inside: Subspace Ring · Aria
```

**R7.1.3** **Esc** or **Back** returns to **Root** for current focused member.

**R7.1.4** Changing party strip member **resets** storage scope to **Root** for that member.

### 7.2 — Party browse modes

**R7.2.1** **Focused Member** (primary): full container navigation (Open / Store / Take).

**R7.2.2** **Party Aggregate** (v1): show **container shells** and **loose** items per member; **do not** inline all ring contents (subtitle e.g. `· 12 inside` on shell row). Opening a container **switches** to Focused Member + that owner + InsideContainer scope.

### 7.3 — List columns

**R7.3.1** **Wt** column shows **encumbrance weight** for the row (0 or `—` when exempt).

**R7.3.2** Optional display: `1.6` with inspect showing catalog `3.2` (multiplier containers).

**R7.3.3** **Subtitle** includes **holder** (which subspace): see §7.10 — e.g. `Subspace Ring II`, `Expandable Backpack`, `Fire Spirit Stash`, or `Loose` / `Equipped [E OffHand]`.

**R7.3.4** Container **shell** rows: visual cue (`▸` prefix or section **Containers**).

**R7.3.5** Sort-by-weight uses **encumbrance weight**.

### 7.4 — Category tabs & search

**R7.4.1** Category filter applies to **visible scope** rows only.

**R7.4.2** Search matches item name and **container name** / location subtitle.

**R7.4.3** **Currency** tab unchanged (party ledger).

**R7.4.4** **Essence** tab (new) or **Stash** scope: lists essence subspace items for focused member; omitted from default **All** bag tab optional (implementation: Essence tab preferred).

### 7.5 — Inspect pane

**R7.5.1** Summary block extends:

```
Value (stack)      …
Weight (stack)     3.2 kg     ← catalog
Encumbrance        0 kg       ← toward limit; omit line if equal to stack
Location           Inside Subspace Ring · Aria
```

**R7.5.2** Container **shell** selected at root adds:

```
Container          Subspace Ring II (5 / 8 types)
Policy             Contents do not count toward encumbrance
```

**R7.5.3** Reuse `InventoryDetailFormatter`; extend `DescribeLocation` for new storage locations.

### 7.6 — Encumbrance strip

**R7.6.1** `BuildWeightAndCurrencyLine` uses **`GetEncumbranceWeight()`** per member.

**R7.6.2** Over-limit coloring unchanged (ratio &gt; 1).

**R7.6.3** Label remains party-oriented e.g. `Party weight: 142 / 180` (encumbrance units, not catalog sum).

### 7.7 — Actions bar

**R7.7.1** Extend contextual actions:

| Action | When enabled |
|--------|----------------|
| **Open** | Selected row is container shell |
| **Store** | Item in loose/root scope; actor has valid target container (last opened or picker) |
| **Take** | Item inside container scope; `CanCarry` to loose |
| **Equip / Use / Drop / Give** | Existing rules; paths use container indices, not only `carriedListIndex` |

**R7.7.2** **Open** hotkey (e.g. `O`) documented in footer when container selected.

**R7.7.3** Drop non-empty container: blocked or confirm “must empty first” (§5.3.2).

### 7.8 — `InventoryViewModel.Row` extensions

**R7.8.1** Add fields (names indicative):

| Field | Purpose |
|-------|---------|
| `StorageScope scope` | RootLoose, Equipped, InsideContainer, EssenceStash |
| `string containerInstanceId` | Parent container when nested |
| `int containerContentIndex` | Index in parent contents |
| `float encumbranceWeight` | Wt column |
| `float displayWeight` | Inspect catalog line |
| `bool isContainerShell` | Has Open action |
| `int? essenceSlotIndex` | When `EssenceStash` |
| `string subspaceHolderLabel` | Which subspace holds this item (empty if loose/equipped shell only) |
| `int distinctTypesInSubspace` | For shell rows: `current / max` types |
| `int maxDistinctItemTypes` | From container or essence definition |

**R7.8.2** `carriedListIndex` remains for loose items only; container children use `containerContentIndex`.

**R7.8.3** `BuildPartyMember` / `BuildPartyAggregate` implement §7.2 rules.

### 7.9 — Section headers

**R7.9.1** `InventoryPresentationModel` may emit headers: **Equipped**, **Loose carried**, **Inside [name]** (when scope allows), **Essence stash**.

**R7.9.2** When a single category tab is active, headers may be sort-only (per UI redesign R4.3.4).

### 7.10 — Subspace holder display & layout mock (authoritative)

Every item row and inspect pane must answer: **“Which subspace is holding this?”** If none, show **Loose** or **Equipped** (shell).

**R7.10.1** Add **Subspace** column (narrow) **or** enforce holder on subtitle line (minimum: subtitle). **Recommended v1:** dedicated **Subspace** column between **Name** and **Qty** for scanability.

**R7.10.2** Shell rows at root show type usage: `5/8 types` in **Qty** or **Subspace** column; not a holder (they *are* the container).

**R7.10.3** **Party Aggregate** mode: `Subspace` column + owner on subtitle (`· Aria`).

**R7.10.4** **InsideContainer** scope: breadcrumb shows holder name; children inherit same label.

#### Mock — Focused member, **All** tab, root scope (multiple subspaces)

Outer chrome unchanged (§ Inventory UI redesign). New column **Subspace** (72px).

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│ INVENTORY                                                                               │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│ PARTY     ● Aria    ○ Bruenor    ○ Imoen          Mode: [ Member ▾ ]                     │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│ ENCUMBRANCE   ████████░░░░  98 / 180        Gold 1,240                                  │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│ [ Search __________________________ ]     Usable only ☐                                 │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│ [ All ] [ Weapons ] [ Armor ] [ Consumables ] [ Essence ]                               │
├───────────────────────────────────────────────┬─────────────────────────────────────────┤
│ Qty   Subspace              Wt    Value       │  INSPECT (Iron Greatsword)              │
│ ── Equipped ──                                │  ┌───────────────────────────────────┐  │
│ ×1    (equipped)            4.0    120       │  │ [icon]  IRON GREATSWORD           │  │
│ ×1    Subspace Ring II      0     45        │  │ Slashing · MainHand               │  │
│       [E OffHand] shell     (ring) 5/8 typ   │  └───────────────────────────────────┘  │
│ ×1    Expandable Backpack   0     12        │  Value (stack)     45                     │
│       [E Torso] shell       (bag)  3/12 typ  │  Weight (stack)    8.0 kg                 │
│ ── Loose carried ──                           │  Encumbrance       0 kg                   │
│ ×2    Loose                 1.8    8        │  Held in           Subspace Ring II       │
│ ── Held in subspace (flattened opt.) ──      │  Owner             Aria                     │
│ ×1    Subspace Ring II      0     45        │  ── Compare vs equipped ──                │
│ ×3    Expandable Backpack   1.5   36        │  …                                        │
│ ×1    Fire Spirit Stash     0     200       │                                           │
│ ×5    Subspace Ring II      0     —         │                                           │
│       (mana stone — ledger)                 │                                           │
└───────────────────────────────────────────────┴─────────────────────────────────────────┘
│ [ Open ] [ Store ] [ Take ] [ Equip ] [ Drop ]   ← Store/Take: no turn (§6.1)          │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

**R7.10.5** **Implementation choice (pick one in PR):**

| Mode | List behavior |
|------|----------------|
| **A — Holder column only at root** | Subspace children visible only after **Open**; root optional section “Held in subspace” duplicates holder labels for search |
| **B — Flattened subspace rows at root** | All items visible with **Subspace** column (mock above); **Open** still drills into one container for bulk manage |

Default recommendation: **B** for Party Aggregate search; **A** acceptable in Focused Member if list noise is a concern.

#### Mock — Single row (column detail)

```
┌────┬────┬────────────────────────────┬──────────────────┬─────┬──────┬────────┐
│ Ltr│Icon│ Name                       │ Subspace         │ Qty │ Wt   │ Value  │
├────┼────┼────────────────────────────┼──────────────────┼─────┼──────┼────────┤
│ d  │ 📷 │ Healing Potion             │ Fire Spirit Stash│ ×3  │ 0    │   12   │
│    │    │ Potion · #a91c2            │                  │     │      │        │
├────┼────┼────────────────────────────┼──────────────────┼─────┼──────┼────────┤
│ e  │ 📷 │ Iron Ingot                 │ Expandable Backp.│ ×1  │ 1.5  │   30   │
│    │    │ Treasure · #b77e1          │  (50% enc.)      │     │      │        │
├────┼────┼────────────────────────────┼──────────────────┼─────┼──────┼────────┤
│ f  │ 📷 │ Rope Coil                  │ Loose            │ ×1  │ 2.0  │    5   │
│    │    │ Junk · subspace full       │                  │     │      │        │
└────┴────┴────────────────────────────┴──────────────────┴─────┴──────┴────────┘
```

When auto-route fails (all subspaces full), subtitle hint: `subspace full` on loose row.

#### Mock — Inspect pane (holder block)

```
Held in             Subspace Ring II
Subspace capacity   5 / 8 item types
Encumbrance here    0 kg (exempt)
```

For backpack-held item:

```
Held in             Expandable Backpack
Encumbrance here    1.5 kg (50% of 3.0 kg stack)
```

---

## 8. Example items (design targets)

| Item | `ContainerDefinition` | `maxDistinctItemTypes` (example) | UI |
|------|----------------------|----------------------------------|-----|
| **Expandable Backpack** | `Multiplier` 0.5 | 12 | Holder column `Expandable Backpack`; 50% enc. in Wt |
| **Subspace Ring** | `Exempt` | 8 | Holder `Subspace Ring II`; Wt `0` |
| **Subspace Ring (better)** | `Exempt` | 15 | Second ring — routing prefers ring with room + higher savings |
| **Priest’s Blessing of Might** | Not a container | — | Out of scope (status buffs — separate spec) |
| **Fire Spirit Essence** | `hasSubspaceInventory`, exempt | 6 | Holder `Fire Spirit Stash`; Essence tab |

---

## 9. Phased delivery

### Phase 1 — Encumbrance core (no UI drill-down)

- `EncumbranceCalculator` + `GetEncumbranceWeight()` / updated `CanCarry`
- Data model: parent id + `ContainerDefinition` on ring asset
- Ring contents in code/API only; UI still flat if needed for testing

### Phase 2 — Subspace ring UI (vertical slice)

- Storage scope: Root + InsideContainer
- Breadcrumb, **Open** / **Take** / **Store** (no turn — §6.1)
- **Subspace** holder column + inspect block (§7.10)
- `maxDistinctItemTypes` on ring asset
- Encumbrance strip uses new weights

### Phase 3 — Auto-routing & multiple containers

- `SubspaceRouting.TryAutoStore` on pickup
- Priority by encumbrance **savings**; multiple ring/backpack/essence targets
- Distinct-type full checks

### Phase 4 — Expandable backpack

- `Multiplier` policy + inspect dual weight lines
- Separate `maxDistinctItemTypes` from ring

### Phase 5 — Essence subspace

- `EssenceData` stash + **Essence** tab / scope
- Essence participates in same routing priority as item subspaces

### Phase 6 — Polish

- Party aggregate flattened rows (§7.10.5 mode B)
- Drop rules for non-empty containers
- Optional player toggle auto-subspace

---

## 10. Acceptance criteria

### Phase 2 (UI + ring)

1. Focused member equips **Subspace Ring**, puts heavy item inside via **Store**; party encumbrance bar **drops** by that item’s catalog weight; **turn not consumed**.
2. **Take** same item to loose bag; bar **rises**; if over limit, **Take** fails; **turn not consumed**.
3. **Open** on ring shows only children; **Back** restores root list with ring shell visible.
4. Inspect shows **Held in: Subspace Ring II** and **Encumbrance: 0 kg** for exempt contents.
5. List **Subspace** column matches inspect holder for every non-loose item.
6. Mana stones / currency behavior unchanged.

### Phase 3 (routing + types + multiple subspaces)

7. Pickup with ring + backpack equipped: new treasure type routes to subspace with **highest encumbrance savings** and free type slot.
8. Ring at **8/8 types**, backpack at **3/12**: new 9th type goes to **backpack**, not loose (if savings &gt; 0).
9. All subspaces full for new type: item lands **Loose**; subtitle/UI may show `subspace full`.
10. Second stack of existing potion in ring does **not** increase type count.
11. Moving potion **ring → backpack** via Inventory: **no turn** spent.
12. Actor with **two** exempt rings (different `maxDistinctItemTypes`): routing fills ring with best savings first, then second ring.

---

## 11. Non-goals (v1)

- BG3 **encumbered / heavily encumbered** movement debuffs
- Drag-and-drop grid inventory
- Merging stacks inside containers automatically
- Cross-actor container access without Give implementation
- Subspace inventory on **enemies**
- Weightless bags that still count as “one slot” only (slot-only limits without weight — future)

---

## 12. Future

| Feature | Notes |
|---------|--------|
| Encumbrance tiers | Speed / action penalties from `encumbrance / limit` ratio |
| Manual pickup override | Force loose vs pick subspace when auto-route would apply |
| Stack split / partial store | Quantity moves between container and loose |
| Player toggle | Disable auto-subspace on pickup (default on in v1) |
| Container on ground | Pile entry holds serialized child list |
| Shared party pool | Single encumbrance cap for whole party (design alternative) |
| Appraisal / value | Unchanged; containers inherit item rules |

---

## 13. Open decisions (resolve in PR)

| # | Question | Default recommendation |
|---|----------|------------------------|
| D1 | Drop non-empty container | Block until empty |
| D2 | Party aggregate shows inner items | No (shells only) |
| D3 | Essence stash UI entry | Horizontal **Essence** tab |
| D4 | Wt column for exempt items | `0` (encumbrance); catalog in inspect |
| D5 | `GetTotalWeight()` deprecation | Keep for debug; UI/limit use encumbrance only |
| D6 | Root list shows all subspace items | **B** flattened + Subspace column (§7.10.5) |
| D7 | Same `ItemData` stack merge in subspace | **Yes** — one type slot (§5.2.3) |
| D8 | Auto-subspace on pickup default | **On** (§6.4.7) |

---

## 14. Traceability — code touchpoints

| Component | Change |
|-----------|--------|
| `ItemInstance` / `ItemStorageLocation` | Parent id, new locations |
| `ItemData` + `ContainerDefinition` | Container flags |
| `InventoryManager` | Recursive encumbrance, container CRUD |
| `EquipmentManager` | Shell equip with attached contents |
| `EssenceSlotManager` + `EssenceData` | Subspace stash lists |
| `EncumbranceCalculator` | Policy application |
| `InventoryViewModel` | Scope-aware rows |
| `InventoryUI` | Breadcrumb, scope state, actions |
| `InventoryDetailFormatter` | Location + encumbrance lines |
| `FloorPickupCoordinator` / menu | Encumbrance preview + post-pickup `SubspaceRouting` |
| `SubspaceRouting` (new) | Auto-store, savings priority, distinct-type capacity |
| `TurnManager` | Ensure subspace transfers do not call action complete |
| `InventoryItemRowView` | Subspace column binding |

---

*Last updated: subspace turn-free transfers, auto-routing, distinct-type limits, multi-container priority, UI holder mock (§7.10).*
