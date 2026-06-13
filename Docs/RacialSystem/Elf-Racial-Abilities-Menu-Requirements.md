# Elf — Racial abilities menu (Elemental Spirit contracts)

**Purpose:** Specify the **Elf body** of the shared [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (`K`): a **read-only reference sheet** for every **contracted Elemental Spirit instance** on the focused Elf, plus the **only** in-menu edit — an optional **nickname** per instance that drives **hotbar summon/dismiss labels**.

**Status:** Implemented (v0) — sorted roster cards, nickname edit, hotbar label wiring, read-only passives/actives.

**Depends on:** [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) (shell, **`K`**, party strip, modal rules), [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md) (`ElementalSpiritContractsRuntime`, contract instances, cumulative level payloads), [Elf — meditation & leveling](Elf-ElementalSpirit-Meditation-Leveling-Requirements.md) (`contractExperience`, bond XP display), [Elf — Fairy Stone contracts](Elf-Fairy-Stone-Spirit-Contract-Requirements.md) (forming new contracts), [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) (`ElementalSpiritSummon` entries, spirit active dedup).

**Related:** [Barbarian Spirit Imprint view model](../../Assets/Scripts/UI/Racial/BarbarianSpiritImprintViewModel.cs) (read-only reference pattern), [Barbarian Spirit Imprint — Shaman NPC](Barbarian-Spirit-Imprint-Shaman-NPC-Requirements.md) (town progression — parallel to meditation / Fairy Merchant).

**Explicitly out of scope (v0):** Forming contracts, leveling spirits, summon/dismiss, or assigning hotbar slots from this menu; **sort mode picker** (level / name / element / summoned — future); respec / remove contracts; editing spirit **type** or **level**; nicknames on **deduped spirit actives** (Sudden Strength stays ability-named); gamepad layout; persisting last-focused party member across sessions.

---

## Locked decisions

| # | Decision |
|---|----------|
| **L1** | Elf body mounts when focused member is `Race.Elf` with `RacialSubsystemKind.ElfElementalContracts`. |
| **L2** | **Read-only reference** — same discipline as Barbarian Spirit Imprint body (§3 parent doc). Banner points player to **Fairy Merchant** (new contracts) and **Meditation Shrine** (bond XP / level). |
| **L3** | **Roster sort (v0):** **Contract level descending**, then **spirit canonical name ascending** (case-insensitive). Stable tie-break: `contractInstanceId`. |
| **L4** | **Future:** Additional sort modes (name, element, summoned first, roster order) — **not v0**; data model and view-model API should not hard-code only one comparator. |
| **L5** | **Only editable field:** optional **`nickname`** per **contract instance** (`contractInstanceId`). |
| **L6** | **Hotbar summon/dismiss label** uses **display label** (§6): `nickname` when non-blank after trim; otherwise **canonical instance name** (§5.3). Format remains `{displayLabel} — Summon` / `{displayLabel} — Dismiss`. |
| **L7** | **Spirit combat actives** on hotbar (deduped by ability asset) **unchanged** — ability display name only ([Elf contracts §5.11](Elf-ElementalSpirit-Contracts-Requirements.md)). Nicknames affect **instance** rows, not shared actives. |
| **L8** | Menu refresh on open (and after nickname commit) — not every frame. |

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | **Roster at a glance** — Player sees every contracted spirit **instance** on the focused Elf with level, bond XP, cap, and summoned state. |
| **G2** | **Predictable order** — Highest **contract level** first; ties broken alphabetically by spirit type name. |
| **G3** | **Barbarian parity** — Information-only sheet; town gates own progression (Fairy Stone, meditation shrine). |
| **G4** | **Hotbar clarity** — Player can nickname duplicate instances (e.g. two Ember Wardens) so summon slots are distinguishable on the ability hotbar. |
| **G5** | **No duplicate systems** — Menu does not replace hotbar assign/use, meditation dialog, or Fairy Stone flow. |
| **G6** | **Extensible sort** — v0 comparator is fixed; later sort UI plugs in without rewriting cards. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **Elf body** | Race-specific panel in `RacialAbilitiesUI` when focused member is an Elf with elemental contracts. |
| **Contract instance** | One roster row: `{ contractInstanceId, spirit, contractLevel, contractExperience, nickname? }`. |
| **Canonical instance name** | Spirit type label used when nickname is blank — §5.3 (includes duplicate disambiguator). |
| **Display label** | String used for hotbar summon/dismiss and as the **primary title** on the menu card when nickname is set; §6. |
| **Bond XP** | `contractExperience` toward next contract level ([meditation doc](Elf-ElementalSpirit-Meditation-Leveling-Requirements.md)). |
| **Sort key (secondary)** | `spirit.displayName` trimmed, fallback `spirit.spiritId`, compared case-insensitively. |

---

## 3. Screen responsibilities (locked)

| UI | Player can… | Cannot… |
|----|-------------|---------|
| **Racial abilities menu — Elf body** | Read roster; **edit nickname** per instance | Form contracts, gain bond XP, change level, summon/dismiss |
| **Fairy Merchant** | Buy Fairy Stones → form contracts | Browse full roster sheet |
| **Meditation shrine** | Award bond XP → level instances | Edit nicknames |
| **Ability hotbar** | Assign summon/dismiss + spirit actives; use in play | Edit nicknames (must use racial menu) |

**Read-only banner (required intent):**

> View only — form new contracts at the **Fairy Merchant**; deepen bonds at the **Meditation Shrine** in town.

No buttons; no teleport links.

---

## 4. Router integration

Extends parent [§5.3 router](../UI/Racial-Abilities-Menu-Requirements.md):

```
RacialAbilitiesUI.RefreshBodyForFocusedMember()
  → …
  Race.Elf + ElfElementalContracts → ElfElementalSpiritBodyView
  (else default placeholder)
```

| Condition | Body |
|-----------|------|
| `Race.Elf` + `ElfElementalContracts` + `ElementalSpiritContractsRuntime` | **Elf contract roster** (this doc) |
| `Race.Elf` but no runtime / wrong subsystem | Default placeholder with Elf-specific copy |
| Not Elf | Default placeholder (unchanged) |

---

## 5. Data model

### 5.1 — Contract instance extension

Add to `ElementalSpiritContractPreset` (serialized on `ElementalSpiritContractsRuntime`):

| Field | Type | Notes |
|-------|------|--------|
| **`nickname`** | string | Optional player label. **Default empty.** Trim on save. |

Existing fields unchanged: `contractInstanceId`, `spirit`, `contractLevel`, `contractExperience`.

**Save:** Nickname travels with party member / runtime blob. New contracts start with `nickname = ""`.

### 5.2 — Nickname validation (v0)

| Rule | Detail |
|------|--------|
| **Max length** | **24** characters after trim (config constant). |
| **Empty** | Whitespace-only → stored as empty → **canonical instance name** used everywhere. |
| **Uniqueness** | **Not required** in v0 (two instances may share a nickname). |
| **Characters** | Printable text; no newlines. Strip leading/trailing whitespace on commit. |
| **Commit** | On field blur or **Enter**; instant hotbar refresh (`AbilityHotbarUI.RefreshAll`). |

### 5.3 — Canonical instance name (no nickname)

When `nickname` is blank, derive the same label used today for duplicate spirit **types**:

1. Base = `spirit.displayName` trimmed, else `spirit.spiritId`.
2. If multiple contracted instances share the same `spiritId`, append **` (2)`**, **` (3)`**, … in **roster order** among same-type instances (first instance has no suffix).

This matches current hotbar / meditation picker disambiguation.

---

## 6. Display label (nickname + hotbar)

```
displayLabel(instance) =
  if nickname is non-empty after trim → nickname
  else → canonicalInstanceName(instance)
```

**Hotbar summon/dismiss** ([Elf contracts §5.10](Elf-ElementalSpirit-Contracts-Requirements.md)):

| State | Overflow / assignable label | Main-row label (when assigned) |
|-------|----------------------------|--------------------------------|
| Not summoned | `{displayLabel} — Summon` | Same pattern + key badge |
| Summoned | `{displayLabel} — Dismiss` | Same pattern + key badge |

**Menu card title row:**

| Nickname set? | Primary title | Subtitle (muted) |
|---------------|---------------|------------------|
| Yes | **Nickname** (bold) | Canonical spirit name + duplicate suffix |
| No | Canonical instance name | Element name · `spiritId` optional in dev |

**Spirit actives** (deduped): still **ability name only** (e.g. “Sudden Strength”) — nicknames do **not** apply.

---

## 7. Sort order (v0 locked)

Apply sort **before** building card list:

1. **`contractLevel` descending** (higher level first).
2. **Canonical sort name ascending** — case-insensitive `string.Compare(..., OrdinalIgnoreCase)` on §5.3 base name **without** duplicate ` (n)` suffix (sort Ember Warden vs Tide Shard by type name, not “Ember Warden (2)”).
3. **`contractInstanceId` ascending** — stable tie-break when same level and same spirit type.

**Future sort modes (out of v0, non-exhaustive):**

| Mode id | Order |
|---------|--------|
| `LevelDescNameAsc` | **v0 default** (above) |
| `NameAsc` | Display label A→Z |
| `ElementThenLevel` | Element enum, then level desc |
| `SummonedFirst` | Summoned instances first, then v0 default |
| `RosterOrder` | Original `contractedSpirits` list order |

Implement v0 with a single strategy + interface hook (`IElementalSpiritRosterSort`) so UI can add a dropdown later without data changes.

---

## 8. Elf body — layout & content

### 8.1 — Section structure

Uses same full-screen chrome as parent [§5.1](../UI/Racial-Abilities-Menu-Requirements.md):

```
┌──────────────── FULL SCREEN ─────────────────────────────────────┐
│ RACIAL ABILITIES                                                 │
│ View only — form new contracts at the Fairy Merchant…            │  banner
│ [F1] [F2] … party strip                                          │
├──────────────────────────────────────────────────────────────────┤
│ ELEMENTAL SPIRIT CONTRACTS                                       │  section label
│  ┌─ Spirit card (sorted) ─────────────────────────────────────┐  │
│  │ [element tint]  Title · Lv 3 · SUMMONED                    │  │
│  │ Nickname: [___________]  (only editable control)           │  │
│  │ Bond XP 12/30 · Cap 5 · Fire                               │  │
│  │ PASSIVES / ACTIVES (read-only, current contract level)     │  │
│  └────────────────────────────────────────────────────────────┘  │
│  (scroll)                                                        │
├──────────────────────────────────────────────────────────────────┤
│ K — racial abilities · Esc — close · F1–F5 — focus member        │
└──────────────────────────────────────────────────────────────────┘
```

### 8.2 — Empty roster

When `contractedSpirits` is empty:

> No elemental spirit contracts yet.  
> Buy **Fairy Stones** from the **Fairy Merchant** and use them on an Elf in your party.

No nickname controls.

### 8.3 — Spirit card (read-only except nickname)

Each **contract instance** renders one card after sort (§7).

| Block | Content | Editable? |
|-------|---------|-----------|
| **Header** | Display label or canonical name; **contract level**; **SUMMONED** badge if instance in `summonedContractInstanceIds` | No |
| **Nickname field** | Single-line TMP input; placeholder *“Nickname (optional)”* | **Yes** |
| **Progress** | Bond XP `current / toNext` or capped / max-level copy ([meditation doc](Elf-ElementalSpirit-Meditation-Leveling-Requirements.md)) | No |
| **Cap line** | `Level cap: N (your character level)` | No |
| **Element** | Fire / Water / Earth / Wind | No |
| **Costs (reference)** | Summon SP, upkeep SP/turn from `ElementalSpiritDefinition` | No |
| **Passives** | Cumulative passives for levels **1 … contractLevel** (names + descriptions) | No |
| **Actives** | Cumulative actives for levels **1 … contractLevel** (ability name, description, costs); footnote: *Assign on the ability hotbar* | No |

**Do not** show: Fairy Stone odds, meditation costs, hotbar key bindings, internal `contractInstanceId` (except debug).

### 8.4 — Missing runtime

| Condition | Body |
|-----------|------|
| Not `Race.Elf` | Default placeholder (router should not mount Elf body). |
| Elf without `ElementalSpiritContractsRuntime` | *“This character cannot form elemental spirit contracts.”* |
| Wrong `racialSubsystem` | Default placeholder |

---

## 9. Nickname editing UX

| Rule | Detail |
|------|--------|
| **Where** | Nickname input **only** on Elf racial menu card — not on hotbar, meditation dialog, or inventory. |
| **When saved** | Blur or **Enter** commits; **Esc** in field reverts to last saved value (does not close menu). |
| **Feedback** | No toast required v0; hotbar labels update immediately on successful commit. |
| **Modal** | Editing nickname does **not** consume a turn; menu stays open. |
| **Service** | `ElementalSpiritNicknameService.TrySetNickname(elf, instanceId, nickname, out reason)` — single mutation path; validates §5.2. |

---

## 10. View-model builder

Suggested API (implementation hint):

```
ElfElementalSpiritViewModel.Build(BaseActor elf)
  → runtime = elf.GetComponent<ElementalSpiritContractsRuntime>()
  → rows = runtime.ContractedSpirits.Where(valid)
  → sorted = ElementalSpiritRosterSort.Apply(rows, LevelDescNameAsc)
  → cards = sorted.Select(preset → SpiritContractCardModel)
      Title, Subtitle, Nickline, Progress, Cap, Element,
      Summoned, Passives[], Actives[], InstanceId
```

Unit tests cover sort order, display label, and nickname validation.

---

## 11. Hotbar & tooltip integration

| Location | Label source |
|----------|----------------|
| **Overflow summon/dismiss** | §6 `{displayLabel} — Summon/Dismiss` |
| **Main hotbar slot** (assigned summon entry) | `ResolveDisplayName` uses display label for `ElementalSpiritSummon` entries |
| **Tooltip title** | Display label; subtitle includes canonical name when nickname set |
| **Deduped spirit actives** | Unchanged — ability `abilityName` |

On nickname change: `AbilityHotbarUI.RefreshAll()` + overflow rebuild.

---

## 12. Integration

| System | Rule |
|--------|------|
| **RacialAbilitiesUI** | Router mounts Elf body; refresh on open and after nickname save. |
| **ElementalSpiritContractsRuntime** | Read roster + nicknames; no level/contract mutation from menu. |
| **HotbarAssignabilityService** | Summon labels use `ElementalSpiritDisplayNames.GetDisplayLabel(preset, roster)`. |
| **HotbarResolver** | Unchanged binding keys (`contractInstanceId`); labels are presentation-only. |
| **Meditation / Fairy Stone** | May continue using canonical or display labels in pickers — **recommend display label** in instance lists for consistency. |
| **Save/load** | `nickname` serialized per preset. |

---

## 13. Acceptance criteria

| ID | Test |
|----|------|
| **A1** | Focus Elf with three contracts → cards appear **level high → low**; same level sorted **A→Z** by spirit type name. |
| **A2** | Two Ember Warden instances, levels 2 and 1 → level **2** card appears **above** level **1** card. |
| **A3** | Set nickname **“Blaze”** on instance → hotbar shows **“Blaze — Summon”**; canonical name still visible as card subtitle. |
| **A4** | Clear nickname → hotbar reverts to **“Ember Warden (2) — Summon”** (or equivalent canonical label). |
| **A5** | No controls to level, contract, summon, or assign hotbar from menu; banner mentions Fairy Merchant + Meditation Shrine. |
| **A6** | Non-Elf focused member still shows default placeholder; Elf body hidden. |
| **A7** | Nickname &gt; 24 chars rejected or truncated per validation policy. |
| **A8** | Deduped **Sudden Strength** hotbar entry unchanged when instance nicknames change. |
| **A9** | **`K` / Esc / F1–F5** behavior unchanged from parent racial menu doc. |

---

## 14. Implementation phases

| Phase | Scope |
|-------|--------|
| **v0 (this doc)** | Sorted roster cards, bond XP/cap display, nickname edit + hotbar label wiring, read-only passives/actives |
| **v0.1** | Sort dropdown (§7 future modes); optional element color accents on cards |
| **v1** | Filter (summoned only / by element); persist sort preference |

---

## 15. Cross-references to update when implemented

| Doc | Update |
|-----|--------|
| [Racial abilities menu](../UI/Racial-Abilities-Menu-Requirements.md) | §6 default Elf row → link here; §14 v1 Elf body **Done** |
| [Elf — Elemental Spirit contracts](Elf-ElementalSpirit-Contracts-Requirements.md) | §5.10 hotbar labels use display label; D4 preset adds `nickname` |
| [Elf — meditation & leveling](Elf-ElementalSpirit-Meditation-Leveling-Requirements.md) | L16 racial menu as persistent XP home |
| [Ability hotbar](../UI/Ability-Hotbar-Requirements.md) | Summon entry display name source |

---

## 16. Document history

| Date | Change |
|------|--------|
| 2026-06-09 | Initial draft — sorted contract roster, nickname-only edit, hotbar label rules, Barbarian parity. |
