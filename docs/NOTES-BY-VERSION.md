# Notes by version

Short, user-facing notes for recent versions, moved here from the README. The full history is in
[`CHANGELOG.txt`](../CHANGELOG.txt).

## Notes in 3.9.0 (source tree; not released yet)

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

Earlier 3.6.x notes (Studio, Copy diagnostics, Clear on pickers) live in [`CHANGELOG.txt`](../CHANGELOG.txt).
