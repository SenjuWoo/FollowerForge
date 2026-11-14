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
