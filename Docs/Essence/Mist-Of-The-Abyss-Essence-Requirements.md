# Mist of the Abyss — Requirements (draft)

**Status:** **Implemented (v0)** — reusable Mist of the Abyss presence effect, essence-active suppress, greyed hotbar, HUD badge + vignette. Content assets created via LotF pack menu.

**Purpose:** Specify **Mist of the Abyss**, a **reusable**, **passive**, **floor-wide** suppress that **disables all active essence effects** for the player party while they remain on a host floor and a living host that projects the mist is present. The effect may be authored as an internal ability / presence asset for naming and tests, but it is **never** a player-droppable or capturable essence.

**Depends on:** [Lord of the Floor](../World/Lord-Of-The-Floor-Requirements.md), [Monster map presence](../World/Monster-Map-Presence-Requirements.md), `EssenceSlotManager` / `EssenceData` / `AbilityAction` (essence **active** execute path), [Sudden Strength](Sudden-Strength-Essence-Requirements.md) / [Telekinesis](Telekinesis-Essence-Requirements.md) (representative essence actives), [Light-emitting items](../World/Light-Emitting-Items-Requirements.md) (Helmet of Light — **item** active, must remain usable), [Human Mage spells](../RacialSystem/Human-Mage-Spells-And-Spellbooks-Requirements.md), [Human Priest divine covenant](../RacialSystem/Human-Priest-Divine-Covenant-Requirements.md) (divine abilities remain usable), [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) (greyed disabled state).

**Related:** Ability hotbar — essence actives should appear unusable / fail cleanly under mist; item actives and class powers remain assignable and executable.

**Explicitly out of scope (v0):** Player capturing, looting, or equipping Mist of the Abyss; particle systems / full-screen color grading / world mist tiles beyond the locked HUD + vignette package; mist affecting NPC enemies’ hypothetical essence loadouts; partial-floor radius modes; stacking multiple simultaneous mist sources on one floor; save/load across sessions; shipping additional LotF hosts beyond Giant Skeleton King (framework must still be reusable).

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Suppress essence actives only** — While mist applies to an actor, that actor **cannot successfully execute** any **essence slot active ability**. |
| **G2** | **Preserve essence passives & stats** — Equipped essence **stat modifiers**, **resistance modifiers**, and **complex passives** (`OnTurnStart` / `Refresh`, etc.) **continue** to function. |
| **G3** | **Preserve non-essence actives** — **Item** actives (e.g. Helmet of Light), **mage magic**, and **priest divine abilities** remain usable. |
| **G4** | **Floor-wide host scope** — While a host projects mist, it covers that host’s **entire configured floor** — not a radius around the boss. |
| **G5** | **Clear escape / end conditions** — Mist (suppression + visuals) stops for the party when they leave the host floor, when the **dungeon ends**, or when the **host dies / despawns**. |
| **G6** | **Data-first & reusable** — One authored Mist of the Abyss presence/effect asset can be attached to **any** future LotF (or other map-presence host) by configuration; v0 assigns it to Giant Skeleton King only. |
| **G7** | **Fail closed & inform** — Blocked essence activations do **not** spend Soul Power or the player action; combat log / UX explain the mist. |
| **G8** | **Persistent visual package** — While mist applies to the party on the host floor, show **HUD status badge + screen-edge vignette**; clear and re-show at the same boundaries as suppression. |

---

## 2. Glossary

| Term | Meaning |
|------|---------|
| **Mist of the Abyss** | The named reusable ability / effect. |
| **Host** | The living enemy that projects mist via map presence (v0: Giant Skeleton King). |
| **Host floor** | Floor id configured on the mist effect / LotF (v0: Floor 1 / `dungeon_floor_01`). |
| **Mist active (on floor)** | A living host is bound and mist presence is registered for the host floor. |
| **Mist applies to actor** | Actor is a **player party member** currently on the **host floor** while mist is active. |
| **Essence active** | An `AbilityAction` referenced from an equipped `EssenceData.activeAbilities` list and invoked via `EssenceSlotManager.TryExecuteAbility` (hotbar, overflow, etc.). |
| **Essence passive / stat package** | `statModifiers`, `resistanceModifiers`, `complexPassives`, body contributions — **not** suppressed. |
| **Item active** | Abilities granted by equipped **items** (Helmet of Light timed light, etc.) — **not** essence actives. |
| **Class / racial power** | Mage spells, Priest divine abilities, Knight auras, folk racial actives — **not** essence actives (unless a design later routes them through essence slots — they must still be excluded from this suppress). |

