# Dwarf — Patron Ancestor & common racial abilities (requirements)

Dwarves use a racial subsystem with **two independent progression layers**:

1. **Common racial abilities** — up to **three** abilities available to **any** Dwarf **without** a patron Ancestor requirement. Unlocked over time by **character level** (not designed yet). Until leveling exists, abilities are **assigned in the Inspector** on the actor prefab (same authoring model as Barbarian **preset** `chosenPathNodeIds`).
2. **Patron Ancestor** — optional. Each Dwarf has **at most one** patron Ancestor (or **none**). A patron grants access to that Ancestor’s **single forward-only ability tree**. Extending the tree requires a **special event / NPC** (same gating model as Barbarian Spirit Imprint), not automatic level-up.

**Subsystem kind (code):** `RacialSubsystemKind.DwarfAncestry` (see `RacialSubsystemKind.cs`).

**Commitment policy:** `Permanent` for **patron choice** and **Ancestor path** picks; common abilities are **permanent once unlocked** (slot assignment may be fixed at unlock time—see §5).

**Depends on:** Phase 1–2 (`RacialLoadoutDefinition` / `RacialLoadoutApplier`, stacking-by-source, `RacialPassiveHooks`), Phase 3 tree patterns (`SpiritImprintGraph` / path validation — **reuse or generalize**, see §7.2).

**Contrast:** [Phase 3 — Barbarian Spirit Imprint](Phase3-Requirements.md) (one tree per race, no “no tree” common track). [Tiefling — Cyborg implants](Tiefling-Cyborg-Implants-Requirements.md) (slot swap, not forward-only tree). [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md) (summon/upkeep, not always-on path).

---

## 1. Goals

**G1 — Vertical slice (v0)**  
A Dwarf actor can have **0–3 common abilities** and **0 or 1 patron Ancestor** with a **preset path** on that Ancestor’s tree. All assigned effects apply at runtime with correct apply/remove and passive hooks.

**G2 — Data-driven content**  
Designers author **Ancestors**, **Ancestor trees**, and **common ability** packages as assets—no new `MonoBehaviour` per ability or per node.

**G3 — Patron cap**  
Runtime and validation enforce **at most one** patron Ancestor per Dwarf. Clearing patron (if ever allowed) is out of v0 unless explicitly added.

**G4 — NPC / preset parity**  
Player and NPC Dwarves use **prefab / serialized preset** for common slots and Ancestor path before play, analogous to Barbarian imprint and Elf contract presets.

**G5 — Persistence (v0 minimum)**  
Serialize patron id (or empty), Ancestor **chosen path**, and which **common ability** ids occupy each of the three slots (including empty slots). Safe defaults on load.

**G6 — Playable Dwarf prefab (required)**  
A dedicated **`DwarfPlayer`** actor prefab exists (same family as `HumanPlayer` / `TieflingPlayer`) so designers can place or spawn a correctly configured Dwarf without hand-wiring components. See **§6.6**.

**G7 — Future leveling hook**  
Common-ability slots are designed so **level-up** can later **unlock** slots 1→2→3 without rewriting the runtime contract (see §5).

---

## 2. Conceptual model (vs other folk)

| | **Barbarian — Spirit Imprint** | **Dwarf — Ancestor + common** |
|--|-------------------------------|------------------------------|
| **Always-on racial progression** | One tree; every Barbarian uses **the same** graph | **Optional** patron → **that** Ancestor’s tree only |
| **Second track** | None | **0–3 common abilities** (no patron required) |
| **Max trees / patrons** | 1 graph per actor | **≤ 1** patron Ancestor; **0–3** common packages |
| **Tree navigation** | Forward-only path | Same: forward-only on **patron** tree only |
| **Advance tree** | Special event / NPC (later); v0 preset path | Same for Ancestor path |
| **Advance common slots** | N/A | **Level-up** (later); v0 Inspector assignment |
| **When effects apply** | Chosen path nodes (always on) | Common + Ancestor path nodes (always on while assigned) |
| **Ongoing cost** | None | None (unless a specific ability adds one in data) |
| **v0 authoring** | Preset `chosenPathNodeIds` | Preset common assignments + optional patron + preset path |

