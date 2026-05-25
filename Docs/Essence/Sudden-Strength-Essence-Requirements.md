# Sudden Strength Essence — Requirements

An essence with a **self-targeted** active, **Sudden Strength**, that grants **+100 Strength** for **10 player phases**, costs **1 Soul Power**, and **cannot be activated while the buff is already active** on the caster.

**Depends on:** `EssenceData`, `EssenceSlotManager`, `AbilityAction`, `CharacterStats`, `Stat` / `StatModifier` (source-based add/remove), `StatType.Strength`, `TurnManager` (`NotifyPartyTurnStart`, `OnPlayerActionComplete`, `CanActorTakeAction`), [Phase 0 stacking glossary](../RacialSystem/Phase0-Glossary-And-Data-Contracts.md).

**Related:** `HealAbility` (`CanExecute` gating, untargeted `ExecuteCore`); `HeroicSpirit` (conditional `AddModifier` / `RemoveModifiersFromSource` / `HasModifierFromSource`); [Telekinesis Essence](Telekinesis-Essence-Requirements.md) (essence active + Soul Power pattern).

**Explicitly out of scope (v0):** UI buff icon/tooltip, save/load of buff duration, applying the buff to allies or enemies, stacking with other Strength buffs beyond global modifier rules, dispel/cleanse interactions.

---

## 1. Goals

**G1 — Timed Strength buff**  
On successful activation, the **caster** gains **+100** effective Strength via the standard stat modifier pipeline.

**G2 — Fixed duration**  
The buff **expires automatically** after **10 player phases** (see §2), removing the modifier cleanly.

**G3 — Soul Power cost**  
Successful activation costs **1** Soul Power (`AbilityAction.soulPowerCost`).

**G4 — No refresh while active**  
If Sudden Strength is **already active** on the caster, activation **fails** before Soul Power or action are spent.

**G5 — Data-first**  
One `EssenceData` + one `SuddenStrengthAbility` asset; duration and bonus are authored fields with the specified defaults.

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Caster** | The `GameObject` passed to `AbilityAction.Execute` — the actor who owns the essence slot (`EssenceSlotManager` on active party member when used from player input). |
| **Player phase** | One party turn cycle: starts when `TurnManager` finishes the enemy phase and runs `NotifyPartyTurnStart()` (`--- New Player Turn ---`). |
| **Buff turn tick** | One decrement of `turnsRemaining` on the buff runtime, fired from that actor’s **player-phase** `NotifyTurnStart` path (see §6.3). |
| **Active buff** | Caster has a live **`SuddenStrengthBuffRuntime`** component **or** `Strength.HasModifierFromSource` for that runtime (see §6.2). |
| **Sudden Strength (buff)** | The +100 Strength modifier and its duration tracker — not the essence passive slot itself. |

**Duration (locked):** **`durationTurns = 10`** means the buff survives **10 buff turn ticks** after application, then expires. Ticks occur on **`NotifyTurnStart`** for the caster (same hook chain as `EssenceSlotManager.NotifyTurnStart` / `RacialPassiveHooks.NotifyTurnStart` today — see §7). The phase in which the ability is **cast** does **not** consume a tick; the **first** tick happens at the **next** player phase start.

---

## 3. Current baseline (as-is)

| Area | Today |
|------|--------|
| **Temporary stat mods** | `Stat.AddModifier(value, source)` / `RemoveModifiersFromSource(source)` / `HasModifierFromSource(source)`. |
| **Turn boundaries** | `TurnManager.NotifyPartyTurnStart()` → per-member `EssenceSlotManager.NotifyTurnStart()` and racial hooks. |
| **Untargeted essence actives** | `requiresTarget = false` → `TryExecuteAbility(slot, index)` → `OnPlayerActionComplete` on success. |
| **CanExecute gate** | `EssenceSlotManager` checks `ability.CanExecute` before execute; logs `{abilityName} conditions not met!` on failure. |
| **Timed buff runtime** | **No** global buff system; **no** existing turn-counted `AbilityAction` buff. |
| **Sudden Strength** | **Not implemented**. |

---

## 4. Content authoring

### D4.1 — `EssenceData` (e.g. `SuddenStrength`)

