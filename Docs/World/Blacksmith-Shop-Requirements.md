# Blacksmith shop — Requirements

**Status:** Implemented — `town_interior_market_blacksmith` two-way weapons-and-armor shop with eight new iron/steel items, Giant's Sword stock, and post-portal calendar reset.

**Purpose:** Activate the **Market blacksmith** in `DistrictTownTest` as a **two-way weapons-and-armor merchant**: the player may **buy iron/steel gear and Giant's Sword** and **sell general loot** in the **same full-screen shop menu**. Author **eight new item assets** (four armor, four weapons) plus wire existing **`Giants_Blade`**. Shop **wallet** and **buy stock** reset to authored baselines on **post-portal calendar days** (same cadence as the [Market item shop](Market-Item-Shop-Requirements.md)).

**Depends on:** [Shop NPCs](Shop-NPC-Requirements.md) (`ShopNpcController`, `ShopNpcDefinition`, `ShopNpcMenuUI`, `ShopTransactionService`, `TownShopStateService`), [Market item shop](Market-Item-Shop-Requirements.md) (two-way shop + mana-stone sell ban + calendar reset pattern), **`GameCalendarService` / `GameCalendarLogic`**, [Town hub — multi-floor](Town-Hub-Multi-Floor-Requirements.md) (`town_market`, building interiors), [NPC dialog](NPC-Dialog-Requirements.md) (Enter + counter talk), [Proficiencies](../Progression/Proficiencies-Requirements.md) (weapon types, damage types), [Subspace inventory](../Inventory/Subspace-Inventory-And-Encumbrance-Requirements.md), [Enemy death loot & mana stones](../Combat/Enemy-Death-Loot-And-Mana-Stones-Requirements.md).

**Related scenes:** `DistrictTownTest.unity` — exterior on `town_market`, interior `town_interior_market_blacksmith`.

**Not this shop:** [Market general store](MarketGeneralStoreLayout) (Mira), [Market item shop](Market-Item-Shop-Requirements.md) (Edda / potions), legacy plaza **Greta** (`town_npc_5` on `town_main`) — Greta remains until explicitly migrated; this doc covers the **district blacksmith** only.

