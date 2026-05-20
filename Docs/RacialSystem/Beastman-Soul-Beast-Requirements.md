# Beastman — Soul Beast (requirements)

Beastmen have **no innate racial progression** beyond an optional **folk baseline** loadout. Their distinctive power comes from bonding with at most **one** **Soul Beast**—a permanent companion whose abilities are unlocked along a **single linear chain** of nodes. Until a Soul Beast is gained (via a **special event**, implemented later), a Beastman has **no Soul Beast abilities**.

**Subsystem kind (proposed code):** `RacialSubsystemKind.BeastmanSoulBeast` (see `RacialSubsystemKind.cs`).

**Commitment policy:** `Permanent` for **Soul Beast choice** (once bonded, **no replacement**) and for **path node** picks along that beast’s chain.

**Depends on:** Phase 1–2 (`RacialLoadoutDefinition` / `RacialLoadoutApplier`, stacking-by-source, `RacialPassiveHooks`), Phase 3 tree patterns (`SpiritImprintGraph` / path validation — **reuse or generalize** for Soul Beast chains; see §6.2).

**Contrast:** [Phase 3 — Barbarian Spirit Imprint](Phase3-Requirements.md) (every Barbarian has an imprint tree from creation; forward-only **tree** may branch). [Dwarf — Patron Ancestor & common abilities](Dwarf-Ancestor-And-Common-Abilities-Requirements.md) (optional patron **plus** a separate common-ability track). [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md) (many spirits, summon/upkeep, not a single permanent bond).

---

## 1. Goals

**G1 — Vertical slice (v0)**  
A Beastman actor can exist with **no Soul Beast** (baseline only) or with **one** preset Soul Beast and a **preset path** along that beast’s node chain. All nodes on the path apply their payloads at runtime with correct apply/remove and passive hooks.

**G2 — Data-driven content**  
Designers author **Soul Beast** definitions and **per-beast node chains** as assets—no new `MonoBehaviour` per beast, per node, or per type.

**G3 — One beast, forever**  
Runtime and validation enforce **at most one** Soul Beast per Beastman. Once bonded, **replacement is forbidden** (v0 and unless design explicitly adds a rare story exception later).

**G4 — No innate racial track**  
Unlike Barbarian imprint or Dwarf common abilities, Beastmen **do not** gain racial progression nodes without a Soul Beast. Folk baseline (`RacialLoadoutDefinition`) may still apply.

**G5 — NPC / preset parity**  
Player and NPC Beastmen use **prefab / serialized preset** for optional Soul Beast id and `chosenPathNodeIds` before play, analogous to Barbarian imprint presets.

**G6 — Playable Beastman prefab (required)**  
A dedicated **`BeastmanPlayer`** actor prefab exists (same family as `HumanPlayer` / `DwarfPlayer`) so designers can place or spawn a correctly configured Beastman without hand-wiring components. See **§6.6**.

**G7 — Future gates (documented, not v0)**  
**Acquisition** (bonding a Soul Beast) and **progression** (advancing along the chain) are gated by **special events / systems TBD**. v0 uses Inspector presets only.

**G8 — Example content = stats only (v0)**  
All sample Soul Beast nodes in shipping v0 content use **simple stat modifications** only. Passive and active ability slots remain in the **data model** but sample assets leave those lists **empty** until abilities are designed.

---

## 2. Conceptual model (vs other folk)

| | **Barbarian — Spirit Imprint** | **Beastman — Soul Beast** |
|--|-------------------------------|---------------------------|
| **Innate progression** | Every Barbarian has an imprint graph | **None** without a Soul Beast |
| **Optional bond** | N/A (always imprinted) | **0 or 1** Soul Beast; gained via **event** (later) |
| **Replace bond** | N/A | **Never** (permanent once chosen) |
| **Structure** | Tree (may branch / exclusivity) | **Single sequential chain** (linear; one child per step) |
| **Chain length** | Fixed per shared graph | **Varies per Soul Beast** asset |
| **Taxonomy** | N/A | **Four types** (content grouping; shared ability patterns) |
| **When effects apply** | Chosen path nodes (always on) | Chosen path nodes on **bonded** beast only |
| **Advance chain** | Special event / NPC (later); v0 preset path | Same for Soul Beast nodes |
| **v0 authoring** | Preset `chosenPathNodeIds` | Preset beast id + path; **no** beast = no chain |