| Field | Requirement |
|-------|-------------|
| **`essenceName`** | e.g. `Sudden Strength` (essence display name). |
| **`description`** | Short player-facing summary of the active. |
| **`statModifiers` / `resistanceModifiers` / `complexPassives`** | Empty for v0 unless design adds always-on flavor. |
| **`activeAbilities`** | One reference: **`SuddenStrengthAbility`** (§4.2). |

**Suggested paths:**

- `Assets/Resources/Item/Essence/SuddenStrength.asset`
- `Assets/Resources/Item/Ability/SuddenStrength_Standard.asset`

### D4.2 — `SuddenStrengthAbility` : `AbilityAction`

| Field | Value / rule |
|-------|----------------|
| **`abilityName`** | `Sudden Strength` |
| **`soulPowerCost`** | **1** |
| **`requiresTarget`** | **false** (self, no reticle) |
| **`range` / `splashRadius`** | **0** (ignored) |
| **`isMovementAbility`** | **false** |
| **`cooldownTurns`** | **0** (v0) |
| **`strengthBonus`** | **100** (serialized, default 100) |
| **`durationTurns`** | **10** (serialized, default 10) |

**Script:** `SuddenStrengthAbility` under `Assets/Scripts/Abilities/` (namespace aligned with `HealAbility` / `FireballAbility`).

---

## 5. Player flow

### F5.1 — Activation

1. Player triggers essence active (hotkey for essence slot / sub-index).
2. Preconditions:
   - `GameState.PLAYER_TURN`.
   - `TurnManager.CanActorTakeAction(caster)`.
   - `currentSoulPower >= 1`.
   - **`CanExecute(caster) == true`** (§6.1 — buff not already active).
3. On success: `ExecuteCore` applies buff (§6.2), deduct **1** Soul Power, **`OnPlayerActionComplete(caster)`** (same as `HealAbility` / untargeted essences).

### F5.2 — Failure (already buffed)

- `CanExecute` returns **false**.
- **No** Soul Power spent; **no** action consumed.
- **Debug.Log** (recommended, in addition to existing generic message):

  ```text
  [Sudden Strength] Already active on {actorName}.
  ```

  Existing pipeline may still log `{abilityName} conditions not met!` from `EssenceSlotManager`.

### F5.3 — Failure (insufficient Soul Power)

- Existing behavior: `CanAfford` / `TryExecuteInternal` — **Not enough Soul Power!** — no action spent.

---

## 6. Behavior specification

### F6.1 — `CanExecute(GameObject user)`

Return **false** if any of:

| # | Condition |
|---|-----------|
| C1 | `user == null` or missing `CharacterStats`. |
| C2 | `user` already has **`SuddenStrengthBuffRuntime`** component. |
| C3 | `Strength.HasModifierFromSource(existing SuddenStrengthBuffRuntime)` (belt-and-suspenders if component exists). |

Otherwise return **true**.

**R6.1.1** Do **not** check HP or combat state for v0.

### F6.2 — `ExecuteCore(GameObject user)` (success path)

1. If `CanExecute` would fail, return **false** (do not apply twice).
2. Add or ensure **`SuddenStrengthBuffRuntime`** on `user`:
   - Fields: `strengthBonus` (from ability asset), `durationTurns` (from ability asset), `turnsRemaining` initialized to **`durationTurns`** (10).
   - **Source object** for the stat mod: the **runtime component instance** (not the ScriptableObject), so removal is unambiguous per actor.
3. `user.CharacterStats.Strength.AddModifier(strengthBonus, buffRuntime)`.
4. **Debug.Log** (recommended): `[Sudden Strength] Applied +{strengthBonus} STR to {user.name} for {durationTurns} player phases.`
5. Return **true**.

**R6.2.1** If a stale component exists without modifier (should not happen), remove component and treat as inactive before applying.

### F6.3 — Duration and expiry (`SuddenStrengthBuffRuntime`)

| Step | Rule |
|------|------|
| **Tick trigger** | Each **player phase start**, when `NotifyTurnStart` runs for `user` (§7). |
| **Tick action** | If `turnsRemaining > 0`, decrement by **1**. |
| **Expiry** | When `turnsRemaining` reaches **0** after a tick: `Strength.RemoveModifiersFromSource(this)`, destroy/disable component, log e.g. `[Sudden Strength] Expired on {user.name}.` |

**Example timeline**

