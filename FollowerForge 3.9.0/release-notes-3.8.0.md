# FollowerForge 3.8.0 (beta)

Rebuild your followers with this version. A plugin that is already built keeps its old values.

## Every follower was written wrong in three places

| Field | Was | Now | Effect of the old value |
| --- | --- | --- | --- |
| Speed multiplier | 0 | 100 | Creation Kit: "Speed multiplier is zero". In game she runs on the spot unless a follower framework resets it |
| Height | 0.0 | 1.0 | Not a value any vanilla NPC carries |
| Level multiplier ("scale with the player") | 16.96 | 1.0 | About seventeen times the player's level |

Measured against Skyrim.esm: 5,105 of its 5,118 NPCs carry speed 100 and a height above zero, and
none carries zero for either. The build now fails if a written plugin holds any of the old values.

## An outdoor follower blanked her worldspace

Her plugin has to carry the worldspace record as the parent of her cell. It carried an empty one:
no name, flags 0, zeroed map data. It now carries the worldspace's own record, without its cells.

## Enemy to Ally, evolution

- Her hostile form never attacked (it copied the follower's Unaggressive setting), showed a default
  face (no FaceGen under its own FormID) and could be placed twice. Fixed.
- Evolution added each phase on top of the last. Starting values are now read once.

## The app

- One damaged plugin no longer empties every list. It is skipped and named.
- Under MO2 the voice and marriage scan read the game's Data folder and missed every mod.
- The window writes a log to `%LOCALAPPDATA%\FollowerForge\logs`.
- Bundled scripts no longer carry the compiler's account and computer name.
- `FollowerForge.Cli.exe` lists all of its commands.

## New: Open follower…

Sidebar, or `Ctrl+K`. Choose the `rebuild-profile.json` in a follower's folder and every page is
filled in again. Choices from mods that are gone are left out and listed; settings the window has no
control for are named before they would be lost.

## Verification

654 Release tests. A follower built against a real 2,944-plugin load order was read back byte by
byte. No in-game check, and no real RaceMenu head export exists on the build machine, so the FaceGen
import path is unverified as before. GitHub issue #2 stays open.
