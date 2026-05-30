# Soul Power regeneration — Requirements

**Soul Power** (the party’s essence-casting resource, analogous to **magic points** in Dungeon Crawl Stone Soup) **regenerates over time** at a **computed rate** that starts from **role-specific base rates**, can be overridden per **enemy species**, and is further modified by passives, actives, items, buffs, debuffs, and future systems. Regeneration **never** drives the effective rate below **zero**. **Current Soul Power** is **shown as an integer** even when regeneration math uses fractional values. **Current Soul Power** must **not exceed `MaxSoulPower`** after regeneration unless an explicit **over-max grant** applies (ability, item, buff, etc.).

**Depends on:** `CharacterStats` (`currentSoulPower`, `MaxSoulPower`, `HumanClassRules.UsesSoulPower`), `TurnManager` (`NotifyPartyTurnStart`, `EnemyTurnSequence`), `EnemyController` / `EnemySpeciesDefinition`, `AbilityAction.soulPowerCost`, `HumanClassAbilityResources`, [Phase 0 stacking glossary](../RacialSystem/Phase0-Glossary-And-Data-Contracts.md) (`Stat` / `StatModifier` sources), [Status effects](../Combat/Status-Effects-Requirements.md) (future regen-affecting statuses).

**Related:** [Human — Class powers](../RacialSystem/Human-Class-Powers-Requirements.md) (Mage/Priest **do not** use Soul Power — use Magic/Divine Power pools separately when those systems gain regen). [Elf — Elemental Spirit contracts](../RacialSystem/Elf-ElementalSpirit-Contracts-Requirements.md) (summon cost + **upkeep** deducts Soul Power at turn boundary — **after** or **before** regen must be ordered explicitly in §6). [Sudden Strength essence](../Essence/Sudden-Strength-Essence-Requirements.md), [Telekinesis essence](../Essence/Telekinesis-Essence-Requirements.md) (spend pattern). [Party experience & leveling](Party-Experience-And-Leveling-Requirements.md) (level-up may raise max and refill gap). [Party member death](../Party/Party-Member-Death-Requirements.md) (dead members do not regen).

**Explicitly out of scope (v0):** Magic Power / Divine Power regeneration (document parallel rules when those pools ship); UI bar animation; save/load of regen accumulator; aut-based action-time scaling (JRogue uses discrete turns, not DCSS auts); “no regen while in combat” toggles; hunger/faith systems; MP-link mutations. **Rest** fast-forward is specified in [Rest](Rest-Requirements.md) (uses this service’s per-turn tick).

**Related:** [Rest](Rest-Requirements.md) — automatic multi-turn SP recovery session (uses `TickRegeneration` each rest step).

---

## 1. Goals

**G1 — Turn-based regeneration**  
Living entities that **use Soul Power** passively recover it on a **defined turn boundary**, similar in spirit to DCSS MP regen each turn.

**G2 — Separate base rates**  
**Party members** and **enemies** have **different default base regeneration rates** (locked v0: **1.0** and **0.5** respectively).

**G3 — Species overrides (enemies)**  
An **enemy species** may define a **special base rate** that replaces the generic enemy default for all instances of that species.

**G4 — Extensible modifiers**  
The **fully calculated** regeneration rate may be increased or decreased by **passive abilities**, **active abilities**, **items**, **buffs**, **debuffs**, and other systems without rewriting the core tick.

**G5 — Non-negative rate**  
After all modifiers, **effective regeneration rate ≥ 0**. Negative modifiers cannot flip the rate below zero.

**G6 — Max cap by default**  
After regeneration, clamp `currentSoulPower` to **`MaxSoulPower`** unless an **over-max exception** (§8) applies on that tick.

**G7 — Integer display**  
UI and player-facing logs show **whole-number** current (and max) Soul Power; internal simulation may use **fractional** accumulation (§5).

