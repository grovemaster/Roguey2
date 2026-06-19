# Market item shop — Requirements

**Status:** Implemented — `town_interior_market_item_shop` clerk (Edda sprite) is a two-way `ShopNpcController` with calendar-linked stock reset.

**Purpose:** Activate the **Market item shop** in `town_market` as a **two-way shop NPC**: the player may **buy Healing Potions** and **sell general loot** in the **same full-screen shop menu**. Shop **wallet** and **buy stock** reset to authored baselines on the **calendar day after each dungeon portal day**, tying merchant inventory to the DistrictTest hub calendar (`GameCalendarService`, §5).

**Depends on:** [Shop NPCs](Shop-NPC-Requirements.md) (`ShopNpcController`, `ShopNpcDefinition`, `ShopNpcMenuUI`, `ShopTransactionService`, `TownShopStateService`), **`GameCalendarService` / `GameCalendarLogic`** (portal cadence; see [Town hub](Town-Hub-Multi-Floor-Requirements.md) DistrictTest calendar bootstrap), [Healing Potion](../RacialSystem/Warrior-Willpower-Healing-Potion-And-Stun-Requirements.md) (`Potion_HealingPotion`), [Town hub — multi-floor](Town-Hub-Multi-Floor-Requirements.md) (`town_market`, `town_interior_market_item_shop`), [NPC dialog](NPC-Dialog-Requirements.md) (Enter + counter talk), [Enemy death loot & mana stones](../Combat/Enemy-Death-Loot-And-Mana-Stones-Requirements.md), [Subspace inventory](../Inventory/Subspace-Inventory-And-Encumbrance-Requirements.md).

**Related scenes:** `DistrictTownTest.unity` — exterior on `town_market`, interior `town_interior_market_item_shop`.

**Not this shop:** **`town_interior_market_general_store`** (Mira / general store) remains separate content; do not conflate the two merchants.

**Explicitly out of scope (v1):** Additional stock lines beyond Healing Potion; rotating stock by quest; haggling; buy-back of items the player sold; selling mana stones to this NPC; shop hours / night closure; save/load shop state across game sessions; gamepad-specific layout beyond existing shop UI; NPC movement; blacksmith / weapon repair.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Market item shop clerk** (`market_item_shop_clerk`) becomes a functional **`ShopNpcController`** merchant in the painted interior. |
| **G2** | Player may **buy** and **sell** in the **same** `ShopNpcMenuUI` session (**Buy** and **Sell** tabs both visible). |
| **G3** | Clerk **sells Healing Potions** at **2 gold** each (`buyValue`). |
| **G4** | Clerk starts with **500 gold** and spends it when buying from the player (standard sell rules). |
| **G5** | Player may sell **any eligible party item** to this clerk **except mana stones** (hard exclude for this shop). |
| **G6** | On each **post-portal calendar day**, clerk **stock** and **gold** reset to authored baselines (§6). |
| **G7** | Shop state persists across town ↔ dungeon travel and district floor swaps within a run (existing `TownShopStateService` pattern). |
| **G8** | Authoring lives under `Assets/Resources/Town/DistrictTest/Building/MarketItemShop/` + `MarketItemShopPackCreator` menu fix-up. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Item shop** | Building interior `town_interior_market_item_shop` west of the general store on `town_market`. |
| **Item shop clerk** | NPC id `market_item_shop_clerk`; display name **Item Shop Clerk**; world sprite **`NPC_Edda.png`**. |
| **General store** | **`town_interior_market_general_store`** — Mira; **out of scope** for this doc. |
| **Two-way shop** | `allowPlayerBuy = true` **and** `allowPlayerSell = true` on the same `ShopNpcDefinition`. |
| **Portal day** | Calendar day when the dimension-square dungeon portal is open (`GameCalendarLogic.IsDungeonPortalDay`). |
| **Post-portal day** | The **immediately following** calendar day (e.g. portal days 1, 4, 7… → reset days 2, 5, 8…). |
| **Baseline snapshot** | Authoring-time **`initialGold`** + **`initialStock`** copied from `ShopNpcDefinition` on reset. |
| **Shop wallet** | Runtime `goldOnHand` on the clerk's `ShopStateSnapshot`. |

