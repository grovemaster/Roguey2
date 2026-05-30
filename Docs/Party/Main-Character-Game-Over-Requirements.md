# Main character death & game over — Requirements

When the **main character** (the player’s original hero — not recruited allies) reaches **0 HP**, the run ends: show a **terminal game over modal** that **cannot be dismissed**. The player must be able to **designate exactly one** party member as the main character **once**; that designation **never changes** during a playthrough. Recruited party members who die use the existing [party member death](Party-Member-Death-Requirements.md) flow (OK dialog, then destroy).

**Depends on:** `PartyManager`, `PartyMemberDeathService`, `HealthComponent`, `PlayerController`, `BaseActor.DisplayName`, `TurnManager` / `GameState`, `InputHandler` (`BlocksGameplay`), modal UI family (`PartyMemberDeathDialogUI`, trap/hazard confirms).

**Related:** [Party member death](Party-Member-Death-Requirements.md) (recruit death UX). [Party experience & leveling](../Progression/Party-Experience-And-Leveling-Requirements.md). [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) (overlay chrome).

**Explicitly out of scope (v0):** Character creator UI; continue / retry / load save from game over; main-character transfer between saves; permadeath meta progression; game over when **only** recruits die; demoting or swapping main-character designation; multiplayer.

**Future (called out):** Player builds the main character in a **character creator** and that build becomes the designated main for the playthrough (§10).

---

## 1. Goals

**G1 — Single immutable main character**  
Exactly **one** party member is the **main character** per playthrough/session. Designation is set **once** and **cannot** be changed, cleared, or reassigned at runtime.

**G2 — Game over on main death only**  
If the main character’s HP reaches **0**, the game enters **game over** — regardless of how many recruits remain alive.

**G3 — Terminal modal**  
Game over presents a **modal** that blocks all gameplay and **cannot be closed** (no OK, no Escape, no click-outside dismiss).

**G4 — Recruit death unchanged**  
When a **non-main** party member dies, use the existing party death pipeline (information dialog + OK + destroy). Game over does **not** trigger.

**G5 — Distinct from “active leader”**  
**Main character** is **not** the same as the **currently controlled** party member (`partyMembers[0]` after swap). Swapping control (F-keys) must **not** change who is the main character.

**G6 — Authoring & bootstrap**  
Designers (and later the character creator) can designate the main character in a scene or at run start without code changes per hero.

**G7 — Debug traceability**  
Logs use prefix **`[GameOver]`** for designation and trigger events.

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Main character** | The player’s original hero for this run; exactly one per session; immutable once designated. |
| **Recruit / ally** | Party member who joined later; death uses recruit death UX. |
| **Active leader** | `PartyManager.partyMembers[0]` — who the player currently moves (may swap). |
| **Designation** | Binding a specific `BaseActor` (or stable id) as main character; **one-shot**. |
| **Game over** | Terminal run state after main character death; no further player commands. |
| **Terminal modal** | Full-screen or centered overlay with **no** dismiss control. |

---

## 3. Current baseline (as-is)

| Area | Today |
|------|--------|
| **Party list** | `PartyManager.partyMembers`; index **0** = controlled leader after swap. |
| **Death** | `PartyMemberDeathService` — all party deaths use recruit-style OK dialog + destroy on OK. |
| **Last member dead** | Logs `[Party:Death] No living party members remain.` — **no** game over screen. |
| **Main character flag** | **None** — no `isMainCharacter` or equivalent. |
| **Game state** | `GameState`: `PLAYER_TURN`, `ENEMY_TURN`, `BUSY` — **no** `GAME_OVER`. |
| **Player death** | Same pipeline as any `PlayerController` party member. |

---

## 4. Main character designation (locked)

### 4.1 — Rules

| Rule | Requirement |
|------|-------------|
| **Count** | Exactly **one** main character per playthrough. |
| **Immutability** | Once designated, **never** change for the lifetime of that session (until new game / scene reload). |
| **Timing** | Designation must complete **before** gameplay damage can apply (Awake/Start/bootstrap), or on first valid register — **not** mid-combat. |
| **Uniqueness attempt** | A second designation attempt **fails** with log + editor warning; does not replace the first. |

### 4.2 — Data model (suggested)

**Option A (recommended v0):** `PartyManager` holds the canonical reference:

```csharp
// PartyManager — set once via bootstrap
public BaseActor MainCharacter { get; }
public bool HasMainCharacter { get; }
public bool TryDesignateMainCharacter(BaseActor actor); // false if already set or actor null
```

**Option B (authoring aid):** Marker component on the hero prefab / instance:

```csharp
// PartyMainCharacterMarker — scene authoring only
public sealed class PartyMainCharacterMarker : MonoBehaviour { }
```

At bootstrap, `PartyManager` scans `partyMembers` (or scene) for **at most one** marker and calls `TryDesignateMainCharacter`.

**Stable id (optional):** Persist designation by `ItemInstance`-style id or `CharacterStats` session id for future save/load — **v0:** live `BaseActor` reference is enough.

