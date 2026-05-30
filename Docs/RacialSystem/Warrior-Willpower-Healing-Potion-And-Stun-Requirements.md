# Warrior Willpower, Pain Tolerance, Healing Potion & Stun — Requirements

Barbarians gain the **non-physical** racial trait **Warrior Willpower**: a **gate flag** for checks and item rules (no passive or active ability). This doc also adds **Pain Tolerance** as a core stat, a **Healing Potion** consumable with **combat restriction** and **post-use Stun**, and the **Stunned** status (negative, blocks movement and actions, formation-aware).

**Depends on:** `CharacterStats`, `Stat` / `StatType`, `RacialLoadoutDefinition` / `RacialLoadoutApplier`, [Phase 0 glossary](Phase0-Glossary-And-Data-Contracts.md), [Status effects](../Combat/Status-Effects-Requirements.md) (`StatusPolarity`, `HasNegativeStatus`), [Inventory usability](../Inventory/Inventory-UI-Redesign-Requirements.md), `InventoryItemUse`, `InventoryUsability`, `InventoryConsumePolicy`, `CombatThreatCoordinator.IsInCombat`, `TurnManager` / `CanActorTakeAction`, `FormationRushService`, `PlayerCommandProcessor`, [Rest](../Progression/Rest-Requirements.md) (Stun blocks rest via negative status).

**Related:** [Phase 3 — Barbarian Spirit Imprint](Phase3-Requirements.md) (separate progression; Warrior Willpower is **folk baseline**, not an imprint node). `BodyCapabilityFlags` (physical / equip only). `HealAbility` (pattern for instant heal item).

**Explicitly out of scope (v0):** Warrior Willpower UI icon; multiple Barbarian “willpower” tiers; pain damage over time; healing potion variants (minor/major); ally-feeding potion; save migration for existing characters’ Pain Tolerance (default on new spawn / Awake).

---

## 1. Goals

**G1 — Non-physical racial gates**  
**Warrior Willpower** unlocks **conditional rules** (inventory, abilities, dialogue later) without being a passive or active ability.

**G2 — Separate from body capabilities**  
Physical anatomy (`BodyCapabilityFlags`) and **racial trait flags** stay **distinct** enums and code paths (§3).

**G3 — Pain Tolerance stat**  
New **`PainTolerance`** on `CharacterStats`; default **10** at creation.

**G4 — Healing Potion**  
Consumable potion: **+50 HP**, **unusable in combat** by default; **Stun** after use out of combat per Pain Tolerance (§7).

**G5 — Warrior Willpower exemption**  
Barbarian with **Warrior Willpower** and **Pain Tolerance ≥ 100** may use the Healing Potion **in or out of combat** with **no Stun** (§8).

**G6 — Stunned status**  
**Negative** status; blocks move/act; respects party formation on/off (§9).

**G7 — Active member spends turn**  
Using the potion from inventory consumes the **active party member’s** action on success. **Formation inactive:** `OnPlayerActionComplete(activeMember)`. **Formation active:** `PartyPlayerActionCompletion` → follower rush + end player phase (same as formation move/ability).

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Warrior Willpower** | `RacialTraitFlags.WarriorWillpower` — Barbarian folk trait for rule checks. |
| **Racial trait flag** | Non-physical capability bit on `CharacterStats` (§4). |
| **Body capability** | Physical/anatomy flag (`BodyCapabilityFlags`) for equipment legality. |
| **Pain Tolerance** | `StatType.PainTolerance` — higher = shorter Stun from Healing Potion (§7). |
| **Healing Potion** | `ItemCategory.Potion` item; +50 HP; special combat/stun rules. |
| **Stunned** | `StatusEffectId.Stunned` — cannot move or take actions for N player phases. |
| **In combat** | `CombatThreatCoordinator.IsInCombat`. |
| **Formation active** | `PartyManager.IsFormationActive`. |

---

## 3. Design decision — separate enum for non-physical traits (locked)

### Question

Should **Warrior Willpower** live on `BodyCapabilityFlags`?

### Answer: **No** — add **`RacialTraitFlags`**