---

## 3. Placement & identity (locked)

| Property | Value |
|----------|--------|
| **District** | `town_market` |
| **Interior floor** | `town_interior_market_item_shop` |
| **Exterior** | 5×4 footprint west of general store ([`MarketItemShopLayout`](../../Assets/Scripts/World/Town/MarketItemShopLayout.cs)) |
| **NPC id** | `market_item_shop_clerk` |
| **Display name** | Item Shop Clerk |
| **World sprite** | `Assets/Art/NPC/Sprites/NPC_Edda.png` |
| **Prefab** | `Assets/Resources/Town/Npc/TownNpc_MarketItemShopClerk.prefab` |
| **Counter talk** | `NpcCounterTalkBinding` — customer row **4**, counter row **5** |
| **Shop definition asset** | `Assets/Resources/Shop/ShopNpc_MarketItemShopClerk.asset` (proposed path) |

Setup menu: **JRogue → Town → Setup Market Item Shop**; scene integration: **Fix District Town Test Scene**.

---

## 4. Shop capabilities (locked)

### 4.1 — Two-way menu

| Flag | Value |
|------|--------|
| **`allowPlayerBuy`** | **true** |
| **`allowPlayerSell`** | **true** |

**Interaction flow** (same as [Shop NPCs §3](Shop-NPC-Requirements.md)):

```text
Enter (counter-adjacent + facing clerk)
  → NpcDialogBoxUI: "Hello. Here to buy and sell?" [Yes] [No]
  → Yes: ShopNpcMenuUI with Buy tab (B) and Sell tab (V)
  → Escape: close shop UI
```

This is the **first DistrictTest merchant** with **both** tabs enabled. Existing `ShopNpcMenuUI` already renders Buy/Sell tabs when both flags are true — **no new menu shell** required unless UX gaps are found in playtest.

### 4.2 — Buy stock (clerk → player)

| Item | Asset | Initial qty | Player price |
|------|-------|-------------|--------------|
| **Healing Potion** | `Assets/Resources/Item/Potion/Potion_HealingPotion.asset` | **10** (authoring default; see §12 Q1) | **2 gold** each (`buyValue = 2`) |

| Rule | Detail |
|------|--------|
| **Price source** | `ShopPriceResolver.GetBuyPrice` → item **`buyValue`** (set **`buyValue = 2`** on `Potion_HealingPotion` if not already). |
| **Stock depletion** | Each purchase decrements shop stock; **out-of-stock** lines hidden or disabled in Buy tab. |
| **Player gold** | Deduct via `PartyCurrencyLedger`; standard insufficient-gold error. |
| **Receipt** | Purchased potions go to **`PartyManager.ActiveShopperMemberIndex`** member if carry rules pass ([Shop NPCs §7.4](Shop-NPC-Requirements.md)). |

### 4.3 — Sell rules (player → clerk)

| Source | Accepted by item shop? |
|--------|-------------------------|
| Carried / bag / subspace / container items (non-equipped) | **Yes** |
| Equipped items | **No** |
| Quest / plot items | **No** (existing category exclude) |
| Party gold coins | **No** |
| **Mana stones** (`PartyManaStoneLedger`) | **No** — **hard exclude for this shop only** |
| Essence slots | **No** |

| Rule | Detail |
|------|--------|
| **Payment** | `sellValue × qty` per line ([Shop NPCs §5](Shop-NPC-Requirements.md)). |
| **NPC gold cap** | Payment requires **`total ≤ goldOnHand`**; UI clamps max qty ([Shop NPCs §7.3](Shop-NPC-Requirements.md)). |
| **Starting wallet** | **500 gold** |
| **Resale** | Items sold to the clerk are stored in internal **`boughtFromPlayer`** inventory and **never** appear in the player Buy list (same as Fenn / NPC 4). |

**Mana stone exclusion (new per-shop rule):**

- `ShopSellableQuery.BuildPartySellOffers` must accept an optional **shop filter** (or read `ShopNpcDefinition` flags) so **`AppendManaStoneOffers` is skipped** when the active shop disallows mana stone buyback.
- Proposed definition field: **`allowPlayerSellManaStones`** default **true**; item shop sets **`false`**.

