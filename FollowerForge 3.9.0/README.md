<p align="center">
  <img src="docs/images/logo.svg" width="72" height="72" alt="FollowerForge mark">
</p>

<h1 align="center">FollowerForge</h1>

<p align="center"><strong>Read your load order. Build an installable Skyrim follower.</strong></p>

<p align="center">
  Out-of-game Windows app that indexes Vortex or Mod Organizer 2 and writes an ESPFE plus assets.<br>
  FollowerForge itself is <strong>not</strong> a Skyrim mod. Do not install the application into Skyrim <code>Data</code>, Vortex, or MO2 as game content.
</p>

<p align="center">
  <a href="https://github.com/SenjuWoo/FollowerForge/actions/workflows/ci.yml"><img src="https://github.com/SenjuWoo/FollowerForge/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-d5a84b?labelColor=101114" alt="MIT License"></a>
  <a href="https://github.com/SenjuWoo/FollowerForge/releases/latest"><img src="https://img.shields.io/github/v/release/SenjuWoo/FollowerForge?label=release&color=e5bc69&labelColor=101114" alt="Latest release"></a>
</p>

<p align="center">
  <a href="https://github.com/SenjuWoo/FollowerForge/releases/latest">Download</a>
  ·
  <a href="#quick-start">Quick start</a>
  ·
  <a href="#honest-status">Honest status</a>
  ·
  <a href="CHANGELOG.txt">Changelog</a>
</p>

<p align="center">
  <img src="docs/images/app-hero.png" alt="FollowerForge Studio: seven categories, next recommended action, and follower dossier" width="100%">
</p>

<p align="center"><sub>Studio workspace, Obsidian Gold, with the Your followers card. Rendered from the 3.9.0 tree.</sub></p>

## Quick start

