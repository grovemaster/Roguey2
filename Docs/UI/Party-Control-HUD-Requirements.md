# Party control HUD — Requirements

During the player phase, JRogue lets the player control **one party member at a time** while each living member may take **one action per turn**. Today that state is invisible: there is no HUD showing **who is selected**, **which F-key selects whom**, or **who has already acted**. This document specifies a **party control HUD** (portrait strip + map highlight) so the player can manage the roster without reading debug logs.

**Status:** **Implemented (v0)** — portrait strip, F-key labels, acted greying, main-character crown, click-to-select, map highlight, top HUD camera offset.

**Depends on:** `PartyManager`, `TurnManager`, `InputHandler` / `PlayerCommandProcessor`, `GameControls` (`SelectPartyMember`, `CyclePartyMembers`), `BaseActor`, `PartyRacePortraitCatalog`, `PortraitDefinition`, `PlayfieldLayout`, [Ability hotbar](Ability-Hotbar-Requirements.md) (hotbar title already shows active member name), [Dungeon log](Dungeon-Log-Requirements.md) (bottom HUD stack).

**Related:** [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) (party member carousel in inventory), [Fog of war](../World/Fog-Of-War-Requirements.md), formation / rush (`FormationRushService`, `PartyManager.IsFormationActive`).

**Explicitly out of scope (v0):** Drag-and-drop portrait reordering; rebinding F-keys; HP/mana/resource bars on portraits; status-effect icon rows; click-to-target on map without selecting member first; gamepad portrait focus; persisting portrait strip layout across sessions; separate “tactical” and “exploration” HUD skins.

**Locked decisions (user, 2026-06-07):** Reserve **top HUD height** in `PlayfieldLayout` for camera vertical offset; show **main-character crown** on portrait; **click portrait** to select (same as F-key).

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Active member obvious** — Player always knows which character they are controlling. |
| **G2** | **F-key map visible** — Each portrait shows its **current** select key (`F1`…`F5`). |
| **G3** | **Acted state visible** — Portraits (and map highlight) show who **has already spent their action** this player phase. |
| **G4** | **Stable slot order** — Portrait strip order matches **`partyMembers` list index** (= F-key order). Swapping control **does not** reorder the list; F-keys stay bound to the same member. |
| **G5** | **Map feedback** — Selected member has a clear **in-world outline / highlight** on the playfield sprite. |
| **G6** | **Hotbar coherence** — Switching member updates [ability hotbar](Ability-Hotbar-Requirements.md) **and** portrait selection in the same frame. |
| **G7** | **Playfield camera** — Lead member centers in the **playfield band** between top portrait strip and bottom console/hotbar (§6.3). |
| **G8** | **Click to select** — Left-click portrait `i` selects that member (same as **F{i+1}**) when gameplay is not modal-blocked. |
| **G9** | **Main character marked** — Party **main character** shows a **crown** badge on their portrait (`PartyManager.IsMainCharacter`). |
| **G10** | **Modal-safe** — Strip may remain visible under full-screen modals but does not accept clicks while `GameplayModalGate.BlocksFloorGameplay`. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Active member** | `PartyManager.GetActiveMember()` — controlled via `ActiveMemberIndex`; **independent** of formation leader (`partyMembers[0]`). |
| **Party list index** | Position in `PartyManager.partyMembers`. Index **0** is the **formation leader** on the map; F-keys map to fixed indices (`F1` → 0, `F2` → 1, …). |
| **Select key** | Function key bound to `SelectPartyMember`: **F1 → index 0**, **F2 → index 1**, … |
| **Acted this turn** | Member recorded in `TurnManager`’s acted set (`!CanActorTakeAction(member)` during `PLAYER_TURN`). |
| **Portrait strip** | Top HUD row of party portraits + key labels. |
| **Main character** | Immutable roster anchor (`PartyManager.MainCharacter` / `IsMainCharacter`); game-over if they die. |
| **Main-character crown** | Small crown icon overlay on the main character’s portrait chip (independent of active selection). |
| **Map highlight** | World-space or screen-space outline around the active member’s sprite. |
| **Playfield band** | Map viewport between top portrait strip and bottom hotbar + console; camera centers the active member here. |
| **Player phase** | `TurnManager.currentState == GameState.PLAYER_TURN`. |

---

## 3. Industry precedes (what we borrow)

