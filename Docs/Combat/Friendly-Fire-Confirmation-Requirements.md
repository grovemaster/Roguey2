# Friendly fire confirmation — Requirements

When a **player-initiated attack or targeted ability** would **harm a party ally** (another party member), the game shows a **blocking confirmation dialog** before resolving the action. **Yes** proceeds with the attack/ability and consumes the turn as normal. **No** (or Esc) **cancels** the action and **does not** consume the player’s turn; targeting mode stays active with the reticle unchanged.

Primary motivating example: **Fireball** — splash damage can hit both enemies and party members standing in the red preview zone. The player must explicitly confirm before allies take damage.

**Status:** Implemented (manual QA recommended for Fireball, bow, and scroll paths).

**Depends on:** `PlayerCommandProcessor` (`ApplyConfirmTarget`, `InputState.Targeting`, `PendingTargetedAbility`), `AbilityAction` / `ExecuteCore`, [Area ability splash targeting](Area-Ability-Splash-Targeting-Requirements.md) (`SplashZoneResolver`, splash preview = resolution), `TargetingResolver` (footprint-aware hit queries), `PartyManager.partyMembers`, `TurnManager`, `BowRangedCombatService` ([Bow and arrow](Bow-And-Arrow-Requirements.md)), [Auto-pickup confirmation](../Inventory/Auto-Pickup-Confirmation-Requirements.md) (modal + gate pattern).

**Related:** [Fireball scroll](../Inventory/Fireball-Scroll-Requirements.md), [Evocable items](../Inventory/Evocable-Items-Requirements.md), [Safe zone](../World/Safe-Zone-Requirements.md) (NPC protection — separate from party friendly fire), [Multi-tile enemies](Multi-Tile-Enemy-Requirements.md) (footprint overlap).

**Explicitly out of scope (v0):** Enemy AI friendly-fire confirmation; “remember my choice for this session” toggle; per-ally checkboxes; confirmation for **self-only** damage (`canHurtCaster` on Fireball — caster harms self without this dialog in v0); confirmation for **beneficial** effects on allies (heals, buffs); confirmation on **melee bump** attacks unless they use the targeted-confirm pipeline; splash preview recolor for allies (see [Area ability §9](Area-Ability-Splash-Targeting-Requirements.md) — UI coloring deferred; this doc adds **confirm dialog only**); PvP / non-party allies.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Confirm before ally harm** — Any player action that would deal **damage** to one or more **party allies** requires explicit confirmation. |
| **G2** | **Cancel preserves turn** — **No** / Esc aborts the pending action and **does not** call `TurnManager.OnPlayerActionComplete` / `ForceEndPlayerTurn`. |
| **G3** | **Cancel preserves targeting** — Player remains in targeting mode with the same primary tile and splash preview (match invalid-confirm / Telekinesis behavior). |
| **G4** | **Preview = resolution** — Ally list in the dialog is computed from the **same cell set and target query** used at execute time ([splash doc §5.3 P2](Area-Ability-Splash-Targeting-Requirements.md)). |
| **G5** | **Footprint-aware** — Multi-tile party members are detected if any footprint cell lies in the effect zone. |
| **G6** | **Consistent UX** — Reuse the project’s **dim overlay + confirm bubble** pattern (`AutoPickupConfirmDialogUI` family). |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Ally (v0)** | Any **`BaseActor`** in `PartyManager.partyMembers` **other than** optional exclusions below (§4.2). |
| **Friendly fire** | Resolving an action that applies **harmful** effects to one or more allies. |
| **Harmful effect (v0)** | **Damage** to HP via `TakeDamage` (any `DamageType`). Healing, pure buffs, and zero-damage abilities are **not** harmful. |
| **Effect zone** | Set of grid cells the action would resolve on at confirm — primary + splash cells for AoE; single tile for bow / single-target. |
| **Pending targeted action** | `PendingTargetedAbility` + reticle position at the moment the player presses confirm. |

---

## 3. Current state vs gap

| Area | Today | Gap |
|------|-------|-----|
| **Fireball / splash abilities** | `FireballAbility` damages all actors in splash cells via `TargetingResolver.GetTargetsInCells`; `canHurtCaster` skips self only. | No confirmation before party members take damage. |
| **Bow aim** | [Bow doc AC5](Bow-And-Arrow-Requirements.md) — confirm on ally tile damages ally immediately. | No confirmation gate. |
| **Targeted confirm** | `ApplyConfirmTarget` executes ability then ends turn. | No pre-execute ally-damage check. |
| **Confirmation UX** | Auto-pickup, trap, hazard, essence pickup dialogs exist. | No friendly-fire dialog. |
| **Splash preview** | Red splash markers show AoE footprint. | Allies in zone not distinguished (color deferred); **dialog** is this milestone. |

