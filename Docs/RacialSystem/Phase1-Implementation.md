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

- **`Assets/Data/Racial/DefaultHumanRacialLoadout.asset`**: empty human loadout (`requiredRace: Human`); extend in the Inspector or duplicate for variants.
- **Player prefab** includes **`RacialLoadoutApplier`** referencing that asset so party leader gets racial hooks with no mechanical effect until you add modifiers/passives.
- Create additional assets via **Create → JRogue → Racial Loadout**.
- Party members: add the same applier + asset (or `requiredRace: Unset` empty asset) on each member prefab if they are not the shared Player prefab.

## Still deferred

- Spirit Imprint graph, equipment predicates, full save-game blob for progression.
- Racial active execution from loadout data.
