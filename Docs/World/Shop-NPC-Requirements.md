# Town shop NPCs — Requirements

Two **Human-derived shop NPCs** in town: one **buy-only** merchant (player sells items; NPC pays **sell value** up to **300 gold**), one **sell-only** merchant (player buys **Giant's Blade** ×2 at **buy value**; NPC holds **100 gold** for future buy support). **Enter-to-talk** opens a short **Yes/No dialog**; **Yes** opens a **full-screen buy/sell menu**. Shop **gold and stock persist** across dungeon ↔ town travel within a run without putting shop NPC `GameObject`s on the DDOL layer.

**Depends on:** `NpcController`, `NpcDialogBoxUI`, `NpcTalkInteraction`, `InputHandler`, `PartyManager`, `PartyCurrencyLedger`, `PartyManaStoneLedger`, `InventoryManager`, `EquipmentManager`, `ItemData`, `ItemInstance`, `ManaStoneItemData`, `TownNpcSetupPhase`, `DungeonFloorInstanceManager`, `RunPartyPersistence`, [NPC dialog](NPC-Dialog-Requirements.md) (Enter adjacency + facing), [Inventory UI redesign](../Inventory/Inventory-UI-Redesign-Requirements.md) (list/inspect patterns), [Enemy death loot & mana stones](../Combat/Enemy-Death-Loot-And-Mana-Stones-Requirements.md) (mana stone tiers), [Subspace inventory](../Inventory/Subspace-Inventory-And-Encumbrance-Requirements.md) (party-wide item locations).

**Related:** [Dungeon time — town arrival](Dungeon-Time-Requirements.md) (party DDOL on forced exit). [Dynamic dungeon floors](Dynamic-Dungeon-Floor-Generation-Requirements.md) (run layer vs floor instances).

**Explicitly out of scope (v0):** save/load shop state across game sessions; haggling / faction discounts; shop restock timers; buy-back of items the player sold; appraisal integration in shop UI; gamepad-specific layout beyond keyboard; shop NPC movement; multiple currency types beyond party gold.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | Add **town NPC 4** (Human, unused sprite + portrait) — **buy-only** shop: player **sells** items; NPC pays **sell value** until **300 gold** is exhausted. |
| **G2** | Add **town NPC 5** (Human, unused sprite + portrait) — **sell-only** shop: stock **2 × Giant's Blade** at **buy value** each; NPC holds **100 gold** (reserved for future buy rules). |
| **G3** | Item economy uses explicit **`buyValue`** / **`sellValue`** on `ItemData` with defaults **2 / 1**; overrides per item; mana stones use tier formula. |
| **G4** | Player may sell **any non-equipped** item from **all party members** (carried, containers, subspace — not equipped slots). |
| **G5** | Shop NPC 4 **never** sells to the player — including items the player previously sold to that NPC. |
| **G6** | **Full-screen shop UI** (CRPG reference layout); **Escape** closes shop and returns to gameplay. |
| **G7** | **v0 dialog** — all shop NPCs: `"Hello. Here to buy and sell?"` → **Yes** opens shop; **No** ends talk. |
| **G8** | Shop **gold + inventory persist** for the run when traveling dungeon ↔ town; shop NPC actors remain **town-scene bound** (not DDOL). |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Shop NPC** | `ShopNpcController` (extends / composes with `NpcController`) bound to a `ShopNpcDefinition` asset. |
| **Buy value** | Gold the **player pays** when **buying from** the shop (`ItemData.buyValue` or resolver default). |
| **Sell value** | Gold the **player receives** when **selling to** the shop (`ItemData.sellValue` or resolver default). |
| **Buy-only shop (player)** | Player can **purchase** from NPC stock only. |
| **Sell-only shop (player)** | Player can **sell to** NPC only (NPC is the buyer). |
| **Shop stock** | NPC-owned `ItemInstance` stacks the player may buy (when shop supports buy). |
| **Shop wallet** | NPC's remaining gold (`goldOnHand`) used to pay the player on sell transactions. |
| **Shop state snapshot** | Serializable run-time record in **`TownShopStateService`** (DDOL) — not the NPC `GameObject`. |
| **Equipped** | Any item in an actor's equipment slots via `EquipmentManager` — **ineligible** to sell. |

---

## 3. Interaction model (locked)

| Rule | Detail |
|------|--------|
| **Open talk** | Same as dialog NPCs: **`Enter`** while orthogonally adjacent + facing (`NpcTalkInteraction`). |
| **Preflight dialog** | Bottom `NpcDialogBoxUI` choice: prompt + **Yes / No** (§8). |
| **Yes** | Close dialog chrome; open **`ShopNpcMenuUI`** full-screen for this shop. |
| **No** | End session; no shop UI; no story flags (v0). |
| **Turn cost** | Opening talk, dialog, shop browse = **no turn**. Completing a **buy or sell transaction** = **no turn** (v0; align with inventory management). |
| **Blocks gameplay** | `ShopNpcMenuUI.BlocksGameplay` registered in `InputHandler.BlocksFloorGameplay()`. |
| **Close shop** | **`Escape`** (`Cancel` action if wired) closes shop UI only; player remains adjacent to NPC. |

```
Enter (adjacent + facing shop NPC)
  → NpcDialogBoxUI: "Hello. Here to buy and sell?" [Yes] [No]
  → No: close
  → Yes: ShopNpcMenuUI.Show(shopDefinition, hydrated state)
  → Escape: close shop UI
```

---

## 4. Town placement

Extend `Stamp_TownPlaza_20x20` and `TownNpcSetupPhase` with two markers (cells TBD in stamp authoring; **2 cells apart** from neighbors where possible):

| NPC | Role | Marker id | Suggested cell | Starting gold | Stock (initial) |
|-----|------|-----------|----------------|---------------|-----------------|
| **NPC 4** | Player **sells** here | `town_npc_4` | `(2, 8, 0)` | **300** | Empty (buys from player only) |
| **NPC 5** | Player **buys** here | `town_npc_5` | `(12, 8, 0)` | **100** | **2 × Giant's Blade** |

Existing dialog NPCs remain at `(4,8)`, `(6,8)`, `(8,8)` per [NPC dialog §4](NPC-Dialog-Requirements.md).

**Prefab:** `HumanNpc` variant + `ShopNpcController`; distinct world sprite and portrait (§12).

---

## 5. Item economy

### 5.1 — Buy / sell fields on `ItemData`

Add (or map from existing economy fields):

| Field | Default | Meaning |
|-------|---------|---------|
| **`buyValue`** | **2** | Price player pays when buying from a shop. |
| **`sellValue`** | **1** | Price player receives when selling to a shop. |

**Default rule:** when `sellValue` is unset (0) and `buyValue` > 0, **`sellValue = floor(buyValue × 0.5)`** (integer math; default **2 → 1**).

**Legacy `goldValue`:** remains for appraisal / floor loot UX until migrated; **`ShopPriceResolver`** uses **`buyValue` / `sellValue`** for shops (not `goldValue`).

**Per-item overrides:** content may set explicit pairs (e.g. rare gear **buy 80 / sell 40**) that need not follow 50%.

### 5.2 — Mana stones

Mana stones use **`PartyManaStoneLedger`** stacks, not carried `ItemInstance` bags.

| Rule | Value |
|------|--------|
| **Buy value** | **0** (shops do not sell mana stones in v0 content). |
| **Sell value** | **`(9 - tier) + 1`** per stone |

Examples:

| Tier | Sell value |
|------|------------|
| 9 | 1 |
| 8 | 2 |
| 5 | 5 |
| 1 | 9 |

Species id does **not** affect sell price in v0 (tier only).

### 5.3 — Giant's Blade (NPC 5 stock)

User-facing name **"Giant's Sword"** maps to existing item **`Giants_Blade`** (`Assets/Resources/Item/Weapon/Giants_Blade.asset`) unless a separate item is authored.

| Field | v0 content |
|-------|------------|
| **buyValue** | **2** |
| **sellValue** | **1** (default 50%) |
| **Shop stock** | Quantity **2** on NPC 5 only |

---

## 6. Shop NPC definitions (v0 content)

### 6.1 — NPC 4 — general buyer (`town_npc_4`)

| Property | Value |
|----------|--------|
| **Capabilities** | **`AllowPlayerSell = true`**, **`AllowPlayerBuy = false`** |
| **Starting gold** | **300** |
| **Starting stock** | None |
| **Accepts** | Any sellable non-equipped item from **all party members** (§7) |
| **Pays** | **`sellValue`** × quantity per transaction line |
| **Gold floor** | NPC **cannot** complete a purchase if **`totalCost > goldOnHand`**; partial multi-qty sells must split or clamp (§7.3) |
| **Resale** | Items sold to NPC 4 are **removed from player** and added to NPC internal inventory but **never** appear in a player-facing buy list |

### 6.2 — NPC 5 — weapon vendor (`town_npc_5`)

| Property | Value |
|----------|--------|
| **Capabilities** | **`AllowPlayerBuy = true`**, **`AllowPlayerSell = false`** |
| **Starting gold** | **100** (unused in v0 buy-only flow; persisted for future) |
| **Starting stock** | **2 × Giants_Blade** |
| **Prices** | Each blade sells for **`buyValue` (2 gold)** |
| **Player buy rule** | Allowed while stock > 0 and player **`PartyCurrencyLedger`** has enough gold |

---

## 7. Transaction rules

### 7.1 — Eligible player items (sell to shop)

Include from **every living party member**:

| Source | Eligible |
|--------|----------|
| Carried / bag / subspace / container contents | **Yes** |
| Equipped slots | **No** |
| **`PartyCurrencyLedger`** gold coins | **No** (gold is payment, not sold) |
| **`PartyManaStoneLedger`** | **Yes** (sell at tier formula; v0) |
| Essence slots | **No** |
| Plot / quest items | **No** if flagged `ItemCategory.QuestItem` or `PlotItem` (v0 hard exclude) |

### 7.2 — Buy from shop (NPC 5)

| Check | Rule |
|-------|------|
| Stock | `shopStock.quantity >= requestedQty` |
| Player gold | `PartyCurrencyLedger` ≥ **`buyValue × qty`** |
| Result | Transfer item instance(s) to chosen party member (§7.4); deduct gold; reduce shop stock |

### 7.3 — Sell to shop (NPC 4)

| Check | Rule |
|-------|------|
| Payment | **`sellValue × qty`** |
| NPC gold | **`payment ≤ shop.goldOnHand`** |
| Multi-qty | If player selects qty whose total exceeds NPC gold, UI **clamps max qty** to what NPC can afford **before** confirm |
| Result | Remove items from player; add gold to **`PartyCurrencyLedger`**; subtract NPC gold; append to NPC internal bought inventory (not for resale) |

### 7.4 — Item receipt target (player buys)

Use existing **`PartyManager.ActiveShopperMemberIndex`** (default **0** = party leader) as the member who receives purchased gear. If carry rules fail, show error and **do not** charge (v0: no auto-routing to other members).

### 7.5 — Appraisal

Shop uses **`buyValue` / `sellValue`** directly in v0 — **not** appraisal `?` flow.

---

## 8. v0 dialog (all shop NPCs)

| Step | Content |
|------|---------|
| **Line / prompt** | `"Hello. Here to buy and sell?"` |
| **Choices** | **Yes** · **No** |
| **No** | Close dialog; end. |
| **Yes** | Open `ShopNpcMenuUI` with modes filtered by shop capabilities (NPC 4 → sell panel only; NPC 5 → buy panel only). |

Implementation: small `NpcDialogProfile` or dedicated `ShopNpcGreetingDialog` helper — data-driven like existing town dialog.

---

## 9. Shop UI — full-screen menu

### 9.1 — Reference (industry patterns)

Follow common JRPG / CRPG shop layouts ([ORK shop UI](https://orkframework.com/guide/documentation/inventory/shops/), RPG Maker **Buy/Sell** + dual list windows):

| Pattern | Adopt |
|---------|--------|
| **Full-screen overlay** | Same viewport margin as [Inventory UI](../Inventory/Inventory-UI-Redesign-Requirements.md) (~8–16 px) |
| **Header strip** | Shop name, **player gold**, **shop gold** (when shop can buy) |
| **Mode tabs** | **Buy** / **Sell** — hide tab when shop disallows that mode |
| **Dual columns** | **Shop stock** (buy mode) or **Player sellable list** (sell mode) **50%** \| **Detail + confirm** **50%** |
| **List columns** | Icon · name · qty · **price each** · line total |
| **Confirm row** | Selected item summary + qty stepper + **[Confirm]** |
| **Footer** | `Esc — Leave shop` (collapsed help OK) |
| **Optional** | NPC portrait thumbnail in header (reuse shop portrait asset) |

### 9.2 — Layout mock

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│  SHOP — {NpcDisplayName}          Player gold: 1,240          Shop gold: 300           │
│  [ Buy ]  [ Sell ]                         ← hide Buy on NPC 4; hide Sell on NPC 5     │
├───────────────────────────────────────────┬─────────────────────────────────────────────┤
│  LIST (50%)                               │  DETAIL (50%)                               │
│  ┌──┬────────────────────┬────┬─────────┐ │  [icon]  Giant's Blade                      │
│  │  │ Item               │ Qty│ Price   │ │  Buy: 2 gold each                           │
│  │  │ Giant's Blade      │  2 │ 2       │ │  ─────────────────────────────────────────  │
│  │  │ …                  │    │         │ │  Qty: [ − ] 1 [ + ]                         │
│  └──┴────────────────────┴────┴─────────┘ │  Total: 2 gold                                │
│                                           │  [ Confirm purchase ]                         │
├───────────────────────────────────────────┴─────────────────────────────────────────────┤
│  Esc — Leave shop                                                                       │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

**Sell mode (NPC 4):** list shows aggregated sellable rows from all party members; detail pane shows **`sellValue`** and max qty limited by **shop gold**.

### 9.3 — Input

| Key | Action |
|-----|--------|
| **↑↓** | Move selection |
| **Enter** | Confirm transaction (when focus on confirm) |
| **+ / −** or **, / .** | Adjust qty (TBD; match inventory UX) |
| **Escape** | Close shop |

---

## 10. Persistence — shop state without DDOL NPCs

### 10.1 — Problem

Shop NPCs live on **`town_main`** floor instances. The party uses **`RunPartyPersistence`** (DDOL) across scene loads. Shop NPC **`GameObject`s must not** `DontDestroyOnLoad` — they are grid actors tied to town tilemaps.

### 10.2 — Locked approach: DDOL state service + town-scene hydration

| Layer | Lifetime | Contents |
|-------|----------|----------|
| **`TownShopStateService`** (DDOL on run layer) | Created with run bootstrap; cleared on **exit dungeon / new run** | `Dictionary<shopNpcId, ShopStateSnapshot>` |
| **Shop NPC instance** (town floor) | Spawned by `TownNpcSetupPhase` when `town_main` generates; parked with floor instance | Visual + grid; **runtime mirror** of snapshot |

**`ShopStateSnapshot`** fields:

| Field | Type |
|-------|------|
| `shopNpcId` | string |
| `goldOnHand` | int |
| `stock` | List of `{ itemDefinitionId, quantity, optional instance seed }` |
| `boughtFromPlayer` | Internal stash (optional separate list; **not offered to player**) |

### 10.3 — Lifecycle

```
First town visit (shop marker spawn)
  → Load ShopNpcDefinition initial gold + stock
  → If TownShopStateService has snapshot for shopNpcId: apply snapshot (overwrite initial)
  → Bind live ShopNpcController to snapshot id

Player transacts
  → Update live controller + immediately write snapshot to TownShopStateService

Leave town for dungeon
  → Town floor parked or scene unloaded; NPC destroyed
  → Snapshot already on DDOL service

Return to town
  → town_main activated (existing or regenerated)
  → TownNpcSetupPhase spawns fresh NPC prefabs
  → Each ShopNpcController hydrates from TownShopStateService
```

### 10.4 — Why not alternatives

| Approach | Verdict |
|----------|---------|
| **DDOL shop NPC GameObjects** | **Reject** — duplicate spawns, stale grid registration, leaked transforms |
| **Rely only on parked `town_main` instance** | **Fragile** — dev regen, missing DDOL manager, or future town regen wipes stock |
| **Save file only** | **Out of scope v0** |

### 10.5 — Service placement

Add **`TownShopStateService`** beside `DungeonRunState` / `PartyCurrencyLedger` on the run DDOL root (`DungeonRunBootstrap.EnsureDungeonRunObjects` or town entry hook). **Not** on shop prefabs.

---

## 11. Data model (implementation sketch)

| Asset / type | Role |
|--------------|------|
| **`ShopNpcDefinition`** | `shopNpcId`, display name, portrait, sprite, capabilities, **initialGold**, **initialStock[]**, greeting profile id |
| **`ShopNpcController`** | Runtime shop; implements talk → greeting → open UI |
| **`ShopPriceResolver`** | `GetBuyPrice(ItemData)`, `GetSellPrice(ItemData)`, mana stone tier formula |
| **`ShopTransactionService`** | Validates and applies buy/sell atomically (ledger + inventory + snapshot) |
| **`ShopNpcMenuUI`** | Full-screen UI; `BlocksGameplay` |
| **`TownShopStateService`** | DDOL snapshots keyed by `shopNpcId` |

Extend **`ItemData`**:

```csharp
[Header("Shop economy")]
public int buyValue = 2;
public int sellValue = 1; // 0 = derive from buyValue × 50%
```

---

## 12. Art assets

| Asset | Path | Notes |
|-------|------|-------|
| NPC 4 world sprite | `Assets/Art/NPC/Sprites/NPC_{Name}.png` | **Must differ** from Mira / Luc / Edda |
| NPC 5 world sprite | `Assets/Art/NPC/Sprites/NPC_{Name}.png` | **Must differ** from all prior town NPCs |
| NPC 4 portrait | `Assets/Art/Portraits/NPC/Portrait_{Name}.png` | 128×128 source |
| NPC 5 portrait | `Assets/Art/Portraits/NPC/Portrait_{Name}.png` | 128×128 source |

Source: remaining unused **[Kenney Toon Characters 1](https://kenney.nl/assets/toon-characters-1)** slots or new CC0 placeholders; ThirdParty README/LICENSE under `Assets/Art/NPC/` and `Assets/Art/Portraits/`.

---

## 13. Acceptance criteria (v0)

| # | Criterion |
|---|-----------|
| **AC1** | Town shows **5** Human NPCs; NPC 4 and 5 use **new** sprite + portrait. |
| **AC2** | Enter + face shop NPC → greeting → **Yes** opens full-screen shop; **No** cancels. |
| **AC3** | NPC 4: player can sell non-equipped items from all members; NPC pays **sell value**; stops when **300 gold** exhausted. |
| **AC4** | NPC 4: player **cannot** buy anything from NPC 4. |
| **AC5** | NPC 5: player can buy **Giant's Blade** ×2 at **2 gold** each while stock lasts. |
| **AC6** | Default item prices **buy 2 / sell 1**; override items respect authored values. |
| **AC7** | Mana stone sell price = **`(9 - tier) + 1`**; buy value 0. |
| **AC8** | **Escape** closes shop menu and unblocks gameplay. |
| **AC9** | After dungeon exit → town return, NPC 4 gold/stock and NPC 5 stock reflect prior transactions (via **`TownShopStateService`**). |
| **AC10** | Shop NPC actors are **not** DDOL; persistence verified without duplicate NPCs. |

---

## 14. Implementation checklist

- [ ] `ItemData.buyValue` / `sellValue` + `ShopPriceResolver` + unit tests (defaults, overrides, mana tier)
- [ ] `ShopNpcDefinition` assets for NPC 4 & 5
- [ ] `TownShopStateService` (DDOL) + hydrate on spawn + write on transaction
- [ ] `ShopNpcController` + greeting dialog hook
- [ ] `ShopNpcMenuUI` full-screen layout + `BlocksGameplay` + Escape close
- [ ] `ShopTransactionService` (party inventory, ledgers, equip guard)
- [ ] Stamp markers `town_npc_4`, `town_npc_5` + `TownNpcSetupPhase` entries + prefabs
- [ ] Art: 2 sprites + 2 portraits + ThirdParty attribution
- [ ] Editor menu: **JRogue → Town → Create Shop NPC Pack** (mirror dialog pack creator)
- [ ] Manual QA: sell until NPC 4 broke; buy both blades; leave dungeon and return

---

## 15. Open questions (non-blocking v0)

| # | Question | Default for v0 |
|---|----------|----------------|
| **Q1** | Display names for NPC 4 & 5 | Author in `ShopNpcDefinition` (e.g. **Fenn**, **Greta**) |
| **Q2** | Exact plaza cells for markers | `(2,8)` and `(12,8)` — adjust in stamp if blocked |
| **Q3** | Sell mana stones by tier aggregate vs per-species stack | **Per ledger stack row**; same tier different species may differ in UI but same price |
| **Q4** | NPC 5 `100 gold` purpose | Persist only; no player→NPC5 selling in v0 |