**Independence:** A Dwarf **may** have only common abilities, only a patron path, both, or neither (beyond baseline `RacialLoadoutDefinition`). Patron does **not** replace or block common abilities unless a future explicit design says otherwise (not in v0).

---

## 3. Glossary

| Term | Meaning |
|------|--------|
| **Common racial ability** | A data-defined package (stats, resistances, passives, actives) assignable to one of **three** Dwarf slots. Does not require a patron. |
| **Common slot** | One of **three** fixed indices (0, 1, 2). Each slot holds **zero or one** common ability reference when “filled.” |
| **Patron Ancestor** | The single Ancestor entity this Dwarf venerates. **At most one** per character. |
| **Ancestor** | Content asset: identity, display copy, and a **tree** of ability nodes. Many Ancestor assets exist in the game; each Dwarf uses **at most one**. |
| **Ancestor path** | Ordered list of node ids **root → deepest chosen** on the **patron’s** tree (same semantics as Barbarian `chosenPathNodeIds`). |
| **Ancestor rank** | Derived: number of **non-root** nodes on the path (same invariant as imprint rank). |
| **Progression event** | Story/NPC/world gate that authorizes **one** new node on the Ancestor path (later phase). Not used for common slots in v0. |
| **Preset (v0)** | Common abilities and Ancestor path are **authored before play** on prefab or linked preset asset. |

---

## 4. Folk baseline (optional, separate from Ancestor/common)

Dwarf **`RacialLoadoutDefinition`** (via `RacialLoadoutApplier`) holds **race-wide** modifiers shared by all Dwarves (e.g. poison resistance, +Constitution)—**not** patron-specific fantasy.

- Baseline is **Pattern B**: not merged into Ancestor or common runtimes.
- Patron trees and common abilities add on top with **distinct modifier/passive sources**.

---

## 5. Common racial abilities (0–3)

### D5.1 — Slot model

- Every Dwarf has exactly **three logical slots** (indices 0, 1, 2).
- At runtime, **between zero and three** slots may be **filled** (reference a common ability definition).
- Empty slots contribute nothing.
- **v0:** Designers set filled slots directly in the Inspector (which ability, if any, occupies each slot). No level check.
- **Later (leveling):** Slot *k* becomes fillable only when the character meets a **level threshold** defined in data (exact formula TBD). Until then, slot *k* must behave as **empty** even if an id was authored for testing—gate in runtime, not by deleting serialized data.

### D5.2 — Common ability definition (data)

Each common ability is a **single package** (not a tree), structurally similar to **one** Spirit Imprint node payload:

| Field | Required | Notes |
|-------|----------|--------|
| Stable **id** | Yes | Saves and validation |
| **displayName** / **description** | Yes | UI / debug |
| **statModifiers** | No | List; same shape as `RacialLoadoutDefinition` / `SpiritImprintNodeData` |
| **resistanceModifiers** | No | List |
| **passiveEffects** | No | List of `PassiveEffect` |
| **activeAbilities** | No | List of `AbilityAction` (execution policy §8) |

- A common ability may have **zero** payload in all lists (valid but useless—warn in editor).
- **No** parent/child links between common abilities.

### D5.3 — Runtime (common)

- Dedicated runtime component (e.g. `DwarfCommonAbilitiesRuntime`) on Dwarf actors.
- Applies each **filled** slot with a **stable source** per slot (e.g. slot index + ability id) so remove/stacking matches Pattern B.
- Participates in `RacialPassiveHooks.RefreshPassives` / turn hooks like imprint and loadout.
- **Eligibility:** `Race.Dwarf` only; optional `racialSubsystem == DwarfAncestry` flag.

### D5.4 — Unlock order (later, not v0)

- Design intent: common abilities are unlocked by **leveling up** (system TBD).
- Documented expectation: typically unlock **slot 0, then 1, then 2** (sequential), unless data specifies per-ability level gates.
- v0 does **not** implement XP or level; Inspector assignment **simulates** any unlock state for playtests.

---

## 6. Patron Ancestor (0 or 1)

### D6.0 — Patron selection rules

- **`patronAncestorId`** (or reference to `AncestorDefinition`): **empty** = no patron; **non-empty** = exactly one patron.
- A Dwarf **cannot** have two patrons. Validation on load and in editor tools must reject dual assignment.
- **v0:** Patron is **preset** on prefab (chosen before play). **Later:** a **one-time** event/NPC sets patron (permanent); changing patron is out of v0 unless design adds a rare respec.