---

## 5. Calendar-linked reset (locked)

### 5.1 — Trigger

Reset the item shop when the hub calendar **enters a post-portal day**:

| Portal days (default cadence) | Reset days |
|------------------------------|------------|
| 1, 4, 7, 10, … | **2, 5, 8, 11, …** |

**Definition:** day **D** is a **post-portal day** when calendar day **D − 1** (previous day) was a **portal day**.

```csharp
// Illustrative — implement in GameCalendarLogic
public static bool IsPostPortalDay(GameCalendarDate date, int interval, int startDay)
{
    GameCalendarDate previous = RewindOneDay(date);
    return IsDungeonPortalDay(previous, interval, startDay);
}
```

### 5.2 — When reset runs

| Event | Reset? |
|-------|--------|
| Calendar **`AdvanceDay`** lands on a post-portal day (inn sleep, dungeon return, debug) | **Yes** — at day boundary |
| Player opens shop on post-portal day (if day advanced while in dungeon) | State already reset before talk |
| Portal day itself | **No** |
| Non-post-portal days | **No** |

Hook: subscribe from a small **`MarketItemShopResetService`** (or extend `GameCalendarService` day-changed callback) when `GameCalendarService.IsEnabled` (DistrictTest hub).

**Legacy `TownTest` / non-calendar scenes:** item shop reset **does not run** until calendar mode is active; shop state persists for the run (acceptable for deprecated scenes).

### 5.3 — What reset restores

| Field | After reset |
|-------|-------------|
| **`goldOnHand`** | **`initialGold` (500)** |
| **`stock`** | **`initialStock`** (10 × Healing Potion) |
| **`boughtFromPlayer`** | **Cleared** (items sold to clerk during the prior cycle are gone) |

Reset **does not** modify player inventory or party gold.

### 5.4 — Cadence example (default 3-day portal)

| Calendar day | Portal? | Post-portal? | Shop reset at day start? |
|--------------|---------|--------------|---------------------------|
| 1 | Yes | No | No |
| 2 | No | Yes | **Yes** |
| 3 | No | No | No |
| 4 | Yes | No | No |
| 5 | No | Yes | **Yes** |

Player buys potions on day 1 → stock depleted. Sleeps at inn → day 2 → **restock + 500 gold** before next shop visit.

---

## 6. Persistence & services

| Concern | Owner |
|---------|--------|
| Runtime shop snapshot | `TownShopStateService` — key **`market_item_shop_clerk`** |
| Baseline authoring | `ShopNpcDefinition` asset |
| Calendar | `GameCalendarService` (DDOL, DistrictTest) |
| Reset orchestration | **`MarketItemShopResetService`** (proposed DDOL helper) or `TownShopStateService.ResetToBaseline(shopNpcId, definition)` |
| NPC actor | Scene-spawned `ShopNpcController` on interior floor — **not** DDOL |

Mirror pattern: Fenn/Greta shops (`TownShopStateService`), innkeeper wallet (`InnLodgingService` + shop snapshot).

---

## 7. NPC conversion (implementation checklist)

Replace static **`NpcDialogProfile`** talk with **`ShopNpcController`**:

| Step | Action |
|------|--------|
| 1 | Create **`ShopNpc_MarketItemShopClerk.asset`** with §4 flags and §4.2 stock |
| 2 | Update **`MarketItemShopPackCreator.EnsureClerkPrefab`** — `ShopNpcController`, remove static dialog-only flow |
| 3 | Remove duplicate plain **`NpcController`** if prefab variant leaves both (same lesson as innkeeper) |
| 4 | Set **`buyValue = 2`** on `Potion_HealingPotion` (content pass) |
| 5 | Implement **`allowPlayerSellManaStones = false`** filter |
| 6 | Implement post-portal reset hook |
| 7 | Run **Setup Market Item Shop** + **Fix District Town Test Scene** |

