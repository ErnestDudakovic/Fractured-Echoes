# Fractured Echoes — Setup

Unity **6000.3.11f1**, Built-in Render Pipeline + Post Processing Stack v2.

> This is a separate Unity project from the HDRP version on the `main` branch.
> The two use different render pipelines and cannot be merged — opening this
> project's assets under HDRP turns every material magenta.

## Required Asset Store packages

These are excluded from the repository (about 3.6 GB combined, over GitHub's
limits). Import them into the project before opening the scenes. The `.meta`
files ship with each package, so every reference in the scenes reconnects on
its own — no manual rewiring.

| Package | Folder it creates |
|---|---|
| Flooded Grounds | `Assets/Flooded_Grounds/` |
| Enemy character pack | `Assets/Enemies/` |
| Unity Standard Assets | `Assets/Standard Assets/` |
| Horror Elements (SFX) | `Assets/Horror Elements/` |
| Baseball Bats | `Assets/Baseball Bats/` |
| Survival Kit Lite | `Assets/Survival Kit Lite/` |
| Horror Axe | `Assets/Horror_Axe/` |
| FPS Arms | `Assets/FPS arms/` |
| Nokobot weapons | `Assets/Nokobot/` |

The terrain (`Assets/Flooded_Grounds/Scenes/Scene_A_Terrain.asset`) IS committed,
because it holds the sculpted level rather than stock package content.

## First run

1. Import the packages above.
2. Open the project. Let it compile.
3. Check **Edit → Project Settings → Tags and Layers** — the tags
   `Apple, Battery, Knife, Axe, Bat, Gun, Crossbow, CabinKey, HouseKey, RoomKey,
   Ammo, Arrows, Door, Enemy, Note, PKnife, PBat, PAxe, PCrossbow` must exist.
4. Run **Tools → Fractured Echoes → Setup Build Scenes** to fill the build list.
5. Open `Assets/Scenes/HorrorScene.unity`.
6. Run **Tools → Fractured Echoes → Build Horror Setup**.

## Build scene order

| Index | Scene |
|---|---|
| 0 | GameCompany (logo) |
| 1 | Main Menu |
| 2 | Opening Cut Scene |
| 3 | HorrorScene |
| 4 | Game Victory |

## Editor tools

All under **Tools → Fractured Echoes**:

- **Setup Build Scenes** — rebuilds the build list in the order the scripts expect
- **Build Horror Setup** — creates sanity zones, notes, phantoms and the escape gate under a `HORROR SETUP` root. Keeps anything you moved by hand
- **Build Horror Setup (RESET positions)** — same, but discards manual placement
- **Capture Note Positions** — copies the current note coordinates to the clipboard
- **Preview / Remove Weapons From Scene** — lists or deletes weapon and ammo props

## Game design

No-combat psychological horror. The player has a flashlight and no weapon.

- **Sanity** drains only inside designated `SanityZone` volumes and while an
  enemy is actively chasing. It recovers elsewhere. Hitting zero is death.
- **Enemies** lose interest after 8 seconds and ignore the player for 25 seconds
  after that. Their chase speed is capped below the player's run speed.
- **Phantoms** are apparitions with no AI and no colliders. They vanish when the
  player looks away.
- **Ten notes** carry the story and point to each objective.
- **Three keys**: House Key (church steps) → Main House; Cabin Key (Main House
  upper floor) → west cabins; Room Key (flooded building) → the street gate.
- **The escape**: taking the Room Key starts a permanent hunt. Sanity drains
  everywhere, enemies never give up and move faster. Reach the street gate.

Weapons are gated behind `GameRules.WeaponsEnabled`. Set it to `true` to restore
the original combat build.
