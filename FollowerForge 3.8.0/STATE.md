# FollowerForge 3.8.0 — followers that can walk, and a follower you can reopen

Parent: candidate 3.7.11 (GitHub pre-release v3.7.11-beta; Latest is v3.7.6). The 3.7.11 tree is
kept unchanged.

## Goal

Audit what a build actually writes, fix what is wrong in it, and let a finished follower be
reopened.

## Done

- NPC record: speed multiplier 100, height 1.0, sound level Normal, disposition 35, NAM5 255. All
  were zero or byte-swapped in every earlier build.
- Level multiplier written once. 1.0 was stored as 16960 (16.96x).
- Outdoor placement carries the worldspace's own record, read from the plugin that defines it.
- Validator: `NPC_SPEED_ZERO`, `NPC_HEIGHT_ZERO`, `LEVEL_MULT_MISMATCH` errors;
  `WORLDSPACE_OVERRIDE_BLANK` warning.
- Enemy to Ally: aggressive hostile form, FaceGen mirrored to her FormID, Run Once quest.
- `FF_Evolution` reads starting values once; recompiled. All four `.pex` headers scrubbed of the
  compiler's account and machine name (`tools\Scrub-PexHeader.ps1`, `PexHeaderTests`).
- Catalogue index skips and names a plugin it cannot read (`LoadOrderBuilder.ImportTolerant`,
  `RecordIndexer.Unreadable`, catalogue meta `unreadable_plugins`).
- MO2 voice and marriage scan reads the plugin view (`VoiceCoverageScanner.Scan(env)`).
- The window logs to `%LOCALAPPDATA%\FollowerForge\logs\ui-*.log`.
- Open follower… (`FollowerReopen`, `WizardWindow.OpenFollower`).
- CLI usage lists every command.

## Verification

See VALIDATION.md. 654 Release tests; a real-load-order build read back byte by byte; a headless
round trip of Open follower on the real catalogue. No in-game check.

## Build and run

```powershell
.\Build-FollowerForge.ps1                    # clean, build, test, CLI boot
.\Publish-FollowerForge.ps1 -Version 3.8.0   # tests, self-contained zip, boot check, zip verified
```

Logs: `%LOCALAPPDATA%\FollowerForge\logs` (`ui-*.log` window, `fforge-*.log` command line).

## Next

1. Owner: one in-game pass with a follower built by 3.8.0 — recruit, walk, check her level against
   yours, fight, change cells, save and reload. Then promote the pre-release to Latest.
2. Owner: Nexus upload (`NEXUS-CHANGELOG-3.8.0.txt`).
3. Open: report files at the package root share one path across followers, which Vortex reports as
   a file conflict between any two of them.
4. Open: GitHub issue #2 (hair below the head); RaceMenu overlays; a real head export to test with.