**Independence:** Soul Beast effects **stack on top of** folk baseline (`RacialLoadoutApplier`) with **distinct sources** (Pattern B). They do not merge into the baseline loadout runtime.

---

## 3. Glossary

| Term | Meaning |
|------|--------|
| **Soul Beast** | A data-defined companion species/entity. Each asset defines identity, **type**, and a **node chain** of abilities granted to the bonded Beastman. |
| **Soul Beast type** | One of **Summoning**, **Enhancement**, **Special Ability**, or **Specialist**. Used for content organization and **shared ability patterns** between beasts of the same type; each beast remains **unique**. |
| **Bond** | The Beastman has committed to exactly one Soul Beast. Empty = unbonded. **Irreversible** in v0. |
| **Soul Beast chain** | Ordered nodes **root → … → leaf** forming **one linear spine** (no parallel branches on the same beast). |
| **Chain node** | One step on the chain. Carries **zero or more** stat modifications, passive effects, and active abilities (lists may all be empty). |
| **Soul Beast path** | Ordered list of node ids **root → deepest chosen** on the **bonded** beast’s chain (same save semantics as Barbarian `chosenPathNodeIds`). |
| **Soul Beast rank** | Derived: number of **non-root** nodes on the path (`rank == chosenPathNodeIds.Count - 1` when the list starts with root). |
| **Acquisition event** | Story/NPC/world gate that grants **one** Soul Beast bond (later). Not in v0. |
| **Progression event** | Story/NPC/world gate that authorizes **one** new node on the bonded beast’s chain (later). Not in v0. |
| **Preset (v0)** | Optional Soul Beast id and path are **authored before play** on prefab or linked preset asset. Unbonded preset = empty beast id, no chain effects. |

---

## 4. Folk baseline (no Soul Beast required)

Beastman **`RacialLoadoutDefinition`** (via `RacialLoadoutApplier`) holds **race-wide** modifiers shared by all Beastmen (e.g. movement, senses—content TBD).

- Baseline is **Pattern B**: not merged into Soul Beast runtime.
- Soul Beast chain nodes add on top with **per-node sources**.
- A Beastman **without** a Soul Beast still receives baseline loadout effects only.

**v0 content rule:** Baseline loadout may be **empty** except `requiredRace: Beastman`; the important proof is “no Soul Beast → no chain effects.”

---

## 5. Soul Beast bond (0 or 1)

### B5.1 — Bond rules

- **`soulBeastId`** (or reference to `SoulBeastDefinition`): **empty** = unbonded; **non-empty** = exactly one Soul Beast.
- A Beastman **cannot** bond two Soul Beasts. Validation on load and in editor tools must reject dual assignment.
- **Replacement:** Once `soulBeastId` is non-empty, **changing** to a different beast id is **invalid** at runtime and in editor validation (v0). Log warning and **keep** the original bond on corrupt saves (pick one behavior and test it).
- **v0:** Bond and path are **preset** on prefab (chosen before play). **Later:** a **one-time acquisition event** sets the beast id (permanent).

### B5.2 — Unbonded Beastman

- No Soul Beast chain effects apply.
- `chosenPathNodeIds` may be empty or ignored.
- Default NPC and recommended **`BeastmanPlayer`** preset: **unbonded**, baseline loadout only.

### B5.3 — Bonded Beastman

