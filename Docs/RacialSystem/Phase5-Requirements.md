# Phase 5 — Requirements: Additional folk & subsystem shapes (content-ready engines)

Phase 5 expands **breadth**: new **`Race` / folk entries** and at least **one new racial subsystem shape** that is **not** a clone of Spirit Imprint (Barbarian). The goal is to **prove the framework generalizes**—different progression verbs (permanent tree vs respec-able implants vs sustained contracts), **shared hooks** (loadout applier, stats, passives, turn boundaries, Phase 4 equip/anatomy), and **save-safe persistence** with versioning.

**Depends on:** Phase 1–2 (folk, optional class, loadout lifecycle, stacking-by-source), Phase 3 (Spirit Imprint v0 as reference vertical), Phase 4 (body capabilities, exclusion bypass, central equip evaluation).

**Explicitly later (Phase 6 or parallel “tooling” track):** character sheet racial section, rich equip failure UX, balance sweep, debug overlay polish—unless pulled in as **minimal** dev-only aids (see §5).

---

## 1. Goals

**G5.1 — Framework proof (two shapes)**  
Deliver **at least two** racial subsystem **patterns** on the same runtime stack—for example:

- **Elf — Spirit contracts (working name):** progression centered on **sustained modes**, upkeep costs (e.g. soul / turns / drain), and explicit **enter / maintain / exit** rules; contrast with Barbarian’s permanent forward-only graph picks.
- **Tiefling — Implants / flesh mods (working name):** progression with **respec or replacement policy** (contrast Barbarian **Permanent**); slots or tiers that can be **changed** under documented constraints.

Exact fantasy naming is content; requirements focus on **mechanical contracts** and **persistence**.

**G5.2 — Content-drop posture**  
New folk + subsystem assets should be **authorable without engine rewrites**: data-driven nodes/options, stable ids, preset archetypes for NPCs.

**G5.3 — Composition**  
Subsystem runtimes **compose** with:

- `RacialLoadoutDefinition` / `RacialLoadoutApplier` (baseline folk loadout).
- Distinct modifier/passive **sources** per subsystem rule (same stacking discipline as Phase 1–3).
- Phase 4 **effective body capabilities** and **exclusion bypass** where racial or sustained effects imply anatomy changes.

**G5.4 — Defaults & NPC policy**  
Each new folk has documented **NPC defaults** (minimal rank / no branches / civilian archetype) analogous to Phase 3 **street Barbarian rank 0**.

---

## 2. Guardrails (approach validation)

These mirror design decisions already endorsed for earlier phases; Phase 5 **must not** violate them.

**GR5.1 — Folk assignment**  
Playable actors **must not** rely on “whatever the prefab serialized.” Prefer **`Race.Unset` + validation** (editor / play-mode assert) until spawn or party data assigns folk—or prefab variants that are never placed raw in scenes without assignment.

**GR5.2 — Human baseline**  
Random humans and default party humans continue to reference a **single canonical default human loadout** (`HumanClass.None`) unless a row overrides; Phase 5 content must not fork that contract silently.

**GR5.3 — Small dumb graphs first**  
Where a subsystem uses graphs or option lists, **nodes stay minimal** (one responsibility per node where possible) so rebalance/rename does not require engine changes.

**GR5.4 — Spirit Imprint stays Barbarian-shaped**  
Do **not** force Elf/Tiefling into the Spirit Imprint graph engine unless design explicitly merges them; Phase 5 validates **alternate data shapes** and runtimes.

---

## 3. Data contracts

### D5.1 — Subsystem registration

- Each new subsystem has a **`RacialSubsystemKind` (or successor)** value and a clear **eligibility rule** (`Race` / folk + optional flags).
- Runtime components **ignore** actors outside eligibility (or are omitted), matching Phase 3 Spirit Imprint behavior.

### D5.2 — Elf contracts (exemplar: sustained subsystem)

**Full spec:** [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md) (summon/upkeep Soul Power, per-spirit levels, toggle actives, preset v0).

Summary for Phase 5 planning:

| Element | Requirement |
|--------|----------------|
| **Contract instances** | `ElementalSpiritDefinition` per spirit; roster on actor; abilities only while **summoned**. |
| **Sustain resource** | **Soul Power** — summon cost + per-turn upkeep per summoned spirit; auto-dismiss if unpaid. |
| **Lifecycle** | summon → apply passives / enable actives → upkeep each turn → dismiss (manual or failed upkeep) → full teardown. |
| **Stacking** | stable source per `spiritId`; Phase 4 body contributions if needed. |

