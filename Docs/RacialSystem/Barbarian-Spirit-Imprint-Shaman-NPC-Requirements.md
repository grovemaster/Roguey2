# Barbarian Spirit Imprint — Shaman NPC (upgrade gate)

**Purpose:** Specify a **town NPC** who lets a **Barbarian party member** spend **gold**, **items**, and/or **story flags** to **extend their Spirit Imprint path by exactly one node** — fulfilling the “special NPC” progression gate deferred from [Phase 3 — Barbarian Spirit Imprint](Phase3-Requirements.md) §D2.2 / §7.3.

**Status:** Implemented (v0).

**Depends on:** [Phase 3 — Barbarian Spirit Imprint](Phase3-Requirements.md) (`SpiritImprintGraph`, `SpiritImprintNodeData`, `SpiritImprintRuntime`, forward-only path, single-node advance), [NPC dialog](../World/NPC-Dialog-Requirements.md) (Enter adjacency + facing, `NpcDialogBoxUI`), `NpcController`, `NpcTalkInteraction`, `PartyManager`, `CharacterStats.race`, `PartyCurrencyLedger`, `InventoryManager`, `GameStoryFlagService`, [Quest system](../World/Quest-Requirements.md) (story-flag costs), `BarbarianPlayer.prefab` / `SpiritImprintRuntime`.

**Related:** [Dwarf — Patron Ancestor](Dwarf-Ancestor-And-Common-Abilities-Requirements.md) (same “event/NPC extends forward-only tree” model). [Shop NPCs](../World/Shop-NPC-Requirements.md) (party gold + inventory mutation patterns). [Safe zone](../World/Safe-Zone-Requirements.md) (town talk in safe zone).

**Explicitly out of scope (v0):** respec / undo imprint picks; upgrading **non-Barbarian** races; upgrading **multiple party Barbarians in one dialog** (one talk = one speaker); save/load imprint across game sessions beyond existing party persistence; bespoke **character sheet** UI for the imprint tree; **this NPC’s final narrative identity** (see §4 — placeholder Shaman only); batch multi-node unlock in one transaction; imprint **active ability** hotbar execution.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Placeholder Shaman NPC** in town — Barbarian appearance, **unused** world sprite + portrait, display name **“Shaman Barbarian”**. |
| **G2** | **Race gate** — non-Barbarian **speaker** gets a single rejection line; no upgrade UI. |
| **G3** | **Dynamic offer** — dialog lists **every valid next imprint node** (direct children of the speaker’s current path tail) with **description + cost**. |
| **G4** | **Per-node costs** — each graph node defines its own unlock cost (gold, items, flags). |
| **G5** | **Choice UX** — one choice per affordable/unaffordable upgrade + **Cancel**; unaffordable options **greyed out** and **not confirmable**. |
| **G6** | **Transaction** — on confirmed affordable pick: **deduct costs**, **append exactly one node** to the speaker’s `chosenPathNodeIds`, **re-apply** imprint effects. |
| **G7** | **Complete state** — when no further children exist on the speaker’s path, Shaman says **“You have all the upgrades.”** |
| **G8** | **Data-driven** — designers author costs on imprint nodes; dialog text is **generated from data**, not hard-coded per node in C#. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Shaman NPC** | Town `NpcController` (v0 id **`shaman_barbarian`**) that runs the Spirit Imprint upgrade dialog flow. |
| **Speaker** | **Active party leader** at talk time (`PartyManager.GetActiveMember()`) — same as [NPC dialog §3](../World/NPC-Dialog-Requirements.md). |
| **Path tail** | Last id in the speaker’s `SpiritImprintRuntime.chosenPathNodeIds` (deepest committed node). |
| **Next node** | A graph node whose `parentNodeId` equals the path tail — a legal **single-step forward** pick. |
| **Unlock cost** | Per-node authored bundle: gold + item stacks + required story flags (all must pass to enable the choice). |
| **Upgrade transaction** | Atomic: validate → pay costs → append one child id → `TryApplyFromSerializedState()` → log/feedback. |

---

