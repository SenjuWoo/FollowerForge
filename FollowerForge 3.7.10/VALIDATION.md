# 3.7.10 group bounds and texture severity validation

## What was found

1. `Walk(start, end)` looped `while (r + 24 <= end)`, so 1-23 stray bytes inside a group ended the
   loop without a complaint and the file still passed the gate. Nested groups had the same hole.
2. A TES4 header claiming more bytes than the file holds made `CountMasters` read past the buffer
   and throw; the validator crashed instead of reporting.
3. `FaceGenSwapper` deduplicated texture paths with a `HashSet` before deciding required/optional, so
   a path first seen in an optional slot never produced the required warning even when a diffuse
   slot used it.

## What was checked after the fix

- `dotnet test src/FollowerForge.slnx -c Release`: **609 passed, 0 failed, 0 skipped**, three
  consecutive runs.
- New regression tests were run against the previous implementation first and failed there:
  `Junk_inside_a_group_is_rejected` (1, 7 and 23 bytes), `Junk_inside_a_nested_group_is_rejected`
  and `A_path_in_an_optional_slot_first_and_a_diffuse_slot_later_is_required`.
- Preserved and still passing: tint restricted to slot 6, other NIF paths untouched, geometry
  round-trip, four FaceGen statuses, partial inventory labels, head-part race resolution, HEDR 1.71
  / formVersion 44 / ESPFE checks.

## Build and package (local, not published)
- First publish attempt failed at the zip step with `PermissionDenied` on `FollowerForge.exe`; no
  artifact was produced. The second attempt, with no FollowerForge process running, reported
  `PUBLISH SUCCEEDED` and "window stayed up".
- Zip `FollowerForge-3.7.10-win-x64.zip`, **99416514 bytes**, SHA-256
  `dfbc2c075caf36e41f79d1864e3ad8184ed3493bd51738366d8c4094a700e7b3`, measured after the publish process exited. Five entries: `FollowerForge.exe`,
  `cli\FollowerForge.Cli.exe`, `README.md` (10707 bytes, 609 tests), `CHANGELOG.txt`,
  `NEXUS-CHANGELOG-3.7.10.txt`.
- Both executables report FileVersion `3.7.10.0` and ProductVersion `3.7.10+55e53f16518893ca401bd4cdc744528435709930`.
- Packaged GUI launched, `Responding = True` after 14 s, then closed.
- CI on `55e53f1`: `xunit suite (latest snapshot)`, `Analyze (csharp)` and `Analyze (actions)` all
  succeeded.

## Live load-order acceptance (packaged CLI)
- `FF_AuditMatch` and `FF_AuditVamp`: no head-part warnings, `SHIP_GATE_PASS` with
  `HEDR=1.71 numRec=13 formVersion=44 (6 records) ESL=True`, `FaceGen : default`.
- `FF_AuditMismatch` (Nord built from a KaPoTun preset): five `PRESET_HEADPART_OTHER_RACE` warnings.
- Generated plugin parsed independently: HEDR 1.71, numRecords 13, ESL flag set, four masters
  (`Skyrim.esm`, `Update.esm`, `Dragonborn.esm`, `DWKaPoTunRace.esp`), records `NPC_`, `WRLD`,
  `CELL`, `ACHR`, `RELA`, `CLFM`, every record formVersion 44, zero trailing bytes.
- FaceGen live conversion: **NOT RUN** — 0 CharGen head exports on this machine.

## Not tested in Skyrim

No gameplay session. Unverified: hair, eyes, brows and face geometry; face and body tint; hire and
dismissal; cell transitions; save/reload; combat and transformation. This machine has **0 CharGen
head exports**, so no real RaceMenu export was converted and the live FaceGen test reported NOT RUN.

