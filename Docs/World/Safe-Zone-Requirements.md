# Safe zone — Requirements

**Purpose:** Define **gameplay safe zones** where the party cannot initiate **hostile or consumptive combat actions**, NPCs cannot be harmed, and hub interactions (talk, shop, levers, portals) remain friction-free. **Town plaza (v0)** is safe; **future town districts** and **dungeon rooms** (shrines, merchant grottos) may opt in or out via data.

**Status:** v0 implemented — `FloorCombatPolicy`, `SafeZonePolicyService`, inventory/combat/essence gates, NPC damage suppression.

**Depends on:** `DungeonFloorDefinition`, `DungeonFloorInstanceManager`, `PlayerController`, `PlayerCommandProcessor`, `InputHandler`, `EssenceSlotManager`, `EquipmentManager`, `InventoryItemUse`, `InventoryUI`, `BowRangedCombatService`, `AbilityAction`, `NpcController`, `DoorKeyItemData`, `DoorService`, `CombatThreatCoordinator`, [NPC dialog](NPC-Dialog-Requirements.md), [Shop NPCs](Shop-NPC-Requirements.md), [Town time & calendar](Town-Time-And-Calendar-Requirements.md), [Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md) (floor instances), [Doors](Door-Requirements.md), [Bow & arrow](../Combat/Bow-And-Arrow-Requirements.md), [Throwing knife](../Inventory/Throwing-Knife-Requirements.md), [Rest](../Progression/Rest-Requirements.md).

**Related:** [Interactable tiles](../Combat/Interactable-Tiles-Requirements.md) (town levers). [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) (Equip vs Use actions).

**Explicitly out of scope (v0):** PvP / friendly-fire policy outside safe zones; arena floors; guard NPCs that arrest the player for hostile attempts; cinematic “crime” systems; save/load of per-cell zone state; audio stinger on deny; partial-quantity item use in safe zones; **spawn safe zone** behavior changes (see §3).

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Town plaza (v0)** is a **gameplay safe zone**: no combat, no essence abilities, no **combat** inventory **Use**; NPCs cannot take damage. **Utility** item Use (e.g. door keys) remains allowed. |
| **G2** | **Bump into NPC** never starts combat or deals damage (movement block + talk path unchanged). |
| **G3** | **Configurable** per floor (v0) and per **region** (v1+) so parts of town can later be **non-safe** (e.g. back alleys, arena pit). |
| **G4** | **Single gate service** — all action pipelines query one API; no scattered `floorId == "town_main"` checks. |
| **G5** | **Clear player feedback** when an action is denied (log + optional toast; no silent failure). |
| **G6** | **Defense in depth** — even if a gate is missed, NPCs / non-hostile actors in safe zones do not lose HP from player actions. |
| **G7** | **Hub affordances preserved** — inventory **Equip/Unequip**, dialog, shop, levers, portals, formation movement, rest (when otherwise legal). |

---

## 2. Glossary — two different “safe zones”

| Term | Scope | Purpose | Config |
|------|--------|---------|--------|
| **Spawn safe zone** | Generation only | Chebyshev disk around player spawn / formation where **enemies, hazards, traps** are not placed ([Dynamic dungeon floors §R7.1](Dynamic-Dungeon-Floor-Generation-Requirements.md)). | `DungeonFloorDefinition.playerSafeRadius` |
| **Gameplay safe zone** | **Runtime** | Player **cannot perform hostile/consumptive actions**; **protected actors** ignore damage. | **`FloorCombatPolicy`** + optional **`SafeZoneRegion`** (§5) |

**Locked:** Do **not** overload `playerSafeRadius` for gameplay policy. Rename in UI/tooltips if confusing (“Population exclusion radius” vs “Gameplay safe zone”).

| Term | Meaning |
|------|--------|
| **Safe zone (gameplay)** | Cell or floor where `SafeZonePolicyService` reports hostile actions blocked. |
| **Normal zone** | Default dungeon behavior — combat and item use allowed per existing rules. |
| **Protected actor** | Actor that cannot be reduced below max HP by player-originated hostile actions while target cell is in a safe zone (v0: **`NpcController`**; extend as needed). |
| **Hostile action** | Any player-initiated action whose primary purpose is dealing damage, applying negative status, or consuming a combat item (§6). |
| **Combat item use** | Inventory **Use** on items blocked in safe zones (potions, scrolls, evocables, bow-ammo invoke, throwing knives, generic `activeAbilities`, etc.) — §6.3. |
| **Utility item use** | Inventory **Use** explicitly allowed in safe zones (door keys; future authored utility items) — §6.3. |
| **Safe zone region** | Optional rect or marker-defined sub-area on a floor with its own policy (v1+). |

