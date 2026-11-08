# FollowerForge 3.7.7

Parent: released 3.7.6 (GitHub v3.7.6, SHA 67a840a). The 3.7.6 tree is kept unchanged.

A correctness hotfix for the head-part race audit. 3.7.5 and 3.7.6 both treated a head part's
`ValidRaces` field as the FormID of a race. It is the FormID of a **FormList**, and the FormList
holds the races. Every verdict the audit gave was therefore built on the wrong record type: the
comparison could not match, so every restricted part read as a mismatch, and 3.7.6 hid that behind
a same-plugin shortcut that guessed.

This build resolves the FormList and compares the follower's race against its members, reports
UNKNOWN when the list cannot be read, and judges vampires against the race they will actually
carry instead of skipping them.

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