- Runtime resolves **beast → chain asset → validate path → apply** node payloads.
- Effects apply to the **Beastman actor** (the bonded character), not a separate combat pawn, unless a future summon-type beast explicitly spawns an entity (out of v0; see §11.7).

---

## 6. Soul Beast definition & chain

### B6.1 — `SoulBeastDefinition` (data)

Each **Soul Beast** asset includes at minimum:

| Field | Required | Notes |
|-------|----------|--------|
| Stable **`soulBeastId`** | Yes | Saves and validation |
| **`displayName`** / **`description`** | Yes | Lore + UI |
| **`soulBeastType`** | Yes | `Summoning` \| `Enhancement` \| `SpecialAbility` \| `Specialist` |
| **`abilityChain`** | Yes | Reference to a **chain graph** asset (B6.2) |

- Many Soul Beast assets exist in the game; each Beastman uses **at most one**.
- **Different beasts** may have **different node counts** (chain length is per asset, not global).

### B6.2 — Soul Beast chain graph (linear, forward-only)

Reuse the **Spirit Imprint graph machinery** ([Phase 3 D2.0–D2.1](Phase3-Requirements.md)) with these **Beastman constraints**:

#### Root node (required)

- First node = **root**; **no gameplay payload** in v0 samples (empty stat/resistance/passive/active lists).
- **Rank 0** = only root on path (or unbonded → no chain).

#### Topology — **linear chain only (resolved)**

- **Sequential spine:** each non-leaf node has **at most one child** (no sibling branches on the same beast).
- **Forward-only:** path only **extends** toward the next node; no unpick, no swapping an earlier step after a deeper node is chosen (v0).
- **Optional:** Do **not** use `siblingExclusivityGroup` on Beastman chains in v0 (redundant if linear). If a beast asset violates linearity, validation **fails** in editor and degrades to **root only** at runtime with a warning.

#### Node payload (per node)

Each node has **zero or more** of each (lists may be empty):

- **statModifiers**
- **resistanceModifiers**
- **passiveEffects**
- **activeAbilities**

Non-root nodes may use any subset; root must stay empty (same as imprint root policy).

**v0 sample content:** Populate **statModifiers** only (e.g. +1 Strength). Leave **passiveEffects** and **activeAbilities** empty on all sample nodes.

### B6.3 — Soul Beast types (content taxonomy)

Four types for design and shared patterns:

| Type | Design intent (high level) | v0 samples |
|------|---------------------------|------------|
| **Summoning** | Beasts that manifest companions or battlefield presence | Stat-only placeholder nodes |
| **Enhancement** | Beasts that buff the Beastman’s body or stats | Stat-only placeholder nodes |
| **Special Ability** | Beasts that grant distinctive actives (later) | Stat-only placeholder nodes |
| **Specialist** | Beasts with narrow, expert kits | Stat-only placeholder nodes |

- **Every Soul Beast is unique** (its own id, chain, and node ids).
- **Type-shared abilities:** Some nodes or ability packages may be **reused** across beasts of the same type (e.g. shared stat package asset referenced from multiple chains). Exact shared assets are **content**, not framework.
- **Exact abilities** (especially passives and actives) are **TBD**; framework must support lists on nodes without requiring them in v0.

### B6.4 — Runtime state (bonded)

When bonded:

- **`chosenPathNodeIds`:** ordered **root → deepest chosen** on **that beast’s** chain (canonical save).
- **`soulBeastRank`:** derived invariant: `soulBeastRank == chosenPathNodeIds.Count - 1` when the list starts with root.
- **Phase v0 — path source:** Preset on prefab/component, like Barbarian Phase 3 v0.
- **Later — progression:** progression event appends **exactly one** valid **next** node on the chain per event; no multi-node batch.
- **Commitment:** `Permanent` for path picks.

When **unbonded:**

- No chain effects; path ignored.

### B6.5 — Runtime component