---

## 3. Host binding (locked)

Mist of the Abyss is a **reusable** map-presence effect. Any future LotF may reference the same effect asset and supply its own **host floor id**.

### 3.1 — First consumer — Giant Skeleton King

| Property | Value |
|----------|--------|
| **Host LotF** | Giant Skeleton King (`lotf_giant_skeleton_king`) |
| **Delivery** | Prefer [monster map presence](../World/Monster-Map-Presence-Requirements.md): apply on bind / spawn; revert on death / unbind |
| **Host floor** | Production **Floor 1** (`dungeon_floor_01`) — entire floor instance |
| **Who is affected** | **Player party members** on the host floor |
| **Who is not affected** | Party members on **other floors**; non-party actors (unless a future rule says otherwise) |

**Re-entry:** If the party leaves the host floor and later returns while the host is **still alive**, mist **applies again** immediately (presence remains on the floor while the host lives; actors re-enter the affected set). Suppression, combat-log re-entry line, and visuals all reapply together.

### 3.2 — Reuse contract (locked)

| Rule | Detail |
|------|--------|
| **One effect, many hosts** | Implement Mist as a configurable `MonsterMapPresenceEffect` (or equivalent), **not** hard-coded to `giant_skeleton_king` |
| **Host-floor field** | Effect authors a **host floor id** (or inherits it from the LotF definition) so a future LotF on Floor 2+ can reuse the same ability |
| **v0 assignment** | Only Giant Skeleton King ships with Mist attached |
| **Future LotFs** | Attach the same Mist asset / effect type; do not fork a King-only copy unless behavior truly diverges |

### 3.3 — Combat-log notifications (locked)

| Event | Required player-facing combat log |
|-------|-----------------------------------|
| Mist first applies when the host appears (and party is on the host floor) | *Mist of the Abyss settles over Floor 1. Essence powers are suppressed.* |
| Party enters / re-enters the host floor while the host remains alive | *Mist of the Abyss still blankets Floor 1. Essence powers are suppressed.* |
| Party changes location within the host floor | **No repeat message** |
| Host dies / despawns and mist reverts | Recommended: *Mist of the Abyss has lifted. Essence powers can be used again.* |

Exact prose may be tuned (including floor naming for future hosts). First-apply and re-entry messages must identify **Mist of the Abyss** and explain that **essence powers / actives are suppressed**. Emit each message once per applicable floor-entry event, not once per party member.

---

## 4. Effect rules (locked)

### 4.1 — What is disabled

| Action | Under mist (actor on host floor) |
|--------|----------------------------------|
| Execute essence **active** (Sudden Strength, Telekinesis, Dash, Adrenaline Rush, Poison Weapon, design stubs, etc.) | **Blocked** |
| Essence-active entry on main hotbar or overflow | **Greyed out** and non-activatable |
| Hotbar / UI confirm that would call `EssenceSlotManager.TryExecuteAbility` | **Blocked** before spend |
| Enter targeting reticle **solely** for an essence active | **Must not enter**; gate before targeting |

### 4.2 — What still works

| Action | Under mist |
|--------|------------|
| Essence **stat** / **resistance** modifiers while equipped | **Works** |
| Essence **complex passives** (turn-start hooks, etc.) | **Works** |
| **Helmet of Light** (and other **item** actives) | **Works** |
| Handheld Torch / passive item lights | **Works** |
| **Mage** spell casting | **Works** |
| **Priest** divine abilities / covenants (as implemented) | **Works** |
| Other class / racial actives not routed as essence actives | **Works** |
| Basic attack, move, inventory, equipment | **Works** |
| Unequip / equip essences | **Works** (passives/stats update normally; actives remain unusable until mist ends) |
| Essence-derived buff activated **before** mist applied | **Continues normally** and expires under its existing duration rules |

**Locked:** Mist blocks **new essence-active activations only**. It does not dispel, pause, shorten, or otherwise modify an effect that was already running when the mist began or when the party entered the host floor.

### 4.3 — Spend / turn semantics on block

When an essence active is attempted while mist applies:

1. **Do not** deduct Soul Power (or Magic Power if somehow mis-routed).  
2. **Do not** consume the player’s action / end the turn.  
3. **Do** log with a clear prefix (recommend **`[MistOfTheAbyss]`** or reuse **`[LotF]`** with ability tag).  
4. **Do** show a short player-facing reason when UX allows (e.g. *Mist of the Abyss suppresses essence powers.*).