Download the Windows x64 zip from the [latest GitHub release](https://github.com/SenjuWoo/FollowerForge/releases/latest). It is self-contained. No separate .NET install.

1. Extract the ZIP **anywhere outside Skyrim `Data`**.
2. Run `FollowerForge.exe`.
3. Start from **Studio**. Follow the next recommended action through the seven categories.
4. Press **Build follower**.
5. Install the **generated** follower package with Vortex or MO2, enable its plugin, and deploy.

### Your followers

The Studio page's **Your followers** card works on everything you have already built:

- **Open a follower…** lists every follower you built and every saved draft. Pick one, change what
  you want, build again. **Save draft…** in the sidebar saves without building.
- **Rebuild followers…** rebuilds them with the current version, each into the folder it is in
  now. Followers built before 3.8.0 carry a speed multiplier of 0, a height of 0 and a 16.96x level
  multiplier; the window tells you how many of yours do when it starts.
- **Make a follower pack…** puts several followers into one zip that installs as a single mod.
- **Repair a plugin…** fixes a follower plugin an older FollowerForge built when her profile is
  gone, for example one someone else shared. It writes a fixed copy and leaves the original alone.

A follower package installs its plugin and FaceGen files where the game reads them, and its notes
(build report, credits, test commands, rebuild profile) under `Data\FollowerForge\<plugin>\`, so
two followers never conflict.

RaceMenu **Export Head** NIF/DDS files are required for a custom face. A slider-only preset without usable exported head geometry may not reproduce the face on an NPC.

Recommended face workflow: [FaceForge](https://github.com/SenjuWoo/FaceForge) → RaceMenu Export Head → FollowerForge.

## Vortex and Mod Organizer 2

FollowerForge auto-detects Vortex first unless MO2 is preferred. The manager control in the left sidebar can switch between them before or during indexing.

MO2 users can click **MO2 setup...** to pick the exact `ModOrganizer.ini` and profile, inspect resolved paths, save that selection, or return to automatic detection. Click **Paths...** for xVASynth and the built-mod output folder.

Empty path boxes keep automatic detection. Game `Data` and the Skyrim saves folder are refused as output. Saved GUI settings live under `%LOCALAPPDATA%\FollowerForge`. FollowerForge does **not** modify MO2 profile/INI files, Vortex deployment data, Skyrim `Data`, or saves.

Do not point FollowerForge at a houseCARL Vortex shim unless you intentionally override the safety gate.

Detailed MO2 notes: [`FollowerForge 3.9.0/docs/MO2.md`](FollowerForge%203.9.0/docs/MO2.md).

If something goes wrong, the window writes `%LOCALAPPDATA%\FollowerForge\logs\ui-*.log` and the command line writes `fforge-*.log` beside it. A plugin the index could not read is skipped and named in the status line.

## Studio (3.6.0+)

- Seven **Focus** categories and a next recommended action
- **Expert Deck** for installed records (FormID + EditorID)
- Guided / Expert (`Ctrl+E`) — Expert opens the catalogue; it does not hide fields
- Command palette (`Ctrl+K`); `Ctrl+0`–`Ctrl+7` jumps categories
- Five themes. Theme, experience, and window size are stored separately from follower profiles
- Sex on Identity updates she/her or he/him. Gear lists show FormIDs so identically named items can be told apart

## CLI and environment

| Option | Purpose |
| --- | --- |
| GUI manager button | Prefer MO2 or Vortex |
| GUI **MO2 setup...** | Select and persist the exact MO2 instance/profile |
| GUI **Paths...** | Set xVASynth and the built-mod output folder |
| `FFORGE_XVASYNTH` / `--xvasynth` | Override xVASynth discovery |
| `FFORGE_PREFER_MO2=1` | Try MO2 before Vortex |
| `FFORGE_MO2_INSTANCE=D:\path\to\instance` | Use that MO2 instance folder |
| `--mo2-instance DIR` | Exact MO2 instance in the CLI |
| `--mo2-profile NAME` | Exact MO2 profile in the CLI |
| `cli\FollowerForge.Cli.exe env` | Print the detected environment |
| `cli\FollowerForge.Cli.exe index` | Rebuild the catalogue |
| `cli\FollowerForge.Cli.exe races` | Races she can be built as |
| `cli\FollowerForge.Cli.exe races --creatures` | Races the transform picker offers |
| `cli\FollowerForge.Cli.exe build --profile FILE` | Build from a profile, including a built follower's `rebuild-profile.json` |
| `cli\FollowerForge.Cli.exe list` | Followers you have built, and which need a rebuild |
| `cli\FollowerForge.Cli.exe rebuild [--only-needed] [--zip]` | Rebuild them in place with this version |
| `cli\FollowerForge.Cli.exe pack --out FILE.zip --builds DIR` | One installable zip from several followers |
| `cli\FollowerForge.Cli.exe repair --plugin FILE` | Fix a plugin an older version built, without its profile |
| `cli\FollowerForge.Cli.exe verify --plugin FILE` | Run the ship gate on any plugin |
| `cli\FollowerForge.Cli.exe` | List every command |

## Requirements

- Windows 10 or 11
- Skyrim Special Edition or Anniversary Edition
- Vortex or Mod Organizer 2
- RaceMenu for custom faces

Optional integrations include xVASynth, dialogue overhauls, High Poly Head, and FSMP when the selected follower assets require them.

## Sharing generated followers

FollowerForge does not grant redistribution rights for third-party assets. Check every asset author’s permissions before uploading a generated follower.

## Honest status

Verified in this tree:

- `FollowerForge 3.9.0/` is the ship snapshot (`CURRENT.txt`)
- 674 Release tests pass locally on the 3.9.0 tree, with 0 build warnings
- Two followers share no file path; only the plugin sits at a package's root
- A follower built by 3.7.11 was found, marked as needing a rebuild, rebuilt in place and repaired from its plugin alone, against a real load order; the repaired plugin passes the ship gate and its original was left untouched
- Two followers were packed into one zip with no shared path
- A built NPC carries speed multiplier 100, height 1.0 and the level multiplier that was asked for, read back from the written bytes. The build fails if it does not
- An outdoor follower's plugin carries her worldspace's own record, read from the plugin that defines it, not an empty one
- A damaged plugin is skipped and named; the rest of the load order is still indexed
- A built follower's `rebuild-profile.json` loads back into the window. A headless run on a real load order rebuilt the same profile from it
- A named Whiterun cell with no coordinates places her on the plaza; coordinates a profile sets are used as given
- Every build lists the head parts it writes, by slot, FormID and EditorID
- A head part restricted to another race is named, and an unrestricted hair on a preset from another race is named with its mesh file. Variants of her own race, from the same plugin, do not warn
- A preset with two parts for one slot, or with no Hair part, is flagged
- A head part whose plugin is missing, or which that plugin no longer holds, is named instead of being skipped
- The face tint is written to shader slot 6 and nowhere else; a CharGen path in another slot is never rewritten
- The ship gate rejects a FormID naming an undeclared master, a record that runs past its group, bytes after the last group, and a group that does not end exactly at its declared size
- A FaceGen result says complete, partial, needs manual repair or failed. A copied mesh with an unrewritten tint path is partial, not success, and every surface reads the same state
- A short head texture list is grown to nine slots and the tint is written on slot 6. An existing specular map on slot 7 is left alone
- Head meshes that live only under CharGen `Exported` or `Presets` are listed

Not claimed:

- In-game confirmation of the placement, the tint, the face list, or the head-part audit. Rebuild to pick up the placement fix; an already-built plugin keeps its old coordinates
- That every "she runs on the spot" or "she is gone after a door" report had this cause. A follower parked at the world origin is consistent with them
- That GitHub issue #2's reporter used a custom-race preset. The mechanism is proven on a preset that does; the audit is what makes the next report decidable
- That FollowerForge moves or repairs a head part. It writes the preset's parts as given and says when they cannot fit
- That a speed multiplier of 0 explains every "she runs on the spot" report. It is what the plugins held, and it is fixed; the reports were not reproduced in game
- The Enemy to Ally, evolution and transformation scripts in a real session
- Nobody has recruited a follower built by this tree in game
- The window's choosers, file pickers and new buttons were rendered headlessly but not clicked by hand
- A FaceGen conversion that finishes on its own. Nothing here was checked in game, and this machine has no head export to convert
- A creature transform surviving a real fight. `SetRace()` is engine behaviour this build cannot exercise
- That a RaceMenu body shape or overlay transfers into the plugin. They do not. The build warns when shaped `bodyMorphs` are present
- A Fallout 4 port, or one zip that contains several followers
- Nexus upload of 3.7.6. GitHub Latest is [v3.7.6](https://github.com/SenjuWoo/FollowerForge/releases/tag/v3.7.6) from `67a840a`. The zip is 99403591 bytes, SHA-256 `77e511b026cd23d6b18d1314d188008aa2916f2d523a9c142f89402725536170`

## Build from source

Ship tree: `FollowerForge 3.9.0\`. Needs the .NET 10 SDK.

```powershell
cd "FollowerForge 3.9.0"
.\Build-FollowerForge.ps1
.\Publish-FollowerForge.ps1 -Version 3.9.0
```

## Credits

Skyrim, Vortex, Mod Organizer 2, RaceMenu, and related names belong to their owners. This project is unofficial.

## License

[MIT](LICENSE)

## Notes in 3.9.0

- **Your followers** on the Studio page: open, rebuild, pack and repair the followers you already built
- Packages keep their notes in `Data\FollowerForge\<plugin>\`; two followers no longer conflict in Vortex or MO2
- **Save draft…** saves a follower without building her
- The window shows its real version
- Dependencies updated; NuGet updates now actually run

## Notes in 3.8.0

- Followers are written with a speed multiplier of 100 and a height of 1.0. Both were 0 in every earlier build
- "Scale with the player" means 1.0 times your level. It was written as 16.96 times
- A follower placed outdoors no longer blanks her worldspace's record
- Enemy to Ally: her hostile form attacks, has her face, and is placed once
- Evolution no longer adds each phase on top of the last
- One damaged plugin no longer empties every list; it is skipped and named
- **Open follower…** reloads a follower you built so you can change her
- The window writes a log

## Notes in 3.7.7

- The head-part race audit now reads the FormList a head part points at, and checks whether your follower's race is one of its members. It previously treated that list as if it were a race
- A list that cannot be read is reported as unknown instead of guessed, and a race list kept in a shared plugin or a master is found
- Vampires are audited against their vampire race instead of being skipped

## Notes in 3.7.6

- A follower built from the default profile now stands on the Whiterun plaza. The default names the Whiterun cell with no coordinates, and those zeros used to be written as the position
- A head part restricted to a vampire or climate variant of her own race no longer warns
- The README inside the download matches the release

## Notes in 3.7.5

- Every build lists the head parts it writes — slot, FormID and EditorID. A build that writes the wrong hair now says which hair it wrote
- A part restricted to another race is named; the engine leaves it off her
- A preset made on one race and built as another is called out. A hair record with no race list is applied to every race, so a mesh modelled around one skull renders around another — the mechanism behind hair sitting low around a follower's jaw
- Two parts for one slot, or no Hair part at all, now warns
- The preset is read once per build instead of twice

## Notes in 3.7.4

- Face tint is written on shader slot 6 even when the exported head's texture list is shorter than that slot
- Faces exported only into CharGen `Exported` or `Presets` show up in the face list
- Skipping the location picker no longer drops her past the Gildergreen

## Notes in 3.7.0

- Combat transformation can turn her into a **creature** (dragons, wolves, trolls, spriggans — whatever installed mods provide). Creature races stay out of the **identity** picker because they have no head data; that rule never applied to the race she **turns into**
- Choosing a legacy outfit **and** hand-picked armour no longer hard-fails the build. The outfit is what she starts in; the pieces stay in inventory
- Build warns when a RaceMenu preset carries a body shape a plugin cannot store

Earlier 3.6.x notes (Studio, Copy diagnostics, Clear on pickers) live in [`CHANGELOG.txt`](CHANGELOG.txt).