---

## 4. When the dialog appears

### 4.1 — Trigger (all must be true)

1. Player confirms a **pending targeted action** (`ApplyConfirmTarget` or bow-aim equivalent).
2. Action passes existing gates (safe-zone allow, turn available, etc.).
3. **`FriendlyFirePreview`** (§7) reports **`WouldHarmAllies == true`** for `(caster, action, primaryTile)`.
4. Action is not opted out via data (§8.1 `skipFriendlyFireConfirmation`).

### 4.2 — Ally detection rules

| Rule | Detail |
|------|--------|
| **Party roster** | Ally = member of `PartyManager.partyMembers`. |
| **Exclude caster (v0)** | The **active caster** (`GetActiveMember()` performing the action) is **not** counted as an ally for this dialog, even if `canHurtCaster` would damage them (e.g. Fireball). *Self-harm confirm is out of scope v0.* |
| **Include other party members** | Any **other** living party member whose footprint intersects the **effect zone** and would receive **damage** counts. |
| **Dead members** | Skip party members already dead / inactive. |
| **NPCs** | Town NPCs and enemies are **not** allies for this dialog. |

### 4.3 — Harmful vs beneficial

| Action type | Dialog? |
|-------------|---------|
| Fireball / damage AoE | **Yes** if ally in zone |
| Bow shot on ally tile | **Yes** |
| Single-target damage spell on ally | **Yes** |
| Healing potion / heal on ally | **No** (not harmful) |
| Telekinesis (no damage) | **No** |
| Buff with no damage | **No** |

**Implementation:** preview uses ability-specific **`GetHarmfulTargetsInZone`** (or shared damage estimate). Default for `AbilityAction` subclasses: any resolved target that is an ally and would receive **`damage > 0`**.

### 4.4 — When dialog does **not** appear

| Situation | Behavior |
|-----------|----------|
| Effect zone hits **enemies only** | Execute immediately; turn consumed on success. |
| Effect zone hits **no actors** | Execute immediately (e.g. Fireball on empty floor). |
| Only caster would take damage (`canHurtCaster`) | Execute immediately (v0). |
| `skipFriendlyFireConfirmation == true` on ability | Execute immediately (authoring escape hatch). |
| Confirm fails validation (`TryExecute` returns false) | No dialog; no turn (existing behavior). |
| Cancel targeting (before confirm) | Existing `CancelTarget` — no dialog. |

---

## 5. Dialog outcomes

### 5.1 — No / Cancel / Esc

| Result | Detail |
|--------|--------|
| Action | **Not executed** — no damage, no item/ammo consume, no cooldown. |
| Targeting | **Stay in targeting mode** — reticle + splash preview unchanged. |
| Turn | **Not consumed** — `CanActorTakeAction` unchanged for active member. |
| Pending state | `pendingTargetedAbility` retained. |

### 5.2 — Yes / Confirm

| Step | Detail |
|------|--------|
| 1 | Close dialog. |
| 2 | Run the **same execute path** as today (`TryExecuteAbility` / `BowRangedCombatService.TryExecuteBowShot` / inventory use). |
| 3 | On success: `ExitTargetingMode`, end turn (`OnPlayerActionComplete` / `ForceEndPlayerTurn`). |
| 4 | On execute failure after Yes: no turn consumed; remain in targeting (same as invalid confirm today). |

---

## 6. Dialog UI specification

### 6.1 — Presentation

| Property | Value |
|----------|--------|
| Component | **`FriendlyFireConfirmDialogUI`** (new), pattern-match `AutoPickupConfirmDialogUI` |
| Style | Modal overlay (dim gameplay) + centered bubble |
| Blocks input | **Yes** — movement, targeting, inventory, other modals blocked until resolved |
| Title | **`Friendly fire?`** or **`Hit allies?`** (pick one; consistent in-game) |

### 6.2 — Required copy elements

