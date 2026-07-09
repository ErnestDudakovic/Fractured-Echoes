# Implementation Notes — System Fixes & New Mechanics

This document summarizes a review-and-implementation pass over the existing
codebase. It covers bug fixes to already-implemented systems and new mechanics
implemented from the roadmap in `Documentation.txt`.

---

## 1. Bug Fixes

### 1.1 Save system silently lost most game state (critical)
`JsonUtility` cannot serialize bare primitives (`float`, `int`) or bare
collections (`List<string>`) — it produces `{}`, so the data was written but
empty. This affected:

| Component | Lost data |
|---|---|
| `SanitySystem` | current sanity |
| `EnvironmentStateManager` | current room phase |
| `InventoryManager` | all carried items |
| `ScriptedEventController` | which one-time events already fired |

**Fix:** added save-safe wrapper classes `SaveFloat`, `SaveInt`,
`SaveStringList` in `Core/SaveLoad/SaveData.cs`. All four components now
capture state through wrappers. `RestoreState` keeps a legacy branch for the
old raw types, so nothing breaks if an old object reference is passed.

> Note: old save files from before this fix contain `{}` for these entries and
> cannot be recovered — new saves work correctly.

### 1.2 The item-use loop was dead (critical gameplay)
`LockedDoor` implements `IItemReceiver`, and `InventoryManager` has
`UseItemOn()` / `FindItemForReceiver()`, but **nothing ever connected them** —
a key in the inventory could never open a door.

**Fix:** `InteractionSystem.HandleInput()` now checks whether the focused
target implements `IItemReceiver` and whether the player carries a matching
item. If so, the item is consumed by the receiver (key → door) instead of the
default interaction. `LockedDoor` additionally shows a `Use <item name>`
prompt when the player is carrying the required key.

### 1.3 Escape-key collisions between UIs
`PauseMenuController`, `SaveStationUI`, `InventoryUI`, and
`InspectItemController` each listened for Escape independently — closing the
save station also opened the pause menu on the same keypress, etc.

**Fix:** new static helper `UI/UIFocus.cs`:
- Modal UIs call `RegisterModalOpen()` / `RegisterModalClosed()`.
- A UI that reacts to Escape calls `ConsumeEscape()`.
- `PauseMenuController` skips Escape when another modal is open or the press
  was already consumed this frame.
- `InventoryUI` now also closes on Escape, and refuses to open while another
  modal is open or the game is paused.
- Static state resets on scene load so counts can't leak.

### 1.4 Puzzle completion went nowhere
`PuzzleData` defined `triggerPhaseIndex`, `rewardItem`, and `prerequisites`,
and `GameStateManager` has `MarkPuzzleCompleted()` — none of it was wired.
Puzzles also started `Locked` with nothing to unlock them.

**Fix (all in `PuzzleController`):**
- On completion: registers with `GameStateManager`, grants the reward item to
  the inventory, triggers the configured environment phase transition, and
  fires `SolvePuzzle`-type scripted events.
- `CheckPrerequisites()` is now implemented (GameStateManager lookup with a
  scene-scan fallback).
- New `_startAvailable` option (default **true**) so puzzles work out of the
  box; set it to false for puzzles unlocked by other systems.
- New public `Data` property for companion components.

### 1.5 Scripted event trigger types with no drivers
`TriggerType.PickUpObject`, `Timer`, and `LookAtObject` could never fire.

**Fix:**
- `ScriptedEventController` now subscribes to `InventoryManager.ItemAdded`
  and fires `PickUpObject`-type events (the existing `requiredItem` condition
  filters which ones actually execute).
- `Timer`-type events are kicked off in `Start()`; their `triggerDelay` acts
  as the timer.
- New component `Environment/LookAtEventTrigger.cs` for `LookAtObject` events
  (see §2.1).

### 1.6 Smaller fixes
- `EnvironmentStateManager.AdvancePhase()` — null/empty guard.
- `EventTriggerZone` — auto-finds the `ScriptedEventController` if not wired.
- `HotbarUI` consumables — now data-driven via a new `ItemData.sanityRestore`
  field instead of matching item-ID substrings ("pill", "medicine"...). The
  substring behaviour remains as a legacy fallback.
- `SanitySystem` → `AudioManager` link: sanity stress now also drives the
  audio tension level (low-pass muffling), not just the camera.

---

## 2. New Mechanics

### 2.1 `LookAtEventTrigger` (Environment/)
Fires a scripted event when the player looks at the object: view-angle +
distance + optional line-of-sight check, with a configurable minimum look
duration so a passing glance doesn't trigger it. Supports one-time or
repeatable firing and a specific event ID or all `LookAtObject` events.
This is the tool for "sound before visual confirmation" and "object was
watching you" moments.

### 2.2 Light-based puzzle (Puzzle/) — Location 3 mechanic
- **`LightSwitchInteractable`** — extends `InteractableObject`; toggles a
  `Light` (plus optional emissive mesh and rotating handle), with on/off
  sounds, and raises a `Toggled` event. Works standalone for lamps/candles.