---

## 3. Reference — how other games handle safe zones

Useful patterns when locking JRogue rules:

| Game / genre | Safe area behavior | Takeaway for JRogue |
|--------------|-------------------|---------------------|
| **MMO capitals** (*WoW*, *FFXIV*) | PvP off; cannot attack NPCs; guards on flagged PvP | **Hard block** + feedback; optional future “guard” flavor text |
| **CRPG hubs** (*Persona*, *SMT*, *Dragon Quest* towns) | No random encounters; no offensive skills in hub | Aligns with **block essence + attacks** in town |
| **Roguelikes** (*DCSS* branches) | Town has no monsters; temple shops; some branches still allow self-target mistakes | Separate **“no enemies”** from **“no hostile player actions”** — JRogue needs explicit policy because **NPCs are `IBattleTarget`** |
| **Action RPG bonfires** (*Dark Souls*) | Enemies don’t aggro; player can still swing | **Not** our model — player attacks must be blocked, not just enemy AI |
| **Sandbox** (*Minecraft* peaceful / spawn protection) | Dimension / chunk flags | Inspires **floor + region** layering (§5) |

**Recommendation (locked for v0):** JRogue safe zones follow **hub RPG** norms (Persona / CRPG town): **no player-initiated hostility**, not merely “no enemy spawns.” Empty enemy tables on `town_main` are **necessary but not sufficient**.

---

## 4. Current baseline (as-is)

| Area | Today | Gap |
|------|-------|-----|
| **Melee bump vs NPC** | `PlayerController.OnBump` only attacks `EnemyController` — NPC bump logs block, **no damage** | ✓ Already safe |
| **Melee bump vs enemy** | N/A on town (no enemies) | — |
| **Bow shot / aim** | `BowRangedCombatService.TryExecuteBowShot` damages any `IBattleTarget` on tile | **Can hit NPCs** |
| **Throwing knife / scroll / evocable Use** | `InventoryItemUse` → ability execute | **Can hit NPCs** |
| **Essence hotkeys (1–9)** | `EssenceSlotManager.TryExecuteAbility` — no zone gate | **Fireball etc. can hit NPCs** |
| **Equipment abilities (Ctrl+slot)** | `EquipmentManager.TryExecuteItemAbility` | Same gap |
| **Inventory Use in town** | `InCombatContext` false (no `CombatThreatCoordinator` in town scene) | **More permissive** than combat, not less |
| **Equip / Unequip** | Works | Should **remain** allowed |
| **Talk / shop / levers** | No combat hooks | ✓ OK |
| **NPC death** | `NpcController.Die` logs warning; HP can still reach 0 | Needs **invulnerability** in safe zones |

---

## 5. Configuration model

### 5.1 — Design decision — floor default + optional regions

#### Question

How should safe zones be authored for **whole town** now and **partial town** later?

#### Recommendation (locked)

| Layer | v0 | v1+ |
|-------|----|-----|
| **Floor default** | `FloorCombatPolicy` on `DungeonFloorDefinition` | Same |
| **Sub-regions** | Not required for plaza | **`SafeZoneRegion`** entries on layout stamp or floor definition |

| Approach | Verdict |
|----------|---------|
| **Floor policy + stamp regions (chosen)** | Matches existing floor/stamp pipeline; plaza = one policy; alley = region override |
| **Hard-code `town_main`** | Rejected — fragile |
| **Per-tile combat overlay tilemap** | Deferred — powerful but heavy for v0 whole-floor town |
| **3D trigger volumes** | Rejected — grid game; query by cell |

### 5.2 — Floor combat policy (v0)

Add to **`DungeonFloorDefinition`**:

```csharp
public enum FloorCombatPolicy
{
    Normal = 0,     // default — dungeon floors
    SafeZone = 1,   // gameplay safe zone for entire floor instance
}

[SerializeField] FloorCombatPolicy combatPolicy = FloorCombatPolicy.Normal;
public FloorCombatPolicy CombatPolicy => combatPolicy;
```

**Authoring (v0):**

| Asset | `combatPolicy` |
|-------|----------------|
| `Assets/Resources/Town/Floor_town_main.asset` | **`SafeZone`** |
| All dungeon floor defs | **`Normal`** (default) |

