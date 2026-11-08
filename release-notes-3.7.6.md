## What this fixes

Two defects shipped in 3.7.5. Both were found by building followers and parsing the plugin, not by
reading the code.

**A follower built from the default profile landed at the world origin.** With no location picked,
Studio writes the Whiterun persistent cell and no coordinates. `FollowerCompiler` honoured a named
cell and wrote those coordinates as given, while the plaza position added in 3.7.4 only applied
when *no* cell was named. So the common path wrote `(0, 0, 0)` into the placed reference — the
corner of the map, outside every loaded area. In game that is a follower who is not where the
report says, and a sandbox package with nowhere to walk, which reads as "she runs on the spot" or
"she is gone after the door".

A named Whiterun cell without coordinates now means the plaza at `(24606, -4167, -2986)`.
Coordinates a profile does set are still used exactly as written.

**The 3.7.5 race warning fired on the correct race.** A custom race mod ships the mortal race plus
its vampire and climate variants, and they share one head. 3.7.5 compared whole FormKeys, so
`00083D:Mod.esp` read as a mismatch for a follower built as `000801:Mod.esp` — same plugin, same
head, warned about anyway. Races defined in the same plugin are now one family. A part from a
different plugin still warns, which is the case worth warning about.

**The README inside the download said 3.7.4.** It ships the current one.

## Verification

546 Release tests (543 parent + 3 new). The output plugins themselves were parsed: the placed
reference now carries the plaza coordinates, the matching race produces no warning, and a
mismatched race still produces both head-part warnings naming the hair and its mesh.

## Not in this release

- No in-game check. The placement fix is proven at the record level.
- The head-part warnings remain advisory. FollowerForge writes the preset's parts as given.
- No change to the shape of the generated plugin: same records, same FormID allocation, same ESL
  header. Only the placed reference's coordinates changed for profiles that named a cell without
  coordinates.
