# Fractured Echoes — Notes (clue chain)

All ten notes are already written into `Assets/Editor/HorrorSetupBuilder.cs` and get
placed automatically by **Tools → Fractured Echoes → Build Horror Setup**.
This file is the reference for what each one does and where it belongs.

Each note points at the next objective, so a player who reads them in order never
has to guess where to go. A player who skips them can still find everything, it
just takes longer.

---

| # | Title | Where it goes | Coordinates | Clue it gives |
|---|---|---|---|---|
| 01 | Intake Form — Room 4 | Lighthouse, at the start | 719, 21.4, 268 | Go north up the road to the lit crossroads |
| 02 | Maintenance Log | Under the crossroads lamps | 624, 27.6, 428 | Light restores sanity, grab every battery, the big house is locked and Reception holds the key |
| 03 | Letter, Unsent | East house, beside the **House Key** | 650, 19.1, 509 | The cabin key is upstairs in the big house |
| 04 | Key Log | Outside the locked Main House door | 494, 22, 506 | Which key opens what, and a warning about the landing |
| 05 | Incident Report | Main House landing, beside the **Cabin Key** | 529, 28.3, 543 | Cabins are west past the bridge, one key opens both |
| 06 | Torn Page | Inside a west cabin | 445, 18.9, 512 | The room key went east, to the building standing in the flood |
| 07 | Session Transcript | On the road east, in the Whispers zone | 612, 19.6, 478 | Phantoms lose interest — keep moving, don't stop and stare |
| 08 | A Note to Himself | Flooded building, beside the **Room Key** | 814, 26.9, 512 | Two doors take this key; only the far house to the north matters |
| 09 | Discharge Papers | Main House upper floor, at the Room door | 480, 27.4, 516 | The decoy room. Confirms the real one is north |
| 10 | The Last One | Far house, top floor | 390, 33, 685 | The ending |

---

## The route the notes create

```
Lighthouse (start)          01
      |  north up the road
Lit crossroads              02          safe zone, batteries
      |  east
Reception / east house      03    ->  HOUSE KEY   (648, 18, 511)
      |  west across the road
Main House door             04          locked, needs house key
      |  upstairs
Main House landing          05    ->  CABIN KEY   (531, 27, 542)
      |  west past the bridge
Cabin row                   06          two locked cabins
      |  all the way back east
Whispers street             07          warning beat
      |
Flooded building            08    ->  ROOM KEY    (816, 26, 510)
      |  back west, optional detour
Main House upper room       09          decoy
      |  north past the cabins
Far house, top floor        10          ENDING
```

That is roughly 25–30 minutes if the player actually reads and doesn't sprint.

---

## Placing them by hand

The Y values are estimates taken from nearby objects in the scene, so some notes
will float or sink into a floor. Fixing that:

1. Run **Tools → Fractured Echoes → Build Horror Setup**
2. In the Hierarchy open `HORROR SETUP / Notes` — they are listed in order, `Note 01` to `Note 10`
3. Click one, hover the Scene view and press **F** to fly to it
4. Move it with the move tool onto a desk, floor, shelf or crate
5. When they all sit right, run **Tools → Fractured Echoes → Capture Note Positions**

That last step prints the corrected coordinates to the Console and copies them to
the clipboard. Paste them back into `BuildNotes` in the builder (or send them
over) so the next rebuild keeps them.

**Important:** re-running Build Horror Setup deletes and recreates everything.
Capture your positions before rebuilding, or the manual placement is lost.
