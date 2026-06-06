# Town NPC dialog — Requirements

Three **Human-derived town NPCs** with **bottom-screen JRPG dialog** (portrait + bordered text), **parameterized lines**, **choice branches**, and **story-flag conditions**. Talk is triggered by **Enter** while **orthogonally adjacent** and **facing** the NPC.

**Depends on:** `BaseActor`, `GridManager`, `GridMover`, `PartyManager`, `InputHandler`, `PlayerCommandProcessor`, `MapInteractOrthogonal`, `GameStoryFlagService`, `FlagPrecondition`.

**Related:** [Map interact & altars](Altar-And-Map-Interact-Requirements.md) (`E` = adjacent interact; NPC talk is **Enter-only**). [Main character game over](../Party/Main-Character-Game-Over-Requirements.md) (`DisplayName`).

**Explicitly out of scope (v0):** quest journal UI, save/load of dialog state across sessions, merchant shops, NPC movement, bump-to-talk, gamepad-specific layout beyond keyboard.

---

## 1. Goals

| ID | Goal |
|----|------|
| **G1** | Three town NPCs, Human-derived, distinct world sprites, placed **2 grid cells apart**. |
| **G2** | Talk trigger: **Enter** while **orthogonally adjacent** and **facing** the NPC (active party leader). |
| **G3** | Bottom-screen dialog panel with **speaker portrait**, **name plate**, and **bordered text box** (*Dragon Quest* / *Octopath Traveler* reference). |
| **G4** | **Parameterized dialog** — `{npcName}`, `{speakerName}`, `{partyName}`, etc. |
| **G5** | **NPC 1 (Mira)** — visit-count branching (1st vs 2nd talk). |
| **G6** | **NPC 2 (Luc)** — choice dialog (`Hello` / `Bonjour`) with follow-up lines. |
| **G7** | **NPC 3 (Edda)** — story-conditioned line based on whether player talked to NPC 1 **or** NPC 2. |
| **G8** | **Portrait pipeline** — per-NPC portraits; party race defaults + per-member overrides. |
| **G9** | **Free CC0 assets** imported with ThirdParty README/LICENSE. |

---

## 2. Glossary

| Term | Meaning |
|------|--------|
| **NPC talk target** | `NpcController` implementing `INpcTalkTarget` on a grid cell. |
| **Dialog profile** | `NpcDialogProfile` ScriptableObject — root graph of lines, choices, conditions. |
| **Talk counter** | Per-`npcId` visit count (`NpcTalkCounterService`). |
| **Story flag** | Boolean progress marker (`GameStoryFlagService`); shared with `FlagPrecondition`. |
| **Portrait catalog** | `PartyRacePortraitCatalog` — race default portraits; actor override wins. |

---

## 3. Interaction model (locked)

| Rule | Detail |
|------|--------|
| **Input** | **`Enter`** (`Confirm` action) when **not** in targeting mode. |
| **Adjacency** | `MapInteractOrthogonal.IsOrthogonallyAdjacent` — 4-way only. |
| **Facing** | Cardinal direction from player cell → NPC cell must match `activeLeader.currentFacing`. |
| **Speaker** | Active party leader (`partyMembers[0]`) provides `{partyName}` via `DisplayName`. |
| **Turn cost** | **No turn** for open/advance/choice. |
| **Separate from E** | Map interact (`E`) unchanged. |

```
Player presses Enter
  → InputState.Normal and no blocking modal?
  → orthogonally adjacent to talk-target NPC?
  → active leader facing toward NPC cell?
  → open NpcDialogBoxUI with profile graph
```

---

## 4. Town NPC placement

| NPC | Name | Cell | Marker id |
|-----|------|------|-----------|
| **NPC 1** | Mira | `(4, 8, 0)` | `town_npc_1` |
| **NPC 2** | Luc | `(6, 8, 0)` | `town_npc_2` |
| **NPC 3** | Edda | `(8, 8, 0)` | `town_npc_3` |

Cells are **2 apart** on X, west of player start `(10, 8)`.

**Prefab:** `HumanNpc.prefab` — Prefab Variant of `HumanPlayer.prefab` with `NpcController`, player-only components stripped.

**Spawn:** `TownNpcSetupPhase` reads stamp markers on `town_main` floor generation.

---

## 5. Dialog data model

