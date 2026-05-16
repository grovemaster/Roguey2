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
| **Contract** | Elf is allowed to use a given spirit (appears in their **contract roster**). v0: roster is **preset**; later: gained via event/NPC/item. |
| **Contract level** | Integer **1 … maxLevel** for that spirit on this Elf. Determines which **level rows** of passives/actives apply. v0: **preset**; later: raised by event/NPC/item. |
| **Summon** | Pay **summon cost** → spirit enters **summoned** state → passives apply, actives available. |
| **Dismiss** | Spirit leaves summoned state → passives removed, actives unavailable, toggles cleared per policy. |
| **Upkeep** | Soul Power paid **each turn** (see §5.3) while summoned; failure → **auto-dismiss**. |
| **Soul Power** | Shared resource with essences (`currentSoulPower`). All summon, upkeep, and ability costs use Soul Power. |

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
| **`contractedSpirits`** | Ordered or keyed list: `{ spiritId, contractLevel }`. **Unlimited** roster size in design; practical limits are content/UX only. |
| **`summonedSpiritIds`** | Set (or list) of spirits currently **summoned**. |
| **`toggleStates`** | Optional map for toggle actives (spirit + active → on/off). |

**Derived:** For each summoned spirit, effective abilities = union of all level rows **1 … contractLevel** for that spirit (or **exactly level N only**—**resolve once**: recommend **cumulative** lower levels + current level, i.e. level 3 grants level 1–3 payloads; document in implementation).

**v0 — preset only:** `contractedSpirits` (and levels) come from **prefab / serialized component / preset asset**, not from in-world contract formation.

### D4.5 — Composition with `RacialLoadoutApplier` — **Pattern B** (same as Barbarian)

- **`RacialLoadoutApplier`**: static **Elf baseline** `RacialLoadoutDefinition` only (folk-wide passives/restrictions).
- **Elemental Spirits**: **separate runtime**; summoned passives/actives use **distinct sources** from loadout and from each other.
- **Coordinator:** `Refresh` / `OnTurnStart` (and turn end if needed) must reach spirit passives and **upkeep** in a defined order relative to essences and imprint.

### D4.6 — Content assets (v0 minimum)

- One **`RacialLoadoutDefinition`** for Elf baseline (may be empty except `requiredRace: Elf`).
- At least **two** `ElementalSpiritDefinition` assets proving:
  - Same element, **different** abilities.
  - Different **`maxLevel`** or different per-level payloads.
- At least one spirit with a **toggle-style** active (Fire weapon imbue pattern).
- One **preset** Elf actor (prefab or test object) with **hard-coded** contract list + levels (e.g. two spirits at level 1).

---

## 5. Functional requirements

### 5.1 — Eligibility

**F4.1** Only `Race.Elf` actors with `RacialSubsystemKind.ElfElementalContracts` run this logic; others ignore the component.

### 5.2 — Contract roster (v0)

**F4.2** An Elf **cannot** summon a spirit not in **`contractedSpirits`**.  
**F4.3** v0: roster is **fixed at authoring**; no runtime “form contract” UI or NPC.

### 5.3 — Summon

**F4.4** Summoning a spirit:

1. Spirit must be **contracted** and **not already summoned** (unless design allows re-summon refresh—default: **no duplicate** entry in `summonedSpiritIds`).
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
- Remove spirit from `summonedSpiritIds`.

### 5.6 — Using actives while summoned

**F4.10** Actives for a spirit are **only** executable if that spirit is **summoned** and contract level includes that active’s level row.

**F4.11** Respect per-active **`soulPowerCost`**, **`consumesTurn`**, **`repeatableSameTurn`**, and global `AbilityAction.CanExecute` / cooldown rules.

**F4.12** Toggle actives: second use in the same turn **reverts** the first use’s effect when authored as toggle (see D4.3 example).

### 5.7 — Passives while summoned

**F4.13** Summoned spirits’ passives participate in **`Refresh`** and **`OnTurnStart`** like essence/racial passives.

**F4.14** Dismissal or auto-dismiss must call passive **`OnRemove`** / equivalent so conditional passives do not leak.

### 5.8 — Later: form contract & level spirit (explicitly out of v0)

**F4.15 (later)** **Form contract:** event/NPC/item adds `{ spiritId, initialLevel }` to `contractedSpirits` (or sets level 1 if new).

**F4.16 (later)** **Level spirit:** event/NPC/item increases **contract level** for one spirit, capped at `maxLevel`; re-apply passives if summoned (remove old level sources, apply new cumulative set).

v0 **must not** depend on these gates; presets suffice for playtests.

### 5.9 — Presets and NPCs

**F4.17** Anonymous Elf NPCs: minimal preset (e.g. one low-level spirit contracted, none summoned at spawn unless archetype says otherwise).

**F4.18** Named/boss Elves: richer **preset** rosters and levels in data.

---

## 6. Non-functional requirements

**N4.1 — Authoring**  
Designers add spirits and level rows by **editing assets**, not code subclasses per spirit.

**N4.2 — Tests (v0 minimum)**  
- Summon → passive applied → dismiss → passive removed.  
- Upkeep paid → spirit stays; upkeep fails → auto-dismiss.  
- Summon/dismiss batch does **not** flag turn consumed (hook to turn system stub if needed).  
- Toggle active: on → off same turn.  
- Cannot summon spirit not in roster; cannot use active when not summoned.

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

---

## 8. Out of scope (v0)

- In-world **form contract** and **level spirit** (NPC/event/item).
- Full **character sheet** / spirit management UI (debug menu acceptable).
- Unified **ability hotbar** binding (may follow Phase 3 policy).
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
| O4 | Multiple instances of same `spiritId` in roster? | **No** — one entry per `spiritId`; level stored once. |
| O5 | `consumesTurn` interaction with summon/dismiss? | Summon/dismiss **never** consume turn; only flagged actives do. |

---

## 10. Relation to other docs

| Doc | Relationship |
|-----|----------------|
| [Phase0 — Glossary](Phase0-Glossary-And-Data-Contracts.md) | Add **Elemental Spirit**, **contract**, **summon** when implementing. |
| [Phase3 — Barbarian Spirit Imprint](Phase3-Requirements.md) | Parallel **preset v0** pattern; different lifecycle. |
| [Phase5 — Requirements](Phase5-Requirements.md) | Elf subsystem fulfills **sustained / upkeep** shape. |

**Tiefling:** [Cyborg implants](Tiefling-Cyborg-Implants-Requirements.md) — slot replace, not summon/upkeep; do not conflate with Elf rules.
