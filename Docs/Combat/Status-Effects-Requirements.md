# Status effects — Requirements

A **data-driven status system** for temporary combat conditions (Poisoned, Might, Drained, Slowed, Hasted, etc.). v0 fully specifies **Poisoned**; other statuses are **extension points** with placeholders only.

**Depends on:** `CharacterStats`, `Stat` / `StatType.Constitution`, `HealthComponent.TakeDamage`, `DamageType.Poison`, `TurnManager` (`NotifyPartyTurnStart`), `EnemyController.TakeTurn`, `PartyManager`, [Phase 0 stacking glossary](../RacialSystem/Phase0-Glossary-And-Data-Contracts.md), [Undead race — poison immunity](../RacialSystem/Undead-Race-Requirements.md), [Traps](Traps-Requirements.md) (future `TrapEffect` → status), [Sudden Strength](../Essence/Sudden-Strength-Essence-Requirements.md) (timed buffs should migrate to this system over time).

**Related:** `IBattleTarget` (future `ApplyStatusEffect`), `Assets/Scripts/Stats/StatTypes.cs`.

**Explicitly out of scope (v0):** Status UI icons/tooltips, save/load active statuses, poison **stack** increasing damage, enemy Constitution saves, cure-poison items (asset mentioned in progression doc only), Might/Drained/Slow/Haste implementation (schema only).

---

## 1. Goals

**G1 — Extensible definitions**  
Designers add new statuses by creating **`StatusEffectDefinition`** assets (or typed subclasses) without changing core controller switch logic for every effect.

**G2 — Central runtime**  
One **`StatusEffectController`** per actor applies, ticks, and removes statuses; traps, abilities, and items call a shared **`StatusEffectService.TryApply`**.

**G3 — Poisoned (v0)**  
**Poisoned** lasts up to **10 turns**, deals **1 Poison damage** per active turn, and allows **player party members** a **Constitution check** each turn to end early. **Enemies** do not get that save.

**G4 — Immunity integration**  
**Undead** (and future immunity flags) **cannot gain** Poisoned; Poison damage from the status respects existing resistance rules.

**G5 — Separate damage type vs status**  
`DamageType.Poison` (resistance on hit) remains distinct from **Poisoned** (ongoing condition), but Poisoned’s tick uses `DamageType.Poison`.

---

## 2. Architecture — where things live

### 2.1 — Design answer (locked)

| Layer | Location | Purpose |
|-------|----------|---------|
| **Definitions** | `Assets/Data/Status/` | ScriptableObjects: `Status_Poisoned.asset`, future `Status_Might.asset`, … |
| **Definition scripts** | `Assets/Data/Status/` (asmdef `JRogue.Data`) | `StatusEffectDefinition`, `PoisonStatusEffectDefinition` (or shared fields) |
| **Runtime** | `Assets/Scripts/Status/` | `StatusEffectController`, `StatusEffectInstance`, `StatusEffectService` |
| **Ids** | `Assets/Scripts/Status/StatusEffectId.cs` | Stable enum/string ids for code, immunity, saves |
| **Requirements** | `Docs/Combat/Status-Effects-Requirements.md` | This document |

**Not** the canonical home: `PassiveEffect` (wrong lifecycle), raw `Stat` modifiers alone (no duration/save/tick), or `DamageType` enum entries (resistance only).

### 2.2 — Trap / item / ability integration (later)

Callers pass a **`StatusEffectDefinition`** reference:

```text
StatusEffectService.TryApply(target, definition, source, stacks: 1);
```

---

## 3. Glossary

| Term | Meaning |
|------|--------|
| **Status definition** | `StatusEffectDefinition` asset (e.g. Poisoned). |
| **Status instance** | Runtime record on one actor: time left, source, definition id. |
| **Turn tick** | One processing of a status for an actor at that actor’s turn boundary (§5). |
| **Player party member** | `BaseActor` in `PartyManager.partyMembers` (player-controlled). |
| **Enemy actor** | `EnemyController` / hostile NPC using enemy turn pipeline. |
| **Poisoned** | Status id `Poisoned`; not the same as being hit by `DamageType.Poison` once. |
| **Early escape** | Constitution check removes Poisoned before duration expires. |

---

## 4. Extensibility model

### D4.1 — `StatusEffectId` (enum or static class)

```csharp
public enum StatusEffectId
{
    None = 0,
    Poisoned = 1,
    // Future: Might, Drained, Slowed, Hasted, ...
}
```

New statuses **add an enum value** (or string id on definition) + **new asset**; avoid hard-coding only Poison in service public API.

### D4.2 — `StatusEffectDefinition` (base ScriptableObject)

| Field | Purpose |
|-------|---------|
| `statusId` | `StatusEffectId` |
| `displayName` | UI / logs (e.g. `Poisoned`) |
| `description` | Designer notes / player text |
| `maxDurationTurns` | Cap (Poisoned: **10**) |
| `immunityTags` | e.g. `Poison`, `UndeadPoison` for filtering |
| `ignoresPoisonImmunity` | bool, default **false** (v0 unused) |

Menu: **`JRogue/Status/Status Effect Definition`**.

