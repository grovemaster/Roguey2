# Phase 0 — Glossary and data contracts

Code contracts live under `Assets/Scripts/Stats/Racial/` (`JRogue.Stats.Racial`) and `Race` in `StatTypes.cs` (`JRogue.Stats`).

## Glossary

| Term | Meaning |
|------|--------|
| **Race** | Ancestry / people: Human, Elf, Barbarian, etc. Stored as `Race : byte` in `StatTypes` with explicit numeric values for saves. |
| **Human class** | Optional specialization for `Race.Human` only (e.g. Summoner). `HumanClass.None` = random civilian / default party human. |
| **Racial loadout** | `RacialLoadoutDefinition` (ScriptableObject): stat/resistance modifiers, passives, and (later) actives. Applied by `RacialLoadoutApplier`. See **Phase1-Implementation.md**. |
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
- Equipment legality hooks and body-capability overrides.
