# FollowerForge 3.9.0 — your followers in one place

Parent: 3.8.0 (GitHub Latest, tag v3.8.0-beta, commit 9e20344). The 3.8.0 tree is kept unchanged.

## Goal

Make the followers a user already built easy to find, change, rebuild, share and fix, and stop
follower packages from writing into the root of Data.

## Done

- Package layout: reports in `FollowerForge\<plugin>\` (`Domain/PackageLayout.cs`); only the plugin,
  FaceGen folders and RSVexclude.ini at the root. Legacy root profiles still found.
- `BuildPipeline/FollowerLibrary.cs`: finds built followers in the output folders, flags the ones a
  pre-3.8.0 build wrote (speed or height 0, read from the plugin).
- `BatchBuilder.Rebuild`: rebuilds in place; `LoadProfiles` accepts a builds folder.
- `BuildPipeline/FollowerPackager.cs`: multi-follower zip; moves legacy root reports, merges
  RSVexclude.ini, refuses real collisions, writes atomically.
- `BuildPipeline/PluginRepairer.cs`: profile-free repair of pre-3.8.0 plugins into a copy.
- CLI: `list`, `rebuild`, `pack`, `repair`.
- Window: Studio "Your followers" card, library chooser behind Open, Save draft, Rebuild / Pack /
  Repair, startup rebuild notice, real version in the header (`WizardWindow.Tools.cs`).
- Manifest `GeneratorVersion`; `<Version>` in Directory.Build.props.
- Packages updated; tests on xunit.v3 (mtp-off keeps the VSTest route); 0 warnings.
- Dependabot points at this snapshot; CI checks dependabot.yml and CURRENT.txt name it.
- Publish writes zip entries with forward slashes.

## Verification

See VALIDATION.md. 674 Release tests; end-to-end CLI runs on the real load order; headless renders
of all five themes. No in-game check.

## Build and run

```powershell
.\Build-FollowerForge.ps1                    # clean, build, test, CLI boot
.\Publish-FollowerForge.ps1 -Version 3.9.0   # tests, self-contained zip, boot check, zip verified
```

Logs: `%LOCALAPPDATA%\FollowerForge\logs`. Builds: `%LOCALAPPDATA%\FollowerForge\workspace\builds`
unless Paths… names another folder. Drafts: `...\workspace\drafts`. Repaired plugins:
`...\workspace\repaired`.

## On the next version bump

Copy the snapshot, update `CURRENT.txt`, and change both paths in `.github/dependabot.yml`. CI fails
until the last two name the new folder.

## Next

1. Owner: one in-game pass — recruit, walk, level, fight, change cells, save and reload — with a
   follower built or rebuilt by 3.9.0.
2. Owner: Nexus upload (`NEXUS-CHANGELOG-3.9.0.txt`).
3. Open: GitHub issue #2 (hair below the head), RaceMenu overlays, black face with custom warpaint;
   all need a real head export to work from.
4. Unverified: RSVexclude.ini's file name and location for the RSV mod (not installed here).

## Resume here (2026-11-14, session cut by usage limit)

- Pushed: `3ee2811` (3.9.0 features) and `0104a8d` (SQLite 11 / SQLitePCLRaw 3 / System.Drawing 11).
  CI is green on every check for both commits (xunit, CodeQL C# and Actions, Dependabot), with 0 open
  Dependabot, code-scanning and secret-scanning alerts.
- NOT released: no v3.9.0 tag or GitHub release yet. GitHub Latest is still 3.8.0.
- A republish from `0104a8d` stopped at its test step with "tests failed (1)"; the failing test's name
  was lost. A rerun straight after passed 674/674. Treat it as a possible intermittent failure: run
  the suite several times before publishing again.
- Local, uncommitted: `release-notes-3.9.0.md` now says "2,937 enabled plugins".
- Dependabot PRs #6-#10 propose the versions `0104a8d` already uses; Dependabot should close them
  on its next run.
- New request, not started: more spawn locations such as farms. Diagnosis: outdoor grid cells are
  marked not placeable (`SpawnLocation.Placeable`), and base-game plugins are skipped by
  `LocationScanner.IsBaseGame`. Plan: place grid spots via the worldspace's persistent cell at their
  coordinates; harvest named map markers (`PlacedObject.MapMarker`: Name, Type) and door arrival
  points (`PlacedObject.TeleportDestination`: Door, Position, Rotation) into named interiors; tag
  places by LocType keyword or marker type; version the library so it rebuilds itself.
- Then: rerun the suite several times, republish, check CI on the final commit, tag v3.9.0, create the
  release, verify the asset hash, fill VALIDATION.md's @PACKAGE@ block and the Nexus kit.
- At release: drop the "3.9.0 not released yet" notes from the root README.md (Quick start),
  docs/STATUS.md (Release status) and docs/NOTES-BY-VERSION.md (3.9.0 heading).