| Game | Pattern JRogue adopts |
|------|----------------------|
| **Baldur’s Gate 3** | Top portrait row; selected portrait framed; grey/desaturated when out of actions. |
| **Divinity: Original Sin 2** | Party portraits with key hints; active character bright ring on map. |
| **XCOM / tactical RPGs** | “Unit has moved” = dimmed portrait + colored outline on active unit. |
| **Final Fantasy / CRPG classics** | Fixed party order UI that reshuffles when leader changes. |

**Locked aesthetic:** Same **dark glass** chrome as [Ability hotbar](Ability-Hotbar-Requirements.md) and [Dungeon log](Dungeon-Log-Requirements.md) — not MMO unit-frame clutter.

---

## 4. Current baseline (as-is)

| Area | Today |
|------|--------|
| **Selection input** | **F1–F5** → `PlayerCommand.SwapPartyMember(index)`; **F** cycles (`CyclePartyMembers`). See `GameControls.inputactions`. |
| **Swap semantics** | `PartyManager.SwapActiveMember(i)` sets **`ActiveMemberIndex = i`** only. **`partyMembers` order is unchanged** (formation, map movement, follower rush still use index 0 as leader). Camera follows the newly controlled member. |
| **Turn tracking** | `TurnManager` private `charactersWhoActed`; `CanActorTakeAction(go)` is public; no UI subscription. |
| **Feedback** | `[SWAP]` / `[TurnManager]` **Debug.Log** only; hotbar header shows `"ABILITY HOTBAR — {DisplayName}"`. |
| **Portraits** | `PartyRacePortraitCatalog.ResolveForActor(actor)` + optional `BaseActor.PortraitOverride`; used by dialog UI, not gameplay HUD. |
| **Map highlight** | **None** for selected party member. |
| **Hotbar doc** | [Ability hotbar §5.3](Ability-Hotbar-Requirements.md) listed portrait chips as **optional v0.1** — **promoted to required** by this doc. |

---

## 5. F-key ↔ party list semantics (locked)

Control selection is **decoupled** from formation / map order.

### 5.1 — Binding rule

| Key | `PartyMemberIndex` | Selects `partyMembers[…]` |
|-----|-------------------|-----------------------------|
| **F1** | 0 | Index **0** |
| **F2** | 1 | Index **1** |
| **F3** | 2 | Index **2** |
| **F4** | 3 | Index **3** |
| **F5** | 4 | Index **4** |

Slots **F6+** are not bound today; if roster exceeds five, show portrait **without** a select key label and document follow-up (see §12).

**Locked UI rule:** Portrait strip always renders **left → right** as `partyMembers[0]` … `partyMembers[n-1]`, labeling slot `i` with **`F{i+1}`**. Labels and positions **do not change** when the player swaps control.

### 5.2 — Swap without reorder (example)

```
Initial list:  [ A*, B, C ]     F1=A  F2=B  F3=C   (* = active, controlled A)

Press F2 (select B):
  SwapActiveMember(1) →  [ A, B*, C ]     F1=A  F2=B  F3=C   (list unchanged; B now controlled)
```

Formation leader remains **A** (`partyMembers[0]`). When a different member is controlled and moves in formation mode, breadcrumbs are **realigned as if that member led the move** (same follower behavior as the old list-reorder swap), without changing `partyMembers` order.

### 5.3 — Active member invariant

After every successful swap, `GetActiveMember()` == `partyMembers[ActiveMemberIndex]`. The **selected border** follows `ActiveMemberIndex`, not a fixed portrait slot.

### 5.4 — Future: manual slot configuration (out of v0)

Later: player-configurable portrait order and F-key bindings (persisted per save). v0 uses **list index = control slot = F-key index**.

---

## 6. Layout — screen partition

### 6.1 — Vertical stack (locked)

