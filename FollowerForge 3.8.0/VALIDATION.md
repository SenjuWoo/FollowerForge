# 3.8.0 validation

## Baseline before any change

- `dotnet test src/FollowerForge.slnx -c Release` on 3.7.11: 615 passed, 0 failed.
- A follower built by 3.7.11 from `sample-profile`, read back byte by byte:
  `ACBS level=16960 SPEEDMULT=0 dispBase=0`, `NAM6 = 0.0`, `NAM5 = 00 FF`, and a `WRLD 0001A26F`
  record of 57 bytes holding only zeroed `ONAM`, `DATA`, `NAM0`, `NAM9`.
- The suite was green over all of it: no test or gate read those fields.

## Reference values, read from the installed game with houseCARL

- `0A2C8E:Skyrim.esm` (HousecarlWhiterun): `LevelMult = 1`, `SpeedMultiplier = 100`,
  `DispositionBase = 35`, `Height = 1`, `SoundLevel = Normal`, `NAM5 = 255`.
- Skyrim.esm NPCs: 5,118 defined; 5,105 match `NAM5 = 255`, `SpeedMultiplier = 100`, `Height > 0`;
  0 match `SpeedMultiplier = 0`; 0 match `Height = 0`; 0 match `NAM5 = 65280`.
- `01A26F:Skyrim.esm` (WhiterunWorld) as carried by a shipped follower plugin placed there:
  EditorID, `Flags = SmallWorld, NoLodWater, NoGrass`, location, LOD multiplier, max-height data.

## After the fix

- `dotnet test` on 3.8.0: **654 passed, 0 failed, 0 skipped**.
- Each fix had a failing test first: 11 of 13 record tests failed before the change, the four
  script-header tests failed before the scrub, the Enemy-to-Ally and MO2 tests did not compile
  against the old API.
- The same sample follower built by 3.8.0 against the real load order (2,944 enabled plugins):
  `ACBS level=1000 SPEEDMULT=100 dispBase=35`, `NAM6 = 1.0`, `NAM5 = FF 00`, `NAM8 = 1`, and a
  `WRLD 0001A26F` record of 8,046 bytes with `EDID WhiterunWorld`, `FULL`, `XLCN`, `WNAM`, `PNAM`,
  `DATA 89`, `NAM0`/`NAM9`, `MHDT` and the large-reference table; no `OFST`.
  `SHIP_GATE_PASS: HEDR=1.71 numRec=11 formVersion=44 (5 records) ESL=True`.
- Unreadable plugins: a plugin with a record flagged compressed but not compressed, and a
  truncated plugin, are each skipped and named while the healthy plugin beside them is indexed.
- Scripts: `FF_Evolution.psc` compiled with the game's own Papyrus compiler against vanilla sources;
  all four `.pex` decompile cleanly after the header scrub (5, 6, 1 and 1 functions).
- Open follower: a profile using a race, voice, class, cloned combat style, outfit, two armor
  pieces, a weapon, a stacked book, arrows, a spell, a perk, custom stats, a fixed level, a
  relationship, a custom line, evolution, werewolf transformation, a place and two alternate places
  was loaded into the real window headlessly (`UiScreenshots --roundtrip`) on the real catalogue.
  24 of 24 field groups came back equal, and loading the result again changed nothing (0
  differences).
- The window's log file was created and written during that run.

## Package

@PACKAGE@

## NOT RUN

- Skyrim itself. No follower built by this tree has been recruited, walked, fought beside or
  reloaded. That a zero speed multiplier is what made followers run on the spot is consistent with
  the reports and with a framework hiding it, but was not reproduced.
- Whether an empty worldspace override visibly damaged a worldspace in game. The record was wrong
  on its face and is now right; its past effect was not measured.
- The evolution, transformation, random-spawn and summon scripts at run time.
- Real FaceGen import. This machine still has presets and no exported head NIF/DDS pair.
- Open follower by hand in the visible app: the file picker and the button were not clicked.
- MO2 end to end. The MO2 paths are covered by fixture tests only; this machine runs Vortex.
