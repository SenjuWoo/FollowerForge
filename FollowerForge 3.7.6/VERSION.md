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
