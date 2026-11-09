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

## Not tested in Skyrim

No gameplay session was run, and the owner's profile and saves were not touched.