**G8 — Debug traceability**  
Optional verbose logs use prefix **`[SoulRegen]`** for rate breakdown and grants (off by default in release).

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Soul Power (SP)** | `CharacterStats.currentSoulPower` — current pool. |
| **Max Soul Power** | `CharacterStats.MaxSoulPower` — cap from Int/Wis/level (`HumanClassRules.ComputeMaxSoulPower`). |
| **Base regen rate** | Starting float before modifiers; from party default, enemy default, or species override. |
| **Effective regen rate** | Final float after all modifiers, **clamped ≥ 0**. |
| **Regen tick** | One application of regeneration for one actor at that actor’s turn boundary (§6). |
| **Accumulator** | Hidden fractional counter (DCSS-style) that converts effective rate into whole SP grants (§5). |
| **Over-max grant** | Deliberate SP increase that may exceed `MaxSoulPower` until consumed or clamped later. |
| **Party member** | `BaseActor` in `PartyManager.partyMembers` with `UsesSoulPower == true`. |
| **Enemy actor** | `EnemyController` (or future hostile `BaseActor`) with SP pool & regen enabled. |

---

## 3. Reference — Dungeon Crawl Stone Soup MP regeneration

DCSS treats MP regeneration separately from HP regeneration but uses the **same accumulator pattern** for both.

### 3.1 — Base MP regen (BMRR → TMRR)

Per turn, the game computes a **base magic regeneration rate** (historically `7 + max_magic_points / 2` in `player_mp_regen()`), then applies **multipliers** (e.g. Mana Regeneration mutation ×2, attuned amulet +50%) and **flat bonuses** from equipment, mutations, god effects, etc. The result is **total magic regeneration rate (TMRR)** — a value added to a counter **each turn**.

