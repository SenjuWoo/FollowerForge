# FollowerForge 3.7.10

Three fixes to checking, not to generation.

## A group has to end where it says

The plugin check walked each group while a full 24-byte record header still fit. One to 23 bytes of
junk at the end of a group therefore ended the loop quietly, and the plugin passed. Every group,
nested ones included, now has to end exactly at its declared size, and anything else is
`ESP_GRUP_LEFTOVER`.

## A malformed header reports instead of crashing

A TES4 header claiming more bytes than the file holds made the master count read past the end of
the buffer. It is now reported as `ESP_TES4_SIZE`, and the read is clamped so the rest of the
checks still run.

## A missing texture is judged by every slot that uses it

Texture paths were deduplicated before their severity was decided. A path that appeared in an
optional slot first and a diffuse or normal slot later was reported as harmless, so a broken face
could read as clean. Each path is now resolved once and judged from every slot that references it;
if any use is diffuse or normal, it is a warning.

## Verification

609 Release tests, three consecutive runs. The new regression tests were run against the previous
code first: five of them failed there. Nothing was tested in game, and no real RaceMenu export
exists on this machine.