### 5.3 — Safe zone regions (v1+ — future town districts)

When part of a floor is **not** safe (e.g. `(14–18, 2–6)` back alley):

```csharp
[Serializable]
public struct SafeZoneRegion
{
    public string regionId;
    public Vector2Int minInclusive;
    public Vector2Int maxInclusive;
    public FloorCombatPolicy policy; // SafeZone or Normal override
}
```

| Rule | Detail |
|------|--------|
| **Source** | `DungeonLayoutStamp.safeZoneRegions[]` or floor definition override list |
| **Query** | `SafeZonePolicyService.GetPolicyAt(Vector3Int cell)` — **most specific wins**: region override > floor default |
| **Overlap** | If regions overlap, **smallest area** wins; tie-break **Normal over SafeZone** (safer for player clarity) |
| **Markers** | Optional stamp marker `unsafe_zone_anchor` for tooling only |

**Example (future):** Plaza floor `combatPolicy = SafeZone`; one region `policy = Normal` for arena pit — only that rect allows combat.

### 5.4 — Central service

**`SafeZonePolicyService`** (DDOL or scene singleton on run layer):

| API | Behavior |
|-----|----------|
| `FloorCombatPolicy GetPolicyAt(Vector3Int worldCell)` | Resolve floor instance + region |
| `bool IsSafeZoneAt(Vector3Int cell)` | `GetPolicyAt == SafeZone` |
| `bool IsSafeZoneForActiveParty()` | Policy at **active leader** grid cell |
| `bool TryAllowEssenceAbility(out string denyReason)` | **Always false** in safe zone — all essences, including buffs and utility (Telekinesis) |
| `bool TryAllowInventoryUse(ItemData item, out string denyReason)` | False for **combat** items; true for **utility** items (§6.3) |
| `bool TryAllowHostileAction(HostileActionKind kind, GameObject user, out string denyReason)` | Returns false in safe zone |
| `bool IsProtectedTarget(IBattleTarget target)` | True when target actor’s cell is safe **and** target is protected class |

**Active floor:** Read from `DungeonFloorInstanceManager.CurrentInstance.Definition` + leader position for region lookup.

---

## 6. Action matrix — safe zone (locked)

Legend: **Allow** | **Deny** | **Allow*** (utility exception)

### 6.1 — Movement & hub

| Action | Safe zone | Notes |
|--------|-----------|-------|
| Walk / formation move | **Allow** | Including bump into NPC (block movement, no damage) |
| **Enter** talk to NPC | **Allow** | [NPC dialog §3](NPC-Dialog-Requirements.md) |
| Shop buy/sell UI | **Allow** | [Shop NPCs](Shop-NPC-Requirements.md) |
| Town time levers | **Allow** | Bump activates lever |
| Town → dungeon portal | **Allow** | Subject to portal time gate |
| Map interact (**E**) | **Allow** | Altars etc. if present |
| Rest (**r**) | **Allow** | If [Rest](Rest-Requirements.md) start gates pass (no combat tension) |

### 6.2 — Combat & hostility

| Action | Safe zone | Notes |
|--------|-----------|-------|
| Melee bump attack | **Deny** | Already no-op vs NPC; gate should short-circuit vs anything |
| Unarmed bump attack | **Deny** | |
| Bow bump shot | **Deny** | |
| Bow aim mode (inventory / hotkey) | **Deny** | Block at **enter aim**, not only on fire |
| Throwing knife Use | **Deny** | Includes targeted throw confirm |
| Essence activated ability (keys **1–9**, Shift/Ctrl variants) | **Deny** | **All** essences — **locked:** includes buffs (Sudden Strength), utility (Telekinesis), and offensive spells. **No** safe-zone exception by `hostileAction` metadata. |
| Equipment activated ability (Ctrl + slot) | **Deny** | |
| Mage class spell hotkeys | **Deny** | Same pipeline as abilities |
| Targeting mode confirm on hostile tile | **Deny** | If player somehow entered targeting, confirm fails |

**Locked (essences):** Safe zones **never** allow essence hotkey activation — hostile or not. Hub areas stay free of essence VFX, targeting, and turn spend from essences.

### 6.3 — Inventory