- **`LightPatternPuzzle`** — solved when the on/off pattern of its registered
  switches matches a target pattern. On solve: raises a `GameEvent`, can
  force-unlock a `LockedDoor`, registers with `GameStateManager`, can trigger
  an environment phase, fires `SolvePuzzle` scripted events, and optionally
  locks the switches. Implements `ISaveable`.

### 2.3 Audio-based puzzle (Puzzle/) — Location 4 mechanic
**`AudioSequencePuzzle`** — a Simon-says companion for the existing
`PuzzleController`. It plays the solution sequence as audio cues (one clip per
input ID, with optional light flashes); the player repeats it via
`PuzzleInteractable` buttons, and the `PuzzleController` state machine
validates the input as before. Supports auto-replay after a failed attempt,
play-on-start, and a `PlayCue()` helper for per-button feedback.

Setup: give `PuzzleData.solutionSequence` the input IDs (e.g. `tone_a`,
`tone_b`, `tone_a`), define one `SequenceCue` per ID, and trigger
`PlaySequence()` from a "play recording" interactable or trigger zone.

---

## 3. Files Changed / Added

**Changed:**
- `Core/SaveLoad/SaveData.cs` (wrappers)
- `Player/SanitySystem.cs`
- `Environment/EnvironmentStateManager.cs`
- `Environment/ScriptedEventController.cs`
- `Environment/EventTriggerZone.cs`
- `Inventory/InventoryManager.cs`
- `Inventory/InspectItemController.cs`
- `Interaction/InteractionSystem.cs`
- `Interaction/LockedDoor.cs`
- `Puzzle/PuzzleController.cs`
- `ScriptableObjects/ItemData.cs`
- `UI/PauseMenuController.cs`, `UI/InventoryUI.cs`, `UI/SaveStationUI.cs`,
  `UI/GameOverUI.cs`, `UI/HotbarUI.cs`

**Added:**
- `UI/UIFocus.cs`
- `Environment/LookAtEventTrigger.cs`
- `Puzzle/LightSwitchInteractable.cs`
- `Puzzle/LightPatternPuzzle.cs`
- `Puzzle/AudioSequencePuzzle.cs`

No public APIs were removed or changed in signature, so existing scenes,
prefabs, and the `TestRoomBuilder` editor tool remain compatible. Unity will
generate `.meta` files for the new scripts on the next editor refresh.

---

## 4. Demo Level & Story Notes (second pass)

### Bug fixed: instant "game over" when entering the test scene
The `TestRoomBuilder` placed a `SanityDrainZone` with default values
(15 entry drain + 10/sec, and no regen below 20 sanity) **directly on the
player's path two meters from spawn** — walking forward killed a fresh
character in seconds. The zone is now in the far corner of Room 1 with gentle
values (5 entry, 4/sec). Rebuild the scene via
*Fractured Echoes → Build Test Scene* to apply.

### New: story note system
- **`Interaction/NoteInteractable.cs`** — a readable note/letter in the world
  (title + body + optional sanity effect on first read).
- **`UI/NoteUI.cs`** — shared self-building full-screen reading overlay
  (parchment panel; closes with E / Esc / click; pauses the game; integrates
  with `UIFocus` so closing a note can't trigger the pause menu or re-open
  the note on the same keypress).
- `UIFocus` gained `ConsumeInteract()` and `InteractionSystem` now ignores
  world interactions while any modal UI is open.

### New: `DemoLevelBuilder` (Editor → *Fractured Echoes → Build Demo Level*)
Builds `Assets/Scenes/DemoLevel.unity` — a linear ~10-minute skeleton
walkthrough of an abandoned apartment building:

1. **Entrance Hall** — spawn, intro letter (story setup), save station.
2. **Living Quarters** — diary note pointing to the Brass Key, Sedative
   pickup (restores 40 sanity via the new `sanityRestore` field), and a
   locked door that consumes the key.
3. **Corridor** — scripted light-flicker event (small sanity hit).
4. **Study** — the candle puzzle (`LightPatternPuzzle`): the journal note
   hints that only the first and third candles may burn. Solving it
   force-unlocks the sealed door and shifts the environment to the
   *Unsettling* phase.
5. **Corridor → Cellar** — ghost-appear event chained into a cellar light
   flicker (sanity hit), final story note, marked "cold spot" sanity drain
   off the main path, save station, END OF DEMO.

Assets are generated under `Assets/Resources/DemoData` and
`Assets/Art/Materials/Demo*`. The scene is added to Build Settings
automatically. `MainMenuSceneBuilder` now points *New Game* / *Load Game*
at `DemoLevel` — rebuild the main menu scene
(*Fractured Echoes → Build Main Menu Scene*) or change `_firstGameScene`
in the existing MainMenu scene's Inspector.
