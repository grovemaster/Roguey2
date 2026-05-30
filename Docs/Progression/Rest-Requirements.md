# Rest — Requirements

Pressing a dedicated **Rest** input (DCSS-style) starts a **rest session** that **fast-forwards turns** until every living party member who **uses Soul Power** reaches **full Soul Power**, unless **interrupted**. While resting, the party also recovers **HP** at **1 HP per rest step** (modifiable), up to a **20% heal budget** derived from damage taken since the **last successfully started** rest. Rest is **blocked** while **in combat**, while any party member has a **negative status effect**, or while any party member would **take ongoing hazard damage** from their current tile. Failed checks and “nothing to restore” attempts **do not consume** the player’s turn.

**Depends on:** `CombatThreatCoordinator.IsInCombat`, `TurnManager` / `GameState`, `PartyManager`, `CharacterStats`, `HealthComponent`, `StatusEffectController`, `HazardService`, `SoulPowerRegenerationService`, `InputHandler` / `GameControls`, `PlayerCommandProcessor`.

**Related:** [Soul Power regeneration](Soul-Power-Regeneration-Requirements.md) (per-turn SP regen during rest steps). [Status effects](../Combat/Status-Effects-Requirements.md) (Poisoned v0; polarity §4). [Environmental hazards](../Combat/Environmental-Hazards-Requirements.md) (occupancy damage). [Human — Class powers](../RacialSystem/Human-Class-Powers-Requirements.md) (Mage/Priest do not use Soul Power — rest end rule §7). [Party member death](../Party/Party-Member-Death-Requirements.md). [Main character game over](../Party/Main-Character-Game-Over-Requirements.md).