## 3. Relationship to Phase 3 Spirit Imprint

| Phase 3 rule | Shaman NPC behavior |
|--------------|---------------------|
| Forward-only path | Shaman only **appends** one child of the current tail; never removes or swaps nodes. |
| Single-node advance | One successful dialog pick = **at most one** new id on `chosenPathNodeIds`. |
| `imprintRank == Count - 1` | After upgrade, rank remains derived from path length (no separate XP pool). |
| Sibling exclusivity | Offer only **children of tail**; exclusivity among siblings is resolved when the player **picks one** child (same as dev `DevTryAppendChild`). |
| Pattern B runtime | Upgrade mutates **`SpiritImprintRuntime`** on the **speaker**; `RacialLoadoutApplier` unchanged. |
| Preset v0 path | Barbarian prefabs may still ship with preset paths for tests; Shaman is the **player-facing** way to extend path in town. |

**Cross-reference:** Phase 3 §D2.2 — *“Later phase (not Phase 3): A special NPC … will authorize the next single-node extension.”* **This document is that NPC.**

---

## 4. NPC identity (placeholder — will change later)

| Field | v0 value | Notes |
|-------|----------|-------|
| **Display name** | **Shaman Barbarian** | Shown in dialog name plate. |
| **Stable id** | `shaman_barbarian` | `NpcDialogProfile.npcId`, stamp marker, logs. |
| **Race (folk)** | Barbarian | World sprite reads as Barbarian; not Human-derived. |
| **Prefab** | **`BarbarianNpc`** variant | Variant of `BarbarianPlayer.prefab` with player-only components stripped + `NpcController`; same pattern as `HumanNpc`. |
| **World sprite** | **New unused asset** | Must **not** reuse `NPC_Mira`, `NPC_Luc`, `NPC_Edda`, `NPC_Fenn`, `NPC_Greta`, or party field sprites. |
| **Portrait** | **New unused asset** | Must **not** reuse `Portrait_Mira`, `Portrait_Luc`, `Portrait_Edda`, `Portrait_Fenn`, `Portrait_Greta`, or `Portrait_Race_Barbarian` (party default). Suggested paths: `Assets/Art/NPC/Sprites/NPC_ShamanBarbarian.png`, `Assets/Art/Portraits/NPC/Portrait_ShamanBarbarian.png`. |
| **Narrative role** | Placeholder shaman | **Will be replaced** in a future content pass (name, portrait, sprite, placement, dialog flavor). Mechanics (cost pay + append node) should survive art/narrative swaps. |

---

## 5. Interaction model (locked)

| Rule | Detail |
|------|--------|
| **Open talk** | **`Enter`** while orthogonally adjacent + facing — same as [NPC dialog §3](../World/NPC-Dialog-Requirements.md). |
| **Speaker** | Active party leader provides `{partyName}` / `{speakerName}` and is the **only** actor whose imprint may be upgraded this session. |
| **Turn cost** | **No turn** for talk, browse choices, or successful upgrade. |
| **Blocks gameplay** | Uses `NpcDialogBoxUI.BlocksGameplay` (same stack as other dialog). |
| **Cancel** | **Cancel** choice or **Esc** closes dialog without charging or changing imprint. |
| **Safe zone** | Talk works in town safe zones; no combat side effects. |

```
Enter (adjacent + facing Shaman)
  → Resolve speaker race + SpiritImprintRuntime
  → Branch: non-Barbarian | maxed | offer upgrades
  → Choice: pick next node OR Cancel
  → On confirm (affordable): pay → append node → re-apply → success line → close
```

---

## 6. Dialog flows

### 6.1 — Non-Barbarian speaker

| Condition | Line (exact v0 copy) |
|-----------|----------------------|
| `speaker.stats.race != Race.Barbarian` | **“Hello. You are not a Barbarian.”** |

- Single **Line** node → advance → close.
- No choices, no cost inspection.

### 6.2 — Barbarian speaker — no `SpiritImprintRuntime`

