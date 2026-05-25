# Phase 0 — Glossary and data contracts

Code contracts live under `Assets/Scripts/Stats/Racial/` (`JRogue.Stats.Racial`) and `Race` in `StatTypes.cs` (`JRogue.Stats`).

## Glossary

| Term | Meaning |
|------|--------|
| **Race** | Ancestry / people: Human, Elf, Barbarian, etc. Stored as `Race : byte` in `StatTypes` with explicit numeric values for saves. |
| **Human class** | Optional specialization for `Race.Human` only: `None` (civilian), then **Knight**, **Mage**, or **Priest** (one-way). See [Human — Class powers](Human-Class-Powers-Requirements.md). |
| **Racial loadout** | `RacialLoadoutDefinition` (ScriptableObject): stat/resistance modifiers, passives, and (later) actives. Applied by `RacialLoadoutApplier`. See **Phase1-Implementation.md**. No `racialBenefits` / `racialRestrictions` lists. |
| **Racial progression payload** | `IRacialProgressionPayload`: restrictions, benefits, stats, passives, actives (zero or more each). Used by **Tiefling** `CyborgImplantDefinition` and **Undead** skill-tree nodes only. See **Tiefling-Cyborg-Implants-Requirements.md**, **Undead-Race-Requirements.md**. |
| **Racial subsystem** | Progression framework keyed off race: Spirit Imprint (Barbarian), Human specialization, Tiefling implants, Elf contracts, Dwarf patron Ancestor + common abilities, etc. |
| **Commitment policy** | Whether subsystem choices are permanent or respec-able (`RacialCommitmentPolicy`). |
| **Body capabilities** | Mutable flags (horns, stature, …) combined with race rules when resolving equipment; essences/curses/artifacts change these. |

## Stacking rules (design contract)

- **Cross-source:** Racial passives **stack** with modifiers from items, essences, buffs, and other systems, using the same `Stat` modifier pipeline with **distinct source objects** (e.g. passive asset instance, item instance id).
- **Same source, duplicate effect:** Whether two copies of the *same* item/essence stack is **out of scope** for the racial system; racial does not add a special exception — follow global item/essence rules when those exist.
- **Ordering:** When implementation lands, document a single evaluation order (e.g. base → race → equipment → temporary) or priority integers on modifiers.

## `Race` numeric values

`Race` uses explicit `byte` assignments (`Unset` = 0, `Human` = 1, …). **Do not renumber** without a save migration plan.

## Later phases

- Spirit Imprint graph data and save blob for chosen nodes.
- Dwarf patron Ancestor trees and common ability slots — see **Dwarf-Ancestor-And-Common-Abilities-Requirements.md**.
- Beastman Soul Beast bond and linear ability chains — see **Beastman-Soul-Beast-Requirements.md**.
- Undead skill tree and shared progression payload with Tiefling implants — see **Undead-Race-Requirements.md**.
- Human class commitment (Knight / Mage / Priest) — see **Human-Class-Powers-Requirements.md**.
- Equipment legality hooks and body-capability overrides.
