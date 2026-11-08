# FollowerForge — Nexus release kit

| | |
|--|--|
| **Version** | 3.7.7 — candidate, not published |
| **Upload** | `FollowerForge-3.7.7-win-x64.zip` (5 files, self-contained) |
| **Size** | 99408491 bytes |
| **SHA-256** | `fe917650b311f9165395be160b0f4b75ea3ea5a2ec00dfbcfd7260e62644a6e8` |
| **Binary revision** | `1bfbe95f68600b9979beb387d808066807276de6` |
| **GitHub** | https://github.com/SenjuWoo/FollowerForge/releases — tag `v3.7.7` is created on approval |

This is an out-of-game Windows tool. It is **not** a Skyrim mod: do not install it into `Data`,
Vortex or MO2. Only the follower package it generates is installed as a mod.

The published GitHub release is still **v3.7.6**. CI passed on the 3.7.7 source commit `1bfbe95`
(xunit suite, Analyze (csharp), Analyze (actions)). No in-game testing was performed.

## Page copy

- **Description field:** paste `NEXUS-DESCRIPTION-3.7.7.txt`. It replaces the older page text that
  told people to download `FollowerForge-3.6.0-win-x64.zip`.
- **Changelog field:** paste `FollowerForge 3.7.7/NEXUS-CHANGELOG-3.7.7.txt`.

## Short changelog (paste)

```
3.7.7
- The head-part race check read the wrong kind of record: a head part names a race LIST, not a
  race, so the check could never match and reported almost every restricted part as belonging to
  another race. It now reads the list, tests membership, and names the list and the races in it.
- The list is read from the plugin its own record ID names, so a shared-lists plugin or a master
  works. An override by a later plugin is used when the app has an index for the run.
- A list that cannot be read is reported as unknown instead of guessed.
- Vampires are judged against the vampire race they actually carry.

3.7.6
- A follower built from the default profile landed at the world origin instead of the Whiterun
  plaza. Fixed: a named Whiterun cell with no coordinates now means the plaza. Coordinates you
  set yourself are unchanged.

3.7.5
- Every build lists the head parts it writes, by slot, FormID and EditorID.
- The RaceMenu preset is read once per build instead of twice.
```

## Notes

- Diagnostics only. This does not repair misplaced hair, black faces or warpaint, and nothing about
  appearance was verified in game.
- Build a follower again to pick up the 3.7.6 placement fix. A plugin already built keeps its old
  coordinates.
- GitHub issue #2 (displaced hair) stays open. This release makes the next report decidable; it does
  not fix the reporter's hair.