| Condition | Line |
|-----------|------|
| Barbarian speaker but missing component / null graph | **“Your spirit imprint is not awakened.”** (implementation fallback; should not occur on `BarbarianPlayer` prefab) |

### 6.3 — Barbarian speaker — all upgrades taken

| Condition | Line (exact v0 copy) |
|-----------|----------------------|
| Path tail has **zero** child nodes in the bound `SpiritImprintGraph` | **“You have all the upgrades.”** |

- Single line → close.
- “All upgrades” means **no legal forward edge** from tail, not “every node in the graph visited.”

### 6.4 — Barbarian speaker — upgrade offer (dynamic)

**Prompt body** (template — `{speakerName}` optional):

> The spirits can deepen your imprint. Choose your next mark:
>
> {for each next node}
> **{displayName}** — {description} **Cost:** {formattedCost}
> {end for}

- **One paragraph block per next node** in the body (count = number of direct children of tail: **1, 2, 3, …**).
- `{description}` comes from `SpiritImprintNodeData.description` (fallback: `displayName`).
- `{formattedCost}` from unlock cost resolver (§8.4); if free, show **“Free”**.

**Choice list** (built at runtime):

| Choice label format | Behavior |
|---------------------|----------|
| **`{displayName}, {shortCostList}`** | Confirms upgrade to that node when **affordable**. |
| **`Cancel`** | Always enabled; closes without changes. |

- **`{shortCostList}`** — comma-separated costs only (no prose): e.g. `50 gold, Iron Ingot ×2, Requires flag: met_shaman`.
- **Order:** graph asset order of sibling nodes (stable); **Cancel** always **last**.
- **Affordable:** normal choice color; Enter/click runs transaction (§9).
- **Unaffordable:** **greyed out**, `interactable = false`, **skipped** by keyboard wrap (cannot focus); Enter does nothing if somehow focused.

**Success line** (after transaction):

> **“The mark is set.”** (or `{displayName} is bound.` — pick one in implementation; v0 either is fine)

**Failure line** (race if costs changed between open and confirm — should not happen if disabled choices work):

> **“You no longer have what the spirits require.”**

---

## 7. Eligibility & target actor (locked)

| Rule | Detail |
|------|--------|
| **R7.1** | Only the **speaker** (active leader) is checked for `Race.Barbarian`. |
| **R7.2** | Upgrade applies to **`SpiritImprintRuntime` on the speaker’s GameObject**, not other party Barbarians. |
| **R7.3** | Switching party leader before talk changes who can upgrade; **no** “pick which Barbarian” sub-menu in v0. |
| **R7.4** | Speaker must use the same **`SpiritImprintGraph`** asset referenced on their runtime (typically shared `BarbarianSpiritImprintSample` or production graph). |
| **R7.5** | Offer set = **all nodes** where `parentNodeId == tailId` (ignore nodes already on path — children of tail are never on path by tree definition). |

---

## 8. Data model — per-node unlock costs

### 8.1 — New fields on `SpiritImprintNodeData`

Extend each **non-root** node (cost on **entering** that node):

```csharp
[Serializable]
public struct SpiritImprintUnlockCost
{
    [Min(0)] public int gold;
    public SpiritImprintItemCost[] items;      // zero or more
    public SpiritImprintFlagCost[] storyFlags; // zero or more
}

[Serializable]
public struct SpiritImprintItemCost
{
    public ItemData item;
    [Min(1)] public int quantity;
}

[Serializable]
public struct SpiritImprintFlagCost
{
    public string flagId;
    public bool expectedValue; // default true — flag must be set
}
```

Add to `SpiritImprintNodeData`:

| Field | Purpose |
|-------|---------|
| **`unlockCost`** | Price to **append this node** from its parent along a valid path. |
| *(existing)* **`displayName`**, **`description`** | Dialog copy for offers. |

**Root node:** **`unlockCost` ignored** (root is starting state, not purchased).

### 8.2 — Example (sample graph)