### D6.1 — Ancestor definition (data)

Each **Ancestor** asset includes at minimum:

| Field | Notes |
|-------|--------|
| Stable **ancestorId** | Saves |
| **displayName** / **description** | Lore + UI |
| **abilityTree** | Reference to a **tree graph** asset (see D6.2) |

- Multiple Dwarf characters may share the same Ancestor asset.
- Different Ancestors use **different** tree graph assets.

### D6.2 — Ancestor ability tree (same rules as Spirit Imprint)

Reuse the **Spirit Imprint tree contract** ([Phase 3 D2.0–D2.1](Phase3-Requirements.md)) unless a renamed duplicate type is required for clarity:

#### Root node (required)

- First node = **root**; **no gameplay payload** (empty stat/resistance/passive/active lists).
- **Ancestor rank 0** = only root on path (or no patron → no tree applied).

#### Tree topology

- **Tree only** (one parent per non-root node).
- **Forward-only:** path only **extends** toward children; no unpick, no branch swap after commitment (v0).
- **Optional sibling exclusivity** via `siblingExclusivityGroup` (same as imprint).

#### Node payload (per node)

Each node has **zero or more** of each (lists may be empty):

- **statModifiers**
- **resistanceModifiers**
- **passiveEffects**
- **activeAbilities**

Non-root nodes may use any subset; root must stay empty.

### D6.3 — Ancestor runtime state

When a patron is set:

- **`chosenPathNodeIds`:** ordered **root → deepest chosen** on **that patron’s** tree (canonical save).
- **`ancestorRank`:** derived invariant: `ancestorRank == chosenPathNodeIds.Count - 1` when list starts with root (same as `imprintRank`).
- **Phase v0 — path source:** Preset on prefab/component, like Barbarian Phase 3 v0.
- **Later:** progression event appends **exactly one** valid child node per event; no multi-node batch.
- **Commitment:** `Permanent` for path picks.

When **no** patron:

- No Ancestor tree effects apply; `chosenPathNodeIds` may be empty or ignored.

### D6.4 — Runtime (Ancestor path)

- Dedicated component (e.g. `DwarfAncestorPathRuntime`) or generalized `ForwardOnlyRacialTreeRuntime` configured with patron + graph.
- Resolves **patron → tree asset → validate path → apply** node payloads with **per-node sources** (Pattern B).
- If patron id is set but path fails validation → **root only** (rank 0) + warning log (mirror corrupt imprint save policy).

### D6.5 — Composition with common abilities

Apply order (recommended, document in code):

1. `RacialLoadoutApplier` (baseline)
2. `DwarfCommonAbilitiesRuntime` (slots 0–2)
3. `DwarfAncestorPathRuntime` (patron path, if any)

Passives: all participate in shared **`Refresh`** / **`OnTurnStart`** hooks without duplicate firing per source.

### D6.6 — Deliverables: actor prefab vs ability data

**Actor prefab (required)**

- Create **`Assets/Prefabs/Actor/Race/DwarfPlayer.prefab`** as a **variant** of the shared **`Player`** prefab (same pattern as `HumanPlayer` / `BarbarianPlayer` / `TieflingPlayer`).
- Minimum configuration on the prefab (v0 — **shipped now**):

  | Field / component | Value |
  |-------------------|--------|
  | `m_Name` | `DwarfPlayer` |
  | `CharacterStats.race` | `Race.Dwarf` (`4`) |
  | `CharacterStats.racialSubsystem` | `RacialSubsystemKind.DwarfAncestry` (`5`) |
  | `CharacterStats.bodyCapabilities` | **`ReducedStature`** (authoring default; adjust if equipment rules change) |
  | `RacialLoadoutApplier.loadout` | **`DefaultDwarfRacialLoadout`** (`Assets/Data/Racial/Dwarf/DefaultDwarfRacialLoadout.asset`) |
  | Other player components | Inherited from base **`Player`** prefab |