| Event | `turnsRemaining` | Strength +100 |
|-------|------------------|---------------|
| Cast in phase A | 10 | Active |
| Phase B start (tick 1) | 9 | Active |
| … | … | Active |
| Phase K start (tick 10) | 0 | **Removed** |

So the modifier is active for the **rest of phase A** plus **10** full subsequent player phases (10 ticks).

### F6.4 — Cleanup edge cases

| Case | Behavior |
|------|----------|
| **Actor destroyed** | `OnDestroy` on runtime: `RemoveModifiersFromSource(this)`. |
| **Essence unequipped** | Buff **remains** until duration expires (buff is from active ability, not essence equip). Document for QA; optional future rule: clear on unequip. |
| **Party swap** | Buff stays on the actor who cast, not the active camera member. |
| **Duplicate cast** | Blocked by `CanExecute` (§6.1). |

### F6.5 — Stacking

| Scenario | v0 rule |
|----------|---------|
| Sudden Strength + essence stat mods | Allowed — different sources. |
| Sudden Strength + Sudden Strength | **Blocked** while first buff active. |
| Sudden Strength + other Strength buffs | Allowed if other systems use different `source` objects. |

---

## 7. Integration — turn tick hook

Today `TurnManager.NotifyPartyTurnStart()` calls `EssenceSlotManager.NotifyTurnStart()` for passives only. Sudden Strength needs a **buff tick** on the same boundary.

**R7.1 (required)** — One of:

| Option | Description |
|--------|-------------|
| **A (preferred)** | `SuddenStrengthBuffRuntime` registers with a small **`TurnStartBuffRegistry`** (or static list) ticked from `TurnManager.NotifyPartyTurnStart()` before or after essence passives. |
| **B** | `EssenceSlotManager.NotifyTurnStart()` also calls `GetComponents<SuddenStrengthBuffRuntime>()` and ticks each. |

**R7.2** Ticks run for **all party members** that have the component, not only the active member.

**R7.3** Enemy actors are out of scope unless an enemy equips this essence later; if so, tick from that actor’s enemy turn-start hook (future).

---

## 8. Functional acceptance (F8.x)

**F8.1 — Apply buff**  
Given caster with STR base **B**, no active buff, SP ≥ 1, and an action available: activate → STR effective **B + 100**, SP −1, action consumed, `turnsRemaining == 10`.

**F8.2 — Expire after 10 phases**  
Given buff active with `turnsRemaining == 1`: next player phase start → modifier removed, component gone, STR returns to prior total (other mods unchanged).

**F8.3 — Block re-cast**  
Given buff active: `CanExecute` false; activation does not spend SP or action; log indicates already active.

**F8.4 — Soul Power gate**  
Given SP == 0: cannot activate (existing afford check).

**F8.5 — Per-actor isolation**  
Given two party members, A casts Sudden Strength: only A gets +100; B can still cast if B has the essence and no buff on B.

---

## 9. Tests (recommended)

| Test | Notes |
|------|-------|
| `CanExecute` false when runtime present | Edit Mode |
| `ExecuteCore` adds modifier with correct source | Edit Mode |
| 10 `NotifyTurnStart` ticks → modifier removed | Edit Mode with mock tick driver |
| `TryExecuteAbility` does not deduct SP when `CanExecute` false | Edit Mode |
| Destroy actor → modifier removed | Play Mode / Edit Mode |

---

## 10. Implementation status

| Asset / type | Purpose | Status |
|--------------|---------|--------|
| `SuddenStrength.asset` | `EssenceData` | **Not created** |
| `SuddenStrength_Standard.asset` | `SuddenStrengthAbility` | **Not created** |
| `SuddenStrengthAbility.cs` | Active logic | **Not created** |
| `SuddenStrengthBuffRuntime.cs` | Duration + tick + cleanup | **Not created** |
| Turn tick hook (§7) | Phase countdown | **Not created** |

---

## 11. Open / later

| Topic | v0 choice |
|-------|-----------|
| Buff UI | None |
| Save/load `turnsRemaining` | Not required |
| Essence unequip clears buff | No — expires by time only |
| “Turn” = individual member action | **No** — player phase (§2) |

---

## 12. Traceability to product request

| Request | Section |
|---------|---------|
| +100 Strength for 10 turns then expires | §4.2, §6.2–6.3 |
| Costs 1 Soul Power | §4.2, §5.1 |
| Cannot cast if buff currently active | §6.1, §5.2, §8.3 |
