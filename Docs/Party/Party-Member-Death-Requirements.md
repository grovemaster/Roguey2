# Party member death — Requirements

When a **party member’s HP** reaches **zero**, that character **dies**: HP must **never** be negative, the game logs the death, a **blocking information dialog** names the fallen member (single **OK** button), and the member’s **`GameObject` is destroyed** and removed from the active party. A **persistent roster of all dead party members** (memorial / run log) is **explicitly deferred**.

**Depends on:** `CharacterStats.currentHP`, `HealthComponent` (`TakeDamage`, `Died` event), `BaseActor` (`HandleDied` → `Die()`), `PlayerController`, `PartyManager`, `GridManager` / `GridMover` (if actors occupy cells), `TurnManager`, `InputHandler` (`BlocksGameplay` gates), existing modal UI pattern (`TrapConfirmDialogUI`, `AutoPickupConfirmDialogUI`).

**Related:** [Party experience & leveling](../Progression/Party-Experience-And-Leveling-Requirements.md) (living vs dead members for XP — today “dead still receive XP”; update when death pipeline exists). [Enemy death loot & mana stones](../Combat/Enemy-Death-Loot-And-Mana-Stones-Requirements.md) (enemy `Die()` → `Destroy` pattern). [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) (modal chrome). [Auto-pickup confirmation](../Inventory/Auto-Pickup-Confirmation-Requirements.md) (`BlocksGameplay` integration).

**Explicitly out of scope (v0):** Permadeath vs revival; corpse / loot drop from dead member’s inventory; game-over when last member dies (minimal safe behavior documented in §8); resurrection spells; HP overheal rules; damage that bypasses `HealthComponent`; save/load mid-death-dialog; **dead-party-member memorial log** (§10 future).

---

## 1. Goals

**G1 — HP never negative**  
After any damage (or other HP loss), `currentHP` is clamped to **`>= 0`**. The stored value must never read negative in UI, logs, or logic.

**G2 — Death at zero HP**  
When HP reaches **0**, the member is **dead** exactly once per death event (no double-death if multi-hit same frame without guard).

**G3 — Destroy actor**  
On death, the party member’s **`GameObject` is destroyed** (same lifecycle intent as `EnemyController.Die()` → `Destroy(gameObject)`).

**G4 — Debug traceability**  
`Debug.Log` (or `Debug.LogWarning` if preferred for visibility) states clearly **which party member died**, using `BaseActor.DisplayName` when set, else `gameObject.name`.

**G5 — Information dialog (OK)**  
A **blocking** modal informs the player **which party member just died**. The dialog has a single **OK** button at the bottom (not a Y/N confirm). Dismissing OK closes the dialog and resumes normal input rules.

**G6 — Party list hygiene**  
`PartyManager.partyMembers` (and formation `positionHistory`) must not retain references to destroyed actors after death processing completes.

**G7 — Future-ready**  
Death handling is centralized so a later **dead-party-member log** can subscribe without rewriting per-class `Die()` stubs.

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Party member** | `BaseActor` listed in `PartyManager.partyMembers` (typically `PlayerController`). |
| **HP** | `CharacterStats.currentHP` (current hit points). |
| **Death** | HP at **0** after damage resolution; triggers death pipeline once. |
| **Information dialog** | Modal with message + **OK** only (acknowledgment, not a choice). |
| **Active member** | `PartyManager.GetActiveMember()` — party list index **0** after swaps. |

---

## 3. Current baseline (as-is)

| Area | Today |
|------|--------|
| **Damage** | `HealthComponent.TakeDamage` subtracts damage; logs HP; fires `Died` when `currentHP <= 0`. |
| **Negative HP** | **Not clamped** — overkill can leave `currentHP` negative until inspected. |
| **Death hook** | `BaseActor` subscribes `health.Died` → `HandleDied()` → abstract `Die()`. |
| **Player death** | `PlayerController.Die()` logs `"Game Over! The Player has fallen."` — **does not** destroy `GameObject` or update `PartyManager`. |
| **Enemy death** | `EnemyController.Die()` — XP, loot, grid unregister, **`Destroy(gameObject)`**. |
| **Modals** | Trap / hazard / auto-pickup use dim overlay + bubble; Y/N or cancel — **no** party-death OK dialog. |
| **Input gating** | `InputHandler` checks `InventoryUI.BlocksGameplay`, trap/hazard/auto-pickup dialogs — **no** death dialog flag yet. |

---

## 4. HP rules (locked)

### 4.1 — Clamp after damage

In `HealthComponent.TakeDamage` (or a single shared helper used by all HP loss):

```text
stats.currentHP = Mathf.Max(0, stats.currentHP - damage);
```

Apply **after** resistance / AC reduction and **before** logging and `Damaged` event.

### 4.2 — Healing and max HP

- Healing (`HealAbility`, level-up HP gain, etc.) continues to cap at `MaxHP` (existing behavior).
- Clamping at 0 does not change max HP or healing formulas.