| Action | Safe zone | Notes |
|--------|-----------|-------|
| Open inventory | **Allow** | |
| **Equip** / **Unequip** | **Allow** | Explicit user requirement |
| **Use — combat items** | **Deny** | Potions, scrolls, evocables, bow-ammo invoke, throwing knives, items with offensive `activeAbilities` |
| **Use — utility items** | **Allow** | Door keys (`DoorKeyItemData`); future items with `allowUseInSafeZone` (§6.3.1) |
| Drop item | **Allow** | Not a hostile action; subject to future story-tagged confirm |
| Sort / mark / inscribe | **Allow** | Non-combat inventory management |
| Subspace open / store / take | **Allow** | Encumbrance management only |

#### 6.3.1 — Utility vs combat item classification (locked)

| Rule | Detail |
|------|--------|
| **R6.3.1** | **`SafeZonePolicyService.IsUtilityInventoryUse(ItemData)`** returns true when Use is allowed in a safe zone. |
| **R6.3.2** | **Always utility:** `DoorKeyItemData` (unlocks adjacent door via `DoorService`; no combat). |
| **R6.3.3** | **Future utility:** optional bool on `ItemData`: **`allowUseInSafeZone`** — author for quest keys, story tools, non-combat interactables. Default **false**. |
| **R6.3.4** | **Always combat (deny):** evocables, healing potions, scrolls, bow ammo invoke path, throwing knives, any item whose Use runs an `AbilityAction` that is not utility unless `allowUseInSafeZone` is set. |
| **R6.3.5** | When denied: same feedback as other safe-zone denies (§8). When allowed: existing turn / adjacency rules for that item type unchanged. |
| **R6.3.6** | **`InventoryUsability.AppearsUsableNow`** and **`InventoryUI`** Use button: reflect utility vs combat per row when in safe zone (utility stays enabled). |

**Examples:**

| Item | Safe zone Use |
|------|----------------|
| `DoorKeyItemData` | **Allow** (if adjacent matching locked door) |
| Healing potion | **Deny** |
| Fireball scroll | **Deny** |
| Throwing knife | **Deny** |
| Evocable (fan) | **Deny** |
| Bow ammo (invoke aim) | **Deny** |
| Future quest “Town Hall pass” with `allowUseInSafeZone` | **Allow** |

### 6.4 — Noise & AI

| Action | Safe zone | Notes |
|--------|-----------|-------|
| Ability noise emission | **Deny** | Blocked with ability — no noise from denied casts |
| Enemy AI attack | N/A in town v0 | In mixed zones, enemies outside safe cells behave normally |

---

## 7. Protected actors & damage (defense in depth)

| Rule | Detail |
|------|--------|
| **R7.1** | While **`IsProtectedTarget(target)`**, player-originated hostile actions **cannot reduce** target HP below current max (implement as **damage immunity** or early **TakeDamage** reject). |
| **R7.2** | v0 protected classes: **`NpcController`** (all town NPCs including shopkeepers). |
| **R7.3** | Party members are never valid hostile targets in safe zones (existing ally filters + policy). |
| **R7.4** | Log **`[SafeZone]`** when damage is suppressed (debug verbosity). |
| **R7.5** | Deny at **action gate** first; invulnerability is **backup** if a new ability forgets to call the gate. |

---

## 8. Player feedback

| Rule | Detail |
|------|--------|
| **R8.1** | On deny: `Debug.Log` / player-visible message: **`"[SafeZone] You can't do that here."`** |
| **R8.2** | Optional UI toast (same string) — follow inventory/combat toast pattern if present |
| **R8.3** | Denied actions **do not consume a turn** (align with failed ability / illegal equip) |
| **R8.4** | Inventory **Use** button: **disabled** with tooltip **"Can't use that in a safe area."** for **combat** items; **enabled** for **utility** items when other Use gates pass |
| **R8.5** | Essence hotkeys in safe zone: no targeting mode entry; optional HUD hint when in town (deferred) |

---

## 9. Integration hooks (implementation map)

Central calls: **`SafeZonePolicyService`** — **`TryAllowEssenceAbility`** (always deny in safe zone), **`TryAllowInventoryUse(item)`** (utility vs combat), **`TryAllowHostileAction`** (attacks / equipment abilities).