| Concern | `BodyCapabilityFlags` | **`RacialTraitFlags` (recommended)** |
|---------|----------------------|--------------------------------------|
| **Purpose** | Anatomy / equipment (`equipRequires`, horns, stature) | Narrative & mechanical **gates** (potions, dialogue, puzzles) |
| **Consumers** | `EquipmentLegalityEvaluator`, essence body masks | `InventoryConsumePolicy`, abilities, future `ICondition` |
| **Barbarian example** | Horns, ReducedStature (if any) | **Warrior Willpower** |
| **Risk if merged** | Equipment rules misread “willpower” as physical; designer confusion | Clear `HasTrait(WarriorWillpower)` queries |

**Locked:** Introduce **`RacialTraitFlags`** (`[Flags]` enum, `ulong` or `uint`) in `JRogue.Stats.Racial`, parallel to `BodyCapabilityFlags`.

**Do not** rename `BodyCapabilityFlags` to a generic “CapabilityFlags” in v0 — keep equip pipeline stable.

### Optional alias in docs

“Non-physical capability flags” in design discussion maps to **`RacialTraitFlags`** in code.

---

## 4. `RacialTraitFlags` & Warrior Willpower (locked)

### 4.1 — Enum (v0)

```csharp
[Flags]
public enum RacialTraitFlags : uint
{
    None = 0,
    WarriorWillpower = 1 << 0,
    // Future: IronDiscipline, AncestorTouched, ...
}
```

### 4.2 — Storage on `CharacterStats`

```csharp
[Tooltip("Non-physical racial traits (folk baseline, blessings, quests).")]
public RacialTraitFlags racialTraits = RacialTraitFlags.None;
```

- Saved with identity / character (extend `RacialIdentitySnapshot` in a follow-up migration when saves matter).
- **Not** modified by equipment `BodyCapabilityFlags` fields on `ItemData`.

### 4.3 — Granting Warrior Willpower (Barbarian)

| Source | v0 |
|--------|-----|
| **`DefaultBarbarianRacialLoadout`** | Add `grantedRacialTraits: WarriorWillpower` on apply (preferred — data-driven). |
| **Prefab fallback** | `BarbarianPlayer` / `CharacterStats` intrinsic `racialTraits` includes bit if loadout missing. |

**Rules:**

- Only **`Race.Barbarian`** actors should receive the bit from folk loadout.
- **No** passive `PassiveEffect` and **no** `AbilityAction` for Warrior Willpower.
- Other races: bit unset unless future quest grants it explicitly.

### 4.4 — Query API (required)

```csharp
public static class RacialTraitQueries
{
    public static bool HasTrait(GameObject actor, RacialTraitFlags trait);
    public static bool HasTrait(CharacterStats stats, RacialTraitFlags trait);
}
```

**Gameplay and inventory must use `HasTrait(..., WarriorWillpower)`** — not `race == Barbarian` alone (allows future non-Barbarian grant).

### 4.5 — “Checks / if-conditions” (extensibility)

v0 implements **one** concrete rule (Healing Potion §8). Future systems reuse the same pattern:

```csharp
public static class HealingPotionRules
{
    public static bool CanUseWithoutCombatBan(BaseActor user);
    public static bool IsExemptFromPainStun(BaseActor user);
}
```

Internally:

```text
IsExemptFromPainStun(user) =
    HasTrait(user, WarriorWillpower)
    && painTolerance.GetValue() >= 100
```

Dialogue / interactables may later call `RacialTraitQueries.HasTrait` directly.

---

## 5. Pain Tolerance stat (locked)

### 5.1 — `StatType`

Add to `StatTypes.cs`:

```csharp
public enum StatType
{
    Strength, Dexterity, Agility, Constitution,
    Intelligence, Wisdom, Charisma, Luck,
    Sight, Hearing, Smell,
    PainTolerance  // new — after Smell or before Senses per style
}
```

### 5.2 — `CharacterStats`

```csharp
[Header("Pain")]
public Stat painTolerance = new Stat(10);
```

| Rule | Value |
|------|--------|
| **Default base** | **10** (via `new Stat(10)` and Awake refresh if needed) |
| **Modifiers** | Standard `Stat` modifier pipeline (items, buffs, racials later) |
| **`GetStatByType`** | Returns `painTolerance` for `StatType.PainTolerance` |

