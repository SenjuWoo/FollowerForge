# FollowerForge — Nexus release kit

| | |
|--|--|
| **Version** | 3.8.0 |
| **Upload** | `FollowerForge-3.8.0-win-x64.zip` (5 files, self-contained) |
| **Size** | 99455604 bytes |
| **SHA-256** | `1c52bda2ae7f7b7f0b487e1961d2a4f46a168853100a8218f093e3a6bb458690` |
| **Binary revision** | `9e203441dff837f84a86a7fce597f385356f1bca` |
| **GitHub** | https://github.com/SenjuWoo/FollowerForge/releases/tag/v3.8.0-beta (Latest) |

This is an out-of-game Windows tool. It is **not** a Skyrim mod: do not install it into `Data`,
Vortex or MO2. Only the follower package it generates is installed as a mod.

v3.8.0-beta is GitHub Latest (the tag keeps its original name). It was promoted by the owner on
2026-11-13; no in-game testing had been performed at that point.

## Page copy

- **Changelog field:** paste `FollowerForge 3.8.0/NEXUS-CHANGELOG-3.8.0.txt`.
- **Description field:** `NEXUS-DESCRIPTION-3.7.7.txt` is still the latest prepared text. Add one
  line near the top: followers built with 3.7.11 or earlier should be rebuilt with 3.8.0.
- Worth a sticky post: the speed-multiplier fix answers the "runs on the spot", "Speed multiplier is
  zero" and "moves very slowly" reports in the comments.

## Same-hash check before uploading

```powershell
(Get-FileHash 'artifacts\FollowerForge-3.8.0-win-x64.zip' -Algorithm SHA256).Hash
# expect 1C52BDA2AE7F7B7F0B487E1961D2A4F46A168853100A8218F093E3A6BB458690
```

## Notes

- Rebuild a follower to pick up the fixes. A plugin already built keeps its old values.
- Nothing about appearance was verified in game. GitHub issue #2 (displaced hair) stays open.
