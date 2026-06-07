# Dungeon log (message console) — Requirements

**Dungeon Crawl Stone Soup (DCSS)** shows a **message console** at the edge of the play view: recent game text scrolls through a compact pane, and the player can open a **full message history** to review everything since the current session began. JRogue adopts the same split: a **persistent bottom console** during gameplay (town and dungeon) plus a **scrollable log menu** for the full buffer. The **map camera** centers the lead party member in the **playfield** above the console—not in the geometric center of the full screen.

**Status:** Implemented (v0).

**Depends on:** `CameraFollow`, `PartyManager.GetActiveMember`, `DungeonEntryService`, `DungeonExitService`, `RunPartyPersistence`, `InputHandler` / modal blocking (`InventoryUI.BlocksGameplay`, quest/shop overlays), [Fog of war](../World/Fog-Of-War-Requirements.md) (playfield sizing), [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) (full-screen overlay pattern), [Quest journal](../World/Quest-Requirements.md), [Shop NPCs](../World/Shop-NPC-Requirements.md), [Dungeon time](../World/Dungeon-Time-Requirements.md).

**Related:** `DungeonGenerationLog` (structured dev logging today — may mirror into console in v0), `Debug.Log` usage across gameplay systems.

**Explicitly out of scope (v0):** Message **channels** with per-channel toggles (DCSS `plain`, `god`, `danger`, …); colorized log lines by severity; click-to-examine links in log text; log export/save; persisting log across app restarts; filtering/search in log menu; mirroring **Unity Editor** console separately from in-game console; shrinking full-screen menus to leave console visible.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **DCSS-style console** — Bottom pane shows the **most recent** messages; older lines scroll off when over capacity. |
| **G2** | **Console scrollback (DCSS parity)** — Player can **scroll** the compact console through recent messages the same way DCSS allows (`-` / `=` on main screen; see §3). |
| **G3** | **Full log menu** — Dedicated overlay lists **every message in the current log session**, scrollable from newest to oldest (DCSS **Ctrl+P** analogue; JRogue uses **P** to avoid Unity editor conflict). |
| **G4** | **Debug.Log mirror (v0)** — Every `Debug.Log` / `Debug.LogWarning` / `Debug.LogError` also appends one line to the in-game log buffer (§7). |
| **G5** | **Session reset** — Log buffer **clears** when **entering** the dungeon from town and when **leaving** the dungeon back to town (§8). |
| **G6** | **Town + dungeon** — Console chrome is **visible in both** town and dungeon scenes; content follows the current session (§8). |
| **G7** | **Playfield camera** — Lead party member stays at the **visual center of the playfield** (area **above** the console), not screen center (§5). |
| **G8** | **Full-screen menus unchanged** — Inventory, quest journal, shop, dialog modals, etc. remain **full-screen** overlays that **cover** the console (§9). |
| **G9** | **Gameplay input** — Console scroll / log menu must not fire while another modal blocks gameplay input. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Message console** | Always-visible bottom UI strip showing the tail of the log (recent lines). |
| **Log session** | In-memory message buffer since the last **boundary clear** (§8). |
| **Boundary clear** | Wipe all log lines on **dungeon entry** or **dungeon exit** scene transition. |
| **Compact capacity** | Max lines visible in the console without scrolling (authored default **4–6**). |
| **Scrollback offset** | How far back the player has scrolled in the compact console from “live tail”. |
| **Log menu** | Full-screen-adjacent modal (not full viewport—see mock §10.2) showing **all** lines in the log session. |
| **Playfield** | Map viewport rect: screen minus console (and any permanent HUD rails). |
| **Lead party member** | Active controlled member (`PartyManager.GetActiveMember()` / formation index 0). |

---

## 3. DCSS reference behavior

| DCSS | JRogue mapping |
|------|----------------|
| Compact message area shows recent lines | **Message console** (§6) |
| `-` / `=` scroll message window on main screen | **Console scrollback** when console focused (§6.3) — **include in v0** (DCSS supports this) |
| **P** — “Show previous messages” full history | **Log menu** (§10) |
| Arrow keys / Home / End scroll full history | Log menu scroll (§10.3) |
| Map occupies space **above** messages | **Playfield** + console split (§5) |

**Locked (user request):** Implement compact-console **scroll** only because DCSS does. DCSS **does** allow scrolling the message pane on the main screen (`-` / `=`). The separate **Ctrl+P** history maps to the **log menu** (JRogue: **P**).

---

