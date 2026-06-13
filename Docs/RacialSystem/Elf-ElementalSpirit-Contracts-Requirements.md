# Elf — Elemental Spirit contracts (requirements)

Elves use a racial subsystem distinct from **Barbarian Spirit Imprint**: instead of a **single forward-only tree** of permanent picks, an Elf **forms contracts** with **Elemental Spirits** and **summons** them in play. Contracted spirits grant **passives and actives only while summoned**; summoning and upkeep cost **Soul Power** (same resource as essences: `CharacterStats.currentSoulPower`).

**Subsystem kind (code):** `RacialSubsystemKind.ElfElementalContracts` (see `RacialSubsystemKind.cs`).

**Depends on:** Phase 1–2 (folk, `RacialLoadoutApplier`, stacking-by-source), Phase 4 optional (body-capability contributions if a spirit or toggle grants anatomy overrides).

**Contrast:** [Phase 3 — Barbarian Spirit Imprint](Phase3-Requirements.md) (tree, forward-only path, effects from **chosen path nodes** while imprint is “active” on the actor).

---

## 1. Goals

**G1 — Vertical slice (v0)**  
An Elf actor can have a **predetermined set of contracted Elemental Spirits** (ids + **levels** per spirit), **summon** and **dismiss** them under the rules below, and **use only the abilities of currently summoned spirits**—without runtime “form contract” or “level spirit” flows.

**G2 — Data-driven spirits**  
Designers author spirits as assets: element, per-level passives/actives, max level, summon/upkeep costs, and per-active execution flags—without new `MonoBehaviour` types per spirit.

**G3 — Summon lifecycle correctness**  
Summoning applies spirit passives (and enables actives); dismissal or upkeep failure **fully removes** spirit contributions (no orphaned stat mods). Multiple spirits may be summoned at once.

**G4 — NPC / preset parity**  
Elves (player and NPC) use **preset contract loadouts** before play, analogous to Phase 3 **preset `chosenPathNodeIds`** for Barbarians.

**G6 — Playable Elf prefab**  
A dedicated **`ElfPlayer`** actor prefab exists (same family as `HumanPlayer` / `BarbarianPlayer`) so designers can place or spawn a correctly configured Elf without hand-wiring components.

**G5 — Persistence (v0 minimum)**  
Serialize which spirits are **contracted** (preset list + levels), which are **currently summoned**, and any **toggle state** required by repeatable actives; safe defaults on load.

---

## 2. Conceptual model (vs Barbarian)

| | **Barbarian — Spirit Imprint** | **Elf — Elemental Spirits** |
|--|-------------------------------|-----------------------------|
| **Structure** | Single **tree**; one **forward-only path** | **Many** spirits; **no tree** between spirits |
| **Progression (later)** | One node per event along path | **Per-spirit level** via event/NPC/item |
| **When abilities apply** | From **committed path nodes** (always-on for chosen path) | Only while spirit is **summoned** |
| **Ongoing cost** | None (path is permanent) | **Upkeep Soul Power per turn** per summoned spirit |
| **Activation cost** | N/A for path | **Summon** costs Soul Power once per summon |
| **Turn cost for management** | N/A | **Summon / dismiss any number** = **no turn consumed** |
| **v0 authoring** | Preset path on actor/prefab | Preset **contract list + levels** on actor/prefab |

---

## 3. Glossary

| Term | Meaning |
|------|--------|
| **Elemental Spirit** | A data-defined entity (asset) with element, level table, costs, and abilities. |
| **Element** | One of **Fire**, **Water**, **Earth**, **Wind**. Used for grouping/content; **not** a hard limit of one spirit per element. |
| **Contract** | Elf is allowed to use a given spirit (appears in their **contract roster**). v0: roster is **preset**; later: gained via [Fairy Stone](Elf-Fairy-Stone-Spirit-Contract-Requirements.md) / event / NPC. |
| **Contract instance** | One row in the roster: `{ contractInstanceId, spiritId, contractLevel }`. The same spirit **type** may appear **multiple times** (no roster cap). |
| **Contract level** | Integer **1 … maxLevel** for **that instance** on this Elf. Determines which **level rows** apply when **that instance** is summoned. |
| **Summon** | Pay **summon cost** → spirit **instance** enters **summoned** state → passives apply, actives available. Triggered via **hotbar summon/dismiss entry** (§5.10). |
| **Dismiss** | Spirit **instance** leaves summoned state → passives removed, actives unavailable, toggles cleared. Same hotbar entry when instance is summoned. |
| **Upkeep** | Soul Power paid **each turn** (see §5.3) while summoned; failure → **auto-dismiss**. |
| **Soul Power** | Shared resource with essences (`currentSoulPower`). Summon, upkeep, and spirit active costs use Soul Power. |
| **Use (spirit active)** | Execute a **summoned** spirit instance’s combat active from the hotbar — **`consumesTurn`** per ability row. **Distinct** from the summon/dismiss hotbar entry (§5.10). |