### 4.3 — Death condition

- Death triggers when **`currentHP == 0`** after clamp (equivalent to `<= 0` once clamp exists).
- **Overkill** (damage greater than remaining HP) still results in **`currentHP == 0`**, not negative.

### 4.4 — Immunity / invulnerability (future)

If a member gains “cannot die” later, it must intercept **before** `Died` fires. **v0:** no invulnerability — all party members can die at 0 HP.

---

## 5. Death pipeline (locked)

### 5.1 — Single entry point

All party-member death side effects run through one service or static coordinator (suggested name: **`PartyMemberDeathService`**), invoked from `PlayerController.Die()` (and any other party `BaseActor` subclass if added later).

**Do not** scatter destroy / party-remove / dialog across multiple `Die()` overrides.

### 5.2 — Order of operations

On death of party member `M`:

1. **Guard** — if `M` already marked dying/dead, return (idempotent).
2. **Mark dying** — prevent re-entry from duplicate `Died` events.
3. **Clamp HP** — assert `currentHP == 0` (defensive `Mathf.Max(0, …)` if needed).
4. **Debug log** — §7.
5. **Party bookkeeping** — §6 (remove from list, fix active leader, snap formation history).
6. **Grid / footprint** — unregister occupancy if the project registers party actors on the grid (mirror enemy footprint unregister where applicable).
7. **Show information dialog** — §9 (blocking).
8. **On OK** (dialog callback):
   - Close dialog.
   - **`Destroy(M.gameObject)`**.
9. **Post-destroy** — if party empty, §8.3; else ensure `GetActiveMember()` is valid.

**Locked:** Dialog appears **before** destroy so UI can read `DisplayName` / portrait hooks from live `GameObject`. Destroy runs **after** OK (not on the same frame as OK if that avoids race with UI referencing the actor).

**Alternative (acceptable if simpler):** Log + party bookkeeping + destroy immediately, dialog shows only the **cached display name** captured at step 4. **Default locked:** show dialog **before** destroy; destroy on OK.

### 5.3 — `PlayerController.Die()` v0

Replace the placeholder “Game Over” log with a call to the shared death pipeline (§5.1). **Full game over** when the last member dies is §8.3 (minimal behavior), not the generic log string today.

### 5.4 — Non-party actors

Enemies and other `BaseActor` types **do not** use this pipeline. `EnemyController.Die()` remains unchanged.

---

## 6. PartyManager integration (locked)

### 6.1 — Remove from roster

- Remove `M` from `partyMembers` **before** `Destroy` (list must not contain destroyed references).
- Remove null entries defensively if any slot was already invalid.

### 6.2 — Active leader

- Party control uses index **0** as leader (`SwapActiveMember` reordering).
- If the dying member was at index **0** and others remain:
  - After removal, promote the **next** member at index **0** (former index 1) as leader.
  - `SnapHistoryToCurrentPositions()` after promotion.
  - Repoint `CameraFollow` to the new leader (same as swap).
- If the dying member was a **follower**, remove and **snap history** without changing who is index 0 unless the removed index was 0.

### 6.3 — Formation history

- After removal, `positionHistory` length must match `partyMembers.Count` (call `SnapHistoryToCurrentPositions()` or equivalent prune).

### 6.4 — Turn system

- If the **active** member dies mid-turn, document in implementation:
  - **v0 default:** end or hand off the player turn to the next living leader if any; if none, §8.3.
  - Cancel pending targeting / inventory / bow aim via existing cancel paths where possible.

---

## 7. Debug logging contract

Prefix: **`[Party:Death]`**.

| Event | Example |
|-------|---------|
| Member died | `[Party:Death] Party_Barbarian_Warrior (Barbarian) has died. HP 0/120.` |
| Duplicate suppressed | `[Party:Death] Ignored duplicate death for …` |
| Roster update | `[Party:Death] Removed from party. Remaining: 2.` |
| Last member | `[Party:Death] No living party members remain.` |

Use **`DisplayName`** in the message when non-empty; include `gameObject.name` in parentheses if useful for debugging.

---

## 8. Edge cases & v0 policy

### 8.1 — Simultaneous deaths

If multiple members reach 0 HP in one resolution window (e.g. AoE):

- **v0:** Process deaths **sequentially** (stable party list order); **one dialog per death**; each OK dismisses before the next dialog shows.
- **Future:** batch summary dialog.

### 8.2 — Damage while dialog open

While the death dialog blocks gameplay, no new player moves. Incoming enemy damage to **other** living members may still apply per turn rules — document in implementation; **v0:** freeze enemy phase until OK if already mid player-turn death, or finish death dialog queue first.

### 8.3 — Last party member dies

**v0 minimum:**

- After final OK + destroy, log `[Party:Death] No living party members remain.`
- **Future:** dedicated game-over screen.
- Do not leave `PartyManager` with an empty list and a null `GetActiveMember()` without a defined idle state (no input exceptions).

### 8.4 — Inventory & equipment

