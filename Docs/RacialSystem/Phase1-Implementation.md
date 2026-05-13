# Phase 1 — Implementation summary

## Character identity (`CharacterStats`)

- **`race`** (`Race` enum from `StatTypes.cs`): ancestry with stable `byte` values (`Unset` = 0, `Human` = 1, …).
- **`[FormerlySerializedAs("folk")]`** on `race` so assets that still have a `folk` YAML key deserialize correctly until re-saved.
- **`humanClass`**, **`racialSubsystem`**, **`bodyCapabilities`** unchanged.

## Racial loadout (`JRogue.Data`)

| Type | Role |
|------|------|
| **`RacialLoadoutDefinition`** | ScriptableObject: stat/resistance modifiers, passives, active list (actives not auto-fired in Phase 1). `requiredRace == Race.Unset` means any race; otherwise must match `CharacterStats.race`. Field was `requiredFolk`; `[FormerlySerializedAs("requiredFolk")]` preserves existing assets. |
| **`RacialLoadoutApplier`** | MonoBehaviour: `Start()` applies loadout; `OnDestroy()` removes. `SetLoadout` for future respec. |
| **`RacialPassiveHooks`** | Static entry points so **essence is optional**: `RefreshPassives` / `NotifyTurnStart` on `GameObject`. |

## Lifecycle wiring

- **`HealthComponent`**: after essence conditional refresh, calls **`RacialPassiveHooks.RefreshPassives`**.
- **`TurnManager.NotifyPartyTurnStart`**: **`RacialPassiveHooks.NotifyTurnStart(member.gameObject)`** then essence `NotifyTurnStart` (racial first).
- **`EnemyController.TakeTurn`**: racial turn start, then essence.

## Authoring

- Create assets via **Create → JRogue → Racial Loadout**. Leave lists empty for “no modifiers.”
- Add **`RacialLoadoutApplier`** to an actor and assign a loadout only when that actor should gain racial SO effects.
- Party humans with no loadout: **omit the component** (or leave loadout null).

## Still deferred

- Spirit Imprint graph, equipment predicates, full save-game blob for progression.
- Racial active execution from loadout data.
