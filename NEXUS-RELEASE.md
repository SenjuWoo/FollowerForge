# FollowerForge — Nexus release kit

| | |
|--|--|
| **Version** | 3.7.6 |
| **Upload** | `FollowerForge-3.7.6-win-x64.zip` (5 files, self-contained) |
| **Size** | 99403591 bytes |
| **SHA-256** | `77e511b026cd23d6b18d1314d188008aa2916f2d523a9c142f89402725536170` |
| **GitHub** | https://github.com/SenjuWoo/FollowerForge/releases/tag/v3.7.6 |

This is an out-of-game Windows tool. It is **not** a Skyrim mod: do not install it into `Data`,
Vortex or MO2. Only the follower package it generates is installed as a mod.

## Short changelog (paste)

```
3.7.6
- A follower built from the default profile landed at the world origin instead of the Whiterun
  plaza. Fixed: a named Whiterun cell with no coordinates now means the plaza. Coordinates you
  set yourself are unchanged.
- The race warning added in 3.7.5 fired on the correct race when a race mod's vampire or climate
  variant shares its head. Fixed: races from the same plugin are one family now.
- The README inside the download said 3.7.4. It ships the current one.

3.7.5
- Every build now lists the head parts it writes, by slot, FormID and EditorID, and warns when a
  part is restricted to another race, or when a preset from one race is built as another. That is
  the mechanism behind hair that sits low or oversized on a custom follower.
- The RaceMenu preset is read once per build instead of twice.
```

## Notes

- Build a follower again to pick up the placement fix. A plugin already built keeps its old
  coordinates.
- In-game placement of the new default has not been checked by the author; it is verified at the
  record level only.