- Dedicated component (e.g. `BeastmanSoulBeastRuntime`) or generalized forward-only chain runtime configured with beast + graph.
- Resolves **beast → chain → validate path → apply** with **per-node sources** (Pattern B).
- Invalid path → **root only** (rank 0) + warning log (mirror corrupt imprint save policy).
- Participates in `RacialPassiveHooks.RefreshPassives` / turn hooks when passives exist (v0: typically no-op for samples).

### B6.6 — Composition order (recommended)

1. `RacialLoadoutApplier` (folk baseline)
2. `BeastmanSoulBeastRuntime` (bonded path only, if any)

Passives: participate in shared **`Refresh`** / **`OnTurnStart`** without duplicate firing per source.

### B6.7 — Deliverables: actor prefab vs ability data

**Actor prefab (required)**

- Create **`Assets/Prefabs/Actor/Race/BeastmanPlayer.prefab`** as a **variant** of the shared **`Player`** prefab (same pattern as `DwarfPlayer` / `BarbarianPlayer`).
- Minimum configuration on the prefab (v0):

  | Field / component | Value |
  |-------------------|--------|
  | `m_Name` | `BeastmanPlayer` |
  | `CharacterStats.race` | `Race.Beastman` (`5`) |
  | `CharacterStats.racialSubsystem` | `RacialSubsystemKind.BeastmanSoulBeast` (proposed `6`) |
  | `CharacterStats.bodyCapabilities` | TBD (authoring default; document when equip rules exist) |
  | `RacialLoadoutApplier.loadout` | **`DefaultBeastmanRacialLoadout`** (`Assets/Data/Racial/Beastman/DefaultBeastmanRacialLoadout.asset`) |
  | Other player components | Inherited from base **`Player`** prefab |

- **When Soul Beast runtime ships:** extend this prefab with **`BeastmanSoulBeastRuntime`**:
  - **Default playtest preset (recommended):** **unbonded** (`soulBeastId` empty), no path effects.
  - **Optional playtest preset:** one bonded beast + preset `chosenPathNodeIds` for vertical-slice testing.
- Do **not** create a second Beastman player prefab when runtimes land—extend **`BeastmanPlayer`**.

**Soul Beast “prefab”? (resolved: data assets, not GameObject prefabs)**

- **v0 does not require** a **GameObject prefab** per Soul Beast. Soul Beasts are **data** (`SoulBeastDefinition` ScriptableObjects + chain graph assets), like `ElementalSpiritDefinition`, `AncestorDefinition`, or Spirit Imprint **graph nodes** — not placeable world actors.
- Bond and chain progress are configured on the **Beastman actor** (`BeastmanPlayer` + `BeastmanSoulBeastRuntime`), which references soul beast **data** by id.
- **Required:** soul beast **data assets** under a sensible folder (e.g. `Assets/Data/Racial/Beastman/SoulBeasts/`).
- **Optional (later):** a **companion / visual prefab** field on `SoulBeastDefinition` (e.g. for **Summoning** types) if a beast needs an in-world entity separate from the Beastman. Out of v0; v0 applies chain payloads to the **Beastman** only (see B5.3).

**Ability data (required for full vertical slice; not blocking prefab v0)**

- **`DefaultBeastmanRacialLoadout`** — folk baseline (`requiredRace: Beastman`).
- **`SoulBeastDefinition_*`** — at least **one sample per type** (four beasts minimum for taxonomy proof) or fewer beasts with type variety called out in §10. **Not** GameObject prefabs.
- **`SoulBeastChain_*`** per beast — linear chain; **varying node counts** across at least two beasts; all v0 payloads **stat-only**.

---

## 7. Functional requirements

**F7.1 — Eligibility**  
Only `Race.Beastman` actors use Soul Beast components; others ignore or omit them.

**F7.2 — Bond cap**  
At most one non-empty `soulBeastId` per actor. Editor and runtime validation enforce this.