| Element | Content |
|---------|---------|
| **Caster** | Display name of active party member (e.g. `Aria`) |
| **Action** | Ability / item name (e.g. `Fireball`, `Short bow shot`) |
| **Target** | Primary tile `(x, y)` |
| **Ally list** | One row per affected ally: display name + optional HP hint |
| **Warning** | Short line: allies in the blast will take damage |
| **Actions** | **Y** / Enter = proceed · **N** / Esc = cancel |

**Example body:**

```text
Aria's Fireball at (12, 8) would hit:

  • Bruenor
  • Theron

Proceed anyway?

Y confirm   ·   N cancel
```

### 6.3 — Input bindings (v0)

| Key | Action |
|-----|--------|
| **Y**, **Enter** | Yes — proceed with execute |
| **N**, **Esc** | No — cancel; no turn spent |

Register `FriendlyFireConfirmDialogUI.BlocksGameplay` in `InputHandler` alongside other modal gates.

---

## 7. Preview / resolution API (recommended)

Centralize ally detection so UI, tests, and execute stay aligned.

### 7.1 — `FriendlyFirePreview` (static service)

```csharp
public static class FriendlyFirePreview
{
    public struct Result
    {
        public bool WouldHarmAllies;
        public IReadOnlyList<BaseActor> AffectedAllies;
    }

    public static Result Evaluate(
        BaseActor caster,
        PendingTargetedAbility pending,
        Vector3Int primaryTile);
}
```

### 7.2 — Evaluation steps (locked)

1. Resolve **`AbilityAction`** / bow / inventory payload from `pending`.
2. If **`skipFriendlyFireConfirmation`**, return `WouldHarmAllies = false`.
3. Build **`SplashZoneContext`** (caster cell, primary tile, facing).
4. **`cells = SplashZoneResolver.GetEffectCells(zone, ctx)`** — same as execute.
5. **`targets = TargetingResolver.GetTargetsInCells(cells)`** — footprint-aware, deduped.
6. For each target that is a **`BaseActor`** ally (§4.2), ask ability **`WouldHarm(target)`** (damage > 0).
7. If any ally harmed → `WouldHarmAllies = true`, list allies (stable party order).

### 7.3 — Bow aim

Bow uses **primary tile only** (no splash zone). `GetTargetsOnTile(primary)`; if target is ally → dialog.

Reuse the same dialog UI; action label = `"Bow shot"` or bow item name.

---

## 8. Data model (optional authoring)

### 8.1 — `AbilityAction` field (new)

| Field | Type | Default | Notes |
|-------|------|---------|--------|
| **`skipFriendlyFireConfirmation`** | bool | `false` | When `true`, never show dialog even if allies would be damaged. Use sparingly (cursed items, story moments). |

No field required on `ItemData` for v0 — gate follows equipped / invoked ability.

### 8.2 — Ability hook (recommended)

```csharp
// On AbilityAction or IFriendlyFirePreviewable
public virtual bool WouldHarm(BaseActor target, GameObject caster);
```

| Ability | `WouldHarm` |
|---------|-------------|
| `FireballAbility` | `true` for any `BaseActor` with HP (respect `canHurtCaster` only at execute, not preview ally list) |
| `HealingPotionAbility` | `false` |
| `ThrowingKnifeAbility` | `true` if damage > 0 and target is ally |

---

## 9. Integration points

### 9.1 — Gate location (locked)

Insert **`FriendlyFireTargetGate.TryInterceptConfirm(...)`** at the **start** of `ApplyConfirmTarget`, **after** `TryAllowPendingTargetedAction` and **before** any `TryExecute*` call:

```text
ApplyConfirmTarget
  → validate pending + safe zone
  → FriendlyFirePreview.Evaluate
  → if WouldHarmAllies: show dialog; return true (handled, no turn)
  → else: existing execute + turn end
```

On **Yes**, dialog callback invokes a shared **`CompletePendingTargetedAction()`** extracted from today’s success path.

### 9.2 — Sources covered (v0)

| `PlayerAbilitySource` | Notes |
|-----------------------|--------|
| **Essence** | Fireball essence, etc. |
| **EquipmentItem** | Evocable / equipment abilities |
| **HumanMageSpell** | Targeted damage spells |
| **InventoryItem** | Fireball scroll, fan of fire |
| **BowAim** | Single-tile ally check |

### 9.3 — Not covered (v0)

| Source | Reason |
|--------|--------|
| Melee bump attack | Not targeted-confirm pipeline; separate spec if needed |
| Enemy actions | AI responsibility |
| Traps / hazards | Existing trap/hazard confirm dialogs |

---

