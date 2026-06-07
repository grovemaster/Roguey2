# Targeting sight range — Requirements (DCSS-style reticle confirm)

**Dungeon Crawl Stone Soup (DCSS)** limits most **ranged and targeted** actions to tiles the player can **currently see**. In JRogue, any player action that uses the **targeting reticle** (`InputState.Targeting`) may only be **confirmed** on a tile that is **live visible** to the party at confirm time. **Bump attacks** (melee move-into-enemy, bow bump shot) are **exempt**: adjacent combat does not use the reticle and is **never** gated by sight — the player is assumed to be able to strike what is physically next to them even if fog would hide a distant view of that tile. Moving the reticle over explored-but-unseen or never-seen tiles is allowed; **confirm** on those tiles is rejected with a debug log and the action does not resolve.

**Status:** Implemented (v1). Debug log only on reject; bump attacks exempt (§4.3).

**Depends on:** `PlayerCommandProcessor` (`ApplyConfirmTarget`, `InputState.Targeting`, `PendingTargetedAbility`, `EnterTargetingMode`), `TargetingReticleView`, `VisibilityManager.IsVisible`, [Fog of war](../World/Fog-Of-War-Requirements.md) (tile knowledge: **Visible** vs **Explored**), [Improved illumination](../World/Improved-Illumination-Requirements.md) (party LOS + light gating in `ComputeCurrentVisibleSet`), `ShadowCaster`, `PartyManager`, [Area ability splash targeting](Area-Ability-Splash-Targeting-Requirements.md) (primary vs splash tiles), [Friendly fire confirmation](Friendly-Fire-Confirmation-Requirements.md) (confirm pipeline ordering).

**Related:** [Fireball scroll](../Inventory/Fireball-Scroll-Requirements.md), [Throwing knife](../Inventory/Throwing-Knife-Requirements.md), [Bow and arrow](Bow-And-Arrow-Requirements.md), [Evocable items](../Inventory/Evocable-Items-Requirements.md), [Telekinesis essence](../Essence/Telekinesis-Essence-Requirements.md) (invalid-confirm / cancel-turn pattern), `SenseSightService` (enemy AI LOS — **not** the player targeting gate).

**Supersedes (for targeting confirm):** “Line-of-sight / max-range enforcement deferred” notes in [Throwing knife §Explicitly out of scope](../Inventory/Throwing-Knife-Requirements.md) and [Bow and arrow §Explicitly out of scope](Bow-And-Arrow-Requirements.md) — **sight confirm** is in scope here; **max Chebyshev range** remains a separate future milestone unless an ability already enforces `AbilityAction.range`.

**Explicitly out of scope (v1):** Blocking reticle **movement** onto unseen tiles (confirm-only gate in v1); player-facing toast/UI message (debug log only in v1); **max range** clamp on reticle movement; confirming through walls when geometric LOS exists but tile is dark/unlit (uses **`IsVisible`**, not raw `ShadowCaster` alone); AI / enemy targeting rules; “blind fire” abilities unless opted out via data (§8); **sight checks on bump attacks** (§4.3); save/load mid-targeting.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Confirm only on visible tiles** — Reticle confirm succeeds only when the **primary target tile** is in the party’s **current visible** set. |
| **G2** | **Same rule for all reticle sources** — Essence, equipment, mage spells, inventory scrolls/missiles, bow aim, and any future `requiresTarget` ability share one gate. |
| **G3** | **Fail soft on invalid confirm** — Out-of-sight confirm: **debug log**, **no** turn spent, **no** ammo/item consumed, targeting mode **stays open** (match Telekinesis / invalid knife confirm). |
| **G4** | **Authoritative visibility** — Use **`VisibilityManager.IsVisible(cell)`** (live **Visible** fog state), not explored memory alone. |
| **G5** | **Party union sight** — A tile is targetable if **any** active party member’s current visibility includes that cell (same union as [Fog of war §G4](../World/Fog-Of-War-Requirements.md)). |
| **G6** | **Composable confirm pipeline** — Sight check runs **before** friendly-fire dialog and **before** execute/consume (§6). |
| **G7** | **Debug traceability** — Rejected confirms log with prefix **`[Targeting:Sight]`** and the cell coordinates. |
| **G8** | **Bump attacks ignore sight** — Melee and bow **bump** paths never call the sight gate; adjacency is sufficient to attack. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Targeting reticle** | `TargetingReticleView` cursor shown while `InputState.Targeting`; white tile = **primary** target (`Position`). |
| **Primary target tile** | Grid cell under the reticle at confirm — passed as `target` / `targetTile` to ability execute. |
| **Live visible** | Cell whose fog state is **`Visible`** in `VisibilityManager` — currently in party LOS **and** passes illumination gating (`ComputeCurrentVisibleSet`). |
| **Explored (memory)** | Previously seen tile now outside live sight — **not** valid for confirm in v1. |
| **Unseen** | Never revealed — **not** valid for confirm. |
| **Confirm** | Player input that calls `ApplyConfirmTarget` (or bow-aim equivalent) while targeting is active. |
| **Designate** | Same as **confirm** in DCSS terms — locking in the reticle tile as the action target. |
| **Bump attack** | Move into an adjacent occupant (`PlayerController` bump / bow bump shot) — **no** reticle, **no** sight gate. |