**v0 dialog:** keep standard shop greeting — *"Hello. Here to buy and sell?"* ([Shop NPCs §8](Shop-NPC-Requirements.md)). Retire or orphan `NpcDialog_MarketItemShopClerk.asset` static line.

---

## 8. UI & controls

Reuse existing shop UI ([Shop NPCs §9](Shop-NPC-Requirements.md)):

| Control | Action |
|---------|--------|
| **B** | Buy tab |
| **V** | Sell tab |
| **Escape** | Close shop |
| Shop gold display | Show clerk **`goldOnHand`** when sell tab relevant |

No separate buy-only or sell-only modes.

---

## 9. Acceptance criteria

| ID | Criterion |
|----|-----------|
| **AC-MIS1** | Enter item shop interior, talk to clerk at counter → Yes opens shop with **both** Buy and Sell tabs. |
| **AC-MIS2** | Buy tab lists **Healing Potion** at **2 gold**; purchase succeeds with sufficient party gold and decrements stock. |
| **AC-MIS3** | Sell tab lists party carried items; **no mana stone stacks** appear. |
| **AC-MIS4** | Selling items pays **`sellValue`** and reduces clerk gold; cannot sell more than **`goldOnHand`** allows. |
| **AC-MIS5** | New run: clerk has **500 gold** and **10** Healing Potions (or authored qty). |
| **AC-MIS6** | Deplete stock and clerk gold on day 1 (portal day); advance to day 2 → stock and gold match baselines before opening shop. |
| **AC-MIS7** | Deplete on day 3; advance to day 4 (portal) → **no** reset; advance to day 5 → **reset**. |
| **AC-MIS8** | Enter dungeon on portal day, return next calendar day → if post-portal, reset applied. |
| **AC-MIS9** | Shop state survives walk **market ↔ item shop interior ↔ dimension square** without spurious reset (non-post-portal days). |
| **AC-MIS10** | General store (Mira) behavior unchanged. |

---

## 10. Implementation notes (non-normative)

| Component | Proposed change |
|-----------|-----------------|
| **`ShopNpcDefinition`** | Add **`allowPlayerSellManaStones`** (default true). |
| **`ShopSellableQuery`** | Parameterize mana stone append by definition flag. |
| **`GameCalendarLogic`** | Add **`IsPostPortalDay`**, **`RewindOneDay`**. |
| **`MarketItemShopResetService`** | On `GameCalendarService.DateChanged`, if post-portal → reset clerk snapshot. |
| **`TownShopStateService`** | `ResetSnapshotFromDefinition(ShopNpcDefinition)` helper. |
| **`MarketItemShopPackCreator`** | Wire shop asset + `ShopNpcController` prefab. |

---

## 11. Related paths

| Asset | Path |
|-------|------|
| Layout | `Assets/Scripts/World/Town/MarketItemShopLayout.cs` |
| Pack creator | `Assets/Editor/World/MarketItemShopPackCreator.cs` |
| Interior floor def | `Assets/Resources/Town/DistrictTest/Building/MarketItemShop/Floor_town_interior_market_item_shop.asset` |
| Healing Potion | `Assets/Resources/Item/Potion/Potion_HealingPotion.asset` |
| Shop catalog pattern | `Assets/Resources/Shop/ShopNpc_Greta.asset`, `ShopNpc_Fenn.asset` |

---

## 12. Open questions

| ID | Question | Proposed default |
|----|----------|------------------|
| **Q1** | Initial **Healing Potion** stock count? | **10** |
| **Q2** | **`sellValue`** for Healing Potion when player sells back? | **1 gold** (50% of buy; standard economy) |
| **Q3** | Reset on **first hub load** (day 1) if day 1 is portal day? | **No** — first reset on day **2** |
| **Q4** | Show clerk **remaining stock** in dialog before shop? | **No** v1 — shop UI only |
| **Q5** | Portal interval follows **`DistrictTownCalendarBootstrap`** config? | **Yes** — use live `GameCalendarService` interval/start day |

---

## 13. Revision history

| Date | Notes |
|------|--------|
| 2026-06-19 | Initial draft — two-way item shop, Healing Potion stock, 500 gold, mana stone sell ban, post-portal calendar reset |
