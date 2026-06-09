# Elf — Fairy Stone spirit contracts (requirements)

**Purpose:** Specify how Elves **gain new Elemental Spirit contracts** using **Fairy Stones** — consumable items sold by a town NPC — in the same progression slot that [Barbarian Spirit Imprint — Shaman NPC](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) fills for Barbarians. This doc implements the **“form contract”** gate deferred from [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md) §5.8 (F4.15).

**Inspiration:** *Surviving the Game as a Barbarian* — Barbarians buy **spirit stones** from a shaman to advance their imprint path; Elves buy **Fairy Stones** and attempt to **contract** a random elemental spirit. Barbarian = deterministic tree node; Elf = probabilistic new spirit at **contract level 1**.

**Status:** Implemented (v0) — spirit **leveling** via Fairy Stone remains a future feature.

**Depends on:** [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md) (`ElementalSpiritDefinition`, `ElementalSpiritContractsRuntime`, cumulative level payloads, summon/upkeep), [Shop NPCs](../World/Shop-NPC-Requirements.md) (party gold, sell-only stock, `ShopTransactionService`), [NPC dialog](../World/NPC-Dialog-Requirements.md) (`NpcDialogBoxUI`), [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) (Use action, greyed-out unusable rows), `InventoryItemUse`, `PartyManager`, `CharacterStats.race`, `ItemData` / `ItemInstance`, `GameplayModalGate`.