---

## 3. DCSS reference behavior

| DCSS | JRogue mapping |
|------|----------------|
| Cannot aim spells/ranged attacks at unseen squares | Confirm rejected when `!VisibilityManager.IsVisible(primaryTile)` |
| Can move cursor over explored map memory | v1: reticle **may** move anywhere; only **confirm** is gated |
| Fog/explored terrain visible but monsters on it are not | Explored tile without live visibility → confirm rejected |
| Some effects require LOS to target center | v1: **primary tile** must be live visible; splash cells follow ability resolution after confirm |

---

## 4. When the sight gate applies

### 4.1 — In scope (must gate on confirm)

Any pending action that reaches **`ApplyConfirmTarget`** with a reticle position, including:

| Source | `PlayerAbilitySource` / path |
|--------|------------------------------|
| Essence slot ability | `Essence` |
| Equipped item ability | `EquipmentItem` |
| Human mage spell | `HumanMageSpell` |
| Inventory scroll / missile / evocable | `InventoryItem` |
| Bow aim | `BowAim` |
| Future targeted abilities | Same pipeline via `EnterTargetingMode` |

### 4.2 — Out of scope (v1)

| Action | Why excluded |
|--------|----------------|
| Self-target / non-target abilities | No reticle |
| Auto-target / AI | Not player reticle |

### 4.3 — Bump attacks — no sight gate (locked)

**Locked:** **Bump attacks always work regardless of line of sight or fog state.** The player can reasonably be expected to attack whatever is **adjacent** to them on the grid, even when that tile is not in the party’s current visible set (e.g. fighting in darkness, edge cases around illumination gating).

| Path | Sight gate? |
|------|-------------|
| **Melee bump** — move into enemy cell (`PlayerController.OnBump` / `AttackEnemy`) | **No** |
| **Bow bump shot** — move into enemy while wielding bow | **No** |
| **Reticle confirm** — fireball, knife, bow aim, scrolls, etc. | **Yes** (§5) |

Implementation must **not** add `TargetingSightGate` (or `VisibilityManager.IsVisible`) to bump resolution. Only **`ApplyConfirmTarget`** (and equivalent reticle confirm entry points) invoke the gate.

---

## 5. Confirm rules

### 5.1 — Primary tile visibility (locked v1)

On confirm, let `primary = reticleView.Position` (z = 0).

| Condition | Result |
|-----------|--------|
| `VisibilityManager.Instance != null` **and** `IsVisible(primary)` | Pass sight gate → continue confirm pipeline (friendly fire, execute, etc.). |
| Manager missing | **Fail closed** — log warning, reject confirm (do not execute). |
| `!IsVisible(primary)` | Reject confirm (§5.3). |

**Locked:** Use **`IsVisible`**, not `IsExplored`, not `IsLitVisible` alone, not raw geometric `ShadowCaster.IsVisible` without illumination rules — so dark/unlit LOS tiles that the UI treats as non-live-visible cannot be designated.

### 5.2 — Splash / AoE

| Rule | Detail |
|------|--------|
| **Gate tile** | Only the **primary** (white) reticle tile must be live visible. |
| **Red splash preview** | May extend onto explored or unseen cells when primary is visible; v1 does **not** require every splash cell to be visible. |
| **Resolution** | After sight + friendly-fire gates pass, execute uses existing splash math ([Area ability §P2](Area-Ability-Splash-Targeting-Requirements.md)). |

*Rationale:* Matches DCSS “pick a visible aim point”; explosion may affect off-screen cells.

### 5.3 — Rejected confirm (out of sight)

When the primary tile is **not** live visible:

1. **Do not** call `CompletePendingTargetedAction`.
2. **Do not** consume turn, soul power, ammo, arrows, or inventory items.
3. **Do not** exit targeting mode — reticle stays at current position.
4. **Log** (exact message v1):

```text
[Targeting:Sight] Cannot designate {primary.x},{primary.y}: tile is out of sight.
```

Optional detail in same log line: explored vs unseen (`IsExplored`) for QA — not required for v1.

### 5.4 — Reticle movement (v1)