### 4.3 — Bootstrap / authoring (v0)

| Source | Behavior |
|--------|----------|
| **Inspector** | `PartyManager` serialized `mainCharacter` reference **or** member with `PartyMainCharacterMarker`. |
| **SampleScene** | Exactly one party prefab (e.g. barbarian) marked as main; recruits unmarked. |
| **Validation** | Editor script or `OnValidate`: error if **0** or **>1** markers in scene/party prefab root. |

### 4.4 — API contract

```csharp
bool TryDesignateMainCharacter(BaseActor actor)
```

| Condition | Result |
|-----------|--------|
| `MainCharacter` already set | `false`, log: already designated |
| `actor == null` | `false` |
| `actor` not in `partyMembers` | `false` (or auto-add to party first — **locked: must already be in partyMembers**) |
| First valid call | `true`, store reference, log success |

**No** `ClearMainCharacter()`, `SwapMainCharacter()`, or `SetMainCharacter()` override API in v0.

### 4.5 — Query helpers

```csharp
bool IsMainCharacter(BaseActor actor);
bool IsMainCharacter(GameObject go);
```

Used by death pipeline, UI, and future systems (achievements, story flags).

---

## 5. Game over trigger (locked)

### 5.1 — Condition

Game over triggers when **all** of the following are true:

1. `HasMainCharacter == true`
2. The main character’s `currentHP == 0` (after [HP clamp](Party-Member-Death-Requirements.md))
3. `HealthComponent` has fired `Died` / `PlayerController.Die()` path reached

**Does not** trigger when:

- A recruit dies but main is alive
- Main is alive and party wipe (all recruits dead) — **v0:** party continues (recruit death logs only)
- Last living member is a recruit — **not** game over

### 5.2 — Integration point

Branch in **`PartyMemberDeathService.HandleDeath`** (or dedicated `GameOverService` called from it) **before** enqueueing recruit death dialog:

```text
if (PartyManager.IsMainCharacter(member))
    → GameOverService.TriggerMainCharacterDeath(member)
    → return (do not enqueue PartyMemberDeathDialogUI for this actor)
else
    → existing recruit death flow
```

### 5.3 — Simultaneous deaths

If main and recruit(s) reach 0 HP in the same resolution window:

- **v0:** Process **main character death first** → game over immediately.
- **Do not** show recruit death dialogs or destroy recruits after game over is active.

### 5.4 — Order relative to party removal

| Step | Main character | Recruit |
|------|----------------|---------|
| Log death | Yes | Yes |
| Remove from `partyMembers` | **v0:** optional — see §5.5 | Yes (before dialog) |
| Modal | **Game over** (terminal) | Death dialog (OK) |
| Destroy `GameObject` | **v0:** do not destroy on OK (no OK) — see §5.5 | On OK |

### 5.5 — Main character GameObject on game over (v0)

**Locked v0:** On game over, the main character’s `GameObject` **remains** in the scene (corpse or standing pose) under the modal — **no** destroy, **no** recruit-style OK step. Rationale: terminal state; future restart may reload scene. Grid unregister may still run to free the tile.

**Recruits** continue to use destroy-on-OK from [party member death](Party-Member-Death-Requirements.md).

---

## 6. Game over state (locked)

### 6.1 — `GameState` extension

Add to `GameState`:

```csharp
GAME_OVER
```

When game over triggers:

- `TurnManager.currentState = GameState.GAME_OVER` (or dedicated `GameOverState` singleton — prefer enum extension for consistency).
- `TurnManager.CanActorTakeAction` → **false** for all actors.
- Enemy turns **do not** start or continue.

### 6.2 — Input freeze

While game over:

- **All** gameplay input disabled: move, wait, abilities, inventory toggle, targeting, party swap, aim bow, floor pickup.
- `InputHandler` treats game over as blocking (same or higher priority than `PartyMemberDeathDialogUI.BlocksGameplay`).
- **No** keyboard or mouse action dismisses game over.

Suggested flag:

```csharp
public static bool IsGameOver => TurnManager.Instance?.currentState == GameState.GAME_OVER;
// or GameOverService.IsActive
```

### 6.3 — Systems that must respect game over

| System | v0 behavior |
|--------|-------------|
| `PlayerCommandProcessor.TryApply` | Reject all commands |
| `InventoryUI` | Cannot open |
| Enemy AI | Frozen / not ticked |
| Hazards / traps on step | No new resolutions |
| Party swap | Disabled |
| Time / turn clock | Frozen |

---

## 7. Game over modal (terminal — locked)

### 7.1 — UX

| Element | Requirement |
|---------|-------------|
| **Style** | Same family as other modals (dim fullscreen overlay + centered panel) |
| **Title** | e.g. `Game Over` |
| **Body** | Names the fallen main character: e.g. `{DisplayName} has fallen.` Optional line: `Your journey ends here.` |
| **Dismiss** | **None** — no OK, Cancel, Escape, or click-outside close |
| **Buttons** | **No** actionable buttons in v0 (future: Retry / Main Menu may appear here) |
| **Sorting** | Above all other UI (`sortingOrder` > death dialog / inventory) |
| **Persistence** | Stays visible until scene unload / new game |