---

## 4. Data model

### D4.1 — `ElementalSpiritDefinition` (ScriptableObject or equivalent)

Per **spirit** (stable **`spiritId`** for saves):

| Field | Requirement |
|-------|-------------|
| **`spiritId`** | Stable string (or int) for saves and UI. |
| **Display** | Name, description, icon optional. |
| **`element`** | `Fire` \| `Water` \| `Earth` \| `Wind`. |
| **`maxLevel`** | ≥ 1; may differ per spirit. |
| **`levels[]`** | One row per level **1 … maxLevel** (see D4.2). |
| **`summonSoulPowerCost`** | Soul Power to **summon** (once per summon action). |
| **`upkeepSoulPowerPerTurn`** | Soul Power due **each turn** while summoned. |

**Content rules:**

- Different spirits **may** share the same element and **different** ability sets.
- Two spirits of the same element **may** share some abilities (same `PassiveEffect` / `AbilityAction` references).
- Different spirits **may** have different **`maxLevel`**.

### D4.2 — Per-level row (`ElementalSpiritLevelData`)

For each level **L** in **1 … maxLevel**:

| Field | Requirement |
|-------|-------------|
| **Passives** | List, **at least one** `PassiveEffect` reference (v0 minimum). |
| **Actives** | List, **at least one** `AbilityAction` reference (v0 minimum). |

Passives/actives on level **L** apply only when the Elf’s **contract level** for that spirit is **≥ L** **and** the spirit is **summoned** (see F4.4).

**Stacking:** Apply passives and stat effects with a **stable source** per spirit (e.g. `ElementalSpirit:{spiritId}`) so dismissal and level changes do not orphan modifiers (same discipline as Spirit Imprint per-node sources and essences).

### D4.3 — Active ability execution metadata

Each active on a spirit level (or referenced from a wrapper row) must declare:

| Flag | Meaning |
|------|--------|
| **`soulPowerCost`** | Uses existing `AbilityAction.soulPowerCost` when &gt; 0; **0** = no Soul Power on use. |
| **`consumesTurn`** | If **true**, using this active **ends** (or consumes) the actor’s turn per global combat rules; if **false**, use does **not** consume a turn. |
| **`repeatableSameTurn`** | If **true**, the active may be used **multiple times in one turn** (subject to `CanExecute` / resources). If **false**, normal once-per-turn (or cooldown) rules apply. |

**Toggle example (required to be representable):**  
A Fire spirit active **“imbue weapon with Fire damage”**: `consumesTurn = false`, `repeatableSameTurn = true`, `soulPowerCost` as authored. **First use** activates the effect; **second use same turn** **deactivates** it. Implementation may be a dedicated `AbilityAction` subclass or a small toggle state keyed by `(spiritId, activeId)` on the Elf runtime—**data must record** toggle behavior (e.g. `activeKind = Toggle` vs `Instant`).

Other actives may be one-shot, sustained buffs, or targeted spells; flags above are the **minimum contract** for the engine.

### D4.4 — Elf runtime state (`ElementalSpiritContractsRuntime` or equivalent)

On eligible actors (`Race.Elf`, subsystem `ElfElementalContracts`):

| State | Description |
|-------|-------------|
| **`contractedSpirits`** | Ordered list of **instances**: `{ contractInstanceId, spiritId, contractLevel }`. **Unlimited** roster size; duplicate spirit types allowed. |
| **`summonedContractInstanceIds`** | Set of **instance ids** currently **summoned** (replaces spirit-id-only set when duplicates exist). |
| **`toggleStates`** | Optional map for toggle actives (spirit + active → on/off). |