ScriptableObject-driven graph in `Assets/Data/Dialog/`.

| Type | Role |
|------|------|
| `DialogLineData` | `textTemplate` with `{npcName}`, `{partyName}`, `{speakerName}` tokens |
| `DialogNodeData` | Line, Choice, or Conditional node (nested serializable) |
| `DialogGraphEvaluator` | Walks graph; evaluates conditions via `DialogContext` |
| `NpcDialogProfile` | `npcId`, `root`, `completionFlagId`, `incrementTalkCountOnStart` |

### v0 NPC scripts (data, not code)

| NPC | Behavior |
|-----|----------|
| **Mira** | Talk count 0 → `"My name is {npcName}. Hello, {partyName}."`; count ≥1 → `"Hello again. My name is {npcName}."` |
| **Luc** | Choice: `"Do you prefer Hello or Bonjour?"` → Hello → `"Then hello to you."`; Bonjour → `"Then bonjour to you sir."` |
| **Edda** | If neither `talked_npc_1` nor `talked_npc_2` → `"Hello World."`; else → `"Greetings."` |

**Flags:** `talked_npc_1`, `talked_npc_2` set on dialog **complete** for NPCs 1 and 2.

---

## 6. Dialog UI — bottom panel

Reference: *Dragon Quest XI* / *Octopath Traveler* — portrait left, dialog box lower third, double-frame border.

| Element | Spec |
|---------|------|
| **Panel** | Bottom ~30% of screen, full width |
| **Portrait** | Left column ~112px; double border (outer `#2a1810`, inner `#c8a060`) |
| **Text box** | Inset bordered panel; TMP body |
| **Advance** | **Enter** advances or closes |
| **Choices** | Vertical buttons; arrow keys + Enter |
| **BlocksGameplay** | Registered in `InputHandler.BlocksFloorGameplay()` |

---

## 7. Portrait system

**Resolution:** source 128×128; display ~96×96 logical pixels.

**Resolution order:**
1. Per-actor `PortraitDefinition` override on `BaseActor`
2. Race default from `PartyRacePortraitCatalog`
3. Generic placeholder

**Paths:**
- `Assets/Art/Portraits/NPC/` — Mira, Luc, Edda
- `Assets/Art/Portraits/Party/Race/` — Human, Barbarian, Elf defaults

---

## 8. Acceptance criteria (v0)

| # | Criterion |
|---|-----------|
| **AC1** | TownTest shows 3 NPCs with distinct sprites at `(4,8)`, `(6,8)`, `(8,8)`. |
| **AC2** | Each NPC is `HumanNpc` prefab variant. |
| **AC3** | Enter adjacent + facing opens dialog; wrong facing does nothing. |
| **AC4** | Mira first/second talk lines per §5. |
| **AC5** | Luc choice dialog per §5. |
| **AC6** | Edda conditional line per §5. |
| **AC7** | Bottom panel with portrait + bordered text; blocks movement. |
| **AC8** | Three distinct NPC portraits; party race catalog + override hook. |
| **AC9** | Dialog strings in ScriptableObject profiles. |
| **AC10** | Art with ThirdParty attribution. |

---

## 9. Implementation checklist

- [x] `GameStoryFlagService` + `NpcTalkCounterService`; wire `FlagPrecondition`
- [x] Dialog data model + `DialogParameterResolver` + unit tests
- [x] `NpcDialogBoxUI` bottom panel
- [x] `NpcController`, `HumanNpc.prefab`, Enter wiring in `InputHandler`
- [x] Three `NpcDialogProfile` assets + portrait catalog
- [x] `TownNpcSetupPhase` + stamp markers
- [x] ThirdParty README/LICENSE for art

---

## 10. Art assets

| Asset | Path | License |
|-------|------|---------|
| NPC world sprites | `Assets/Art/NPC/Sprites/NPC_*.png` | CC0 procedural placeholders (replaceable with [Kenney Toon Characters 1](https://kenney.nl/assets/toon-characters-1)) |
| NPC portraits | `Assets/Art/Portraits/NPC/Portrait_*.png` | CC0 procedural placeholders |
| Party race portraits | `Assets/Art/Portraits/Party/Race/Portrait_*.png` | CC0 procedural placeholders |

**Import:** PPU **32** for world sprites (Point filter, pivot `(0.5, 0.25)`); portraits PPU **128**.