| # | Hook location | Blocks |
|---|---------------|--------|
| 1 | `PlayerController.OnBump` | Bump attacks (melee, bow, unarmed) |
| 2 | `PlayerCommandProcessor.ProcessAbilityInput` | Essence / equipment / mage abilities |
| 3 | `PlayerCommandProcessor.ApplyConfirmTarget` | Targeted ability confirm |
| 4 | `PlayerCommandProcessor` bow aim entry | Enter aim mode |
| 5 | `BowRangedCombatService.TryExecuteBowShot` | Ranged damage |
| 6 | `InventoryItemUse.TryUseCarriedItem` | **Combat** inventory Use only; **pass through** utility items |
| 7 | `InventoryUI` / `InventoryUsability` | Disable Use for combat items; keep utility enabled |
| 8 | `AbilityAction.Execute` (optional central wrapper) | Last-resort hostile execute |
| 9 | `BaseActor.TakeDamage` or `HealthComponent` | Protected target immunity |

**Do not use** `CombatThreatCoordinator.IsInCombat` as the safe-zone signal — town has no coordinator; safe zone is **orthogonal** to combat tension.

---

## 10. Town (v0) authoring checklist

| Item | Value |
|------|--------|
| Floor | `Floor_town_main.asset` → `combatPolicy = SafeZone` |
| Scene | `TownTest.unity` / production Town — ensure `SafeZonePolicyService` bootstrap |
| NPCs | Default protected via `NpcController` |
| QA | Verify Sudden Strength / Telekinesis essence hotkeys denied in plaza |
| QA | Verify door key **Use** works in safe dungeon room (when adjacent locked door) |
| QA | Verify Equip Giant's Blade, shop transaction, lever advance still work |

---

## 11. Relationship to other systems

| System | Interaction |
|--------|-------------|
| **Combat tension / Rest** | Safe zone ≠ in combat. Rest may still start in town if other gates pass. |
| **Shop** | Shop UI is not inventory **Use** — allowed. |
| **Dungeon return** | Arriving in town places party in **safe** plaza cells — policy follows floor + cell. |
| **Future unsafe town district** | Same floor asset, add **`SafeZoneRegion` Normal** rect; content places enemies or duel NPC there. |
| **Spawn safe zone** | Unchanged — only affects initial population. |

---

## 12. Open questions (post-v0)

| # | Question | Status |
|---|----------|--------|
| Q1 | Allow **non-hostile** essences (buffs) in safe zones? | **Locked: No** — all essence hotkeys denied (§6.2) |
| Q2 | Allow **door key** / utility **Use** in safe zones? | **Locked: Yes** — utility classification (§6.3.1) |
| Q3 | **Drop** explosive / hazardous items in safe zone? | Allow drop; ground hazard if any is separate system |
| Q4 | Show **safe zone icon** on HUD when in town? | Optional polish |
| Q5 | **Formation rush** into NPC | Allow as movement; no damage (already true) |

---

## 13. Acceptance criteria

| ID | Test |
|----|------|
| **AC1** | On `town_main`, bump adjacent NPC — no damage, talk still works on **Enter**. |
| **AC2** | Essence hotkey (offensive **or** buff e.g. Sudden Strength) — **denied**, no turn spent |
| **AC3** | Inventory **Use** on potion / scroll / knife — **denied**; **Use** on adjacent **door key** — **works**; **Equip** — **works** |
| **AC4** | Bow aim cannot be entered (or shot denied) in plaza. |
| **AC5** | Shop purchase and town lever phase advance succeed in safe zone. |
| **AC6** | Dungeon floor with `Normal` policy — all above actions behave as today. |
| **AC7** | (v1+) Cell inside `Normal` region on otherwise safe floor — hostile action **allowed** there only. |

---

## 14. Implementation checklist

- [x] Add `FloorCombatPolicy` + field to `DungeonFloorDefinition`
- [x] Set `Floor_town_main.asset` → `SafeZone`
- [x] Implement `SafeZonePolicyService` (floor + cell query; `IsUtilityInventoryUse`, `TryAllowEssenceAbility`, `TryAllowInventoryUse`)
- [x] Add optional `allowUseInSafeZone` on `ItemData` (default false); `DoorKeyItemData` implicit utility
- [x] Wire hooks §9 (1–7 minimum for v0)
- [x] Protected target damage immunity for `NpcController`
- [x] Inventory UI / usability: disable **Use** for combat items only in safe zone
- [x] Unit tests: deny all essences; deny combat Use; allow door key Use; allow equip
- [ ] Cross-link from [Town time](Town-Time-And-Calendar-Requirements.md) and [Shop NPCs](Shop-NPC-Requirements.md)
- [ ] Disambiguate “spawn safe zone” in [Dynamic dungeon floors §R7.1](Dynamic-Dungeon-Floor-Generation-Requirements.md) glossary footnote
- [ ] (v1+) `SafeZoneRegion` stamp authoring + overlap rules