**Derived:** For each summoned spirit, effective abilities = union of all level rows **1 … contractLevel** for that spirit (or **exactly level N only**—**resolve once**: recommend **cumulative** lower levels + current level, i.e. level 3 grants level 1–3 payloads; document in implementation).

**v0 — preset only:** `contractedSpirits` (and levels) come from **prefab / serialized component / preset asset**, not from in-world contract formation.

### D4.5 — Composition with `RacialLoadoutApplier` — **Pattern B** (same as Barbarian)

- **`RacialLoadoutApplier`**: static **Elf baseline** `RacialLoadoutDefinition` only (folk-wide passives/restrictions).
- **Elemental Spirits**: **separate runtime**; summoned passives/actives use **distinct sources** from loadout and from each other.
- **Coordinator:** `Refresh` / `OnTurnStart` (and turn end if needed) must reach spirit passives and **upkeep** in a defined order relative to essences and imprint.

### D4.6 — Content assets (v0 minimum)

- One **`RacialLoadoutDefinition`** for Elf baseline (may be empty except `requiredRace: Elf`).
- At least **two** **`ElementalSpiritDefinition`** ScriptableObject assets (see D4.7 — **not** GameObject prefabs) proving:
  - Same element, **different** abilities.
  - Different **`maxLevel`** or different per-level payloads.
- At least one spirit with a **toggle-style** active (Fire weapon imbue pattern) — **at level ≥ 2** on Ember Warden after §5.12 test authoring.
- **Level 1 test actives:** both v0 spirits use **Sudden Strength** at level 1 (§5.12).

### D4.7 — Deliverables: actor prefab vs spirit data

**Actor prefab (required)**

- Create **`Assets/Prefabs/Actor/Race/ElfPlayer.prefab`** as a **variant** of the shared **`Player`** prefab (same pattern as `HumanPlayer` / `BarbarianPlayer`).
- Minimum configuration on the prefab:
  - `CharacterStats.race` = `Elf`
  - `CharacterStats.racialSubsystem` = `ElfElementalContracts`
  - `RacialLoadoutApplier` → Elf baseline `RacialLoadoutDefinition`
  - **Elemental spirit runtime** component with **preset** `contractedSpirits` (and levels), referencing spirit **data assets** below
  - Other standard player components inherited from the base prefab (inventory, equipment, party hooks, etc.)

**Elemental Spirit “prefab”? (resolved: data assets, not GameObject prefabs)**

- **v0 does not require** a **GameObject prefab** per Elemental Spirit. Spirits are **data** (`ElementalSpiritDefinition` ScriptableObjects), like `EssenceData` or Spirit Imprint **graph nodes** — not world actors.
- **Required:** at least two spirit **data assets** under a sensible folder (e.g. `Assets/Data/Racial/Elf/ElementalSpirits/`).
- **Optional (later):** a **visual prefab** (VFX, companion mesh) referenced from spirit data if summoned spirits need an in-world representation; out of v0 unless you explicitly add presentation scope.

---

## 5. Functional requirements

### 5.1 — Eligibility

**F4.1** Only `Race.Elf` actors with `RacialSubsystemKind.ElfElementalContracts` run this logic; others ignore the component.

### 5.2 — Contract roster (v0)

**F4.2** An Elf **cannot** summon a spirit not in **`contractedSpirits`**.  
**F4.3** v0: roster is **fixed at authoring**; no runtime “form contract” UI or NPC.

### 5.3 — Summon

**F4.4** Summoning a spirit:

1. Instance must be **contracted** and **not already summoned** (each `contractInstanceId` at most once in `summonedContractInstanceIds`).
2. Elf must have **Soul Power ≥ `summonSoulPowerCost`**; on success, **deduct** summon cost.
3. Spirit enters **summoned** state.
4. Apply **all applicable level passives** for that spirit’s **contract level**; register actives for execution.
5. **Does not consume a turn.**

**F4.5** Elf may **summon any number of spirits in one action batch** (same player input or AI tick), each paying its own summon cost, subject to Soul Power availability (order documented: e.g. player-selected order, or roster order).

### 5.4 — Upkeep and auto-dismiss

**F4.6** At a defined **turn boundary** (recommend: **start of Elf’s turn**, before or after other `OnTurnStart`—pick one and document), for **each summoned spirit**:

