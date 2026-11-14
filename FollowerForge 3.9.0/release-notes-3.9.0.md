# FollowerForge 3.9.0

Everything you already built, in one place, and follower packages that no longer clash.

## Your followers

A new card on the Studio page, and the same actions in `Ctrl+K`:

| Action | What it does |
| --- | --- |
| **Open a follower…** | Lists every follower you have built and every saved draft, newest first. Pick one and keep editing. |
| **Rebuild followers…** | Rebuilds them with this version, each into the folder it is in now: the ones that need it, all, or one. |
| **Make a follower pack…** | Several followers in one zip that installs as a single mod. |
| **Repair a plugin…** | Fixes a plugin an older FollowerForge built when its profile is gone. Writes a fixed copy; the original is not changed. |
| **Save draft…** | Saves a follower without building her. |

Every follower built before 3.8.0 carries a speed multiplier of 0, a height of 0 and a 16.96x level
multiplier. The window says how many of yours do when it starts; **Rebuild followers…** fixes them.

Command line: `list`, `rebuild [--only-needed] [--zip]`, `pack --out FILE.zip (--follower DIR … | --builds DIR)`,
`repair --plugin FILE`.

## Packages no longer write into the root of Data

Each package put eight reports at its root, so every follower installed `manifest.json`,
`credits.md`, `rebuild-profile.json` and the rest into the root of Data under the same names. Vortex
and MO2 reported any two followers as conflicting. They now install to `Data\FollowerForge\<plugin>\`.

## Also

- The header shows the real version; manifests record it.
- Dependabot now updates NuGet packages (its config pointed at a folder with no projects), and CI
  fails if a version bump leaves it behind.
- Avalonia 12.1.6, Mutagen 0.54.4, Microsoft.Data.Sqlite 11.0.0 with SQLitePCLRaw 3 (SQLite 3.53.3),
  System.Drawing.Common 11.0.0; tests on xunit v3; 0 build warnings.
- The release zip uses forward slashes in its paths.

## Verification

674 Release tests. Against a real 2,944-plugin load order: a follower built by 3.7.11 was found,
marked as needing a rebuild, rebuilt in place, and separately repaired from its plugin alone (speed
100, height 1.0, level multiplier 1.0, full WhiterunWorld record, ship gate passed, original
untouched); two followers were packed with no shared path. The window was rendered headlessly in all
five themes. No in-game check.