Sources: [CrawlWiki — Magic points](http://crawl.chaosforge.org/Magic_points), `crawl-ref/source/player.cc` (`player_mp_regen`), `crawl-ref/source/player-reacts.cc` (`_regenerate_hp_and_mp`).

### 3.2 — Accumulator (100 points = 1 MP)

While `magic_points < max_magic_points`:

1. Add `TMRR × (action_delay / baseline_delay)` to a hidden **`magic_points_regeneration`** counter.
2. **While** counter **≥ 100**: increase current MP by **1**, subtract **100** from counter (carry remainder).
3. Counter is always in **[0, 99]** after processing.

Regeneration **stops accumulating** when already at max MP (no overfill via regen).

### 3.3 — Design takeaways for JRogue

| DCSS idea | JRogue adoption (locked) |
|-----------|---------------------------|
| Per-turn regen, not per-action-button in v0 | Regen on **turn boundary** (§6) |
| Fractional internal math, integer MP display | **Accumulator** + integer `currentSoulPower` (§5, §9) |
| Base rate + multipliers + flats | **Base rate** + modifier pipeline (§7) |
| Rate can be boosted by items/mutations | Items, buffs, passives, etc. (§7) |
| No regen below 0 rate | **Clamp effective rate ≥ 0** (§7.4) |
| No MP over max from regen | Clamp after regen unless **over-max grant** (§8) |
| Slower enemies / different species | **Enemy base 0.5**, species override (§7.2) |

JRogue **does not** copy DCSS’s `7 + MMP/2` formula; v0 uses the **authored base rates** in §7.1 instead. The **accumulator scale** is chosen so that **rate 1.0 ⇒ 1 Soul Power per regen tick** (§5.2).

---

## 4. Current baseline (as-is)

| Area | Today |
|------|--------|
| **Soul Power pool** | `CharacterStats.currentSoulPower`, max from Int×5 + Wis×5 + `levelSoulPowerBonus` when `UsesSoulPower`. |
| **Spend** | Abilities / essences deduct via `AbilityAction`, `HumanClassAbilityResources`, Elf summon/upkeep, etc. |
| **Regeneration** | `SoulPowerRegenerationService` on turn boundaries; party tick **before** `RacialPassiveHooks` (Elf upkeep). |
| **Turn hooks** | `TurnManager.NotifyPartyTurnStart` ticks racial passives, essences, statuses — **no** SP regen. |
| **Enemy species** | `EnemySpeciesDefinition` — XP, loot; **no** `soulPowerRegenRate` field. |
| **Mage / Priest** | `MaxSoulPower == 0`; should **skip** Soul Power regen entirely. |
| **Display** | `currentSoulPower` is already `int`; no fractional pool field. |

---

## 5. Regeneration model (locked)

### 5.1 — Who regenerates Soul Power

Regeneration runs only when **all** are true:

1. Actor is **alive** (`currentHP > 0` or equivalent death guard).
2. `HumanClassRules.UsesSoulPower(humanClass)` is **true** (party **`None`** and **`Knight`** today).
3. `MaxSoulPower > 0`.
4. Not in a future **`SoulRegenSuppressed`** state (status, zone, cutscene) — **v0:** no suppression list; hook reserved.

**Enemies:** Same rules if given a Soul Power pool (author `currentSoulPower` / max on spawn). Enemies without SP authoring **skip** regen.

### 5.2 — Accumulator scale (DCSS-aligned)

Use a hidden **`soulPowerRegenAccumulator`** (float, **per actor**, runtime-only, not serialized in v0):

```text
REGEN_SCALE = 100   // 100 accumulator points = 1 Soul Power (same spirit as DCSS)
```

On each **regen tick** (§6), if `currentSoulPower < MaxSoulPower`:

```text
effectiveRate = max(0, ComputeEffectiveSoulPowerRegenRate(actor))
accumulator += effectiveRate * REGEN_SCALE
while accumulator >= REGEN_SCALE and currentSoulPower < MaxSoulPower:
    currentSoulPower += 1
    accumulator -= REGEN_SCALE
```

After the loop, **clamp** `currentSoulPower = min(currentSoulPower, MaxSoulPower)` unless an **over-max grant** is being applied on the same tick (§8).

**Examples (v0 base rates):**

| Base + mods | Effective rate | Result per tick |
|-------------|----------------|-----------------|
| Party default | **1.0** | +1 SP (when below max) |
| Enemy default | **0.5** | +1 SP every **2** ticks on average |
| Party + 0.5 item | **1.5** | +1 SP guaranteed; +1 SP again when accumulator carries ≥100 |
| Party − 0.3 debuff | **0.7** | +1 SP every ~1.43 ticks |

### 5.3 — At max Soul Power

When `currentSoulPower >= MaxSoulPower` **before** the tick:

- **Do not** add to the accumulator (mirror DCSS: no MP regen at full).
- **Optional v0:** reset accumulator to **0** on hitting max to avoid “stored” burst after spending — **locked: reset accumulator to 0 when SP reaches max** at end of any tick that ended at max.

### 5.4 — Effective rate is fractional; grants are integer

The **rate** may be **non-integer**; **grants** from regeneration are always **whole +1** steps via the accumulator. Do not round the rate to int before accumulation.

---

## 6. Turn timing (locked)

### 6.1 — Party members

**When:** Start of **player phase**, per living party member, inside `TurnManager.NotifyPartyTurnStart()` (same pass as `RacialPassiveHooks.NotifyTurnStart`, `EssenceSlotManager.NotifyTurnStart`, `StatusEffectController.TickStatuses`).

**Order relative to Elf upkeep (locked recommendation):**

```text
1. Soul Power regeneration tick (this doc)
2. Elf elemental spirit upkeep deduction (existing contract doc)
3. Other OnTurnStart hooks that spend SP
```

Rationale: regen first so upkeep can spend freshly regenerated SP; avoids auto-dismiss solely because upkeep ran before regen on the same boundary.

### 6.2 — Enemies

**When:** At the **start of that enemy’s turn**, immediately before `EnemyController.TakeTurn()` AI runs (inside `TurnManager.EnemyTurnSequence` loop).

**Not** once per enemy phase batch at the first enemy only — each enemy with SP gets its own tick.

### 6.3 — GAME_OVER / BUSY

No Soul Power regeneration while `GameState.GAME_OVER`. **v0:** still regen during `ENEMY_TURN` / `PLAYER_TURN` normally; **no** regen during `BUSY` if that state blocks turn boundaries (if `BUSY` is transient, skip regen only for actors that did not receive a turn boundary).

### 6.4 — Multi-tile / summoned allies

Follow the same rule as the **controller** that owns the turn boundary: party member → player phase; enemy → enemy turn. Future summons use the same hook as their controller type.

---

## 7. Computing effective regeneration rate

### 7.1 — Base rates (v0 locked)

| Actor class | Base rate (float) | Source |
|-------------|-------------------|--------|
| **Party member** | **1.0** | Global default constant |
| **Enemy** | **0.5** | Global default constant |
| **Enemy (species)** | **Authoring** | `EnemySpeciesDefinition.soulPowerRegenRate` when **≥ 0** and **useCustomSoulPowerRegen** (or `-1` sentinel = use default) |

**Species override:** If the species asset defines a custom rate, it **replaces** the generic enemy **0.5**, not adds to it. Document per-species examples in data (e.g. arcane enemy **1.0**, mindless brute **0.25**).

**Party members** do **not** use species assets in v0; racial `Race` / class may add **modifiers** (§7.3), not replace the 1.0 base.

### 7.2 — Suggested data (enemies)

Add to `EnemySpeciesDefinition`:

```csharp
[Header("Soul Power")]
public bool usesSoulPower = false;  // if true, spawn with SP pool (separate authoring)
public float soulPowerRegenRate = -1f; // < 0 => use global enemy default (0.5)
```

Spawn/setup must initialize `currentSoulPower` and max for enemies that cast abilities.

### 7.3 — Modifiers (additive v0)

**Locked v0 stacking:** All modifiers are **additive** on the base rate, then clamp:

```text
effectiveRate = max(0, baseRate + sum(flatModifiers))
```

| Source type | Example | API direction |
|-------------|---------|----------------|
| **Passive ability** | Racial passive “+0.2 SP regen” | Register flat float with `object source` |
| **Active ability** | Channeled aura +0.5 while active | Duration-tied registration |
| **Item / equipment** | Ring of soul recovery +0.3 | Equip/unequip register |
| **Buff / debuff** | Hasted soul +0.25; drained −0.4 | `StatusEffectController` or stat-like service |
| **Essence passive** | Essence slot modifier | `EssenceSlotManager` hook |
| **Future** | Zone aura, difficulty | Environment service |

**Multiplicative modifiers** (×1.5 regen) are **out of scope v0**; add in a later revision with explicit ordering after flats.

### 7.4 — Non-negative clamp

```text
effectiveRate = Mathf.Max(0f, baseRate + modifierSum);
```

Individual modifiers may be negative; the **sum** is clamped **once** at the end.

### 7.5 — Suggested service API

Central coordinator (names indicative):

```csharp
public static class SoulPowerRegenerationService
{
    public static float ComputeEffectiveRate(GameObject actor);
    public static void TickRegeneration(GameObject actor);
    public static void RegisterFlatModifier(GameObject actor, float delta, object source);
    public static void UnregisterModifiersFromSource(GameObject actor, object source);
}
```

Runtime accumulator lives on a small **`SoulPowerRegenerationState`** component or inside the service’s per-actor map keyed by `instanceId`.

---

## 8. Maximum Soul Power and over-max exceptions

### 8.1 — Default cap after regen

After regeneration grants:

```text
currentSoulPower = min(currentSoulPower, MaxSoulPower);
```

`MaxSoulPower` may change mid-run (level-up, debuff); re-clamp on next tick.

### 8.2 — Over-max grants (explicit only)

These **may** raise `currentSoulPower` above `MaxSoulPower`:

- Active ability effect (“overflow 3 SP”)
- Item on use
- Buff that stores temporary SP buffer
- Designer debug cheat

**Regeneration never** causes over-max.

Suggested API:

```csharp
public static void GrantSoulPower(GameObject actor, int amount, bool allowOverMax = false);
```

When `allowOverMax == false`, grant is clamped to max. When **true**, SP may exceed max until spent or a **`ClampSoulPowerToMax()`** runs (end of turn, buff end, etc.).

### 8.3 — Level-up refill

Existing level-up logic may increase max and add the delta to current — **not** considered regeneration; keep `PartyExperienceService` behavior, then clamp with the same max rules.

---

## 9. Display and rounding (locked)

| Surface | Rule |
|---------|------|
| **HUD / character sheet** | Show `currentSoulPower` and `MaxSoulPower` as **integers** (no decimal places). |
| **Combat log** | Whole numbers for spend/grant (“Soul Power 4/12”, “+1 Soul Power (regen)”). |
| **Internal accumulator** | **Never** shown to player in v0. |
| **Effective rate (debug)** | May log as float with one decimal in `[SoulRegen]` verbose mode. |

If UI ever needs “regen per turn” preview, show **effective rate** rounded **to one decimal** (e.g. `0.5`, `1.0`, `1.5`) — not the raw accumulator.

---

## 10. Interaction with spend and costs

| Event | Regen interaction |
|-------|-------------------|
| **Ability `soulPowerCost`** | Spend after `CanAfford`; no regen on same frame unless turn boundary fires. |
| **Elf upkeep** | Runs **after** regen on same boundary (§6.1). |
| **Spend below max** | Next regen tick resumes accumulator add. |
| **Death** | No regen; accumulator discarded. |
| **Mage/Priest commit** | `MaxSoulPower = 0`; disable regen hooks for that actor. |

---

## 11. Acceptance criteria

| ID | Test |
|----|------|
| **AC1** | Party member at 0/`MaxSoulPower` with default rate **1.0** gains **+1 SP** per player phase until max. |
| **AC2** | Enemy with default rate **0.5** gains **+1 SP** every **second** enemy turn (with empty accumulator start). |
| **AC3** | Species with custom rate **1.0** regens like party (1 per tick), not like 0.5 default. |
| **AC4** | Modifier **−999** still yields effective rate **0** (no negative SP from regen). |
| **AC5** | At full SP, regen tick adds **0**; accumulator resets per §5.3. |
| **AC6** | Regen at 11/12 max does not exceed 12 without over-max grant. |
| **AC7** | `GrantSoulPower(..., allowOverMax: true)` can show **13/12** in UI until clamped. |
| **AC8** | Mage/Priest never regen Soul Power. |
| **AC9** | Dead party member does not regen on player phase. |
| **AC10** | Elf upkeep after regen can use SP gained same boundary. |

---

## 12. Implementation checklist

- [x] `SoulPowerRegenerationService` + per-actor accumulator (§5, §7.5)
- [x] Constants: party base **1.0**, enemy base **0.5** (§7.1)
- [x] `EnemySpeciesDefinition` optional regen rate (§7.2)
- [x] Hook: `TurnManager.NotifyPartyTurnStart` (§6.1)
- [x] Hook: `EnemyController.TakeTurn` before racial hooks (§6.2)
- [x] Order: regen before Elf upkeep (§6.1) — `TickRegeneration` before `RacialPassiveHooks.NotifyTurnStart`
- [ ] `GrantSoulPower` / clamp helpers (§8)
- [ ] Modifier register/unregister from item/buff/passive call sites (§7.3) — stub if no content yet
- [ ] Unit tests: accumulator math, clamp, species override, non-negative rate
- [ ] Play-mode AC1–AC10

---

## 13. Document history

| Date | Note |
|------|------|
| 2026-05-29 | Initial requirements — DCSS-inspired accumulator, party 1.0 / enemy 0.5 base rates, species override, modifier pipeline |
