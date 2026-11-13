# FollowerForge 3.7.11 (beta)

Release engineering and one diagnostic, not generation changes.

## The package proves itself

A publish run failed with `PermissionDenied` on `FollowerForge.exe` because a copy was still running,
and the calling pipeline hid the exit status, so the failure read like a success. The script now:

- closes stray FollowerForge processes and refuses to continue if any survive;
- reopens the archive it just wrote and checks that `FollowerForge.exe`, `cli\FollowerForge.Cli.exe`,
  `README.md` and `CHANGELOG.txt` are present and non-empty;
- prints the entry count, the byte size and the SHA-256 it measured;
- exits non-zero on any failure.

## A plugin can be gated on its own bytes

`FollowerForge.Cli.exe verify --plugin <file>` runs the shipping checks against any plugin, including
one extracted from a release archive or a follower package.

## The hair warning names the race

An unrestricted hair part is applied to every race. When its mesh lives under `DWRaces\<Race>\`, the
warning now says which head that mesh was built for, which is the actionable half of GitHub issue #2.
The issue stays open: nothing here moves a mesh.

## Verification

615 Release tests. No in-game check, and no real RaceMenu export exists on this machine, so the
FaceGen import path remains unverified against a genuine export.