| Node id | displayName | Example unlock cost |
|---------|-------------|---------------------|
| `tier1_str` | First Mark — Strength | 30 gold |
| `tier1_dex` | First Mark — Dexterity | 20 gold, 1 × `Giants_Blade` (extreme sample — normally junk item) |
| `tier2_constitution` | Second Mark — Constitution | 50 gold, flag `quest_skeleton_proof` completed |

Designers tune per node; siblings can differ.

### 8.3 — Affordability evaluation

Evaluate against **party-wide** pools (consistent with quests / shop):

| Cost type | Pass condition |
|-----------|----------------|
| **Gold** | `PartyCurrencyLedger` total ≥ `gold` |
| **Item** | Sum of `ItemData` quantity across **all party members’ carried** inventories ≥ required (equipped items **do not** count). |
| **Story flag** | `GameStoryFlagService.IsSet(flagId) == expectedValue` |

**All** specified cost lines must pass (**AND**). Empty cost bundle = free upgrade.

### 8.4 — Cost formatting (UI)

| Context | Format |
|---------|--------|
| **Body `formattedCost`** | Human-readable: `50 gold`, `Iron Ingot ×2`, `Requires: quest flag 'portal_opened'`. Omit zero sections. |
| **Choice `shortCostList`** | Comma-separated compact: `50 gold, Iron Ingot ×2, flag: portal_opened`. |
| **Free** | `Free` / empty short list |

---

## 9. Upgrade transaction (locked)

On confirm of an **enabled** choice targeting node **`N`**:

1. **Re-validate** affordability (party gold, items, flags).
2. **Re-validate graph:** `N.parentNodeId == tail` and append yields valid path (`ValidateAndNormalizePath`).
3. **Pay costs** atomically:
   - Deduct gold from `PartyCurrencyLedger`.
   - Remove item quantities from party carried inventories (order: speaker first, then other members — stable party index order).
   - **Do not** clear story flags (flags are gates, not consumed unless later design adds consumption).
4. **Append** `N` to speaker’s `chosenPathNodeIds`.
5. Call **`SpiritImprintRuntime.TryApplyFromSerializedState()`** (or production-safe public wrapper mirroring `DevTryAppendChild` success path).
6. Show success line → close dialog.
7. Log: `[SpiritImprint] {speaker} upgraded to '{N}' via Shaman; paid {cost summary}.`

| Rule | Detail |
|------|--------|
| **R9.1** | **Atomic** — if payment fails mid-transaction, **no** path change (rollback or pre-check only). |
| **R9.2** | **No partial item pay** — exact quantities or abort. |
| **R9.3** | **One node only** — never append multiple ids. |
| **R9.4** | **No refund** on confirm (permanent imprint policy). |
| **R9.5** | Persist through **`RunPartyPersistence`** / existing party save hooks same as other imprint path changes. |

---

## 10. UI — disabled dialog choices

**Gap:** `NpcDialogBoxUI` v0 choices do not support disabled options.

| Requirement | Detail |
|-------------|--------|
| **U10.1** | Extend choice API with **`DialogChoiceOptionData.enabled`** (default `true`) or parallel **`DialogChoiceStep.OptionStates`**. |
| **U10.2** | Disabled: button **`interactable = false`**, label color **~50% grey**, not selectable via keyboard navigation. |
| **U10.3** | **`Cancel`** always enabled. |
| **U10.4** | Disabled choice label **still shows** full `{displayName}, {shortCostList}` so player sees what they’re missing. |

---

## 11. Town placement (v0)

| Field | Suggested value |
|-------|-----------------|
| **Marker id** | `shaman_barbarian` |
| **Cell** | **`(10, 6, 0)`** or next free plaza cell — **≥2 cells** from existing NPCs; finalize in stamp authoring. |
| **Setup** | Extend `Stamp_TownPlaza_20x20` + `TownNpcSetupPhase` (same pipeline as [NPC dialog §4](../World/NPC-Dialog-Requirements.md), [Shop §4](../World/Shop-NPC-Requirements.md)). |

Shaman is **not** a shop NPC — standard `NpcController` + custom session handler (or specialized `SpiritImprintShamanController`).

---

## 12. Services & code layout (recommended)