**Explicitly out of scope (v1):** Weapon/armor **repair**; **re-forging** or upgrade services; custom orders; enchantment; haggling; buy-back of items the player sold; selling **mana stones**; rotating stock by quest; shop hours; save/load shop state across game sessions; gamepad-specific layout beyond existing shop UI; NPC movement; procedural item drops from the blacksmith.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Blacksmith** (`market_blacksmith`) becomes a functional **`ShopNpcController`** merchant in a painted forge interior on `town_market`. |
| **G2** | Player may **buy weapons and armor** and **sell general loot** in the **same** `ShopNpcMenuUI` session (**Buy** and **Sell** tabs). |
| **G3** | Author **eight new `ItemData` assets** (iron/steel chestplate, helmet, knife, sword) with a clear **power ladder** (§5). |
| **G4** | Blacksmith **buy stock** includes all eight new items **plus** **`Giants_Blade`** (display **Giant's Sword**). |
| **G5** | Player may sell **any eligible party item** to the blacksmith **except mana stones** (`allowPlayerSellManaStones = false`). |
| **G6** | Blacksmith starts with authored **wallet gold** and spends it when buying from the player; **player purchases credit** shop gold ([`ShopTransactionService`](../../Assets/Scripts/Shop/ShopTransactionService.cs) buy path). |
| **G7** | On each **post-portal calendar day**, blacksmith **stock**, **gold**, and **`boughtFromPlayer`** reset to baselines (§8). |
| **G8** | Shop state persists across town ↔ dungeon travel within a run (`TownShopStateService`). |
| **G9** | Authoring lives under `Assets/Resources/Town/DistrictTest/Building/MarketBlacksmith/` + **`BlacksmithShopPackCreator`** menu fix-up. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Blacksmith** | Building interior `town_interior_market_blacksmith`; NPC id **`market_blacksmith`**. |
| **Iron tier** | Entry smithing line — lower stats, lower price. |
| **Steel tier** | Upgrade line — strictly better stats than matching iron piece. |
| **Two-way shop** | `allowPlayerBuy = true` and `allowPlayerSell = true`. |
| **Post-portal day** | Calendar day immediately after a dungeon portal day (e.g. portal days 1, 4, 7… → reset days 2, 5, 8…). |
| **Baseline snapshot** | `initialGold` + `initialStock` from `ShopNpcDefinition` on reset. |
| **Giant's Sword** | Player-facing name for existing asset **`Giants_Blade`**. |

---

## 3. Placement & identity (locked)

| Property | Value |
|----------|--------|
| **District** | `town_market` |
| **Interior floor** | `town_interior_market_blacksmith` |
| **Exterior footprint** | **6×5** cells, **east** of the general store (item shop remains **west** of general store) |
| **NPC id** | `market_blacksmith` |
| **Display name** | Blacksmith |
| **World sprite** | `Assets/Art/NPC/Sprites/NPC_KnightDrillMaster.png` (chainmail smith; replace with dedicated blacksmith sprite when authored) |
| **Portrait** | New `Portrait_Blacksmith.asset` or reuse knight drill portrait until art pass |
| **Prefab** | `Assets/Resources/Town/Npc/TownNpc_MarketBlacksmith.prefab` |
| **Counter talk** | `NpcCounterTalkBinding` — customer row **4**, counter row **5** (match other market interiors) |
| **Shop definition** | `Assets/Resources/Shop/ShopNpc_MarketBlacksmith.asset` |
| **Layout constants** | `Assets/Scripts/World/Town/MarketBlacksmithLayout.cs` (proposed) |

**Exterior anchor (proposed):**

```text
General store origin: (16, 16) — 8×8
Item shop: west — 5×4 at (11, 16)
Blacksmith: east — 6×5 at (24, 16)  // ExteriorOriginX = 16 + 8
```

Setup menu: **JRogue → Town → Setup Blacksmith Shop**; scene integration: **Fix District Town Test Scene**.

---

## 4. Shop capabilities (locked)

### 4.1 — Two-way menu

| Flag | Value |
|------|--------|
| **`allowPlayerBuy`** | **true** |
| **`allowPlayerSell`** | **true** |
| **`allowPlayerSellManaStones`** | **false** |

**Interaction flow** (same as [Shop NPCs §3](Shop-NPC-Requirements.md)):

```text
Enter (counter-adjacent + facing blacksmith)
  → NpcDialogBoxUI: "Hello. Here to buy and sell?" [Yes] [No]
  → Yes: ShopNpcMenuUI with Buy tab (B) and Sell tab (V)
  → Escape: close shop UI
```

### 4.2 — Buy stock (blacksmith → player)

| Item | Asset id (proposed) | Initial qty | Notes |
|------|---------------------|-------------|-------|
| Iron Chestplate | `Armor_IronChestplate` | **2** | §5.1 |
| Steel Chestplate | `Armor_SteelChestplate` | **1** | §5.1 |
| Iron Helmet | `Armor_IronHelmet` | **2** | §5.2 |
| Steel Helmet | `Armor_SteelHelmet` | **1** | §5.2 |
| Iron Knife | `Weapon_IronKnife` | **3** | §5.3 — pierce |
| Steel Knife | `Weapon_SteelKnife` | **2** | §5.3 — pierce |
| Iron Sword | `Weapon_IronSword` | **2** | §5.4 — slash |
| Steel Sword | `Weapon_SteelSword` | **1** | §5.4 — slash |
| **Giant's Sword** | `Giants_Blade` (existing) | **1** | Premium line; keep existing item stats |

| Rule | Detail |
|------|--------|
| **Price source** | `ShopPriceResolver.GetBuyPrice` → item **`buyValue`**. |
| **Stock depletion** | Each purchase decrements shop stock. |
| **Player gold** | Deduct via `PartyCurrencyLedger`. |
| **Shop gold credit** | Successful buy adds **`totalCost`** to `goldOnHand`. |
| **Receipt** | `PartyManager.ActiveShopperMemberIndex`; standard carry / equip rules. |

### 4.3 — Sell rules (player → blacksmith)

Same eligibility as [Market item shop §4.3](Market-Item-Shop-Requirements.md):

| Source | Accepted? |
|--------|-----------|
| Carried / bag / subspace / container (non-equipped) | **Yes** |
| Equipped items | **No** |
| Quest / plot items | **No** |
| Party gold coins | **No** |
| **Mana stones** | **No** |
| Essence slots | **No** |

| Rule | Detail |
|------|--------|
| **Payment** | `sellValue × qty` |
| **NPC gold cap** | `payment ≤ goldOnHand`; UI clamps max qty |
| **Starting wallet** | **800 gold** (proposed — higher than item shop; pays for junk sales) |
| **Resale** | Sold items go to **`boughtFromPlayer`** only; **never** appear in Buy tab |

---

## 5. New item authoring (locked)

All new items use standard **`ItemData`** (not `LightSourceItemData`). **`requiresAppraisal = false`** for shop gear v1 (prices visible in UI). Icons: placeholder Kenney / DCSS weapon & armor sprites until dedicated art lands under `Assets/Art/Items/Sprites/`.

### 5.1 — Chestplates (`EquipmentSlot.Torso`)

| Field | Iron Chestplate | Steel Chestplate |
|-------|-----------------|------------------|
| **`itemName`** | Iron Chestplate | Steel Chestplate |
| **`category`** | `Armor` | `Armor` |
| **`slotType`** | `Torso` | `Torso` |
| **`weight`** | 8 | 12 |
| **`buyValue` / `sellValue`** | **40 / 20** | **75 / 37** |
| **`statModifiers`** | **+1 Constitution** | **+2 Constitution** |
| **Design note** | Entry body armor | Strictly better than iron |

**Implementation:** embed `StatModifierEffect` sub-assets or inline list entries targeting **`StatType.Constitution`** (mirrors existing `ItemData.statModifiers` pattern). Future dedicated **AC-on-armor** field may supersede Constitution-only mitigation ([Proficiencies §8.2](../Progression/Proficiencies-Requirements.md)).

### 5.2 — Helmets (`EquipmentSlot.Head`)

| Field | Iron Helmet | Steel Helmet |
|-------|-------------|--------------|
| **`itemName`** | Iron Helmet | Steel Helmet |
| **`category`** | `Armor` | `Armor` |
| **`slotType`** | `Head` | `Head` |
| **`weight`** | 3 | 4 |
| **`buyValue` / `sellValue`** | **25 / 12** | **45 / 22** |
| **`statModifiers`** | **+1 Constitution** | **+2 Constitution** |

**Not in scope:** **`Armor_HelmetOfLight`** remains a separate light-source artifact (Greta / light-items content); blacksmith does **not** stock it.

### 5.3 — Knives (`WeaponType.Dagger`, pierce)

| Field | Iron Knife | Steel Knife |
|-------|------------|-------------|
| **`itemName`** | Iron Knife | Steel Knife |
| **`category`** | `Weapon` | `Weapon` |
| **`slotType`** | `MainHand` | `MainHand` |
| **`weaponType`** | `Dagger` | `Dagger` |
| **`handsRequired`** | 1 | 1 |
| **`weight`** | 0.8 | 1.0 |
| **`buyValue` / `sellValue`** | **15 / 7** | **28 / 14** |
| **`damageModules`** | **6 Pierce** | **9 Pierce** |
| **`isThrowable`** | **true** | **true** |

Reference baseline: [Rusty Sword](../../Assets/Resources/Item/Weapon/RustySword.asset) (~10 blunt); knives are lighter, faster, lower damage.

### 5.4 — Swords (`WeaponType.Sword`, slash)

| Field | Iron Sword | Steel Sword |
|-------|------------|-------------|
| **`itemName`** | Iron Sword | Steel Sword |
| **`category`** | `Weapon` | `Weapon` |
| **`slotType`** | `MainHand` | `MainHand` |
| **`weaponType`** | `Sword` | `Sword` |
| **`handsRequired`** | 1 | 1 |
| **`weight`** | 3.0 | 3.5 |
| **`buyValue` / `sellValue`** | **35 / 17** | **60 / 30** |
| **`damageModules`** | **12 Slash** | **16 Slash** |

**Power ladder (locked ordering):**

```text
Iron Knife (6 pierce) < Steel Knife (9 pierce) < Iron Sword (12 slash) < Steel Sword (16 slash) << Giant's Sword (1000 slash)
```

Steel Knife must **not** exceed Iron Sword damage. Iron Sword must **not** exceed Steel Sword damage.

### 5.5 — Giant's Sword (existing)

| Property | Value |
|----------|--------|
| **Asset** | `Assets/Resources/Item/Weapon/Giants_Blade.asset` |
| **Display name** | Giant's Sword |
| **Stock** | **1** at blacksmith (premium) |
| **Stats** | **Unchanged** — 1000 slash; weight 20; existing `buyValue` / `sellValue` |
| **Note** | Absurd damage is intentional legacy / easter-egg tier; blacksmith stocks **one** copy as top-tier shelf item |

---

## 6. Asset paths (proposed)

| Asset | Path |
|-------|------|
| Iron Chestplate | `Assets/Resources/Item/Armor/Armor_IronChestplate.asset` |
| Steel Chestplate | `Assets/Resources/Item/Armor/Armor_SteelChestplate.asset` |
| Iron Helmet | `Assets/Resources/Item/Armor/Armor_IronHelmet.asset` |
| Steel Helmet | `Assets/Resources/Item/Armor/Armor_SteelHelmet.asset` |
| Iron Knife | `Assets/Resources/Item/Weapon/Weapon_IronKnife.asset` |
| Steel Knife | `Assets/Resources/Item/Weapon/Weapon_SteelKnife.asset` |
| Iron Sword | `Assets/Resources/Item/Weapon/Weapon_IronSword.asset` |
| Steel Sword | `Assets/Resources/Item/Weapon/Weapon_SteelSword.asset` |
| Shop definition | `Assets/Resources/Shop/ShopNpc_MarketBlacksmith.asset` |
| NPC prefab | `Assets/Resources/Town/Npc/TownNpc_MarketBlacksmith.prefab` |
| Interior floor def | `Assets/Resources/Town/DistrictTest/Building/MarketBlacksmith/Floor_town_interior_market_blacksmith.asset` |
| Facade overlay | `Assets/Resources/Town/DistrictTest/Building/MarketBlacksmith/FacadeOverlay_town_interior_market_blacksmith.asset` |

**Pack creator:** `Assets/Editor/World/BlacksmithShopPackCreator.cs` — creates items, shop asset, prefab, floor def, facade, and integrates **`DistrictTownTest`**.

---

## 7. Shop definition snapshot (proposed)

```yaml
shopNpcId: market_blacksmith
displayName: Blacksmith
allowPlayerBuy: true
allowPlayerSell: true
allowPlayerSellManaStones: false
initialGold: 800
initialStock:
  - Armor_IronChestplate × 2
  - Armor_SteelChestplate × 1
  - Armor_IronHelmet × 2
  - Armor_SteelHelmet × 1
  - Weapon_IronKnife × 3
  - Weapon_SteelKnife × 2
  - Weapon_IronSword × 2
  - Weapon_SteelSword × 1
  - Giants_Blade × 1
```

Add constant to **`TownShopNpcIds`** (or shared shop id registry): `MarketBlacksmith = "market_blacksmith"`.

---

## 8. Calendar-linked reset (locked)

Mirror [Market item shop §5](Market-Item-Shop-Requirements.md):

| Event | Reset? |
|-------|--------|
| **`AdvanceDay`** lands on post-portal day | **Yes** |
| Portal day itself | **No** |
| Non-post-portal days | **No** |
| Non-calendar scenes | **No** reset |

**Service:** extend **`MarketItemShopResetService`** → rename or add **`MarketBlacksmithResetService`**, or generalize to **`DistrictShopResetService`** that resets **both** calendar-linked merchants from a list of `ShopNpcDefinition` resource paths.

**Reset fields:** `goldOnHand`, `stock`, clear **`boughtFromPlayer`**.

**Bootstrap:** `DistrictTownCalendarBootstrap` calls reset for current post-portal day on hub load.

---

## 9. UI & controls

Same as [Market item shop §7](Market-Item-Shop-Requirements.md):

| Key | Action |
|-----|--------|
| **B** | Buy tab |
| **V** | Sell tab |
| **Escape** | Close shop |

Show **player gold** and **shop gold** (`goldOnHand`) in header when sell tab is enabled.

Buy tab lists all in-stock weapons/armor with **`buyValue`** and remaining quantity. Sell tab lists party items; **no mana stone rows**.

---

## 10. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC-BS1** | Enter blacksmith interior on `town_market`, talk at counter → Yes opens shop with **both** Buy and Sell tabs. |
| **AC-BS2** | Buy tab lists all **nine** stock lines (eight new + Giant's Sword) with correct prices and quantities. |
| **AC-BS3** | Purchasing gear deducts player gold, **credits shop gold**, reduces stock, and delivers item to active shopper when carry rules pass. |
| **AC-BS4** | **Iron Knife** deals **Pierce** damage; **Iron Sword** / **Steel Sword** deal **Slash**; steel variants exceed their iron counterparts. |
| **AC-BS5** | Equipping chestplates and helmets applies **Constitution** modifiers; steel pieces grant **+2**, iron **+1**. |
| **AC-BS6** | Sell tab lists party carried items; **no mana stone stacks**; payment respects **`goldOnHand`** cap. |
| **AC-BS7** | New run: blacksmith has **800 gold** and full baseline stock (§7). |
| **AC-BS8** | Deplete stock/gold on portal day **1**; advance to day **2** → stock and gold match baselines before opening shop. |
| **AC-BS9** | Deplete on day **3**; day **4** portal → **no** reset; day **5** → **reset**. |
| **AC-BS10** | Shop state survives market ↔ blacksmith ↔ dimension square without spurious reset. |
| **AC-BS11** | Market item shop, general store, and legacy Greta behavior **unchanged**. |

---

## 11. Implementation notes (non-normative)

| Component | Change |
|-----------|--------|
| **`MarketBlacksmithLayout`** | Exterior/interior cells, portal link ids, counter cells, NPC marker. |
| **`BlacksmithShopPackCreator`** | Author items + shop asset + prefab (`ShopNpcController`) + floor/facade; menu under **JRogue → Town**. |
| **`BlacksmithShopResetService`** | Post-portal reset (or shared district shop reset helper). |
| **`TownDistrictTestPaths`** | `MarketBlacksmithFolder` constants. |
| **`DistrictTownTestSceneCreator`** | Register interior floor in catalog; paint facade on `town_market`. |
| **`ShopCounterService`** | `EnsureMarketBlacksmithCounters()` if counter cells need runtime registration. |
| **Item icons** | Editor menu creates placeholder sprites; art pass can swap without logic changes. |
| **Greta (`town_npc_5`)** | Leave on `town_main` for now; optional follow-up to remove overlapping weapon stock from Greta once blacksmith ships. |

---

## 12. Related paths

| Asset | Path |
|-------|------|
| Existing Giant's Sword | `Assets/Resources/Item/Weapon/Giants_Blade.asset` |
| Existing Helmet of Light (not stocked) | `Assets/Resources/Item/Armor/Armor_HelmetOfLight.asset` |
| Market item shop pattern | `Docs/World/Market-Item-Shop-Requirements.md` |
| Shop transaction gold | `Assets/Scripts/Shop/ShopTransactionService.cs` |
| Calendar reset | `Assets/Scripts/World/Town/MarketItemShopResetService.cs` |
| Damage types | `Assets/Scripts/Stats/StatTypes.cs` (`Pierce`, `Slash`) |

---

## 13. Open questions

| ID | Question | Proposed default |
|----|----------|------------------|
| **Q1** | Blacksmith **starting gold**? | **800** |
| **Q2** | **Giant's Sword** shop price — keep legacy **`buyValue = 2`**? | **Yes** v1 (intentionally cheap easter egg until economy pass) |
| **Q3** | Reset service — **one helper for all calendar shops** vs per-shop class? | **Shared `DistrictCalendarShopResetService`** with definition list |
| **Q4** | Dedicated **blacksmith NPC sprite** vs knight drill placeholder? | Knight drill **v1**; swap when `NPC_Blacksmith.png` is composed |
| **Q5** | Should **Helmet of Light** ever appear in blacksmith stock? | **No** v1 — unique light artifact |
| **Q6** | Exterior size **6×5** vs **8×8** to match general store massing? | **6×5** v1 (fits east slot without overlapping market edge) |

---

## 14. Revision history

| Date | Notes |
|------|--------|
| 2026-06-19 | Initial draft — two-way blacksmith, eight new iron/steel items, Giant's Sword stock, mana stone sell ban, post-portal reset, market district placement |
| 2026-06-19 | Implemented — layout, items, shop, pack creator, district integration, shared calendar reset service |