- **When Ancestor/common runtimes ship:** extend this prefab (do not create a second Dwarf player prefab) with:
  - **`DwarfCommonAbilitiesRuntime`** — preset **0–3** common ability references in slots 0–2 (Inspector assignment until leveling exists).
  - **`DwarfAncestorPathRuntime`** (or shared forward-only tree runtime) — optional **patron** + preset **`chosenPathNodeIds`** for playtest builds that include a patron.
- **Default playtest preset (recommended):** no patron, **zero** common abilities filled (rank-0 / empty slots), baseline loadout only—same spirit as anonymous Barbarian NPC at imprint rank 0.

**Ability data (required for full vertical slice; not blocking prefab v0)**

- **`DefaultDwarfRacialLoadout`** — folk baseline (`requiredRace: Dwarf`); may start with empty modifier lists.
- **`DwarfCommonAbility_*`** — data assets per §D5.2 (not GameObject prefabs).
- **`AncestorDefinition_*`** + **tree graph** assets per §D6.1–D6.2 (reuse or generalize Spirit Imprint graph shape).

---

## 7. Functional requirements

**F7.1 — Eligibility**  
Only `Race.Dwarf` actors use Dwarf Ancestor/common components; others ignore or omit them.

**F7.2 — Patron cap**  
At most one non-empty patron per actor. Editor and runtime validation enforce this.

**F7.3 — Common slot count**  
At most **three** non-empty common assignments. A single ability cannot occupy two slots.

**F7.4 — Path validation (Ancestor)**  
`chosenPathNodeIds` must be a valid root-to-node walk on the **patron’s** graph. Invalid saves → root only + warning.

**F7.5 — Forward-only path (Ancestor)**  
Same as Barbarian F3.2: no backward navigation; v0 preset only; later single-node append via event.

**F7.6 — Exclusivity (Ancestor)**  
If sibling exclusivity groups are used, a valid path cannot include two siblings from the same group.

**F7.7 — Presets (v0)**  
Player, ally, and enemy Dwarves use serialized common slots and optional patron + path. Default anonymous Dwarf NPC: **no patron**, **zero common abilities**, baseline loadout only (unless archetype overrides).

**F7.8 — Integration hooks**  
Common and Ancestor passives use the same refresh/turn pipeline as racial loadout, imprint, and essence.

**F7.9 — Active abilities**  
Same policy as Phase 3 §7.5: **data-only** in shipping v0; **test-only** execution gated by editor/development defines (see N7.4).

**F7.10 — No patron, no tree**  
If patron is unset, Ancestor runtime applies nothing and does not require a graph reference.

**F7.11 — Patron without path**  
If patron is set but path is empty or invalid, treat as **rank 0** (root only) after normalization.

---

## 8. Non-functional requirements

**N7.1 — Authoring**  
Designers edit Ancestor assets, tree graphs, and common ability assets in the Editor—no per-ability code subclasses.

**N7.2 — Reuse imprint machinery**  
Prefer **one** tree graph type and validator (generalize `SpiritImprintGraph` / `SpiritImprintNodeData` or subclass) shared by Barbarian imprint and Dwarf Ancestor trees to avoid divergent validation bugs.

**N7.3 — Tests**  
At minimum:

- Dwarf with 0 common, no patron → baseline only.
- Three distinct common abilities → all modifiers/passives applied with distinct sources.
- Patron + valid path depth 2 → root + one child payloads apply; rank invariant holds.
- Invalid path → degrades to root only.
- Two patrons (invalid data) → rejected or second ignored with warning (pick one behavior and test it).
- Save round-trip for patron id, path, and three slot ids.

**N7.4 — Test-only active execution**  
Same as Phase 3 N4.4: dev-only `AbilityAction` trigger; stripped/disabled in release.

**N7.5 — Migration**  
Existing Dwarf actors without subsystem state: no patron, empty common slots, no path effects.

---

## 9. Acceptance criteria (examples)

- Given a Dwarf with **no patron** and **two** common abilities in slots 0 and 1, only those two packages apply; slot 2 is inert.
- Given a Dwarf with patron **“Forge-Father”** and path `[root, ember_hammer]`, only payloads for nodes on that path apply; `ancestorRank == 1`.
- Given the same Dwarf also has three common abilities assigned, **both** Ancestor path effects and all three common effects are active (stacking rules per global racial contract).
- Given a preset path that violates exclusivity, load normalizes to **root only** and logs a warning.
- Given a Barbarian actor, Dwarf Ancestor/common components do not apply effects.
- **`DwarfPlayer.prefab`** exists under `Assets/Prefabs/Actor/Race/`, drops into a scene, and enters play with `Race.Dwarf`, `DwarfAncestry` subsystem, `ReducedStature`, and **`DefaultDwarfRacialLoadout`** applied via `RacialLoadoutApplier`.