**F7.3 — No replacement**  
If a bond exists, attempts to set a **different** `soulBeastId` fail validation (editor + runtime). Saves that violate this policy normalize to the **first** valid bond or unbonded per chosen error policy (document in tests).

**F7.4 — Unbonded = no chain**  
Empty `soulBeastId` → Soul Beast runtime applies nothing; no chain reference required.

**F7.5 — Path validation**  
`chosenPathNodeIds` must be a valid **root-to-node walk** on the **bonded beast’s** linear chain. Invalid saves → root only + warning.

**F7.6 — Forward-only path**  
Same as Barbarian F3.2: no backward navigation; v0 preset only; later single-node append via progression event.

**F7.7 — Linear chain**  
Each parent has at most one child on the chain asset. Non-linear graphs are **authoring errors**.

**F7.8 — Presets (v0)**  
Player, ally, and enemy Beastmen use serialized optional beast + path. Default anonymous Beastman NPC: **unbonded**, baseline loadout only.

**F7.9 — Integration hooks**  
Soul Beast passives (when present) use the same refresh/turn pipeline as racial loadout and imprint.

**F7.10 — Active abilities**  
Same policy as Phase 3 §7.5: **data-only** in shipping v0; **test-only** execution gated by editor/development defines. v0 sample nodes omit actives.

**F7.11 — Bond without path**  
If bonded but path is empty or invalid, treat as **rank 0** (root only) after normalization.

**F7.12 — No innate racial nodes**  
Beastmen **must not** receive racial progression from a separate track (no parallel “Beastman talents” in this subsystem). Only baseline loadout + Soul Beast chain.

---

## 8. Non-functional requirements

**N8.1 — Authoring**  
Designers edit Soul Beast assets and chain graphs in the Editor—no per-beast code subclasses.

**N8.2 — Reuse imprint machinery**  
Prefer **one** graph type and validator shared with Barbarian/Dwarf trees, with a **linear-chain** validation mode for Beastman assets.

**N8.3 — Tests**  
At minimum:

- Unbonded Beastman → baseline only; no chain effects.
- Bonded + valid path depth 2 → root + one child payloads apply; rank invariant holds.
- Invalid path → degrades to root only.
- Attempt to set second beast id → rejected.
- Attempt to **replace** beast id on bonded actor → rejected.
- Two beasts in save data (invalid) → rejected or first wins + warning (match F7.3 policy).
- Save round-trip for `soulBeastId` and `chosenPathNodeIds`.
- Chain assets with **different node counts** validate independently.

**N8.4 — Test-only active execution**  
Same as Phase 3 N4.4 when actives are added later.

**N8.5 — Migration**  
Existing Beastman actors without subsystem state: unbonded, empty path, baseline only.

---

## 9. Acceptance criteria (examples)

- Given an **unbonded** Beastman, only **`DefaultBeastmanRacialLoadout`** (if any) applies; no Soul Beast modifiers.
- Given a Beastman bonded to **“Ember Wolf”** with path `[root, feral_strength]`, only payloads for nodes on that path apply; `soulBeastRank == 1`.
- Given a bonded Beastman, attempting to bond **“Stone Tortoise”** instead **does not** change effects or save state (replacement blocked).
- Given a preset path that skips a node on the linear chain, load normalizes to **root only** and logs a warning.
- Given a **Barbarian** actor, Beastman Soul Beast components do not apply effects.
- Given two Soul Beast assets with **5** and **8** nodes respectively, each validates and applies only within its own chain length.
- **`BeastmanPlayer.prefab`** exists under `Assets/Prefabs/Actor/Race/`, drops into a scene, and enters play with `Race.Beastman`, `BeastmanSoulBeast` subsystem, and **`DefaultBeastmanRacialLoadout`** via `RacialLoadoutApplier` (default: **unbonded**).

---

## 10. Content assets (minimum for v0 proof)