**Related:** [Barbarian Spirit Imprint — Shaman NPC](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) (parallel town progression NPC). [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (future Elf body — read-only contract roster). [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) (summoned spirit actives).

**Explicitly out of scope (v0):** **Leveling** an existing spirit **contract instance** via Fairy Stone (separate future gate); respec / remove contracts; Fairy Stone drops in dungeon loot; save/load contract state across game sessions beyond existing party persistence; bespoke full-screen “contract ceremony” UI; **roster size cap** (roster is **unlimited**); cross-spirit exclusivity (one Fire only); **Race.Fairy** folk (enum exists — no content); gamepad layout.

---

## Locked decisions

| # | Decision |
|---|----------|
| **L1** | Item display name: **Fairy Stone** (Elf flavor). Internal id: `fairy_stone`. |
| **L2** | Town **sell-only** NPC stocks Fairy Stone at **1 gold** each (party gold). |
| **L3** | Fairy Stone **Use** is **Elf-gated**: if **no** `Race.Elf` in the live party, the item is **greyed out** and cannot be used. |
| **L4** | If at least one Elf is in the party, **Use** opens a **confirmation dialog** listing **every live party Elf** + **Cancel** at the bottom. |
| **L5** | **Cancel** or **Esc** closes the dialog; **does not consume** the stone. |
| **L6** | On confirmed Elf pick: **consume one Fairy Stone**, then **50%** chance that Elf **forms a new contract** with a **random** elemental spirit at **contract level 1**. |
| **L7** | **Failed 50% roll still consumes the stone** (locked). Show failure feedback line. |
| **L8** | **Duplicates allowed** — success may add another roster entry for a spirit the Elf **already** contracts (same `spiritId`, independent **contract instance**). **No cap** on roster size. |
| **L9** | New contract **does not auto-summon**; player **summons** via **hotbar** summon/dismiss entry (§5.10 parent Elf doc). |
| **L13** | **Summon / dismiss** is a **hotbar ability** per contract instance — not a separate menu, not inventory Use. |
| **L14** | **v0 test authoring:** level 1 active on **Ember Warden** and **Tide Shard** = **`SuddenStrength_Standard`** (parent Elf doc §5.12). |
| **L15** | **Hotbar active dedup:** same `AbilityAction` from multiple summoned instances → **one** hotbar slot (parent Elf doc §5.11). |
| **L10** | **Fairy Stone Use** (inventory) = **no turn consumed** (align with shop / inventory management). |
| **L11** | **Summon / dismiss** elemental spirits = **no turn consumed** (parent Elf doc F4.5 / F4.8). |
| **L12** | **Spirit actives** — turn cost is **per ability** via `ElementalSpiritActiveEntry.consumesTurn` (some yes, some no); independent of Fairy Stone Use and summon/dismiss. |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Town access** — Player can buy Fairy Stones cheaply (1 gold) from a dedicated NPC. |
| **G2** | **Race gate** — Item clearly unusable without an Elf in the party (greyed out + tooltip). |
| **G3** | **Target picker** — When multiple Elves are in the party, player chooses which Elf attempts the contract. |
| **G4** | **Probabilistic contract** — 50% success appends a new **contract instance** at **level 1** (duplicates of same spirit allowed). |
| **G5** | **Data-driven pool** — Random spirit drawn uniformly from full `ElementalSpiritRegistry`. |
| **G6** | **Feedback** — Success, failure, and cancel paths each have distinct player-visible messages. |
| **G7** | **Parent runtime integration** — Mutations go through a single service API (mirror `SpiritImprintUpgradeService` discipline). |
| **G8** | **Content runway** — Document current spirits + planned expansion (including **multiple spirits per element**). |

---

## 2. Glossary — “Use” and related terms

Three different player actions share the word **use** in conversation; this doc distinguishes them explicitly.

| Term | Meaning | Turn cost |
|------|---------|-----------|
| **Use (Fairy Stone)** | Inventory **Use** on a Fairy Stone → Elf picker dialog → contract attempt (§6). | **None** (L10) |
| **Summon** | Bring a **contracted spirit instance** into the **summoned** state via **hotbar** (§5.10 parent Elf doc). Pay summon Soul Power. | **None** (L11) |
| **Dismiss** | End a summoned **instance** via the **same hotbar entry**. | **None** (L11) |
| **Use (spirit active)** | Execute a **summoned** instance’s combat active from the hotbar (separate assignable from summon/dismiss). | **Per active** — §2.1 |

| Term | Meaning |
|------|--------|
| **Fairy Stone** | Consumable `ItemData` that triggers the contract attempt flow. |
| **Fairy Merchant** | Town NPC who **sells** Fairy Stones. |
| **Contract attempt** | One confirmed Fairy Stone Use: consume stone → 50% roll → maybe append instance. |
| **Contract roster** | Ordered list of **contract instances** on `ElementalSpiritContractsRuntime` — **unlimited** size (L8). |
| **Contract instance** | One roster row: `{ contractInstanceId, spirit, contractLevel }`. Same `spiritId` may appear **many times**. |
| **Initial level** | **Contract level 1** for a newly formed instance. |
| **Eligible random pool** | **Entire** `ElementalSpiritRegistry` on every success roll (no duplicate filtering). |
| **Target Elf** | Party member chosen in the Fairy Stone dialog. |

### 2.1 — Spirit active turn costs

While a spirit instance is **summoned**, its actives obey **`ElementalSpiritActiveEntry`** (parent [Elf doc](Elf-ElementalSpirit-Contracts-Requirements.md) D4.3):

| Flag | Meaning |
|------|--------|
| **`consumesTurn = false`** | Active use **does not** end the actor’s turn (e.g. Ember Weapon Imbue). |
| **`consumesTurn = true`** | Active use **does** consume the turn (e.g. Tide Mend at level 1). |
| **`repeatableSameTurn`** | May use multiple times same turn when true (subject to Soul Power / `CanExecute`). |

| Spirit / active | Contract level | Turn cost on **active** use |
|-----------------|----------------|----------------------------|
| **Sudden Strength** (v0 L1 test active) | 1 | Per [Sudden Strength essence](../Essence/Sudden-Strength-Essence-Requirements.md) |
| Ember Weapon Imbue | 2–3 (Ember Warden) | **No** |
| Tide Mend | 2+ (Tide Shard) | L2+: **No**; if authored at L1 elsewhere, **Yes** |

**Summon**, **dismiss**, and **Fairy Stone Use** never consult `consumesTurn`.

---

## 3. Relationship to Elf Elemental Spirit contracts

| Parent rule ([Elf doc](Elf-ElementalSpirit-Contracts-Requirements.md)) | Fairy Stone behavior |
|--------------------------------------------------------------------------|-------------------|
| F4.15 — form contract via event/NPC/item | **This doc is that item gate (v0).** |
| F4.2 — cannot summon uncontracted spirit | Unchanged; new contract adds to roster only. |
| F4.16 — level spirit (later) | **Out of v0** — Fairy Stone does **not** raise an instance’s level. |
| O4 — duplicate `spiritId` in roster | **Yes** — each success adds a **new instance**; unlimited roster (L8). **Runtime refactor:** summon/dismiss/hotbar must key by **`contractInstanceId`**, not `spiritId` alone. |
| Cumulative level payloads | New instance at level 1 grants level-1 row when **that instance** is summoned. |
| Preset v0 roster on prefab | `ElfPlayer` may start with `[]`; contracts gained via stones + future gates. |

**Barbarian parallel:**

| Barbarian (Shaman) | Elf (Fairy Stone) |
|--------------------|-------------------|
| Spirit stone / gold at Shaman | Fairy Stone (1 gold at merchant) |
| Pick **next imprint node** (deterministic) | **Random spirit** from pool (50%) |
| Speaker = active leader only | **Any party Elf** choosable on Use |
| Append one graph node | Append new **contract instance** `{ spirit, level 1 }` |

---

## 4. Fairy Stone item

### 4.1 — Data (`FairyStoneItemData` or flag on `ItemData`)

| Field | v0 value |
|-------|----------|
| **`itemId` / asset name** | `fairy_stone` |
| **Display name** | **Fairy Stone** |
| **Description** | Short flavor + mechanic hint: *“An Elf may attempt to contract a new elemental spirit. Success is not guaranteed.”* |
| **`buyValue`** | **1** (player pays merchant) |
| **`sellValue`** | **0** or **1** (designer choice; recommend **0** — not meant to be farmed) |
| **Stack size** | ≥ 1 (allow stacks in subspace) |
| **Consumable** | **Yes** — removed on confirmed attempt (success or failure) |
| **Weight** | Light (e.g. 0.1) |
| **Icon** | New unused asset (fairy / crystal motif); placeholder acceptable v0 |

### 4.2 — Usability rules (`InventoryUsability` / `InventoryItemUse`)

| Condition | Use allowed? | UI |
|-----------|--------------|-----|
| No live party member with `Race.Elf` | **No** | Row **greyed out**; inspect hint: *“Requires an Elf in the party.”* |
| At least one Elf in party | **Yes** | Normal row; Use opens §6 dialog |
| Not player turn / action already spent | Per global item policy (recommend **allow** per L10) | If denied, show standard turn message |
| Safe zone only? | **No extra gate** — usable in town and dungeon unless global policy blocks |

**Note:** Unlike healing potions, Use is **not** restricted to the item **owner** being the target Elf — any party member may carry the stone; the dialog picks **which Elf** attempts the contract.

---

## 5. Fairy Merchant NPC

### 5.1 — Identity (placeholder)

| Field | v0 value |
|-------|----------|
| **Display name** | **Fairy Merchant** (placeholder) |
| **Stable id** | `fairy_merchant_elf` |
| **Race (folk)** | Elf |
| **Prefab** | `ElfNpc` variant + `ShopNpcController` |
| **World sprite / portrait** | **New unused assets** (do not reuse party `Portrait_Elf` or existing town NPC art) |

### 5.2 — Shop behavior

| Rule | Detail |
|------|--------|
| **Mode** | **Sell-only** (player buys from NPC) — same pattern as shop NPC 5 in [Shop NPC doc](../World/Shop-NPC-Requirements.md) |
| **Stock** | **Unlimited** or high cap **Fairy Stone** stacks at **buyValue = 1 gold** |
| **NPC wallet** | Not required for sell-only (player pays party gold via `ShopTransactionService`) |
| **Greeting** | v0: *“Hello. Fairy Stones, one gold each.”* → **Yes** opens shop / **No** closes |
| **Turn cost** | None |
| **Placement** | Town plaza marker `town_fairy_merchant` (cell TBD in stamp authoring) |

---

## 6. Use flow — Elf picker dialog

### 6.1 — Trigger

1. Player selects Fairy Stone in inventory and presses **Use** (or bound Use action).
2. `InventoryItemUse` branches to `FairyStoneUseService.TryBeginUse(row)`.
3. Service verifies party has ≥ 1 eligible Elf (`Race.Elf` + `ElementalSpiritContractsRuntime` present).
4. Opens **`FairyStoneContractDialogUI`** (or reuses `NpcDialogBoxUI.ShowChoice` with dynamic options).

### 6.2 — Dialog layout

| Element | Content |
|---------|---------|
| **Title / prompt** | *“Use Fairy Stone on which Elf?”* |
| **Choices** | One button per **live party Elf**, label = `{DisplayName}` (optional: portrait chip) |
| **Cancel** | Bottom choice **Cancel** + **Esc** |
| **Disabled Elves** | None — all live Elves are valid targets |

### 6.3 — Outcomes

```
Use (Fairy Stone)                          [no turn cost]
  → No Elf in party? → greyed out (never reaches dialog)
  → Dialog: pick Elf or Cancel
  → Cancel / Esc → close, stone retained
  → Confirm Elf:
       → consume 1 stone (always, before roll)
       → if registry empty → failure line (edge case; should not ship)
       → else roll 50%
            → fail → “The stone crumbles; no spirit answers.”
            → success → uniform random spirit from full registry
                 → TryFormContract(elf, spirit, level: 1) → new contractInstanceId
                 → “{Elf} forms a contract with {SpiritName}!”

Summon / Dismiss (separate flow)           [no turn cost — parent Elf doc]

Use (spirit active) from hotbar            [consumesTurn per ability row]
```

### 6.4 — Feedback lines (v0 copy)

| Case | Line |
|------|------|
| Roll failed (50%) | **“The stone crumbles to dust. No spirit answers.”** |
| Registry empty (authoring error) | **“The stone has nothing left to offer.”** |
| Success (including duplicate spirit type) | **“{Elf} forms a contract with {SpiritDisplayName}!”** |
| Cancel | (no line) |

---

## 7. Contract transaction (code)

### 7.1 — New API on runtime / service

Recommend **`ElementalSpiritContractService`** (static or instance) wrapping runtime mutation:

```csharp
bool TryFormContract(BaseActor elf, ElementalSpiritDefinition spirit, int initialLevel, out string contractInstanceId, out string failureReason);
```

| Step | Requirement |
|------|-------------|
| Validate | Target is `Race.Elf`, has `ElementalSpiritContractsRuntime`, spirit non-null, `initialLevel == 1` |
| Mutate | Append new `ElementalSpiritContractPreset` with fresh **`contractInstanceId`** (GUID or monotonic id), `{ spirit, contractLevel = 1 }` |
| Duplicates | **Allowed** — same `spiritId` may appear on multiple roster rows |
| Re-apply | If that **instance** was summoned (edge case), refresh payloads — normally not summoned on form |
| Log | `[FairyStone] {elf} contracted {spiritId} instance {contractInstanceId}` |

**Runtime follow-up (parent Elf doc):** `summonedSpiritIds` becomes **`summonedContractInstanceIds`** (or parallel map) so two Ember Wardens can be contracted with only one summoned; hotbar bindings reference **instance + active**, not `spiritId` alone.

### 7.2 — Random selection

| Field | Requirement |
|-------|-------------|
| **Registry** | `ElementalSpiritRegistry` listing all `ElementalSpiritDefinition` assets eligible for random contract |
| **Filter** | **None** — full registry every success roll (duplicates OK) |
| **RNG** | `UnityEngine.Random.value < 0.5f` for success; on success `Random.Range(0, registry.Count)` |
| **Stone on fail** | **Always consumed** on confirm, success or fail (L7) |

### 7.3 — Persistence

Contract list lives on **`ElementalSpiritContractsRuntime`** serialized state — must participate in existing **party/run persistence** when that hook exists (same gap as parent Elf doc G5).

---

## 8. Elemental spirit catalog

### 8.1 — Implemented today (random pool candidates)

Only **two** spirits are authored. **Earth** and **Wind** elements exist in `ElementalElement` but have **no** spirit assets yet.

**v0 test authoring (locked — parent §5.12):** level **1** active on **both** spirits = **`SuddenStrength_Standard`**. Enables Fairy Stone + duplicate-instance + hotbar dedup testing without bespoke per-spirit L1 kits.

#### Ember Warden (`ember_warden`) — **Fire**

| Field | Value |
|-------|-------|
| **Max contract level** | 3 |
| **Summon cost** | 2 Soul Power |
| **Upkeep** | 1 Soul Power / turn |
| **Asset** | `Assets/Data/Racial/Elf/ElementalSpirits/EmberWarden.asset` |

| Level | Stat modifiers (cumulative while summoned) | Passive | Active |
|-------|---------------------------------------------|---------|--------|
| **1** | +1 Dexterity | Spirit Dexterity (+1 Dex) | **Sudden Strength** — `SuddenStrength_Standard`; **v0 test active** (§5.12) |
| **2** | +1 Dexterity (total **+2 Dex** from levels 1–2) | Same Dex passive | **Ember Weapon Imbue** — toggle; **no turn cost**; **repeatable same turn**; 0 SP; **+2 fire weapon damage** |
| **3** | +1 Strength (total **+2 Dex, +1 Str**) | Same Dex passive | Same Ember Weapon Imbue |

#### Tide Shard (`tide_shard`) — **Water**

| Field | Value |
|-------|-------|
| **Max contract level** | 2 |
| **Summon cost** | 2 Soul Power |
| **Upkeep** | 1 Soul Power / turn |
| **Asset** | `Assets/Data/Racial/Elf/ElementalSpirits/TideShard.asset` |

| Level | Stat modifiers (cumulative while summoned) | Passive | Active |
|-------|---------------------------------------------|---------|--------|
| **1** | +1 Wisdom | Spirit Wisdom (+1 Wis) | **Sudden Strength** — `SuddenStrength_Standard`; **v0 test active** (§5.12) |
| **2** | +1 Wisdom (total **+2 Wis**) | Same Wis passive | **Tide Mend** — heal **5 HP**; **2 SP**; **does not consume turn** |

### 8.2 — Expansion required (content backlog)

The random pool should grow via **sub-categories within each element** — e.g. **two Fire spirits with different kits** (Ember Warden = weapon imbue; a second Fire spirit = burn / DoT / area). Same pattern for Water, Earth, Wind. Duplicates in roster then mean “another copy of that spirit type at level 1,” not “blocked content.”

| Priority | Item | Notes |
|----------|------|-------|
| **P0** | **Earth spirit** (≥1 definition) | First earth sub-category |
| **P0** | **Wind spirit** (≥1 definition) | First wind sub-category |
| **P1** | **Second Fire spirit** | Different abilities from Ember Warden — **element sub-category** |
| **P1** | **Second Water spirit** | Different abilities from Tide Shard |
| **P1** | **Third+ spirits per element** | As design expands; all registered in pool |
| **P2** | **Deepen per-spirit level tables** | Ember L3+, Tide L3+, etc. |
| **P2** | **Level instance (F4.16)** | Separate gate — raises **one instance’s** contract level |
| **P3** | **Racial menu Elf body** | Read-only instance list + levels |

**Designer checklist per new spirit:**

1. `ElementalSpiritDefinition` with unique `spiritId`, element, `maxLevel`, summon/upkeep costs.
2. Level rows 1…N each with ≥ 1 passive + ≥ 1 active (`ElementalSpiritLevelData`).
3. Register asset in **random contract registry**.
4. Add to acceptance test pool fixture.

---

## 9. UI & inventory integration

| Surface | Requirement |
|---------|-------------|
| **Inventory list** | Fairy Stone row greyed when `!PartyHasElf()` |
| **Inspect pane** | Elf requirement + 50% chance + note that duplicate spirit **types** are allowed |
| **Use dialog** | Modal; `BlocksGameplay` while open |
| **Message log** | Success / failure lines also posted to `MessageHistory` if available |

---

## 10. Services & code layout (recommended)

| Type | Responsibility |
|------|----------------|
| `FairyStoneItemData` | Item marker / metadata |
| `ElementalSpiritRegistry` | Authorable list of spirits eligible for random contract |
| `ElementalSpiritContractService.TryFormContract` | Validate + append preset |
| `FairyStoneUseService` | Usability check, dialog launch, consume + roll |
| `FairyStoneContractDialogUI` | Elf picker (or extend `NpcDialogBoxUI`) |
| `InventoryItemUse` | Branch on `FairyStoneItemData` |
| `InventoryUsability` | Grey-out when no Elf |
| `FairyMerchant` shop definition asset | Stock: Fairy Stone @ 1 gold |

---

## 11. Acceptance criteria

| ID | Given | When | Then |
|----|-------|------|------|
| **AC1** | Party has no Elf | View Fairy Stone in inventory | Use disabled / greyed; hint shown |
| **AC2** | Party has 2 Elves, player owns stone | Use → pick Elf A → Cancel | Dialog closes; stack unchanged |
| **AC3** | Same | Confirm Elf A; RNG success | Stone −1; Elf A gains **new instance** at level 1; success message |
| **AC4** | Elf A already has Ember Warden; roll Ember Warden again | Confirm | Stone −1 on success path; **second** Ember instance added |
| **AC5** | RNG failure (50%) | Confirm | Stone −1; failure message; **no** new instance |
| **AC6** | Merchant in town | Buy Fairy Stone | Party gold −1; stone added |
| **AC7** | New instance formed | Bind summon entry on hotbar; press | Instance summons/dismisses; level-1 payloads apply while summoned |
| **AC8** | Three instances with L1 Sudden Strength summoned | View hotbar pool | **One** Sudden Strength active entry; **three** summon toggles |
| **AC9** | Ember L2+ / Tide L2+ | Use distinct actives | Separate hotbar entries (Imbue, Mend) when applicable |

---

## 12. Implementation checklist

- [ ] `FairyStoneItemData` + `fairy_stone` asset
- [ ] `ElementalSpiritRegistry` + include Ember Warden + Tide Shard
- [ ] `ElementalSpiritContractService.TryFormContract`
- [ ] `FairyStoneUseService` + dialog UI
- [ ] `InventoryItemUse` + `InventoryUsability` hooks
- [ ] Fairy Merchant NPC + shop definition (1 gold)
- [ ] Town stamp marker + setup phase
- [ ] Hotbar: `ElementalSpiritSummon` entries per contract instance ([Ability hotbar §8.1](../UI/Ability-Hotbar-Requirements.md))
- [ ] Authoring: wire **Sudden Strength** as L1 active on Ember Warden + Tide Shard (§5.12)
- [ ] Hotbar: dedupe spirit actives by ability asset (§5.11)
- [ ] Runtime: **contract instance ids** + summon/dismiss/hotbar keyed by instance (parent Elf doc O4)
- [ ] Unit tests: form contract, **duplicate spirit type allowed**, failed roll consumes stone, 50% roll (injected RNG)
- [ ] Content backlog: Earth + Wind spirits (§8.2)

---

## 13. Resolved decisions

| # | Question | Decision |
|---|----------|----------|
| **O1** | Consume stone on failed 50% roll? | **Yes** (L7) |
| **O2** | Duplicate spirit types in roster? | **Yes** — unlimited instances (L8) |
| **O3** | Fairy Stone Use during combat? | **Yes**, no turn cost (L10) |
| **O4** | Item name | **Fairy Stone** in UI |
| **O5** | Success rate | v0 fixed **50%** |
| **O6** | Spirit preview before confirm? | **No** v0 |
| **O7** | Define “Use” | Three meanings — §2 |
| **O8** | L1 test active | **Sudden Strength** on both v0 spirits (§5.12) |
| **O9** | Hotbar active dedup | One slot per unique ability asset (§5.11) |

---

## 14. Relation to other docs

| Doc | Relationship |
|-----|--------------|
| [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md) | Parent runtime; §5.8 F4.15 fulfilled here |
| [Barbarian Spirit Imprint — Shaman NPC](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) | Structural template for town gate + service split |
| [Shop NPCs](../World/Shop-NPC-Requirements.md) | Merchant buy flow |
| [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) | Future read-only Elf contract roster |