| Behavior | v1 |
|----------|-----|
| Move reticle onto unseen / explored tiles | **Allowed** |
| Visual hint on invalid tiles | **Deferred** (no red/gray reticle tint in v1) |
| Snap reticle to nearest visible tile | **No** |

---

## 6. Confirm pipeline order

In `ApplyConfirmTarget` (and bow-aim confirm if split), after existing **safe-zone / allow** checks and **before** friendly-fire intercept:

```text
1. TryAllowPendingTargetedAction (safe zone, etc.)
2. Read primary tile from reticle
3. TargetingSightGate.TryAllowConfirm(primary)   ← NEW
4. FriendlyFireTargetGate.TryInterceptConfirm (if applicable)
5. CompletePendingTargetedAction → execute + consume + end turn
```

If step 3 fails, return `true` from `ApplyConfirmTarget` (input handled) but **without** executing — same as other “handled but cancelled” invalid confirms.

---

## 7. Implementation sketch

### 7.1 — `TargetingSightGate` (recommended)

Static helper in `Assets/Scripts/Combat/Targeting/` (or `Core/Targeting/`):

```csharp
public static class TargetingSightGate
{
    public const string LogPrefix = "[Targeting:Sight]";

    public static bool IsPrimaryTileDesignatable(Vector3Int primaryTile)
    {
        VisibilityManager visibility = VisibilityManager.Instance;
        if (visibility == null)
            return false;

        primaryTile.z = 0;
        return visibility.IsVisible(primaryTile);
    }

    public static bool TryAllowConfirm(Vector3Int primaryTile, out string denyReason)
    {
        if (IsPrimaryTileDesignatable(primaryTile))
        {
            denyReason = null;
            return true;
        }

        denyReason = $"Cannot designate {primaryTile.x},{primaryTile.y}: tile is out of sight.";
        return false;
    }
}
```

### 7.2 — `PlayerCommandProcessor` hook

After `TryAllowPendingTargetedAction`, before friendly fire:

```csharp
if (!TargetingSightGate.TryAllowConfirm(target, out string sightDeny))
{
    Debug.Log($"{TargetingSightGate.LogPrefix} {sightDeny}");
    return true;
}
```

### 7.3 — Tests (recommended)

| Test | Assert |
|------|--------|
| Visible primary | `TryAllowConfirm` → true |
| Explored-only primary | false (mock `IsVisible` false, `IsExplored` true) |
| Unseen primary | false |
| Null `VisibilityManager` | false (fail closed) |

Use unit tests on `TargetingSightGate` with injected visibility stub; optional playmode confirm on SampleScene behind wall.

---

## 8. Data opt-out (optional v1.1)

For future “Blind cast” / “Scrying” abilities that may target unseen tiles:

| Field | On `AbilityAction` |
|-------|---------------------|
| **`ignoreSightRangeGate`** | `bool`, default **false**. When **true**, skip §5 sight check for that ability only. |

Not required for initial implementation.

---

## 9. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC1** | Confirm fireball on a **live visible** tile casts normally. |
| **AC2** | Confirm on an **explored but not visible** tile logs `[Targeting:Sight] … out of sight`, does not cast, does not end turn. |
| **AC3** | Confirm on **unseen** tile — same as AC2. |
| **AC4** | Throwing knife, bow aim, and inventory scroll confirm paths all use the same gate. |
| **AC5** | After rejected confirm, player remains in targeting mode with reticle unchanged. |
| **AC6** | Friendly-fire dialog still appears only when sight gate passes and allies would be harmed. |
| **AC7** | Melee bump and bow bump shot succeed against an adjacent enemy **even when** that enemy’s tile is not `IsVisible` to the party (no sight gate on bump paths). |

---

## 10. Related doc updates (when implemented)

| Doc | Update |
|-----|--------|
| [Area ability splash targeting](Area-Ability-Splash-Targeting-Requirements.md) | Remove “full line-of-sight gating for splash preview” from out-of-scope; point here for **confirm** gate. |
| [Throwing knife](../Inventory/Throwing-Knife-Requirements.md) | Replace “LoS … deferred” with link to this doc. |
| [Bow and arrow](Bow-And-Arrow-Requirements.md) | Same for aim confirm; bump shot still exempt. |
| [Friendly fire confirmation](Friendly-Fire-Confirmation-Requirements.md) | Note sight gate runs **before** friendly-fire intercept in §6. |

---

## 11. Open questions (defaults locked for v1)

| Question | v1 default |
|----------|------------|
| Must every **splash** cell be visible? | **No** — primary only (§5.2). |
| Block reticle movement on unseen tiles? | **No** — confirm-only. |
| Player-facing UI message? | **Debug log only**. |
| Use lit-visible vs live-visible? | **`IsVisible`** (live visible, includes dark-tile rules). |
| Sight gate on bump attacks? | **Never** — adjacency only (§4.3). |