### 5.3 — Display

- Character sheet / debug: show **Pain Tolerance** as integer (`GetValue()`).
- Rare in combat UI v0 — no dedicated HUD required.

### 5.4 — Racial / loadout (v0)

**Not** granted by Warrior Willpower automatically. Designers may add `AttributeModifier` on `DefaultBarbarianRacialLoadout` later. v0: all actors **10** unless modified in inspector.

---

## 6. Stunned status (locked)

### 6.1 — Identity

| Field | Value |
|-------|--------|
| `StatusEffectId` | **`Stunned`** (new enum value) |
| `polarity` | **`Negative`** |
| **Blocks** | Movement, abilities, inventory use that spends a turn, party swap **to** stunned leader for movement purposes |

Add **`StatusEffectId.Stunned`** to `StatusEffectPolarityRules` defaults → **Negative**.

### 6.2 — Duration model

**Stunned** uses **turn counter** on the status instance (`turnsRemaining`), decremented on the bearer’s **player-phase** tick (same boundary as Poisoned / [Sudden Strength](../Essence/Sudden-Strength-Essence-Requirements.md) buff ticks — `NotifyPartyTurnStart` → `StatusEffectController.TickStatuses`).

**No damage** on tick.

### 6.3 — Behavior while Stunned

| System | Behavior |
|--------|----------|
| **`PlayerCommandProcessor`** | Reject **Move**, **Wait**, **Ability**, **inventory use** that requires action — **no turn consumed** on rejected attempt (§9.3). |
| **`CanActorTakeAction`** | **`false`** while Stunned (recommended central gate). |
| **Negative status queries** | `HasNegativeStatus()` **true** → blocks [Rest](../Progression/Rest-Requirements.md). |
| **Enemy phase** | Stunned party members are not player-controlled; no change. |

### 6.4 — `StatusStunnedDefinition` (suggested asset)

`Assets/Data/Status/Status_Stunned.asset` — may be plain `StatusEffectDefinition` with `statusId = Stunned`; optional subclass for tooling.

---

## 7. Healing Potion — item & heal (locked)

### 7.1 — Item identity

| Field | Value |
|-------|--------|
| **Name** | Healing Potion (display); asset e.g. `Potion_HealingPotion` |
| **Category** | `ItemCategory.Potion` |
| **Weight** | Per potion balance (suggest **0.5** until playtest) |
| **Stack** | Yes (`ItemInstance.quantity`) |
| **Icon** | Red liquid in **glass vial** (§7.5) |

### 7.2 — Effect

| Rule | Value |
|------|--------|
| **Heal amount** | **50 HP** (not percentage) |
| **Target** | **User** (carrier / executor) only — untargeted, like `HealAbility` |
| **Overheal** | Clamp to `MaxHP` |
| **Implementation** | Dedicated **`HealingPotionAbility`** : `AbilityAction` on `ItemData.activeAbilities[0]`, or subclass of `HealAbility` with `healAmount = 50` + hooks §7–8 |

### 7.3 — Consumption pipeline

Same path as other potions:

1. Inventory **Use** on carried row.
2. **`InventoryItemUse.TryUseCarriedItem`** — active member must `CanActorTakeAction`.
3. **`ability.Execute(user)`** on **row.Owner** (the character whose inventory holds the potion — typically active member).
4. On success: **`TryConsumeCarriedQuantity`**, then **`PartyPlayerActionCompletion.CompleteActiveMemberAction(activeMember)`** (formation → rush + end phase).

**Locked:** Turn spender = **`PartyManager.GetActiveMember()`** at confirm time (must match inventory UX for “who drank it”).

### 7.4 — Combat usability (default)

| Condition | Can use Healing Potion? |
|-----------|-------------------------|
| **Out of combat** | Yes (subject to Stun §8) |
| **In combat** | **No** (default) |
| **In combat + Warrior Willpower + Pain Tolerance ≥ 100** | **Yes**, no Stun (§8) |

**Inventory UI:** `AppearsUsableNow` false in combat unless exempt (§8).