| Asset | Purpose | Status |
|-------|---------|--------|
| `DefaultBeastmanRacialLoadout` | `requiredRace: Beastman`; optional baseline stats | **Not created** |
| `BeastmanPlayer` prefab | Variant of `Player`; baseline loadout wired; default **unbonded** | **Not created** |
| `SoulBeastDefinition_*` | ≥1 per **type** (4 types); unique ids and chains | Deferred |
| `SoulBeastChain_*` | Linear graph per beast; **varying lengths**; v0 nodes **stat-only** | Deferred |
| `BeastmanPlayer` + runtime | Add `BeastmanSoulBeastRuntime` when runtime ships | Later |

**v0 sample stat ideas (content, not framework):** e.g. Summoning-type beast: +1 Wisdom on node 1; Enhancement-type: +1 Constitution; etc. Use existing `AttributeModifier` shapes only.

---

## 11. Design decisions (resolved + open)

### 11.1 — Bond optional until event — **Resolved**

- **Zero** Soul Beasts is the default state.
- **Max one** after acquisition; **no replacement**.

### 11.2 — No innate Beastman progression — **Resolved**

- No racial ability tree or slots without a Soul Beast.
- Folk baseline only via `RacialLoadoutDefinition`.

### 11.3 — Chain shape — **Resolved: linear sequential**

- One parent → at most one child per step.
- Contrasts with Barbarian **branching** imprint trees.

### 11.4 — Progression gates — **Resolved**

| Gate | v0 | Later |
|------|-----|-------|
| Acquire Soul Beast | Inspector preset bond (testing only) | **Special acquisition event** |
| Advance chain | Inspector preset `chosenPathNodeIds` | **Special progression event**, **one node** per event |

### 11.5 — Pattern B — **Resolved**

- `RacialLoadoutApplier` = Beastman baseline only.
- Separate runtime for Soul Beast chain.

### 11.6 — Payloads in v0 samples — **Resolved: stats only**

- Data model includes passives and actives on nodes.
- All **example** nodes use **statModifiers** only; other lists empty.

### 11.7 — Open (not blocking v0)

- **Acquisition event** fiction (shrine, trial, NPC, story flag).
- **Progression event** fiction (training, hunt, soul feeding, etc.).
- **Summoning-type** beasts: whether some nodes spawn a **separate actor** vs only modify the Beastman.
- **Type-shared ability** packaging: shared ScriptableObject references vs duplicated node payloads.
- **Baseline Beastman loadout** numbers—content, not framework.
- **`bodyCapabilities`** default on `BeastmanPlayer`.
- **UI:** bond portrait, chain progress, type icon.

---

## 12. Code touchpoints (implementation checklist)

When implementing, expect to add or extend:

| Area | Action |
|------|--------|
| `RacialSubsystemKind` | Add `BeastmanSoulBeast` |
| Data | `SoulBeastDefinition`, `SoulBeastType` enum, chain graph (reuse or alias imprint graph + linear validator) |
| Runtime | `BeastmanSoulBeastRuntime` (or shared forward-only chain runtime) |
| Saves | `soulBeastId`, `chosenPathNodeIds` |
| Prefabs | `BeastmanPlayer`, sample NPCs |
| Tests | Under `Assets/Tests/UnitTests/Racial/` |
| Docs | Link from [Phase0-Glossary](Phase0-Glossary-And-Data-Contracts.md) when shipped |

---

## 13. Related documents

- [Phase 0 — Glossary and data contracts](Phase0-Glossary-And-Data-Contracts.md)
- [Phase 1 — Implementation summary](Phase1-Implementation.md)
- [Phase 3 — Barbarian Spirit Imprint](Phase3-Requirements.md) (path + node payload semantics)
- [Dwarf — Patron Ancestor & common abilities](Dwarf-Ancestor-And-Common-Abilities-Requirements.md)
- [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md)
- [Phase 5 — Additional folk & subsystem shapes](Phase5-Requirements.md)