- If `currentSoulPower >= upkeepSoulPowerPerTurn`, **deduct** upkeep.
- Else **auto-dismiss** that spirit (same teardown as manual dismiss).

**F4.7** Upkeep is **per summoned spirit** (summoning three spirits costs three upkeep payments per turn).

### 5.5 — Manual dismiss

**F4.8** Elf may **dismiss any number of summoned spirits** without consuming a turn.

**F4.9** On dismiss (manual or auto):

- Remove all passives/modifiers sourced from that spirit.
- Disable actives; clear **toggle state** for that spirit.
- Remove instance from `summonedContractInstanceIds`.

### 5.6 — Using actives while summoned

**F4.10** Actives for a spirit are **only** executable if that spirit is **summoned** and contract level includes that active’s level row.

**F4.11** Respect per-active **`soulPowerCost`**, **`consumesTurn`**, **`repeatableSameTurn`**, and global `AbilityAction.CanExecute` / cooldown rules.

**F4.12** Toggle actives: second use in the same turn **reverts** the first use’s effect when authored as toggle (see D4.3 example).

### 5.7 — Passives while summoned

**F4.13** Summoned spirits’ passives participate in **`Refresh`** and **`OnTurnStart`** like essence/racial passives.

**F4.14** Dismissal or auto-dismiss must call passive **`OnRemove`** / equivalent so conditional passives do not leak.

### 5.8 — Later: form contract & level spirit (explicitly out of v0)

**F4.15 (later)** **Form contract:** event/NPC/item adds `{ spiritId, initialLevel }` to `contractedSpirits` (or sets level 1 if new).  
→ **Specified:** [Elf — Fairy Stone spirit contracts](Elf-Fairy-Stone-Spirit-Contract-Requirements.md) (town merchant + consumable item, 50% random spirit at level 1).

**F4.16 (later)** **Level spirit:** event/NPC/item increases **contract level** for one spirit, capped at `maxLevel`; re-apply passives if summoned (remove old level sources, apply new cumulative set).  
→ **Specified:** [Elf — meditation & leveling](Elf-ElementalSpirit-Meditation-Leveling-Requirements.md) (town meditation shrine, spirit XP, character-level cap).

v0 **must not** depend on these gates; presets suffice for playtests.

### 5.9 — Presets and NPCs

**F4.17** Anonymous Elf NPCs: minimal preset (e.g. one low-level spirit contracted, none summoned at spawn unless archetype says otherwise).

**F4.18** Named/boss Elves: richer **preset** rosters and levels in data.

### 5.10 — Hotbar summon / dismiss (locked)

Summon and dismiss are **player-facing hotbar abilities**, not a separate character sheet or debug command. Implements [Ability hotbar §8.1](../UI/Ability-Hotbar-Requirements.md) `ElementalSpiritSummon` entries alongside existing spirit **active** entries.

| Rule | Detail |
|------|--------|
| **One entry per contract instance** | Each row in `contractedSpirits` exposes **one** assignable hotbar action keyed by **`contractInstanceId`**. Duplicate spirit types → duplicate hotbar entries (e.g. two Ember Warden instances → two summon slots). |
| **Toggle behavior** | If instance **not summoned**: hotbar press → **summon** (`TrySummon`). If **summoned**: same bound slot → **dismiss** (`TryDismiss`). Label/icon reflects state (*“Ember Warden — Summon”* / *“… — Dismiss”*). **Display name** = optional instance **nickname** from [Elf racial menu](Elf-Racial-Abilities-Menu-Requirements.md) §6 when set; else canonical spirit name (with duplicate suffix). |
| **Turn cost** | **None** — does **not** consume the actor’s turn (F4.5 / O5). |
| **When allowed** | Active party member’s turn in combat (`TurnManager`); freely in town / safe zone when `GameplayModalGate` allows. Failed summon (insufficient Soul Power) → greyed entry + log message. |
| **No targeting** | Summon/dismiss does **not** enter reticle / targeting mode. |
| **Spirit actives separate** | While summoned, spirit **combat actives** appear as hotbar assignables — only when at least one summoned instance exposes that active. **Deduped by ability asset** — §5.11. |
| **Per-character bar** | Entries appear only on **that Elf’s** hotbar (`HotbarAssignabilityService` scoped to actor). |
| **Binding key (implementation)** | Summon: `ElementalSpiritSummon:{contractInstanceId}`. Active: `ElementalSpiritActive:{abilityAssetId}` (deduped — §5.11). |