### D5.3 — Tiefling implants (exemplar: respec-capable subsystem)

**Full spec:** [Tiefling — Cyborg implants](Tiefling-Cyborg-Implants-Requirements.md) (seven body slots, replace per slot, Fire resist + horns baseline, preset v0).

Summary for Phase 5 planning:

| Element | Requirement |
|--------|----------------|
| **Slots** | Seven locations: arms, torso, heart, head (cyborg), legs — one implant each. |
| **Commitment policy** | **`RespecAllowed`** — replace removes old implant effects before applying new. |
| **Folk baseline** | Fire damage resistance + intrinsic **Horns** (helmets blocked via Phase 4). |
| **Apply/remove** | stable source per **slot**; NPC install/swap later. |

### D5.4 — Persistence blobs

- Versioned save fields per subsystem (chosen ids, active sustained contract ids, cooldowns if any).
- **Old saves**: new fields **default safely**; migrations documented if enums or layouts shift.
- **Identity snapshot**: continue to persist **intrinsic** folk identity separately from **derived** runtime anatomy unless Phase 0 glossary is explicitly revised.

---

## 4. Functional requirements

**R5.1 — Vertical slices**  
For **each** subsystem delivered in Phase 5:

- One **player-capable** path (preset or minimal UI acceptable).
- One **NPC default** preset (minimal mechanical footprint).
- Automated **tests** proving apply/remove, save-load round-trip or serialization sanity, and interaction with **at least one** shared hook (`Refresh`, `OnTurnStart`, or damage/move—subsystem-specific minimum documented per subsystem).

**R5.2 — Respec / sustained correctness**  
- Tiefling (or respec subsystem): changing choices **fully removes** prior contributions before applying new ones.
- Elf contracts (or sustained subsystem): **no stale upkeep** after exit, death, or load; violating affordability **breaks** contract per authored rules.

**R5.3 — Integration with Phase 4**  
If a contract or implant grants **body capability OR** or **exclusion bypass**, it **must** register through the same **`CharacterStats` contribution API** used by essences (stable keys), so **`EquipmentLegalityEvaluator`** stays the single equip gate.

**R5.4 — Human class (optional stretch)**  
If Phase 5 includes **Human class** beyond `HumanClass.None`, scope it as a **small third vertical** with its own loadout / progression asset—not as implicit edits scattered across prefabs.

**R5.5 — Tooling (recommended, lightweight)**  
Not required for “Phase 5 ship” polish tier, but strongly recommended **during** Phase 5 implementation:

- Editor or debug menu: **simulate apply/remove** for new subsystem presets.
- Log or overlay line: **active subsystem state** (contract on/off, implant ids)—enough to debug sustained/respec bugs without attaching a debugger.

---

## 5. Out of scope (unless promoted)

- Full **character sheet** racial UX and tooltip polish (defer to Phase 6 unless blocking validation).
- **Balance pass** across all folk (iterate after mechanics prove out).
- Porting **every** planned folk—Phase 5 proves **patterns**; remaining folk are content drops using the same contracts.

---

## 6. Acceptance checklist

- [ ] At least **two** distinct subsystem **engines** (Barbarian Spirit Imprint does not count toward this quota).
- [ ] Each engine has **data-only** authoring path for designers (within Unity constraints).
- [ ] Persistence + safe defaults documented for **new** fields.
- [ ] Tests cover apply/remove and lifecycle edge cases listed in **R5.2**.
- [ ] Phase 4 equip gate respected when subsystem touches anatomy (**R5.3**).
- [ ] NPC defaults documented per new folk (**G5.4**).

---

## 7. Relation to roadmap

| Phase | Relationship |
|-------|----------------|
| **4** | Supplies anatomy/equip predicates consumed by racial sustained effects or implants. |
| **5** | **This doc** — breadth + alternate subsystem shapes. |
| **6** | Presentation: sheet, tooltips, debug overlay polish, tuning. |

Recommended sequencing **within** Phase 5 implementation: land **one** subsystem completely (Elf *or* Tiefling), then the second—shared refactoring extracted only when the second reveals real duplication.