**Reference:** [Dungeon Crawl Stone Soup — Rest](http://crawl.chaosforge.org/Rest) / `rest` command — repeat wait until HP & MP full, interrupted by monsters or damage.

**Explicitly out of scope (v0):** Rest UI progress bar; rest while enemies are visible but not “InCombat”; Magic/Divine Power rest; hunger; resting to full HP as end condition; partial rest key hold; multiplayer; save/load rest snapshot.

---

## 1. Goals

**G1 — DCSS-like Rest key**  
One new **Rest** binding (suggested default: **`r`**) attempts to start a rest session.

**G2 — Safe start only**  
Rest **cannot start** in combat, with **negative** statuses on any party member, or while **hazard occupancy** would damage a party member.

**G3 — Fast-forward Soul Power**  
During rest, **turns advance automatically** (rest steps) applying normal **Soul Power regeneration** until all SP-using members are full.

**G4 — Bounded HP recovery**  
During the same rest session, grant **+1 HP per rest step** per eligible member (rate modifiers apply), capped by a **20% heal budget** (§6).

**G5 — Track HP since last successful rest**  
Persist per-member **HP snapshots** from the **last rest that actually started** (not failed attempts) to compute the 20% budget.

**G6 — Interrupt on danger**  
Rest **ends immediately** when combat tension becomes **InCombat** (e.g. patrol enters sight).

**G7 — No wasted turns**  
Invalid start and “nothing to restore” **do not** mark the active member as having acted and **do not** end the player phase.

**G8 — Clear logs**  
Use prefix **`[Rest]`** for start, deny, skip, tick, complete, interrupt.

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Rest session** | Automatic loop of **rest steps** until SP-full or interrupted. |
| **Rest step** | One advancement of dungeon time: player-phase regen ticks + enemy phase (§8). |
| **Successfully started rest** | Rest passed all start gates and entered the session loop. |
| **Failed rest attempt** | Start gate failed (combat, status, hazard, etc.) — **no** snapshot update. |
| **Heal budget** | Total HP that may be restored this session via rest HP ticks (§6). |
| **HP regen tick** | +1 HP (or modified rate) toward budget for one member. |
| **Negative status** | `StatusEffectDefinition` with **`polarity == Negative`** (§4). |
| **Positive status** | e.g. **Might** — does **not** block rest. |
| **In combat** | `CombatThreatCoordinator.IsInCombat` (`CombatTensionState.InCombat`). |
| **Ongoing hazard damage** | Party member occupies a **persistent** hazard cell that applies damage on occupancy ticks (§5.3). |

---

## 3. Reference — Dungeon Crawl Stone Soup

| DCSS behavior | JRogue rest (locked) |
|---------------|----------------------|
| Rest until HP & MP full | Rest until **all SP-using party members** at full SP (§7); HP via **budget + 1 HP/step**, not necessarily full HP |
| Blocked in combat | `IsInCombat` blocks **start** and **interrupts** active rest |
| Blocked by some conditions | **Negative status** or **hazard damage** blocks start |
| Interrupted by monsters | **InCombat** ends rest immediately |
| Advances time | Each **rest step** runs player-phase hooks + **enemy phase** (§8) |

---

## 4. Status effects — current baseline & polarity (required)

### 4.1 — Polarity (implemented)

Use **`StatusPolarity`** on `StatusEffectDefinition` — do **not** hard-code `StatusEffectId.Poisoned` for rest or recovery gates.

| Status | `polarity` | Tick logic shipped? |
|--------|------------|---------------------|
| **Poisoned** | **Negative** | Yes |
| **Drained** | **Negative** | No (placeholder) |
| **Slowed** | **Negative** | No (placeholder) |
| **Might** | **Positive** | No (placeholder) |
| **Hasted** | **Positive** | No (placeholder) |

Defaults: `StatusEffectPolarityRules.GetDefaultPolarity(statusId)`; inspector `OnValidate` syncs from `statusId`.

### 4.2 — Rest gate queries (use these)

```csharp
// Per actor
statusController.HasNegativeStatus();

// Party-wide (rest, etc.)
PartyStatusQueries.AnyLivingMemberHasNegativeStatus();
```

**Party-wide:** rest blocked if **any living** party member has a **negative** polarity status.

---

## 5. Start gates (locked)

Rest **start** runs when the player presses **Rest** during **`GameState.PLAYER_TURN`**, active member can act (same as Wait/abilities), and no blocking UI (`BlocksFloorGameplay`).

Evaluate **in order**; first failure logs and **returns without consuming a turn**:

### 5.1 — Combat

| Condition | Log (example) | Turn |
|-----------|---------------|------|
| `CombatThreatCoordinator.IsInCombat` | `[Rest] Cannot rest while in combat.` | **Not** consumed |

### 5.2 — Negative status (any party member)

| Condition | Log | Turn |
|-----------|-----|------|
| Any living member `HasNegativeStatus()` | `[Rest] Cannot rest while a party member is under a negative status effect ({name}).` | **Not** consumed |

### 5.3 — Ongoing hazard damage (any party member)

| Condition | Log | Turn |
|-----------|-----|------|
| Member occupies a **persistent** hazard that applies **occupancy damage** on `TickOccupancyOnPlayerPhaseStart` / wait / enter (v0: **Poison Gas** — 1 damage) | `[Rest] Cannot rest while a party member is exposed to hazardous terrain ({hazard displayName}).` | **Not** consumed |

**Query (suggested):** `HazardService.WouldDealOccupancyDamageTo(actor)` — true if `GetHazardAt(actor.GridPosition)` is persistent and has non-zero occupancy damage.

**Not required for v0:** blocking rest merely because a **passage** hazard (Lava) is **nearby**; only **occupancy** that **deals damage**.

### 5.4 — Nothing to restore

If **no** living party member needs Soul Power restoration **and** **no** member has **remaining heal budget > 0** for this session (§6.4), log and exit:

`[Rest] Rest is not necessary.`

**Turn not consumed.**

**Soul Power need:** `UsesSoulPower` and `currentSoulPower < MaxSoulPower`.

**Heal need:** `remainingHealBudget > 0` for at least one member (computed at start, §6).

### 5.5 — All gates passed

Log: `[Rest] Rest started.`

Enter **rest session** (§8). Record snapshots (§6.2). **Turn consumption** for the initiating key press: **locked v0 — starting rest does not call `OnPlayerActionComplete`**; the session runs under `GameState.BUSY` (or `RESTING`) until complete/interrupted, then returns to player turn with party **not** marked acted for that press.

---

## 6. HP recovery — 20% budget & snapshots (locked)

### 6.1 — Per-member heal budget at rest **start**

For each **living** party member `M`:

```text
if first successful rest ever for M:
    healBudget[M] = floor(0.20 * MaxHP[M])
else:
    hpLost = hpAtLastSuccessfulRestStart[M] - currentHP[M]
    healBudget[M] = floor(0.20 * max(0, hpLost))
```

- **`hpAtLastSuccessfulRestStart`** — stored HP when the **previous** rest session **successfully started** (not failed attempts).
- **First rest:** use **20% of `MaxHP`**, not “20% of missing HP”.
- **`healBudget`** is an integer **remaining HP** pool for **this** session only.

### 6.2 — Snapshots on successful start

When rest **starts** (§5.5), for each living member:

```text
hpAtLastSuccessfulRestStart[M] = currentHP[M]   // commit at START of this rest
```

Persist in **`PartyRestState`** (party singleton or `PartyManager` component) until the **next** successful rest start overwrites it.

**Failed rest attempts** do **not** update `hpAtLastSuccessfulRestStart`.

### 6.3 — HP regen during rest — 1 HP per rest step

Each **rest step** (§8), for each living member with `remainingHealBudget > 0` and `currentHP < MaxHP`:

```text
hpGain = effectiveHpRegenPerStep   // v0 default: 1
actualGain = min(hpGain, remainingHealBudget, MaxHP - currentHP)
currentHP += actualGain
remainingHealBudget -= actualGain
```

**v0 default:** `effectiveHpRegenPerStep = 1`.

**Future modifiers** (same spirit as Soul Power regen): passives, actives, items, buffs add to **`effectiveHpRegenPerStep`** (additive, then `max(0, …)`).

Suggested service: **`HealthRegenerationService`** parallel to `SoulPowerRegenerationService`.

### 6.4 — “Nothing to restore” revisited

Rest is unnecessary when **every** living member satisfies:

- (`!UsesSoulPower` OR `currentSoulPower >= MaxSoulPower`), **and**
- `remainingHealBudget == 0` **if** computed at hypothetical start (for deny-before-start, compute budgets as §6.1 without mutating snapshots).

---

## 7. Soul Power recovery during rest (locked)

### 7.1 — End condition

Rest session **ends successfully** when **all living party members** with `HumanClassRules.UsesSoulPower(humanClass)` have:

```text
currentSoulPower >= MaxSoulPower
```

Members with **`UsesSoulPower == false`** (Mage/Priest) do not affect this condition.

**Edge case:** If **no** party member uses Soul Power, rest ends after **one** rest step if only HP budget applied, or immediately if §5.4 — document in implementation: **end after first step when SP condition vacuously true and HP budgets exhausted**.

### 7.2 — Per rest step

Invoke existing **`SoulPowerRegenerationService.TickRegeneration(member)`** for each living party member **before** Elf upkeep (same order as [Soul Power regeneration](Soul-Power-Regeneration-Requirements.md) §6.1).

Rest **does not** use a special faster SP rate unless a future item says so; **skipping turns** provides acceleration.

### 7.3 — Elf upkeep during rest

During each rest step’s player-phase boundary, **Elf spirit upkeep** still runs **after** SP regen. If upkeep cannot be paid, spirits dismiss per existing Elf rules — **does not** by itself interrupt rest unless resulting combat or damage triggers §9.

---

## 8. Rest session loop (locked)

### 8.1 — Game state

While resting:

```text
GameState.BUSY   // v0: reuse BUSY; optional future GameState.RESTING
```

Block normal movement, abilities, inventory, Wait, and **Rest** re-press (ignore or “already resting” log).

### 8.2 — One rest step

```text
1. Interrupt check (§9) — if true, exit session
2. Player phase boundary for each living party member (order):
   a. SoulPowerRegenerationService.TickRegeneration
   b. RacialPassiveHooks.NotifyTurnStart (Elf upkeep, etc.)
   c. EssenceSlotManager.NotifyTurnStart
   d. StatusEffectController.TickStatuses
   e. HealthRegenerationService.TickRestHeal (1 HP + budget, §6.3)
   f. HazardService.TickOccupancyOnPlayerPhaseStart
3. If any member took damage in step 2f or status tick poison: interrupt (§9.2)
4. Re-evaluate SP end condition (§7.1) — if met, exit success
5. Run full enemy phase (TurnManager enemy sequence)
6. CombatThreatCoordinator.EvaluateThreat after enemy phase
7. Interrupt check again
8. If not done, repeat from step 1
```

**Coroutine / async:** Implement as `TurnManager` or **`RestSessionService`** coroutine with yields between steps for frame spread (optional v0: synchronous is OK for tests).

### 8.3 — Session exit

| Outcome | Log | Snapshots |
|---------|-----|-----------|
| **Success** (SP full) | `[Rest] Rest complete.` | `hpAtLastSuccessfulRestStart` already updated at **start** |
| **Interrupted** | `[Rest] Rest interrupted: {reason}.` | Snapshots **unchanged** from this session’s start commit (still reflect **this** rest’s start HP until next successful start) |
| **GAME_OVER** | No rest | — |

After exit: `GameState.PLAYER_TURN`; party **not** auto-marked as having acted for the rest key.

---

## 9. Interrupt rules (locked)

### 9.1 — Combat interrupt

If **`IsInCombat`** becomes true **during** a rest step (typically after enemy phase or `EvaluateThreat`):

```text
[Rest] Rest interrupted: combat started.
```

End session **immediately**; do not apply further rest steps.

Subscribe to **`CombatThreatCoordinator.OnEnterCombat`** while session active, or poll after each step.

### 9.2 — Damage & negative status during rest

**v0 locked:** Also interrupt if **any living party member**:

- Takes **any damage** (hazard occupancy, poison tick, trap, enemy attack during enemy phase), OR
- Gains a **negative** status during the step, OR
- **`HasNegativeStatus()`** becomes true (poison applied mid-rest)

Log: `[Rest] Rest interrupted: party took damage.` / `… negative status.`

**Enemy attacking during rest enemy phase** → damage → interrupt (expected DCSS-like behavior).

### 9.3 — No interrupt

- Positive statuses (Might) — OK.
- SP not yet full without damage — continue.
- HP budget exhausted but SP still regaining — **continue** until §7.1 met.

---

## 10. Input (locked)

### 10.1 — `GameControls`

Add action **`Rest`** (button).

| Binding (v0 default) | Notes |
|----------------------|-------|
| **`r`** keyboard | Distinct from **Wait** (existing binding). |

Regenerate `GameControls.cs` from Input Actions asset after edit.

### 10.2 — `InputHandler`

```csharp
public void OnRest(InputAction.CallbackContext context)
{
    if (IsContextInvalid(context) || BlocksFloorGameplay()) return;
    RestSessionService.TryStartOrDeny();
}
```

**Invalid contexts:** `GAME_OVER`, `ENEMY_TURN`, `BUSY` (including already resting), targeting mode.

### 10.3 — `PlayerCommandProcessor`

**v0:** Rest is **not** a `PlayerCommandKind` — handled directly by `RestSessionService` to avoid turn-completion side effects. Alternative (if unified): `PlayerCommandKind.Rest` with special casing — prefer **separate service**.

---

## 11. Suggested code layout

| Piece | Location |
|-------|----------|
| **`PartyRestState`** | `Assets/Scripts/Manager/Progression/PartyRestState.cs` — snapshots, remaining heal budgets |
| **`RestSessionService`** | `Assets/Scripts/Manager/Progression/RestSessionService.cs` — gates, loop, interrupt |
| **`HealthRegenerationService`** | `Assets/Scripts/Manager/Progression/HealthRegenerationService.cs` — HP/step + modifiers |
| **`StatusPolarity`** | `Assets/Data/Status/` or `Assets/Scripts/Status/` |
| **Input** | `GameControls.inputactions`, `InputHandler.OnRest` |

### 11.1 — Public API sketch

```csharp
public static class RestSessionService
{
    public static bool IsResting { get; }
    public static bool CanStartRest(out string denyReason);
    public static void TryStartOrDeny();
    public static void CancelRest(string reason);  // interrupt
}
```

---

## 12. Acceptance criteria

| ID | Test |
|----|------|
| **AC1** | In combat, Rest logs deny; active member **not** marked acted. |
| **AC2** | Party member Poisoned, Rest denied; turn not consumed. |
| **AC3** | Member on Poison Gas tile, Rest denied. |
| **AC4** | Full SP + no heal budget, Rest logs “not necessary”; turn not consumed. |
| **AC5** | Valid rest: SP regens over multiple steps until all SP users full. |
| **AC6** | First rest: heal budget = 20% max HP; heals 1 HP/step until budget or max. |
| **AC7** | Second rest after damage: budget = 20% of HP lost since **last successful rest start**. |
| **AC8** | Failed rest attempt does not update last-rest HP snapshot. |
| **AC9** | Enemy enters combat during rest → session ends, log interrupt. |
| **AC10** | Might (positive) does not block rest when implemented. |

---

## 13. Implementation checklist

- [x] `StatusPolarity` + field on `StatusEffectDefinition`; defaults via `StatusEffectPolarityRules`
- [x] `StatusEffectController.HasNegativeStatus()` + `PartyStatusQueries.AnyLivingMemberHasNegativeStatus()`
- [x] `HazardService.WouldDealOccupancyDamageTo(actor)`
- [x] `PartyRestState` snapshots + heal budgets
- [x] `HealthRegenerationService` (1 HP/step, modifiers stub)
- [x] `RestSessionService` loop + interrupts
- [x] `GameControls` **Rest** (`r`) + `InputHandler.OnRest`
- [x] `TurnManager` / `GameState` integration (`BUSY`)
- [x] Subscribe `OnEnterCombat` interrupt
- [x] Unit tests: gates, budget math, interrupt (partial)
- [x] Update [Soul-Power-Regeneration-Requirements.md](Soul-Power-Regeneration-Requirements.md) cross-link (rest uses normal SP tick)
- [ ] Play-mode AC1–AC10

---

## 14. Document history

| Date | Note |
|------|------|
| 2026-05-29 | Initial requirements — DCSS-inspired rest, SP/HP recovery, combat/status/hazard gates |