```
┌────────────────────────────────────── PLAYFIELD ──────────────────────────────────────┐
│  ┌──────────────── Party portrait strip (NEW) ────────────────┐                       │
│  │  [F1 B*]  [F2 A]  [F3 C]   ← top-center, compact           │                       │
│  └─────────────────────────────────────────────────────────────┘                       │
│                                                                                        │
│                              @  ← active member map highlight                          │
│                                                                                        │
├────────────────────────────────────────────────────────────────────────────────────────┤
│                         (existing bottom HUD unchanged)                                │
│  Ability hotbar → Message console → (screen bottom)                                    │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

| ID | Rule |
|----|------|
| **L1** | Portrait strip anchored **top-center** with safe margin from screen edge (authored default **8–12 px** at 1080p reference). |
| **L2** | Strip height is authored in `PlayfieldLayout` (see §6.3); does not overlap bottom hotbar or message console. |
| **L3** | Strip visible in **town and dungeon** during gameplay (same scenes as ability hotbar). |
| **L4** | Hidden or collapsed during `GameState.GAME_OVER` and while dead-member / game-over modals block control. |

### 6.3 — PlayfieldLayout + camera (locked)

Extend `PlayfieldLayout` symmetrically with the bottom HUD stack ([Dungeon log §5](Dungeon-Log-Requirements.md)):

| Constant / API | Authored default (1080p reference) | Purpose |
|----------------|--------------------------------------|---------|
| `PartyStripHeightPixels` | **96** | Total vertical space reserved for portrait strip + top margin |
| `GetPartyStripHeightPixels()` | `PartyStripHeightPixels * Scale` | Scaled strip height |
| `GetTopHudHeightPixels()` | alias of `GetPartyStripHeightPixels()` | Top rail for layout math |
| `GetVerticalHudHeightPixels()` | top + bottom | Sum of both rails |
| `GetPlayfieldHeightPixels()` | `Screen.height - GetVerticalHudHeightPixels()` | Usable map band |

**Camera vertical offset (locked):** Update `GetCameraVerticalOffsetWorld` so the follow target sits at the **center of the playfield band**, not screen center:

```
netOffsetY = (bottomHudFraction - topHudFraction) * 0.5 * orthographicSize
```

Where `bottomHudFraction = GetBottomHudHeightPixels() / Screen.height` and `topHudFraction = GetTopHudHeightPixels() / Screen.height`. Today only bottom offset is applied (negative Y); top rail adds a compensating **positive** shift so the active member stays visually centered between rails.

| ID | Rule |
|----|------|
| **PL1** | `CameraFollow` continues to use `PlayfieldLayout.GetCameraVerticalOffsetWorld` — no duplicate offset math in view code. |
| **PL2** | Recompute offset when screen size changes (same pattern as bottom HUD scaling). |
| **PL3** | Portrait strip UI is positioned within the top **PartyStripHeightPixels** band; playfield entities must not draw under the strip. |

### 6.2 — Portrait strip mock

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         PARTY                                                  │
│   ┌─────────┐   ┌─────────┐   ┌─────────┐   ┌─────────┐                      │
│   │  F1     │   │  F2     │   │  F3     │   │  F4     │                      │
│   │ ┌─────┐ │   │ ┌─────┐ │   │ ┌─────┐ │   │ ┌─────┐ │                      │
│   │ │👑🧔 │ │   │ │ 🧝  │ │   │ │ 🛡  │ │   │ │ 🔮  │ │   ← crown = main char │
│   │ └─────┘ │   │ └─────┘ │   │ └─────┘ │   │ └─────┘ │                      │
│   │ Bruenor │   │  Aria   │   │  Thorin │   │  Elara  │                      │
│   └─────────┘   └─────────┘   └─────────┘   └─────────┘                      │
│      ▲ active ring + full color (click or F-key to select)                    │
│        others: full color if can act · grey if acted                          │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Portrait strip — behavior

### 7.1 — Data source

| ID | Rule |
|----|------|
| **P1** | One chip per **living** entry in `partyMembers` (see §9 for dead members). |
| **P2** | Portrait image from `PartyRacePortraitCatalog.ResolveForActor(member)`; fallback silhouette if missing. |
| **P3** | Short name label: `BaseActor.DisplayName` (truncate with ellipsis if wider than chip). |
| **P4** | Key label: **`F{index+1}`** for indices `0…4`; omit key label when index ≥ 5 until F6 binding exists. |
| **P5** | **Main-character crown:** if `PartyManager.IsMainCharacter(member)`, show a small **crown icon** (top-right of portrait frame, above the face art). Crown remains visible when portrait is greyed (acted); crown does **not** imply active selection. Tooltip on hover: `"Main character"`. |

### 7.2 — Selected (active) affordance

| ID | Rule |
|----|------|
| **S1** | **Active** portrait (`ActiveMemberIndex`) shows a **bright border / outer glow** (authored default: warm gold `#e8c547` or party-accent cyan — pick one in impl). |
| **S2** | Active chip **full saturation** even if member has acted (player still “holding” that character). |
| **S3** | Map highlight (§8) **must** match the same member as the framed portrait. |