## 4. Current baseline (as-is)

| Area | Today |
|------|--------|
| **Player feedback** | `Debug.Log` to Unity Console only; no in-game message pane. |
| **Camera** | `CameraFollow` lerps to active member at **screen** center (`CameraFollow` + full viewport). |
| **Town ↔ dungeon** | `DungeonEntryService` / `DungeonExitService` scene loads; no shared message buffer. |
| **Modals** | `InventoryUI`, quest journal, shop UI, confirm dialogs — full-screen or large overlays. |
| **Structured logs** | Prefix conventions (`[MonsterSpawn]`, `[Quest]`, `[Shop]`, …) already used — console can show raw text. |

---

## 5. Layout — playfield, console, camera

### 5.1 — Screen partition (locked)

```
┌─────────────────────────────────────────────── PLAYFIELD (map + entities) ───┐
│                                                                              │
│                         ·  ← lead member centered HERE                       │
│                        @@@                                                   │
│                                                                              │
│   (fog, entities, targeting reticle when active)                             │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│ MESSAGE CONSOLE  (fixed height, e.g. 96–120 px @ 1080p)                      │
│  You hit the goblin for 4 damage.                                            │
│  The goblin hits you for 2 damage.                                           │
│  [PgUp/PgDn or -/+ scroll when focused]                                      │
└──────────────────────────────────────────────────────────────────────────────┘
```

| Region | Behavior |
|--------|----------|
| **Playfield** | Top `(100% - consoleHeight)` of **gameplay canvas** (not full screen when HUD exists elsewhere—console is the only permanent bottom deduction in v0). |
| **Console** | Bottom strip; **always visible** during town and dungeon gameplay. |
| **Full-screen menus** | Cover **entire** screen including console when open (§9). |

### 5.2 — Camera centering (locked)

| Rule | Detail |
|------|--------|
| **Center point** | Geometric center of the **playfield rect**, not the full screen. |
| **Follow target** | Lead party member world position (existing `CameraFollow` target). |
| **Implementation hint** | `CameraFollow` (or a `PlayfieldLayoutService`) applies a **vertical offset** equal to half the console height (in world units via orthographic size / pixel-per-unit), **or** adjusts camera pixel rect to the playfield and keeps member at rect center. |
| **Targeting reticle** | Uses same grid as today; only camera framing changes. |
| **Scene parity** | Same rule in **TownTest** and **DungeonFloorTest** (and production town/dungeon scenes when added). |

**Acceptance:** With console visible, the lead member sits visually midway between the top of the playfield and the console top edge—not midway between top and bottom of the monitor.

---

## 6. Message console (compact pane)

### 6.1 — Content

| Property | v0 default |
|----------|------------|
| **Visible lines** | **5** (designer-tunable `compactVisibleLines`) |
| **Font** | Monospace or readable bitmap; match existing UI density |
| **Newest line** | Bottom of console (DCSS-style) |
| **Overflow** | Oldest lines drop from **buffer** only when exceeding **session max** (§7.2); compact view shows sliding window |

### 6.2 — Live tail vs scrollback

| State | Display |
|-------|---------|
| **Default (offset = 0)** | Console auto-scrolls to show **newest** messages as they arrive. |
| **Scrolled back (offset > 0)** | Console shows older slice; **new messages do not** force scroll until player returns to tail (DCSS behavior). |
| **Return to tail** | `End` key or click “Jump to latest” affordance (optional v0.1); v0 minimum: scroll until offset = 0. |

### 6.3 — Scrolling the compact console (DCSS parity)

When **no gameplay-blocking modal** is open and log menu is closed:

| Input | Action |
|-------|--------|
| **`-`** / **`=`** (or **`[` / `]`**) | Scroll console back / forward one line |
| **Page Up / Page Down** | Scroll one page (visible line count) |
| **Mouse wheel** over console | Scroll back / forward (tiles-friendly) |

Console must **not** steal input while `InventoryUI`, quest journal, shop, or other `BlocksGameplay` modals are open.

### 6.4 — Opening log menu from console

| Input | Action |
|-------|--------|
| **P** (primary) | Open **log menu** (§10) |
| Optional hotkey | **`L`** if not conflicting — defer if bound elsewhere |

---

## 7. Log capture — v0 `Debug.Log` mirror

### 7.1 — API

Introduce **`GameLog`** (or **`DungeonLogService`**) as the single append path for the in-game buffer:

```csharp
public static class GameLog
{
    public const string LogPrefix = "[GameLog]";

    public static void Info(string message);
    public static void Warn(string message);
    public static void Error(string message);
}
```

**v0 requirement:** Register **`Application.logMessageReceived`** (or a thin wrapper installed at bootstrap) so **every** `Debug.Log`, `Debug.LogWarning`, and `Debug.LogError` also calls the append path **once** with the same string Unity receives.

| Source | Mirrored? |
|--------|-----------|
| `Debug.Log*` | **Yes** (v0) |
| `Debug.LogException` | **Yes** — single line summary + exception type |
| Unity internal spam | Optional filter list (exclude `[Physics]`, etc.) — defer unless noisy |

**Duplicate prevention:** Wrapper helpers (`GameLog.Info`) call `Debug.Log` **and** append, **or** rely solely on `logMessageReceived` — pick **one** path to avoid double entries (locked: **prefer `logMessageReceived` only** so existing `Debug.Log` calls need no edits).

### 7.2 — Session buffer

| Property | v0 default |
|----------|------------|
| **Max lines** | **500** per log session (ring buffer or truncate oldest) |
| **Line format** | Plain text; optional `[HH:mm:ss]` prefix (designer toggle) |
| **Threading** | Main thread only; queue if needed from async |

### 7.3 — Future (post-v0)

Gradually replace high-traffic `Debug.Log` with `GameLog` for structured categories; optional `GameLog.Write(ref LogChannel, …)` — **not required for v0**.

---

## 8. Log session lifecycle (clear rules)

**Locked:**

| Event | Action |
|-------|--------|
| **Enter dungeon** — `DungeonEntryService` confirmed load to dungeon scene | **`ClearSession()`** before / on scene load |
| **Leave dungeon** — forced exit or voluntary return to town (`DungeonExitService` load town) | **`ClearSession()`** before / on scene load |
| **Floor transition inside dungeon** | **No clear** — same log session |
| **Death / game over** | **No clear** until town load (exit path clears) |
| **New game / app quit** | Clear in memory; no persist |

Town gameplay after exit starts with an **empty** console until new messages arrive. Entering dungeon again clears any town messages from the prior town visit.

**Note:** “Entire dungeon run” in the log menu means **entire current log session** (since last boundary clear)—not the full multi-day campaign across town visits.

---

## 9. Interaction with full-screen menus

| UI | Console visibility | Log input |
|----|-------------------|-----------|
| **Inventory** | Hidden under full-screen panel | Blocked |
| **Quest journal** | Hidden | Blocked |
| **Shop / buy-sell** | Hidden | Blocked |
| **NPC dialog** | Hidden | Blocked |
| **Friendly-fire / auto-pickup confirm** | Hidden | Blocked |
| **Log menu** | **Visible** (or full-screen list—see mock); closes back to gameplay | Active while open |

Full-screen menus **do not** resize to leave a console strip (user requirement).

---

## 10. Mocks

### 10.1 — Gameplay layout (town or dungeon)

Authoritative reference for playfield + console + camera center (`·` = screen playfield center target for lead member):

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ░░░░░░░░░░░░░░░░░░░░░░  PLAYFIELD  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░░░░░░░░░░░░░·····░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░░░░░░░░░░░··@ Leader··░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░░░░░░░░░░░·····░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │
│ ░░░  #  #  #  wall / floor tiles / fog  #  #  #  ░░░░░░░░░░░░░░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │
├─────────────────────────────────────────────────────────────────────────────┤
│ You open the door.                                                          │
│ Greta offers her wares.                                                     │
│ You buy a throwing knife for 3 gold.                                        │
│ [Missile:ThrowingKnife] Added to inventory.                                 │
│ Press P for full message history                         -/+ scroll  ▲▼      │
└─────────────────────────────────────────────────────────────────────────────┘
     ↑ console height fixed (e.g. 20% of gameplay canvas or 100px min)         