**Flow:**

```
Hotbar key / click (ElementalSpiritSummon entry)
  → Resolve contractInstanceId on active Elf
  → If not summoned: TrySummon(instance) — deduct summon SP, apply passives
  → If summoned: TryDismiss(instance) — teardown passives, clear toggles
  → No turn consumed; dungeon log feedback
```

**Default authoring:** New contract instances appear in the **overflow assignable pool** (Racial group); player drags to main row. Optional later: auto-pin first N instances.

### 5.11 — Hotbar spirit active deduplication (locked)

When **multiple summoned instances** expose the **same** `AbilityAction` asset (e.g. three spirits all grant **Sudden Strength** at level 1), the hotbar shows **one** assignable slot for that ability — **not** one slot per spirit instance.

| Rule | Detail |
|------|--------|
| **Dedup key** | Stable **`AbilityAction` asset identity** (ScriptableObject instance id / authored asset name — pick one in code and document). |
| **Assignable pool** | Union actives from **all summoned instances** at their contract levels → **collapse** rows sharing the same ability asset → **one** overflow/hotbar entry per unique active. |
| **Label** | Ability display name only (e.g. **“Sudden Strength”**) — omit spirit name when deduped. |
| **Execution** | On hotbar press, resolve **any** summoned instance on this Elf that exposes the ability and passes `CanExecute` (recommend: first in roster order). |
| **Summon entries unchanged** | §5.10 — **one summon/dismiss slot per contract instance**; dedup applies **only** to combat actives. |
| **Different abilities** | Ember Weapon Imbue vs Tide Mend vs Sudden Strength → **separate** hotbar entries (different assets). |

**Example:** Elf summons three instances that each include **Sudden Strength** at level 1 → hotbar assignable pool contains **one** “Sudden Strength” active, plus **three** summon/dismiss toggles (one per instance).

**Cross-ref:** [Ability hotbar §8.2.1](../UI/Ability-Hotbar-Requirements.md).

### 5.12 — v0 test authoring: Sudden Strength at level 1 (locked)

To simplify playtesting (Fairy Stone contracts, duplicate instances, hotbar dedup), **both** shipped spirit definitions use the **same** level-1 active:

| Spirit | Level 1 active (v0 test) | Asset |
|--------|--------------------------|-------|
| **Ember Warden** | **Sudden Strength** | `Assets/Resources/Item/Ability/SuddenStrength_Standard.asset` |
| **Tide Shard** | **Sudden Strength** | same |

| Rule | Detail |
|------|--------|
| **Level 1 only** | Replace level-1 `activeEntries` on both spirits with **`SuddenStrength_Standard`** reference. |
| **Higher levels** | May keep distinct actives (Ember Weapon Imbue, Tide Mend, …) for differentiation — not required for v0 test slice. |
| **Metadata** | Copy `consumesTurn` / `soulPowerCost` from essence pattern or use ability defaults ([Sudden Strength essence doc](../Essence/Sudden-Strength-Essence-Requirements.md)). |
| **Dedup test** | Contract duplicate instances → summon multiple → confirm **one** Sudden Strength hotbar slot (§5.11). |

---

## 6. Non-functional requirements

**N4.1 — Authoring**  
Designers add spirits and level rows by **editing assets**, not code subclasses per spirit.

**N4.2 — Tests (v0 minimum)**  
- Summon → passive applied → dismiss → passive removed.  
- Upkeep paid → spirit stays; upkeep fails → auto-dismiss.  
- Hotbar summon/dismiss → **no turn consumed**; hotbar spirit active with `consumesTurn=true` **does** consume turn.  
- Toggle active: on → off same turn.  
- Cannot summon uncontracted instance; cannot use active when instance not summoned.  
- Hotbar summon entry greyed when insufficient Soul Power for summon cost.
- Three summoned instances sharing **Sudden Strength** at L1 → **one** deduped active on hotbar; three summon toggles remain.

**N4.3 — Migration**  
Elves without contract state: empty roster, nothing summoned, no errors.

**N4.4 — Shipping vs dev execution**  
Same policy as Phase 3: actives may be **data-only** in shipping until unified hotbar exists; **dev-only** execute path allowed under `#if` / development defines, stripped in release.