### 7.3 — Acted-this-turn affordance

| ID | Rule |
|----|------|
| **A1** | During `PLAYER_TURN`, if `!TurnManager.CanActorTakeAction(member.gameObject)` **and** member is **not** stunned-only edge case without having acted — treat as **acted** when member is in acted set. **Implementation:** expose `TurnManager.HasActedThisTurn(GameObject)` (or equivalent event) — do not duplicate turn logic in UI. |
| **A2** | **Acted** portrait: **desaturate / grey** portrait image (~40–55% saturation), mute name text; **keep** F-key label readable (dark badge + outline, same pattern as hotbar key labels). |
| **A3** | **Can still act:** full-color portrait. |
| **A4** | On **`TurnManager` player-phase reset** (all members cleared from acted set), all non-dead portraits return to full color immediately. |
| **A5** | **Stunned** but not yet acted: show **distinct** affordance (v0 minimum: full grey + small “stunned” tooltip on hover; optional icon v0.1). Do **not** mark as acted unless they actually consumed their action. |

### 7.4 — Refresh triggers

Rebuild or refresh strip when:

- `PartyManager.SwapActiveMember` / `CycleActiveMember` completes  
- `TurnManager.OnPlayerActionComplete` marks a member  
- Player phase ends (`charactersWhoActed.Clear`)  
- Party roster changes (death removal, recruit — future)  
- Hotbar `RefreshAll` may call shared `PartyControlHud.Refresh()` to stay in sync  

### 7.5 — Click to select (locked, v0)

| ID | Rule |
|----|------|
| **C1** | **Left-click** portrait at list index `i` → invoke the same path as **`PlayerCommand.SwapPartyMember(i)`** / **F{i+1}**. |
| **C2** | Ignored when `GameplayModalGate.BlocksFloorGameplay`, `currentState != PLAYER_TURN`, or member is dead. |
| **C3** | Clicking the **already-active** portrait is a no-op. |
| **C4** | Hover cursor: **pointer** on clickable chips during player turn. |
| **C5** | Keyboard selection (**F1–F5**, **F** cycle) remains fully supported; click is additive, not a replacement. |

---

## 8. Map highlight — active member outline

### 8.1 — Goal

Player can locate the controlled character on a busy tile without hunting for the camera center.

### 8.2 — Mock

```
        ┌───┐
        │ @ │  ← 1–2 px bright outline / corner brackets around sprite bounds
        └───┘
     (pulses subtly optional v0.1)
```

| ID | Rule |
|----|------|
| **H1** | Highlight **only** `GetActiveMember()`. |
| **H2** | Render **above** floor tile, **below** floating UI / targeting reticle (sort order documented in impl). |
| **H3** | Style: **high-contrast outline** (not filled overlay) so tile terrain remains visible. Authored default: **2 px** outer ring, color matches portrait active border (§7.2). |
| **H4** | Updates **immediately** on swap; removed when active member is null or dead. |
| **H5** | Visible through fog **only** on tiles the party already sees (do not reveal hidden enemies via outline). |
| **H6** | **Acted state** does **not** remove map highlight — outline shows **selection**, not action budget. Optional v0.1: dim outline when acted. |

### 8.3 — Implementation options (pick one)

| Option | Pros | Cons |
|--------|------|------|
| **A. Sprite outline shader** | Clean at any zoom | Needs material on party prefabs |
| **B. Child `SpriteRenderer` ring** | Simple, no shader | Manual offset per prefab scale |
| **C. World-space UI quad** | Matches portrait color exactly | Extra canvas |

**Recommendation (v0):** **B** or pooled ring child on `BaseActor` toggled by `PartyControlHud` — lowest risk.

---

## 9. Edge cases

| Case | Portrait strip | Map highlight |
|------|----------------|---------------|
| **Member dead (`currentHP <= 0`)** | Omit from strip **or** show skull chip greyed (pick **omit** for v0; dead already removed from control). | N/A |
| **Enemy turn / `BUSY`** | All portraits muted; acted styling **frozen** from end of player input | Hide highlight |
| **Only one living member** | Single portrait, **F1** only | Highlight that member |
| **Formation active (`T`)** | No change — still per-member acted tracking when individuals act | Highlight active leader |
| **Targeting mode** | Strip remains; highlight stays on caster | Reticle is separate |
| **Safe zone / modal open** | Strip visible; **clicks blocked** | Highlight unchanged |