```

### 10.2 — Log menu (full session history)

Scrollable list of **all** lines in the current session. **Not** full viewport like inventory—use a large centered panel (or nearly full screen) with obvious close affordance. DCSS Ctrl+P uses a dedicated review screen; JRogue may use ~80% height panel.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ░░░░░░░░░░░░░░░░░░░░░░░░░  (dimmed gameplay behind)  ░░░░░░░░░░░░░░░░░░░░░ │
│ ░░░░░┌───────────────────────────────────────────────────────────────┐░░░░░░░ │
│ ░░░░░│  MESSAGE HISTORY                                    [ Esc ] │░░░░░░░ │
│ ░░░░░├───────────────────────────────────────────────────────────────┤░░░░░░░ │
│ ░░░░░│ ▲ scroll                                                    │░░░░░░░ │
│ ░░░░░│ You enter the dungeon.                                      │░░░░░░░ │
│ ░░░░░│ [DungeonEntry] Loading dungeon scene...                     │░░░░░░░ │
│ ░░░░░│ [MonsterSpawn] Day 1 floor=dungeon_floor_01 spawned=3       │░░░░░░░ │
│ ░░░░░│ You hit the skeleton for 5 damage.                          │░░░░░░░ │
│ ░░░░░│ The skeleton misses you.                                    │░░░░░░░ │
│ ░░░░░│ ...                                                           │░░░░░░░ │
│ ░░░░░│ ▼                                                             │░░░░░░░ │
│ ░░░░░├───────────────────────────────────────────────────────────────┤░░░░░░░ │
│ ░░░░░│ 142 messages · session since dungeon entry    Home End PgUp/Dn│░░░░░░░ │
│ ░░░░░└───────────────────────────────────────────────────────────────┘░░░░░░░ │
│ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │
├─────────────────────────────────────────────────────────────────────────────┤
│ (console still visible beneath modal OR fully covered — pick one in impl)   │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Locked for v0 mock:** Log menu is a **large modal panel** (not inventory-scale full chrome). Esc closes. Scroll with arrows, PgUp/PgDn, Home/End, mouse wheel on list.

**Implementation choice:** Covering console during log menu is acceptable; gameplay remains paused/dimmed.

---

## 11. Implementation sketch

### 11.1 — Components

| Piece | Responsibility |
|-------|----------------|
| **`GameLogSession`** | Ring buffer, `ClearSession()`, events on append |
| **`GameLogMirror`** | `Application.logMessageReceived` → append |
| **`MessageConsoleUI`** | Renders tail / scrollback; handles `-`/`=`/wheel |
| **`MessageHistoryUI`** | Log menu; binds to full buffer |
| **`PlayfieldLayout`** | Computes playfield rect, console height, camera offset |
| **`CameraFollow`** | Uses playfield center (§5.2) |

### 11.2 — Bootstrap

- DDOL or scene singleton on **`TownTest`** / **`DungeonFloorTest`** gameplay canvas.
- Subscribe to **`DungeonEntryService`** / **`DungeonExitService`** (or scene load callbacks) for **`ClearSession()`**.
- Hook **`RunPartyPersistence`** boundaries if entry/exit already centralized there.

### 11.3 — Input priority

```
1. Full-screen modal (inventory, shop, …) → consumes all input
2. Log menu open → scroll / close only
3. Console scroll (-/=, wheel on console) when gameplay active
4. Normal gameplay
```

---

## 12. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC1** | Enter town → play dungeon → console shows dungeon messages; return to town → log **cleared**. |
| **AC2** | Enter dungeon from town → log **cleared** at entry. |
| **AC3** | Existing `Debug.Log("You hit the goblin.")` appears in console **and** Unity Console. |
| **AC4** | Compact console shows last **N** lines; `-`/`=` scrolls older lines (DCSS parity). |
| **AC5** | **P** opens log menu with **all** session lines; scrollable; Esc closes. |
| **AC6** | Lead member sits at playfield center, not screen center, with console visible. |
| **AC7** | Open inventory → console not visible / not interactive; close → console returns. |
| **AC8** | Console visible in **TownTest** during shop/dialog messages. |
| **AC9** | Floor change inside dungeon does **not** clear log. |

---

## 13. Open questions (defaults locked for v0)

| Question | v0 default |
|----------|------------|
| Console height | **100 px** at 1080p reference; scale with canvas |
| Log menu covers console? | **Yes** (dimmed full-screen overlay) |
| Timestamp on lines? | **Off** by default |
| Also mirror `DungeonGenerationLog`? | **Yes** — goes through `Debug.Log` already |
| `-`/`=` vs `[`/`]` | **`-` / `=`** primary (DCSS) |

---

## 14. Related doc updates (when implemented)

| Doc | Update |
|-----|--------|
| [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) | Note gameplay canvas reserves bottom console strip |
| [Dungeon time](../World/Dungeon-Time-Requirements.md) | Phase-change logs appear in console |
| [Targeting sight range](../Combat/Targeting-Sight-Range-Requirements.md) | `[Targeting:Sight]` lines visible in console |
