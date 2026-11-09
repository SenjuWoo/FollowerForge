# 3.7.11 release-engineering validation

## What was found

1. The publish script reached `PUBLISH SUCCEEDED` after `Compress-Archive` failed on a locked
   `FollowerForge.exe`, and the caller's pipeline hid the exit status. Nothing in the script checked
   that the archive it wrote was complete.
2. There was no way to gate a plugin from a release archive — only from the working tree.
3. The unrestricted-hair warning described the symptom without naming the race the mesh was built
   for, which is the part a user can act on.

## What was checked after the fix

- `dotnet test src/FollowerForge.slnx -c Release`: **615 passed, 0 failed, 0 skipped**.
- `verify --plugin` run against a plugin extracted from a follower package zip:
  `SHIP_GATE_PASS: HEDR=1.71 numRec=13 formVersion=44 (6 records) ESL=True`, `PLUGIN OK`, exit 0.
- Publish now reports entry count, byte size and SHA-256 itself, and fails non-zero when the archive
  is short a file.
- Real records behind the hair warning, read with houseCARL:
  `0009F1:DWKaPoTunRace.esp` **FemaleHairKapotun01** has `ValidRaces = null link` (unrestricted) and
  its mesh is `actors\character\DWRaces\TigerKhajiit\Hair\FemaleHairKapotun01.nif`;
  `0009A8:DWKaPoTunRace.esp` **FemalePiercingsKapotun** is restricted to `000800:DWKaPoTunRace.esp`.

## Real FaceGen import: NOT RUN

This machine has 7 RaceMenu `.jslot` presets and **0 exported head NIF/DDS pairs** anywhere in
`Data` or the Vortex staging folder. No genuine export exists to convert, so:

- Real export discovery, conversion, NIF round-trip and tint write against a RaceMenu head: **NOT RUN**.
- Hair placement in Skyrim, tint, eyes, brows: **NOT RUN**.
- Recruitment, dismissal, cell transitions, save/reload, combat: **NOT RUN**.

A synthetic mesh is not a substitute for any of these and none is claimed.

## Build and package (local, not published)
- `Publish-FollowerForge.ps1 -Version 3.7.11`, run **without a pipeline**, so its exit status is the
  script's own: **PUBLISH_EXIT=0**. It closed no stray processes, reported "window stayed up",
  reopened the archive, verified 5 entries (FollowerForge.exe, cli\FollowerForge.Cli.exe, README.md,
  CHANGELOG.txt present and non-empty) and printed its own size and hash.
- Zip `FollowerForge-3.7.11-win-x64.zip`, **99417448 bytes**, 579b4466b3001e7d16fbf4890af5130c6b715f1b7d511d538c1fa3789da454fa-256 `579b4466b3001e7d16fbf4890af5130c6b715f1b7d511d538c1fa3789da454fa`. The script's figure and an
  independent `sha256sum` agree.
- Earlier attempts in this pass failed and produced **no artifact**: one hit PermissionDenied on a
  locked FollowerForge.exe at the zip step, one failed on a flaky test. Both are why the script now
  waits for the boot-check process and why the build tests are serialised.
- Both executables report FileVersion `3.7.11.0` and ProductVersion
  `3.7.11+986b62562c71aaa39ebfa6344c6dcf41b632a9fa`.
- Packaged GUI launched, `Responding = True` after 14 s, then closed.
- `cli\FollowerForge.Cli.exe verify --plugin` run **from the packaged build** against a plugin
  extracted from a follower package zip: SHIP_GATE_PASS (HEDR=1.71, 13 records, formVersion 44,
  ESL), PLUGIN OK, exit 0.

## Test-suite reliability
Three test classes still used fixed temp roots and the build-heavy classes ran in parallel; one
random failure appeared every few runs (different test each time), which made the publish gate
unreliable. Roots are now per-instance and the build classes share a non-parallel xUnit collection:
**eight consecutive clean runs**.

## Real FaceGen import: NOT RUN
This machine holds 7 RaceMenu .jslot presets and **0 exported head NIF/DDS pairs** anywhere under
Data or the Vortex staging folder: SKSE/Plugins/CharGen holds only Presets and Tags, with no .nif or
.dds. No genuine export exists, so discovery, conversion, NIF round-trip, tint write and the Vortex
install path against a real head are all **NOT RUN**.

## Issue #2 evidence (records, not gameplay)
Read with houseCARL from the live load order:
- `0009F1:DWKaPoTunRace.esp` **FemaleHairKapotun01**: ValidRaces = **null link**; mesh
  `actors\character\DWRaces\TigerKhajiit\Hair\FemaleHairKapotun01.nif`.
- `0009A8:DWKaPoTunRace.esp` **FemalePiercingsKapotun**: ValidRaces = `000800:DWKaPoTunRace.esp`.
An unrestricted hair is applied to every race, so on a Nord follower a TigerKhajiit mesh is used
against a Nord head. 3.7.11 names that race in the warning. The issue stays **open**: nothing here
moves a mesh, and the reporter's preset has never been reproduced.

## Not tested in Skyrim

No gameplay session was run, and the owner's profile and saves were not touched.

