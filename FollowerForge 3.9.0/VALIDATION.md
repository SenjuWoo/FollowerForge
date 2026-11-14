# 3.9.0 validation

## Baseline

- 3.8.0 tree: 654 passed. Dependabot history: only GitHub Actions PRs (#3, #4); no NuGet PR ever,
  because `.github/dependabot.yml` pointed `nuget` at `/`, which holds no project.
- A 3.8.0 build's package root held `FF_X.esp`, `manifest.json`, `source-assets.json`,
  `dependency-report.json`, `rebuild-profile.json`, `build-report.html`, `credits.md`,
  `SHARE-CHECKLIST.txt`, `test-in-game.txt`: nine files, eight of them named identically for every
  follower.

## Suite

- `dotnet test src/FollowerForge.slnx -c Release`: **674 passed, 0 failed, 0 skipped** on xunit v3.
- `dotnet build` (Release, whole solution): **0 warnings, 0 errors**.
- New tests: package root holds only the plugin; two followers share no path; library scan (new and
  legacy layouts, non-follower folders); batch loads a builds folder; rebuild in place replaces an
  old build and removes its root reports; pack (union, legacy relocation, collision refused with
  nothing written, folder with no plugin refused); repair (all fixes applied to a copy, records,
  IDs, masters and ESL flag unchanged; a current plugin is left alone; unreadable worldspace
  reported; refuses to overwrite the original; level-multiplier inversion for 1.0, 0.75, 0.5);
  Open follower finds the new layout and refuses to guess in a pack folder.

## End to end, real load order (Vortex, 2,937 enabled plugins)

- A follower built by the **3.7.11** CLI: `ACBS level=16960 SPEEDMULT=0 dispBase=0`, `NAM6 0.0`,
  `NAM5 00ff`, `NAM8 00000000`, 57-byte WRLD.
- `list` marked it `[REBUILD]`.
- `repair --plugin` on a copy: speed 0 -> 100, height 0 -> 1.0, disposition/sound/NAM5 set, level
  multiplier 16.96 -> 1, worldspace replaced from Skyrim.esm. Read back: `level=1000 SPEEDMULT=100
  dispBase=35`, `NAM6 1.0`, `NAM5 ff00`, `NAM8 01000000`, 8,046-byte WRLD with `EDID WhiterunWorld`.
  `verify --plugin`: `SHIP_GATE_PASS`, `PLUGIN OK`. The input file compared byte-identical afterwards.
- `rebuild --only-needed`: rebuilt it in its own folder (`Rebuilt 1/1`); `list` then showed `[ok]`;
  the folder holds the plugin at the root and eight reports under `FollowerForge\FF_AriaForge\`.
- `pack --builds` with a second follower: 19 entries, forward-slash paths, each follower's reports in
  its own folder, a contents file, no shared path.

## Dependencies

- Microsoft.Data.Sqlite 11.0.0 resolves SQLitePCLRaw 3.0.5 throughout, with the native library pinned at
  3.53.3; no 2.x package remains in the graph.
- On that stack: `races` and `search` read the existing 188 MB catalogue, and `index` rebuilt a fresh
  one from the real load order — 357,061 records, 855,613 assets, 3,017 plugins, 58 s, nothing skipped.
- After the 3.9.0 push Dependabot ran against the NuGet projects for the first time and opened PRs
  #6-#10 (the SQLite and System.Drawing majors); those versions are what this tree now uses.

## Window

- Headless render (`tools/UiScreenshots`) of all five themes at 1320x900, 2560x1440 and the 1040x700
  minimum: header reads `VERSION 3.9.0`; the Studio "Your followers" card shows with four buttons;
  the sidebar Open/Save pair fits at 256 px. Review page unchanged.

## Package

@PACKAGE@

## NOT RUN

- Skyrim. No follower built, rebuilt or repaired by this tree has been loaded in game.
- The choosers, file and folder pickers and new buttons by hand in the visible app.
- RSV itself: `RSVexclude.ini`'s expected name and location are as earlier versions wrote them,
  unverified because the mod is not installed here.
- MO2 end to end (fixture tests only; this machine runs Vortex).