- **v0:** Inventory / equipment on the destroyed `GameObject` are destroyed with the actor (no corpse drop).
- **Future:** drop bag at tile or migrate items to party stash.

### 8.5 — Friendly fire / hazards

Death from bow friendly fire, traps, hazards, status ticks — **same pipeline** as melee; no special case in v0.

---

## 9. Information dialog (locked)

### 9.1 — UX pattern

Reuse the project’s **dim overlay + centered bubble** family (see `TrapConfirmDialogUI` / `AutoPickupConfirmDialogUI`):

| Element | Requirement |
|---------|-------------|
| **Title** | e.g. `Party member fallen` (exact copy tunable) |
| **Body** | States **which member died**: `{DisplayName} has died.` |
| **Primary control** | Single **`OK`** button anchored at the **bottom** of the bubble |
| **Dismiss** | OK click **or** **Enter** / **Space** (keyboard affordance — match OK) |
| **Cancel/Escape** | **v0:** Escape **also** dismisses (same as OK) — information only, no cancel choice |
| **Blocking** | `PartyMemberDeathDialogUI.BlocksGameplay == true` while open |

### 9.2 — New UI type

Suggested: **`PartyMemberDeathDialogUI`** in `JRogue.UI.Gameplay`:

```csharp
public static bool BlocksGameplay { get; }
public static PartyMemberDeathDialogUI EnsureInstance();
public void Show(string memberDisplayName, Action onOk);
```

- `Show` receives the **display name string** captured at death time (not a live `BaseActor` reference after destroy).
- `onOk` runs destroy + follow-up party logic if not already done per §5.2 ordering.

### 9.3 — Input integration

Extend `InputHandler` (and any central `BlocksGameplay` aggregator) to include:

```csharp
PartyMemberDeathDialogUI.BlocksGameplay
```

Same priority as trap / hazard dialogs — movement, abilities, inventory toggle ignored while open.

### 9.4 — Copy (v0 default)

```text
Title: Party member fallen
Body:  {DisplayName} has died.
Button: OK
```

Optional second line (non-normative): `HP reached zero.`

---

## 10. Future — dead party member log

**Deferred.** When implemented:

- Append-only record per run: `DisplayName`, race/class, level, death tile, killer/source, turn number, timestamp.
- Subscribe from `PartyMemberDeathService` **before** destroy.
- **Do not** block v0 on persistence format.

---

## 11. Implementation design (suggested)

| Component | Responsibility |
|-----------|----------------|
| `HealthComponent` | Clamp HP ≥ 0; fire `Died` at 0 |
| `PartyMemberDeathService` | Idempotent death orchestration (§5) |
| `PlayerController.Die()` | Delegate to service |
| `PartyManager` | `RemovePartyMember(BaseActor)`, leader promotion helpers |
| `PartyMemberDeathDialogUI` | Modal + OK + `BlocksGameplay` |
| `InputHandler` | Gate on death dialog |

### 11.1 — Tests (recommended)

| Test | Assert |
|------|--------|
| Overkill damage | `currentHP == 0`, never negative |
| Death at 0 | `Died` fired once; destroy called (mock or playmode) |
| Party remove | `partyMembers` no longer contains actor |
| Dialog | `Show` called with expected display name (mock UI) |

---

## 12. Acceptance criteria

| ID | Test |
|----|------|
| **AC1** | Deal damage reducing HP to 0 → `currentHP` is **0**, not negative. |
| **AC2** | Deal 999 damage to a member at 5 HP → `currentHP` is **0**, not -994. |
| **AC3** | On death → Console shows `[Party:Death] …` with correct member name. |
| **AC4** | On death → Information dialog shows **which member** died; only **OK** at bottom. |
| **AC5** | OK dismisses dialog; gameplay input resumes. |
| **AC6** | After OK → member `GameObject` is **destroyed** (hierarchy / scene). |
| **AC7** | `PartyManager.partyMembers` no longer lists destroyed member. |
| **AC8** | If leader dies and others live → another member becomes controllable leader (index 0). |
| **AC9** | Enemy death unchanged (still uses enemy loot / XP pipeline). |
| **AC10** | Two members die in one effect → two dialogs, sequential OKs, both removed. |

---

## 13. Implementation checklist

- [x] Clamp `currentHP` to `>= 0` in `HealthComponent`
- [x] `PartyMemberDeathService` (idempotent death orchestration)
- [x] `PartyManager` removal + leader promotion + history snap
- [x] `PartyMemberDeathDialogUI` (OK button, `BlocksGameplay`)
- [x] `InputHandler` / gameplay gates include death dialog
- [x] `PlayerController.Die()` wired to service (remove placeholder game-over string)
- [x] Grid unregister if party actors register cells
- [x] Unit tests: clamp, single death, party list
- [ ] Play-mode AC1–AC10

---

## 14. Document history

| Date | Note |
|------|------|
| 2026-05-29 | Initial requirements — HP clamp, destroy on death, OK information dialog; dead-member log deferred |