### 7.2 — Component (suggested)

**`GameOverModalUI`** in `JRogue.UI.Gameplay`:

```csharp
public static bool BlocksGameplay { get; }  // true while visible
public static bool IsVisible { get; }
public static GameOverModalUI EnsureInstance();
public void ShowTerminal(string mainCharacterDisplayName);
// No Close(), no Show(..., Action onDismiss)
```

### 7.3 — `GameOverService` (suggested)

Central coordinator:

```csharp
public static class GameOverService
{
    public static bool IsGameOver { get; }
    public static void TriggerMainCharacterDeath(BaseActor main);
}
```

Responsibilities:

1. Guard idempotent (`IsGameOver` already true → return)
2. Log §8 messages
3. Set `GameState.GAME_OVER`
4. Cancel targeting / close inventory if open
5. `GameOverModalUI.ShowTerminal(displayName)`
6. Optional: `Time.timeScale = 0` — **locked v0:** use input/state gating only (avoid side effects on animations unless desired)

### 7.4 — Debug log (game over)

Prefix: **`[GameOver]`**.

| Event | Example |
|-------|---------|
| Designated | `[GameOver] Main character designated: {DisplayName} ({gameObject.name}).` |
| Triggered | `[GameOver] Main character {DisplayName} has died. Game over.` |
| Duplicate trigger | `[GameOver] Ignored — already in game over.` |
| Invalid designation | `[GameOver] Cannot designate {name}: main character already set.` |

---

## 8. Relationship to party member death doc

| Scenario | UX |
|----------|-----|
| **Recruit** dies, main alive | [Party member death](Party-Member-Death-Requirements.md) — OK dialog → destroy |
| **Main** dies | **This doc** — terminal game over modal, no dismiss, no recruit dialog for main |
| **Main** dies with recruits alive | Game over — **recruits do not** inherit “win” or full party control |
| **All recruits dead**, main alive | **Not** game over (v0); main may still play |

Update [Party-Member-Death-Requirements.md](Party-Member-Death-Requirements.md) §8.3 when implementing: “last party member” is **not** game over unless that member **is** the main character.

---

## 9. Acceptance criteria

| ID | Test |
|----|------|
| **AC1** | Scene bootstrap designates exactly one main character; log confirms. |
| **AC2** | Second `TryDesignateMainCharacter` call fails; first unchanged. |
| **AC3** | Recruit dies → recruit death dialog with OK; game over modal does **not** appear. |
| **AC4** | Main dies (recruits alive) → game over modal appears; **no** OK on recruit death dialog for main. |
| **AC5** | Game over modal cannot be dismissed via OK, Escape, Enter, or overlay click. |
| **AC6** | After game over, movement / inventory / abilities / party swap do nothing. |
| **AC7** | `GameState` is `GAME_OVER` (or equivalent) after main death. |
| **AC8** | Swapping active leader to a recruit does **not** change who is main character. |
| **AC9** | Main dies while not the active leader (follower) → still game over. |
| **AC10** | Editor warns if zero or multiple main-character markers in SampleScene. |

---

## 10. Future — character creator integration

When the **character creator** ships:

| Concern | Direction |
|---------|-----------|
| **Output** | Creator produces a hero prefab or runtime build flagged `PartyMainCharacterMarker` / registered via `TryDesignateMainCharacter` on run start. |
| **Designation timing** | Call `TryDesignateMainCharacter` once when the run starts from creator output — **before** entering dungeon gameplay scene. |
| **Immutability** | Same rule: designation never changes mid-run. |
| **Game over** | Unchanged — creator hero is always the main character for that run. |
| **Recruits** | Join party later without marker; never eligible for designation. |

No character creator UI, save format, or stat generation in v0.

---

## 11. Implementation checklist

- [x] `PartyMainCharacterMarker` or `PartyManager.TryDesignateMainCharacter` (§4)
- [x] SampleScene: mark one hero as main (§4.3) — `PartyMainCharacterMarker` on `BarbarianPlayer` prefab (`Party_Barbarian_Warrior`)
- [x] Editor validation: single main marker (§4.3) — **`JRogue/Party/Validate Main Character Markers in SampleScene`**
- [x] `GameState.GAME_OVER` + `TurnManager` gating (§6)
- [x] `GameOverService.TriggerMainCharacterDeath` (§5, §7)
- [x] Branch in `PartyMemberDeathService` (§5.2)
- [x] `GameOverModalUI` terminal modal — no dismiss (§7)
- [x] `InputHandler` / `BlocksGameplay` includes game over (§6.2)
- [x] Update [Party-Member-Death-Requirements.md](Party-Member-Death-Requirements.md) cross-reference §8.3
- [x] Unit tests: designation immutability, `IsMainCharacter`, game over branch
- [ ] Play-mode AC1–AC10

---

## 12. Document history

| Date | Note |
|------|------|
| 2026-05-29 | Initial requirements — immutable single main character, terminal game over modal |