**Failure message (combat, not exempt):** e.g. `Cannot drink this potion during combat.` — `Debug.Log` prefix `[HealingPotion]`.

### 7.5 — Art asset (authoring)

**Requirement:** Icon shows a **vial** filled with **red liquid**, readable at inventory scale.

**Suggested sources (verify license before import):**

| Source | Notes |
|--------|--------|
| [OpenGameArt — potion bottles](https://opengameart.org/content/potion-bottles) | Check per-file license (CC0 / CC-BY). |
| [Kenney — RPG expansion](https://kenney.nl/assets) | CC0; may need recolor to red. |
| [itch.io — potion icon packs](https://itch.io/game-assets/free/tag-potion) | Filter free + commercial use. |

**Project path (v0):** `Assets/Resources/Item/Potion/Potion_HealingPotion.asset` + `Assets/Art/UI/Items/Potion_HealingPotion.png` (or under existing item art folder).

**Placeholder until art lands:** reuse generic potion silhouette tinted **#B02030** in editor.

---

## 8. Healing Potion — Stun & Warrior Willpower (locked)

### 8.1 — Stun after use (default)

When Healing Potion use **succeeds** and user is **not** exempt (§8.2):

1. Apply **50 HP** heal.
2. Apply **`Stunned`** for **`stunTurns`** player phases:

```text
stunTurns = max(3, 100 / painToleranceValue)
```

- **`painToleranceValue`** = `stats.painTolerance.GetValue()` (integer ≥ 1).
- **Integer division** (C# `/`): e.g. tolerance **10** → `100/10 = 10` turns; **100** → `100/100 = 1` → **`max(3,1) = 3`** turns minimum.
- Apply via `StatusEffectService.TryApply(user, Status_Stunned)` with `turnsRemaining = stunTurns` (refresh duration if already Stunned — **locked: replace duration with new value if longer**).

**Log:** `[HealingPotion] {name} healed for 50 HP and is Stunned for {stunTurns} turns (Pain Tolerance {value}).`

### 8.2 — Exemption (Warrior Willpower + high Pain Tolerance)

```text
isExempt =
    RacialTraitQueries.HasTrait(user, RacialTraitFlags.WarriorWillpower)
    && user.stats.painTolerance.GetValue() >= 100
```

| Case | Heal | Stun | Combat use |
|------|------|------|------------|
| Default | +50 | Yes §8.1 | Out of combat only |
| **Exempt** | +50 | **No** | **In and out of combat** |

**Log (exempt):** `[HealingPotion] {name} healed for 50 HP (Warrior Willpower — no stun).`

### 8.3 — Undead

Existing **`InventoryConsumePolicy`** Undead potion ban **still applies** — Healing Potion is not drinkable by Undead regardless of Warrior Willpower.

---

## 9. Formation & turn skipping while Stunned (locked)

### 9.1 — Formation **inactive** (`IsFormationActive == false`)

When the **active** member is Stunned:

- **Move / Wait / abilities / inventory (action-spend):** rejected; **turn not consumed** (same as “already acted” rejection path).
- **Party cycle:** If game design marks “acted” per member, Stunned members are treated as **`CanActorTakeAction == false`** — when control would pass to them, they **auto-skip** (no player input required). **v0:** central **`CanActorTakeAction`** false is sufficient if UI only offers input for members who can act.

When a **non-active** party member is Stunned:

- They cannot be swapped to for meaningful action until Stun ends (optional: allow swap for inspection only — **v0: allow swap, still cannot act**).

### 9.2 — Formation **active**

| Role | Stunned behavior |
|------|------------------|
| **Follower** | **`FormationRushService`** does **not** move them during rush (treat like already acted: hold tile, register position). |
| **Leader (active member)** | Player **cannot move** leader via movement input; attempts **do not consume** a turn. |
| **Rush after another member acted** | Stunned followers skipped in §3 planning loop (`!CanActorTakeAction` already partially exists — extend to Stunned). |

### 9.3 — Turn consumption summary

| Action | Stunned active member |
|--------|----------------------|
| Attempt move | **Fail**, turn **not** spent |
| Attempt wait | **Fail**, turn **not** spent |
| Successful potion (if somehow allowed) | N/A while Stunned — use blocked |
| Enemy attacks stunned member | Normal damage; Stun duration unchanged unless future rule |

---

## 10. Current baseline (as-is)

| Area | Today |
|------|--------|
| **Barbarian folk loadout** | `DefaultBarbarianRacialLoadout` — stats/resist/passives only; **no** Warrior Willpower |
| **Capability enums** | **`BodyCapabilityFlags` only** — no racial trait flags |
| **Pain Tolerance** | **Does not exist** |
| **Potions in combat** | Scroll-like: usable in combat only via `InventoryPolicy` ally rules; potions usable **out of combat** freely |
| **Heal item** | `HealAbility` sample (20 HP); no intense-pain / stun |
| **Stunned** | **Not** in `StatusEffectId` |

---

## 11. Suggested code layout

| Piece | Location |
|-------|----------|
| `RacialTraitFlags` | `Assets/Scripts/Stats/Racial/RacialTraitFlags.cs` |
| `RacialTraitQueries` | `Assets/Scripts/Stats/Racial/RacialTraitQueries.cs` |
| `grantedRacialTraits` on loadout | `RacialLoadoutDefinition` + `Apply`/`Remove` |
| `PainTolerance` | `StatTypes.cs`, `CharacterStats`, `GetStatByType` |
| `HealingPotionAbility` | `Assets/Scripts/Abilities/Heal/HealingPotionAbility.cs` |
| `HealingPotionRules` | `Assets/Scripts/Manager/Inventory/HealingPotionRules.cs` |
| `Status_Stunned` | `Assets/Data/Status/` |
| Item asset | `Assets/Resources/Item/Potion/Potion_HealingPotion.asset` |
| Formation | `FormationRushService` — skip stunned followers |
| Input / commands | `PlayerCommandProcessor`, `CanActorTakeAction` — Stun gate |

---

## 12. Acceptance criteria

| ID | Test |
|----|------|
| **AC1** | Barbarian with default loadout has `WarriorWillpower` trait; Human does not. |
| **AC2** | New character has Pain Tolerance **10** (modifiable in inspector). |
| **AC3** | Healing Potion heals **50** HP, consumes one charge, spends active member turn. |
| **AC4** | In combat, non-exempt character cannot use potion; log/message clear. |
| **AC5** | Out of combat, tolerance 10 → Stun **`max(3, 100/10)=10`** turns. |
| **AC6** | Barbarian + Warrior Willpower + Pain Tolerance **100** → combat use OK, **no** Stun. |
| **AC7** | Stunned character cannot move; move attempt does not consume turn. |
| **AC8** | Formation on: stunned follower does not rush-move. |
| **AC9** | Formation on: stunned leader cannot move; turn not consumed on attempt. |
| **AC10** | Stunned blocks Rest (negative status). |

---

## 13. Implementation checklist

- [x] `RacialTraitFlags` + `RacialTraitQueries` (§4)
- [x] `RacialLoadoutDefinition.grantedRacialTraits`; `DefaultBarbarianRacialLoadout` → Warrior Willpower
- [x] `StatType.PainTolerance` + `CharacterStats.painTolerance` default 10 (§5)
- [x] `StatusEffectId.Stunned` + asset + polarity Negative (§6)
- [x] `HealingPotionAbility` + item asset + icon placeholder (§7)
- [x] `HealingPotionRules` + `InventoryUsability` / `InventoryItemUse` hooks (§7–8)
- [x] `CanActorTakeAction` / `PlayerCommandProcessor` Stun gates (§9)
- [x] `FormationRushService` stunned follower skip (§9.2 — via `CanActorTakeAction`)
- [x] Unit tests: stun duration math, exemption, combat gate
- [x] Update [Status-Effects-Requirements.md](../Combat/Status-Effects-Requirements.md) §4.6 Stunned row
- [ ] Play-mode AC1–AC10 (editor: **JRogue → Inventory → Seed Healing Potions on Party_Barbarian_Warrior**)

---

## 14. Document history

| Date | Note |
|------|--------|
| 2026-05-29 | Initial requirements — `RacialTraitFlags`, Pain Tolerance, Healing Potion, Stun, Warrior Willpower potion rules |