---

## 10. Turn / action semantics (UI must mirror code)

Actions that call `TurnManager.OnPlayerActionComplete` for the active member count as **acted**, including:

- Successful move (non-formation bump paths)  
- Wait  
- Ability / hotbar use that completes turn  
- Inventory use that completes turn  

Actions that **do not** mark acted:

- Opening inventory, quest journal, log menu  
- Swapping party member (**F-key**)  
- Toggle formation (unless it consumes leader action — today it does **not** if leader hasn’t acted)  
- Starting targeting (completion on confirm marks acted)  

UI **must not** infer acted state from movement animation alone — only `TurnManager` / `CanActorTakeAction`.

---

## 11. Integration with ability hotbar

| ID | Rule |
|----|------|
| **I1** | On swap, existing `AbilityHotbarUI.RefreshAll()` runs (already on active-member change). |
| **I2** | Hotbar header `"ABILITY HOTBAR — {name}"` remains; portrait strip is the **primary** who-am-I control — header is redundant but kept for v0. |
| **I3** | [Ability hotbar §5.3](Ability-Hotbar-Requirements.md) superseded by this document for portrait strip scope. |

---

## 12. Backend gaps (implementation prerequisites)

| Gap | Required change |
|-----|-----------------|
| **Acted query** | `TurnManager`: public `bool HasActedThisTurn(GameObject actor)` or `IReadOnlyCollection` + event `PlayerActedMemberChanged`. |
| **Swap event** | `PartyManager`: optional `event Action ActiveMemberChanged` fired after `SwapActiveMember` (UI subscribes instead of polling). |
| **Portrait catalog** | Ensure gameplay scene loads `PartyRacePortraitCatalog` (Resources or serialized ref on HUD bootstrap). |
| **PlayfieldLayout** | Add `PartyStripHeightPixels`, `GetTopHudHeightPixels()`, update `GetCameraVerticalOffsetWorld` for top + bottom rails (§6.3). |
| **Crown asset** | Small crown sprite or TMP icon for main-character badge (fallback: Unicode ♔ with outline if no art). |
| **F6+ party size** | If six-member parties ship, add `f6` binding + sixth chip — track as separate task. |

---

## 13. Acceptance criteria (v0)

1. Portrait strip visible top-center during dungeon/town gameplay with one chip per living member.  
2. Each chip shows **correct F-key** for its **fixed** list index; after **F2** swap, portrait order and labels **unchanged** — only the selected border moves.  
3. Active member portrait has **clear selected border**; others do not.  
4. After a member moves/acts, their portrait **greys out**; untouched members stay full color.  
5. New player phase clears grey on all living members.  
6. Active member has **visible map outline** matching selection; outline moves on swap.  
7. No overlap with message console or ability hotbar.  
8. Strip updates without requiring scene reload when swapping mid-turn.  
9. **Main character** portrait shows **crown** badge; non-main members do not.  
10. **Left-click** portrait selects that member (same as F-key) when not modal-blocked.  
11. **Camera** centers active member in the **playfield band** between top portrait strip and bottom HUD (verified at 1080p and one other resolution).  

---

## 14. Resolved decisions

| # | Decision | Resolution |
|---|----------|------------|
| **Q1** | Reserve top HUD height in `PlayfieldLayout` for camera offset? | **Yes (locked)** — §6.3 |
| **Q2** | Show HP pips on portraits? | **No (v0)** — remains out of scope |
| **Q3** | Click portrait to select? | **Yes (locked, v0)** — §7.5 |
| **Q4** | Main character crown on portrait? | **Yes (locked, v0)** — §7.1 **P5** |

---

## 15. Document history

| Version | Date | Notes |
|---------|------|-------|
| Draft | 2026-06-07 | Initial requirements from party-control UX request; promotes hotbar §5.3 portrait strip to first-class HUD. |
| Draft | 2026-06-07 | Locked: top HUD camera offset, main-character crown, click-to-select. |
| Implemented (v0) | 2026-06-07 | `PartyControlHudUI`, `PartyMemberMapHighlight`, `PlayfieldLayout` top rail, `TurnManager.HasActedThisTurn`, `PartyManager.ActiveMemberChanged`. |