---

## 10. Content assets (minimum for v0 proof)

| Asset | Purpose | Status |
|-------|---------|--------|
| `DefaultDwarfRacialLoadout` | `requiredRace: Dwarf`; optional baseline resist/stats | **Created** — `Assets/Data/Racial/Dwarf/` |
| `DwarfPlayer` prefab | Variant of `Player`; baseline loadout wired | **Created** — `Assets/Prefabs/Actor/Race/` |
| `DwarfCommonAbility_*` (≥2 samples) | e.g. “Stoneheart”, “Deep Delver” packages | Deferred (needs runtime) |
| `AncestorDefinition_*` (≥2 samples) | Two patrons with different fantasy | Deferred (needs runtime) |
| `AncestorTree_*` per ancestor | Root + ≥1 child + optional exclusivity branch | Deferred (needs runtime) |
| `DwarfPlayer` + runtimes | Add common/Ancestor components when runtimes ship | Later |

---

## 11. Design decisions (resolved + open)

### 11.1 — Patron optional — **Resolved**

- **Max one** patron; **zero** is valid.
- Common abilities do **not** require a patron.

### 11.2 — Common vs Ancestor — **Resolved: independent**

- Common slots and Ancestor path stack mechanically (separate sources).
- Neither replaces the other.

### 11.3 — Tree shape for Ancestor — **Resolved: Spirit Imprint parity**

- Single-path, forward-only tree per Ancestor.
- Node payloads: zero or more stats / resistances / passives / actives per node.

### 11.4 — Progression gates — **Resolved**

| Track | v0 | Later |
|-------|-----|-------|
| Ancestor path | Inspector preset `chosenPathNodeIds` | Special event/NPC, **one node** per event |
| Common slots | Inspector fill slot 0–2 | Level-up unlocks slots (thresholds TBD) |

### 11.5 — Pattern B — **Resolved**

- `RacialLoadoutApplier` = Dwarf baseline only.
- Separate runtimes for common slots and Ancestor path.

### 11.6 — UI — **Resolved: debug for v0**

- Inspector + debug logging/menu sufficient for v0.
- Later: Dwarf character sheet with patron portrait, path, and common slots.

### 11.7 — Open (not blocking v0)

- **Patron pick timing:** first shrine visit vs character creation UI.
- **Can common abilities be respec’d** after unlock, or permanent per id?
- **Baseline dwarf loadout** numbers (poison resist, etc.)—content, not framework.
- **Whether `RacialSubsystemKind` is required** on all Dwarves or only when any common/patron data is present.

---

## 12. Code touchpoints (implementation checklist)

When implementing, expect to add or extend:

| Area | Action |
|------|--------|
| `RacialSubsystemKind` | `DwarfAncestry` (**added**) |
| Data | `DwarfCommonAbilityDefinition`, `AncestorDefinition`, tree graph (reuse or alias imprint graph) |
| Runtime | `DwarfCommonAbilitiesRuntime`, `DwarfAncestorPathRuntime` (or shared tree runtime) |
| Saves | `patronAncestorId`, `chosenPathNodeIds`, `commonAbilityIds[3]` (or slot array) |
| Prefabs | `DwarfPlayer` (**created**), sample NPCs |
| Tests | Under `Assets/Tests/UnitTests/Racial/` |
| Docs | Link from [Phase0-Glossary](Phase0-Glossary-And-Data-Contracts.md) when shipped |

---

## 13. Related documents

- [Phase 0 — Glossary and data contracts](Phase0-Glossary-And-Data-Contracts.md)
- [Phase 1 — Implementation summary](Phase1-Implementation.md)
- [Phase 3 — Barbarian Spirit Imprint](Phase3-Requirements.md) (tree + path semantics)
- [Tiefling — Cyborg implants](Tiefling-Cyborg-Implants-Requirements.md)
- [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md)