The normal hotbar path should prevent attempts by marking essence actives disabled through `HotbarUsabilityService`. The execute-time suppress check remains mandatory as a safety gate for non-hotbar callers and stale UI state.

### 4.4 — End / escape conditions

Mist **stops applying** to an actor (suppression **and** visuals) when **any** of:

| Condition | Result |
|-----------|--------|
| Party exits the **host floor** (e.g. Floor 1 → Floor 2 for v0) | No mist on other floors; visuals clear |
| **Dungeon run ends** | Presence torn down with run; visuals clear |
| **Host dies** | Map presence **reverts**; essence actives work again on that floor; visuals clear |
| **Host despawns** (LotF consumed) | Same as death for mist revert |

**Reapply:** If the party **re-enters the host floor** while the host is **still alive**, mist suppression, re-entry combat log, HUD badge, and vignette **all reapply**.

**Non-condition:** Simply moving to another region **on the host floor** does **not** escape the mist — scope is the **whole floor**.

---

## 5. Why this is a unique essence ability

Mist of the Abyss is unusual compared to player essences:

| Typical essence active | Mist of the Abyss |
|------------------------|-------------------|
| Player spends SP to activate | **Passive** while host lives |
| Affects caster or targeted tile | Affects **entire host floor** + **party** |
| Equipped in a slot to use | Hosted by **LotF / map presence** only |

**Locked:** Mist of the Abyss is **not droppable** and **not capturable**. Do **not** add it to any `EnemyLootTable`. Do **not** expose it as a player-equippable `EssenceData` in shops, floor piles, or capture flows.

Optional internal authoring asset (name / description / icon for designers and tests) is allowed **only** if it cannot enter player inventory or essence slots.

---

## 6. Content authoring (target)

### 6.1 — Display

| Field | Value |
|-------|--------|
| **Name** | Mist of the Abyss |
| **Description (proposed)** | A floor-choking abyss mist that smothers essence evocations. Essence actives cannot be used on this floor while the Lord lives. Item powers, magic, divine rites, and essence passives still function. |

### 6.2 — Suggested assets

| Asset | Path (suggested) | Notes |
|-------|------------------|-------|
| `Essence_MistOfTheAbyss` (optional) | Internal-only metadata if useful | **Must not** be lootable / equippable by players |
| `MistOfTheAbyssFloorSuppressEffect` | `Assets/Data/Enemy/MapPresence/` | Reusable `MonsterMapPresenceEffect`; host-floor configurable |
| Profile on King species / prefab | King `mapPresenceProfile` | v0 first assignment of the reusable effect |
| Future LotF profile | That LotF’s `mapPresenceProfile` | Same effect asset / type; different host floor as needed |
| Mist HUD badge UI | Gameplay canvas status area | §7 |
| Mist edge-vignette UI | Gameplay canvas above playfield | §7 |

### 6.3 — Implementation approach (engineering guidance)

**Preferred gate point:** a single check in `EssenceSlotManager.TryExecuteInternal` (and any parallel “can afford / can open targeting” helpers) consulting a small **`EssenceActiveSuppressService`** (or map-presence global flag scoped by floor id):

```text
if (EssenceActiveSuppressService.IsSuppressed(actor)) → fail closed
```

**Apply/Revert:** Host presence effect registers suppress for its **configured host floor** while alive; revert clears it. Actors query “am I on a suppressed floor?” rather than per-actor buffs when possible (keeps leave/re-enter correct).

**Do not** hard-code species id `giant_skeleton_king` inside the suppress service — bind through the presence effect / floor registration so future LotFs can reuse it.

**Do not** strip equipped essences or remove modifiers — suppression is **execute-time** for actives only.

---

## 7. Visual cue (locked) — HUD badge + edge vignette

**v0 ships package A + B:**

| Piece | Requirement |
|-------|-------------|
| **HUD status badge** | Persistent icon + label **Mist of the Abyss** near other gameplay status UI; tooltip: essence actives are suppressed on this floor |
| **Screen-edge vignette** | Low-opacity dark violet-red mist around screen edges; transparent center; atmospheric companion to the badge |

### 7.1 — Vignette constraints