## 10. Turn and targeting interaction

| Event | Turn consumed? | Targeting |
|-------|----------------|-----------|
| Open dialog (allies would be hit) | **No** | Stays active |
| Dialog **No** | **No** | Stays active |
| Dialog **Yes**, execute success | **Yes** (today’s rules) | Exit targeting |
| Dialog **Yes**, execute fails | **No** | Stays active |
| Confirm with no allies harmed | **Yes** on success | Exit targeting |

Formation mode: same rules; **`ForceEndPlayerTurn`** only after successful execute post-Yes.

---

## 11. Examples

### 11.1 — Fireball with ally in splash

1. Player targets tile; red splash overlaps Bruenor.
2. Confirm → dialog lists Bruenor.
3. **N** → still targeting; Fireball not cast; turn available.
4. Player moves reticle clear of allies, confirms → no dialog; Fireball resolves; turn ends.

### 11.2 — Fireball hitting ally + enemy

Dialog lists **ally names only** (not enemies). **Yes** damages both per ability rules.

### 11.3 — Bow aimed at ally tile

Dialog: `"Aria's Bow shot at (10, 6) would hit: • Theron"`. **N** → still aiming.

### 11.4 — Fireball on empty tile, ally adjacent outside splash

No dialog — ally not in **effect zone** cells.

---

## 12. Acceptance criteria

| ID | Test |
|----|------|
| **AC1** | Fireball confirm with ally in splash → dialog shows ally name(s). |
| **AC2** | Dialog **N** → no damage, no scroll/essence consume, **turn not spent**, reticle unchanged. |
| **AC3** | Dialog **Y** → Fireball resolves, allies damaged, turn ends. |
| **AC4** | Fireball confirm with **no** ally in effect zone → no dialog; immediate execute. |
| **AC5** | Bow aim at ally tile → dialog; **N** → no arrow consumed, turn not spent. |
| **AC6** | Bow aim at enemy → no dialog. |
| **AC7** | Healing / non-damage targeted ability on ally → no dialog. |
| **AC8** | Preview ally list matches actual damage recipients on **Y** (same cells + resolver). |
| **AC9** | Multi-tile party member partially in splash → listed in dialog. |
| **AC10** | Caster in Fireball splash with `canHurtCaster` → caster **not** listed; other allies are. |
| **AC11** | Dialog open blocks movement and other gameplay input. |

---

## 13. Implementation checklist

- [x] **`FriendlyFirePreview`** + unit tests (Fireball disk, bow tile, footprint ally)
- [x] **`FriendlyFireConfirmDialogUI`** (modal pattern)
- [x] **`FriendlyFireTargetGate.TryInterceptConfirm`**
- [x] Hook **`ApplyConfirmTarget`** (all `PlayerAbilitySource` paths)
- [x] **`AbilityAction.skipFriendlyFireConfirmation`** (optional field)
- [x] **`WouldHarm`** on damage abilities (`FireballAbility`, bow service, etc.)
- [x] Register **`BlocksGameplay`** in `InputHandler`
- [ ] Manual QA: Fireball + bow + scroll + essence
- [x] Cross-link from [Area ability splash targeting](Area-Ability-Splash-Targeting-Requirements.md) §9 out-of-scope note

---

## 14. Resolved design decisions

| # | Question | Locked answer |
|---|----------|---------------|
| **Q1** | Who is an ally? | **`PartyManager.partyMembers`**, excluding caster for dialog list (v0). |
| **Q2** | Self-damage (Fireball caster)? | **No dialog** in v0; execute rules unchanged. |
| **Q3** | Cancel cost? | **No turn**; stay in targeting. |
| **Q4** | Beneficial friendly target? | **No dialog** — harmful = damage only. |
| **Q5** | Preview vs execute? | **Same** splash cells + `TargetingResolver`. |
| **Q6** | Bow included? | **Yes** — ally tile aim triggers dialog. |
| **Q7** | Remember choice? | **No** v0 — confirm every time. |

---

## 15. References

- Splash math: `SplashZoneResolver`, [Area ability splash targeting](Area-Ability-Splash-Targeting-Requirements.md)
- Fireball: `Assets/Resources/Item/Ability/Fireball_Standard.asset`, `FireballAbility`
- Confirm pattern: [Auto-pickup confirmation](../Inventory/Auto-Pickup-Confirmation-Requirements.md)
- Targeting entry: `PlayerCommandProcessor.ApplyConfirmTarget`
