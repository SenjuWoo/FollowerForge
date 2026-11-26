# What is verified, and what is not

This page holds the detailed status that used to live in the README. It describes the
`FollowerForge 3.9.0/` source tree (the snapshot named in `CURRENT.txt`). The full evidence
for each line is in that folder's `VALIDATION.md` and `STATE.md`.

## Release status

- GitHub Latest is **3.8.0** (tag `v3.8.0-beta`, `FollowerForge-3.8.0-win-x64.zip`, 99,455,604 bytes).
- **3.9.0 is not released yet.** Its features (Your followers, packs, repair, drafts) are in the
  source tree and pass the test suite; build from source to use them before the release.

## Verified in this tree

- `FollowerForge 3.9.0/` is the ship snapshot (`CURRENT.txt`)
- 674 Release tests pass on the 3.9.0 tree, with 0 build warnings; CI runs the same suite on every push
- Two followers share no file path; only the plugin sits at a package's root
- A follower built by 3.7.11 was found, marked as needing a rebuild, rebuilt in place and repaired from its plugin alone, against a real load order; the repaired plugin passes the ship gate and its original was left untouched
- Two followers were packed into one zip with no shared path
- A built NPC carries speed multiplier 100, height 1.0 and the level multiplier that was asked for, read back from the written bytes. The build fails if it does not
- An outdoor follower's plugin carries her worldspace's own record, read from the plugin that defines it, not an empty one
- A damaged plugin is skipped and named; the rest of the load order is still indexed
- A built follower's `rebuild-profile.json` loads back into the window. A headless run on a real load order rebuilt the same profile from it
- A named Whiterun cell with no coordinates places her on the plaza; coordinates a profile sets are used as given
- Every build lists the head parts it writes, by slot, FormID and EditorID
- A head part restricted to another race is named, and an unrestricted hair on a preset from another race is named with its mesh file. Variants of her own race, from the same plugin, do not warn
- A preset with two parts for one slot, or with no Hair part, is flagged
- A head part whose plugin is missing, or which that plugin no longer holds, is named instead of being skipped
- The face tint is written to shader slot 6 and nowhere else; a CharGen path in another slot is never rewritten
- The ship gate rejects a FormID naming an undeclared master, a record that runs past its group, bytes after the last group, and a group that does not end exactly at its declared size
- A FaceGen result says complete, partial, needs manual repair or failed. A copied mesh with an unrewritten tint path is partial, not success, and every surface reads the same state
- A short head texture list is grown to nine slots and the tint is written on slot 6. An existing specular map on slot 7 is left alone
- Head meshes that live only under CharGen `Exported` or `Presets` are listed

## Not claimed

- In-game confirmation of the placement, the tint, the face list, or the head-part audit. Rebuild to pick up the placement fix; an already-built plugin keeps its old coordinates
- That every "she runs on the spot" or "she is gone after a door" report had this cause. A follower parked at the world origin is consistent with them
- That GitHub issue #2's reporter used a custom-race preset. The mechanism is proven on a preset that does; the audit is what makes the next report decidable
- That FollowerForge moves or repairs a head part. It writes the preset's parts as given and says when they cannot fit
- That a speed multiplier of 0 explains every "she runs on the spot" report. It is what the plugins held, and it is fixed; the reports were not reproduced in game
- The Enemy to Ally, evolution and transformation scripts in a real session
- Nobody has recruited a follower built by this tree in game
- The window's choosers, file pickers and new buttons were rendered headlessly but not clicked by hand
- A FaceGen conversion that finishes on its own. Nothing here was checked in game, and the build machine has no head export to convert
- A creature transform surviving a real fight. `SetRace()` is engine behaviour this build cannot exercise
- That a RaceMenu body shape or overlay transfers into the plugin. They do not. The build warns when shaped `bodyMorphs` are present
- A Fallout 4 port