- Leave the center of the playfield mostly clear.
- Ignore raycasts and never block input.
- Render above the world / playfield and below the hotbar, combat log, menus, and modal UI.
- Avoid changing actual illumination, visibility calculations, fog of war, or Helmet of Light behavior.
- If an accessibility / VFX setting later disables the vignette, the **HUD badge remains**.

### 7.2 — Show / hide / reapply (locked)

| Event | Visuals |
|-------|---------|
| Mist first applies while party is on the host floor (e.g. King summoned on Floor 1) | **Show** badge + vignette |
| Party **leaves** the host floor (e.g. descends to Floor 2) | **Hide** badge + vignette |
| Party **re-enters** the host floor while the host is still alive | **Show again** badge + vignette |
| **Host dies** (Giant Skeleton King slain) | **Hide** badge + vignette |
| **Host despawns** | **Hide** badge + vignette |
| **Dungeon ends** | **Hide** badge + vignette |
| Party moves within the host floor | **No change** (remain visible) |

Visuals, essence-active suppression, and combat-log apply/re-entry messaging share these boundaries.

---

## 8. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC1** | With King alive on Floor 1, party on Floor 1: essence active execution **fails**; no SP spent; turn not consumed. |
| **AC2** | Same setup: Sudden Strength buff / Telekinesis pickup **do not** occur. |
| **AC3** | Same setup: essence **passive stats** still apply (e.g. Iron Skin defenses if equipped). |
| **AC4** | Same setup: Helmet of Light active **succeeds**. |
| **AC5** | Same setup: Mage spell **succeeds**; Priest divine ability **succeeds**. |
| **AC6** | Party on Floor 2 while King still alive on Floor 1: essence actives **succeed** on Floor 2; badge + vignette **hidden**. |
| **AC7** | Return to Floor 1 with King alive: essence actives **blocked** again; badge + vignette **reshown**; re-entry combat log fires. |
| **AC8** | King dies: essence actives on Floor 1 **succeed** immediately after death cleanup; badge + vignette **hidden**. |
| **AC9** | Mist does not require the party to stand adjacent to the King — any Floor 1 cell is in scope. |
| **AC10** | King death / loot never yields Mist of the Abyss as an essence item. |
| **AC11** | Initial application emits one combat-log message naming the mist and stating that essence powers are suppressed. |
| **AC12** | Re-entering Floor 1 while the King lives emits one re-entry combat-log message; movement within Floor 1 does not spam it. |
| **AC13** | Every essence active is greyed and non-activatable on the main hotbar and overflow while mist applies; item, magic, and divine actives retain normal presentation. |
| **AC14** | An essence buff started before mist application continues and expires normally; no new essence buff can be activated under mist. |
| **AC15** | HUD badge + edge vignette appear/clear/reappear per §7.2 without changing actual floor illumination. |
| **AC16** | Mist suppress/visual service is not hard-coded solely to Giant Skeleton King; a second host can attach the same effect by data in a future milestone. |

---

## 9. Resolved decisions

| ID | Decision |
|----|----------|
| **R1** | Mist of the Abyss is **not** droppable or capturable — presence-only ability on the host. |
| **R2** | Combat log announces both initial application and host-floor entry / re-entry while mist remains active. |
| **R3** | Essence actives are **greyed out** and non-activatable on the hotbar and overflow. |
| **R4** | Previously activated essence buffs continue normally; mist blocks **new activations** only. |
| **R5** | Visual package is **HUD status badge + subtle screen-edge vignette**, with show/hide/reapply per §7.2. |
| **R6** | Mist is **reusable** for future LotFs; v0 ships it only on Giant Skeleton King. |

## 10. Open questions

| ID | Question | Default if unresolved |
|----|----------|------------------------|
| **Q1** | Exact vignette color / opacity values? | Dark violet-red, low opacity; tune in playtest |
| **Q2** | Exact HUD badge placement relative to existing status UI? | Near other persistent gameplay status chrome |

---

## 11. Revision history

| Date | Change |
|------|--------|
| 2026-07-24 | Initial draft — Mist of the Abyss for Giant Skeleton King; essence-active suppress; floor-wide scope; escape/end rules |
| 2026-07-24 | Lock: **not** droppable or capturable |
| 2026-07-24 | Lock initial/re-entry combat logs, greyed essence actives, and continuation of existing buffs; add visual cue options |
| 2026-07-24 | Lock HUD badge + vignette with show/hide/reapply; mark Mist **reusable** for future LotFs |
