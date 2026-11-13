# FollowerForge 3.8.0

Parent: candidate 3.7.11 (GitHub pre-release v3.7.11-beta; Latest is still v3.7.6). The 3.7.11 tree
is kept unchanged.

A correctness release found by auditing the bytes a build writes rather than the code that writes
them. Every follower carried a speed multiplier of 0, a height of 0 and a level multiplier of 16.96,
and an outdoor follower replaced her worldspace with an empty record. All four are fixed and the
first three are now build errors. Enemy to Ally, the evolution script, the catalogue index, the MO2
voice scan and the window's logging were repaired in the same pass, and a built follower can be
reopened in the window.

Versions 3.7.8 to 3.7.11 are described in CHANGELOG.txt; this file skipped them.

# FollowerForge 3.7.7

Parent: released 3.7.6 (GitHub v3.7.6, SHA 67a840a). The 3.7.6 tree is kept unchanged.

A correctness hotfix for the head-part race audit. 3.7.5 and 3.7.6 both treated a head part's
`ValidRaces` field as the FormID of a race. It is the FormID of a **FormList**, and the FormList
holds the races. Every verdict the audit gave was therefore built on the wrong record type: the
comparison could not match, so every restricted part read as a mismatch, and 3.7.6 hid that behind
a same-plugin shortcut that guessed.

This build resolves the FormList and compares the follower's race against its members, reports
UNKNOWN when the list cannot be read, and judges vampires against the race they will actually
carry. The list is read from the plugin its FormID names, so one kept in a shared-lists plugin or a
master is found; a later plugin's override is followed when a catalogue was built for the run.

# FollowerForge 3.7.6

Parent: released 3.7.5 (GitHub v3.7.5, SHA 001cac8). The 3.7.5 tree is kept unchanged.

A hotfix for two defects that shipped in 3.7.5.

The default profile placed the follower at (0, 0, 0) in WhiterunWorld — the world origin, not the
plaza — because Studio names the Whiterun cell and no coordinates, and the plaza position was only
used when no cell was named at all. A named Whiterun cell with no coordinates now means the plaza.

The 3.7.5 race warning fired on the correct race: a race mod's mortal, vampire and climate
variants share one head, and comparing whole FormKeys called them a mismatch. Races defined in the
same plugin are now one family.

The README inside the published zip also still said 3.7.4. It ships the current one.