### D4.3 — Typed extensions (recommended)

| Type | When |
|------|------|
| **`PoisonStatusEffectDefinition`** | Poison-only fields (§6); inherits or embeds base. |
| **Future `StatModifierStatusDefinition`** | Might, Drained — timed `AttributeModifier` list. |
| **Future `MovementStatusDefinition`** | Slowed, Hasted — movement/action modifiers. |

**R4.3.1** `StatusEffectController` dispatches tick/apply by `statusId` or virtual method on definition (`IStatusEffectBehavior`) so adding Slowed does not fork the whole controller.

### D4.4 — Registry (optional v0)

`StatusEffectCatalog.asset` — list of all definitions for editor dropdowns. Not required for v0 if references are direct.

### D4.5 — Placeholder statuses (authoring only, no logic v0)

| Status | Intended role (future doc) |
|--------|----------------------------|
| **Might** | Timed stat buff (e.g. +Strength). |
| **Drained** | Timed stat penalty and/or max HP reduction. |
| **Slowed** | Reduced movement or actions per turn. |
| **Hasted** | Extra movement or actions per turn. |

Create **stub assets** optional; **no** tick/apply code until specified.

---

## 5. Turn boundaries and ticking

### F5.1 — When statuses tick

| Actor kind | Tick trigger |
|------------|----------------|
| **Player party member** | Start of each **player phase**: `TurnManager.NotifyPartyTurnStart()` → `StatusEffectController.TickStatuses()` on each member (same boundary as essence `NotifyTurnStart`). |
| **Enemy** | Start of that enemy’s **turn**: `EnemyController.TakeTurn()` **before** `brain.ExecuteTurn` → `TickStatuses()`. |

**R5.1.1** One Poisoned actor can tick **once per player phase** (party) or **once per enemy turn** (enemies), not both.

### F5.2 — Tick order (per actor, per tick)

For each active status instance on that actor (Poison first in v0):

1. **Apply turn effect** (Poison: damage, §6.3).
2. **Early escape check** if applicable (Poison: player CON only, §6.4).
3. If status still active: **decrement** `turnsRemaining` (or increment elapsed; see §6.2).
4. If `turnsRemaining <= 0` after decrement: **remove** status.

**R5.2.1** Apply poison damage **before** the player’s Constitution check on the same tick (damage then chance to shake off).

### F5.3 — Application timing

- `TryApply` can occur any time (trap, attack, spell).
- First tick occurs on the actor’s **next** turn boundary after apply (same convention as [Sudden Strength](../Essence/Sudden-Strength-Essence-Requirements.md): application phase does not tick immediately unless design later adds “tick on apply”).

---

## 6. Poisoned — full specification (v0)

### D6.1 — Asset

| Item | Value |
|------|--------|
| **Path** | `Assets/Data/Status/Status_Poisoned.asset` |
| **Type** | `PoisonStatusEffectDefinition` (or base + poison flag) |
| `statusId` | `Poisoned` |
| `displayName` | `Poisoned` |
| `maxDurationTurns` | **10** |
| `damagePerTick` | **1** |
| `damageType` | `DamageType.Poison` |
| `escapeDifficulty` | **12** (tunable on asset) |
| `escapeUsesConstitution` | **true** for party members only (hard-coded rule, not on asset) |

### D6.2 — Duration

| Rule | Detail |
|------|--------|
| **On apply** | `turnsRemaining = maxDurationTurns` (**10**). |
| **Each tick** | After effect + escape check, `turnsRemaining--`. |
| **Expiry** | When `turnsRemaining` reaches **0**, remove Poisoned. |
| **Reapply while active** | **Refresh** duration to **10** (v0); do not stack damage. |
| **Stacks** | v0: **no** stack count; one Poisoned instance per actor. |

### D6.3 — Damage each turn

On each Poisoned **tick** for that actor:

```text
HealthComponent.TakeDamage(damagePerTick, DamageType.Poison, source);
```

| Field | v0 |
|-------|-----|
| `damagePerTick` | **1** |
| `damageType` | **Poison** |

**Resistance:** Uses existing `GetResistance(Poison)` in `HealthComponent` (Undead racial +999 ⇒ effectively **0** net damage unless immunity blocks application entirely).

**Death:** If HP ≤ 0, normal death pipeline; remove statuses on destroy.

### D6.4 — Constitution check (players only)

| | |
|--|--|
| **Who** | **Player party members** only (`PartyManager.partyMembers` contains actor). |
| **Who not** | **Enemies** — no early escape; only duration and death end Poisoned. |
| **When** | Same tick, **after** poison damage (§5.2). |
| **Formula (v0)** | `1d20 + Constitution.GetValue() >= escapeDifficulty` |
| **Default DC** | **12** (`escapeDifficulty` on asset) |
| **RNG** | Project loot RNG or `UnityEngine.Random`; injectable for tests. |
| **On success** | Remove Poisoned immediately; log `[Status] {name} resisted Poisoned (CON {total} vs {dc}).` |
| **On failure** | Remain poisoned; decrement duration per §6.2. |

**R6.4.1** Constitution is `CharacterStats.Constitution.GetValue()` (includes modifiers).