**N4.5 — Phase 4**  
If a spirit passive grants body capabilities or equip bypass, use **`CharacterStats.RegisterBodyEquipmentContribution`** with stable keys (e.g. `ElementalSpirit:{spiritId}`), cleared on dismiss.

---

## 7. Acceptance criteria (examples)

- Given an Elf with Fire spirit **contracted at level 1** but **not summoned**, Fire passives/actives are **inactive**.
- Given sufficient Soul Power, **summon** Fire spirit → Fire passives apply; **imbue toggle** can be turned on and off in the **same turn** without ending the turn.
- Given two spirits summoned, **upkeep** deducts **both** costs at turn start; if only enough Soul Power for one, policy dismisses **insufficient** spirits (document whether all-or-nothing vs partial—recommend **per-spirit** check in roster order).
- Given **manual dismiss**, all modifiers from that spirit are gone and actives fail `CanExecute`.
- Given preset NPC Elf with one contracted spirit level 1, load/play matches preset without contract UI.
- Given Ember Warden **contract instance** bound to hotbar slot `3`, pressing `3` **summons** then **dismisses** without ending the Elf’s turn.
- **`ElfPlayer.prefab`** exists under `Assets/Prefabs/Actor/Race/`, drops into a scene, and enters play with Elf race, subsystem, baseline loadout, and preset contracted spirits as authored.

---

## 8. Out of scope (v0)

- In-world **form contract** and **level spirit** (NPC/event/item) — **form contract** now specified in [Elf — Fairy Stone spirit contracts](Elf-Fairy-Stone-Spirit-Contract-Requirements.md) (implementation pending).
- **Level spirit** via Fairy Stone (separate future gate).
- Full **character sheet** / spirit management UI beyond hotbar (debug menu acceptable).
- **Non-hotbar** summon/dismiss UI (no dedicated racial menu action for summon in v0).
- Unified **ability hotbar** binding for spirit **actives** — **in scope** via [Ability hotbar](../UI/Ability-Hotbar-Requirements.md); summon/dismiss entries added in §5.10.
- Limiting **number of contracts** (design is **unlimited** roster).
- Cross-spirit exclusivity (“only one Fire”) unless added in a later doc.
- Tiefling implants, Human class, Barbarian imprint changes.

---

## 9. Open decisions (resolve before implementation)

| # | Question | Recommendation |
|---|----------|----------------|
| O1 | Level payloads **cumulative** (1+2+3) vs **current level only**? | **Cumulative** for clearer growth; document in code. |
| O2 | Upkeep timing: turn **start** vs **end**? | **Start of Elf turn** (predictable with dismiss before acting). |
| O3 | Partial summon batch if Soul Power runs out mid-batch? | **Summon until funds exhausted**; remainder stay unsummoned; log feedback. |
| O4 | Multiple instances of same `spiritId` in roster? | **Yes** — unlimited **contract instances**; each row has unique **`contractInstanceId`**. Summon/dismiss/hotbar key by **instance**, not `spiritId` alone. See [Fairy Stone doc](Elf-Fairy-Stone-Spirit-Contract-Requirements.md) L8. |
| O5 | `consumesTurn` interaction with summon/dismiss? | Summon/dismiss **never** consume turn; only flagged actives do. |

---

## 10. Relation to other docs

| Doc | Relationship |
|-----|----------------|
| [Phase0 — Glossary](Phase0-Glossary-And-Data-Contracts.md) | Add **Elemental Spirit**, **contract**, **summon** when implementing. |
| [Phase3 — Barbarian Spirit Imprint](Phase3-Requirements.md) | Parallel **preset v0** pattern; different lifecycle. |
| [Elf — Fairy Stone spirit contracts](Elf-Fairy-Stone-Spirit-Contract-Requirements.md) | **Form contract** item + merchant gate (implements F4.15). |
| [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) | Summon/dismiss + spirit actives on per-Elf hotbar (§5.10). |
| [Phase5 — Requirements](Phase5-Requirements.md) | Elf subsystem fulfills **sustained / upkeep** shape. |

**Tiefling:** [Cyborg implants](Tiefling-Cyborg-Implants-Requirements.md) — slot replace, not summon/upkeep; do not conflate with Elf rules.