| Piece | Responsibility |
|-------|----------------|
| **`SpiritImprintUpgradeService`** (static or DDOL) | Query next nodes, evaluate affordability, format costs, execute transaction. |
| **`SpiritImprintShamanDialogSession`** | Replaces generic `NpcDialogSession` for this NPC; builds dynamic `DialogChoiceStep`. |
| **`SpiritImprintShamanNpcController`** | `INpcTalkTarget`; wires profile or code-driven flow. |
| **Graph data** | `unlockCost` on `SpiritImprintNodeData`; sample values in `BarbarianSpiritImprintSample.asset`. |
| **Tests** | Unit tests for affordability, append validation, payment deduction, exclusivity preserved. |

**Do not** encode upgrade logic only in dialog ScriptableObject nodes — offer set is **runtime-derived** from graph + speaker path.

---

## 13. Acceptance criteria

| ID | Test |
|----|------|
| **AC1** | Human (non-Barbarian) leader talks → hears **“Hello. You are not a Barbarian.”** → no choices. |
| **AC2** | Barbarian at root-only path with **two** tier-1 children → body shows **two** descriptions with costs → **two** upgrade choices + **Cancel**. |
| **AC3** | Barbarian with **one** child available → **one** upgrade choice + **Cancel**. |
| **AC4** | Barbarian at max depth (no children) → **“You have all the upgrades.”** |
| **AC5** | Option unaffordable (missing gold/item/flag) → choice **greyed out** and **not confirmable**. |
| **AC6** | Affordable pick → gold/items deducted → `chosenPathNodeIds` extended by **one** → stat/passive effects apply immediately. |
| **AC7** | After picking `tier1_str`, a later talk offers **`tier2_constitution`** only (not `tier1_dex`) — forward-only + exclusivity. |
| **AC8** | Upgrade survives **town → dungeon → town** within the same run (party persistence). |
| **AC9** | Shaman uses **new** sprite + portrait not shared with existing town NPCs. |

---

## 14. Implementation checklist

- [x] Add `SpiritImprintUnlockCost` (+ item/flag rows) to `SpiritImprintNodeData`
- [x] Author costs on `BarbarianSpiritImprintSample` (and production graph when ready)
- [x] `SpiritImprintUpgradeService` — query, afford, pay, append
- [x] `NpcDialogBoxUI` — disabled/greyed choice support
- [x] `SpiritImprintShamanDialogSession` + controller
- [x] New sprite, portrait, `BarbarianNpc` prefab, `Portrait_ShamanBarbarian` asset *(run **JRogue → Town → Create Shaman Barbarian Pack** in Unity if assets are missing)*
- [x] Town stamp marker + `TownNpcSetupPhase` spawn
- [x] Unit tests (afford, pay, append, reject wrong race)
- [x] Cross-link from [Phase 3](Phase3-Requirements.md) §D2.2 / §7.3 to this doc

---

## 15. Resolved design decisions

| # | Decision | Locked answer |
|---|----------|---------------|
| **Q1** | Who gets upgraded? | **Speaker** (active party leader) if Barbarian. |
| **Q2** | Who can open the menu? | Same — leader must be Barbarian at talk time. |
| **Q3** | Cost scope | **Party-wide** gold and carried items; **global** story flags. |
| **Q4** | Nodes offered | **All direct children** of path tail (0 → maxed message). |
| **Q5** | NPC identity | **Placeholder** “Shaman Barbarian”; art/narrative **will change**. |
| **Q6** | Cancel | Always available; no cost. |
| **Q7** | Story flags | **Required** for unlock; **not consumed** on pay (v0). |

---

## 16. Future (out of v0)

- Replace placeholder Shaman with lore-final NPC (name, art, dialog voice).
- Town phase / calendar gates on Shaman availability ([Town time](../World/Town-Time-And-Calendar-Requirements.md)).
- Consume quest items on upgrade (optional `consumeFlags` / item destruction rules).
- Character sheet imprint tree UI (Phase 3 §7.4) — Shaman remains one upgrade vector.