**R6.4.2** Future: reuse a shared `SavingThrowService` if added; v0 poison-only is fine.

### D6.5 — Application and immunity

`StatusEffectService.TryApply(target, poisonDefinition, source)`:

| Step | Rule |
|------|------|
| 1 | If `target` has **`StatusImmunity.Poisoned`** or Undead poison immunity (§6.6) → **fail**, log, no instance. |
| 2 | If already Poisoned → **refresh** `turnsRemaining` to 10 (§6.2). |
| 3 | Else add instance; log apply. |

### D6.6 — Undead and poison immunity

Per [Undead race requirements](../RacialSystem/Undead-Race-Requirements.md):

| Rule | v0 |
|------|-----|
| **Apply Poisoned** | **Fails** on Undead actors (`Race.Undead` or immunity flag). |
| **Poison tick damage** | If somehow applied, `DamageType.Poison` resistance still applies; prefer **block apply** so DoT never starts. |
| **Implementation** | `StatusEffectService` checks `RacialLoadout` / `StatusImmunity` component on `CharacterStats` — exact hook TBD; **behavior** locked. |

### D6.7 — Logging (debug)

| Event | Message (example) |
|-------|-------------------|
| Apply | `[Status] {target} is now Poisoned ({turns} turns).` |
| Tick damage | `[Status] {target} takes {n} Poison from Poisoned.` |
| CON success | `[Status] {target} shook off Poisoned.` |
| Expire | `[Status] Poisoned expired on {target}.` |
| Immune | `[Status] {target} is immune to Poisoned.` |

---

## 7. Runtime components

### D7.1 — `StatusEffectInstance` (serializable / runtime)

| Field | Purpose |
|-------|---------|
| `definition` | Reference or `statusId` |
| `turnsRemaining` | int |
| `source` | `GameObject` or opaque source id (attribution) |

### D7.2 — `StatusEffectController` (`MonoBehaviour`)

On `BaseActor` / `EnemyController` host (same object as `CharacterStats`).

| API | Purpose |
|-----|---------|
| `bool HasStatus(StatusEffectId id)` | |
| `bool TryApply(StatusEffectDefinition def, GameObject source)` | Delegates to service rules |
| `void TickStatuses()` | Turn boundary |
| `void ClearAll()` | Death / zone transition (future) |

### D7.3 — `StatusEffectService` (static or singleton)

Central **TryApply**, immunity checks, Undead rules, refresh vs stack policy.

### D7.4 — `StatusImmunity` (optional component or flags on `CharacterStats`)

Flags: `ImmunePoisoned`, etc., populated from racial loadout / passives.

---

## 8. Integration hooks (future)

| Source | Behavior |
|--------|----------|
| **Traps** | `TrapDefinition.futureEffects` → `TryApply(Poisoned)` |
| **Attacks** | On-hit status proc |
| **Potions** | Cure: `RemoveStatus(Poisoned)` |
| **IBattleTarget** | `ApplyStatusEffect(StatusEffectId, source)` → service |

---

## 9. Functional acceptance — Poisoned (F9.x)

**F9.1 — Apply to player**  
Party member gains Poisoned; `turnsRemaining == 10`.

**F9.2 — Tick damage**  
After next player phase tick, actor takes **1** Poison damage (resistance applied).

**F9.3 — Player CON escape**  
With high Constitution and/or mocked RNG, actor clears Poisoned before 10 ticks; log success.

**F9.4 — Enemy no CON escape**  
Poisoned enemy endures until 10 enemy-turn ticks without CON check (mock: CON 99, never auto-clears early).

**F9.5 — Expire at 10**  
Without early escape, Poisoned removed after 10th tick decrement.

**F9.6 — Undead immune**  
Undead cannot receive Poisoned; `TryApply` returns false.

**F9.7 — Refresh**  
Reapply while Poisoned resets to 10 turns; damage per tick still 1.

---

## 10. Tests (recommended)

| Test | Notes |
|------|-------|
| Apply + tick damage | Edit Mode, mock `HealthComponent` |
| Player CON success removes | Mock RNG ≥ threshold |
| Enemy no CON path | Enemy controller, 10 ticks only |
| Undead apply fails | `Race.Undead` |
| Refresh duration | Two applies → `turnsRemaining == 10` |

---

## 11. Implementation status

| Deliverable | Status |
|-------------|--------|
| `StatusEffectId` | **Done** |
| `StatusEffectDefinition` / `PoisonStatusEffectDefinition` | **Done** |
| `Status_Poisoned.asset` | **Pending authoring** (create via `JRogue/Status/Poison Definition`) |
| `StatusEffectController` / `StatusEffectService` | **Done** |
| Turn hooks (party + enemy) | **Done** |
| Might / Drained / Slow / Haste | **Placeholder only** (§4.5) |

---

## 12. Traceability

| Request | Section |
|---------|---------|
| Extensible for more statuses | §4 |
| Poison fully specified | §6 |
| Max 10 turns | §6.2 |
| Player CON check each turn to end early | §6.4 |
| 1 damage per active turn | §6.3 |
| Enemies no CON check | §6.4 |
| Where to define statuses | §2 |
