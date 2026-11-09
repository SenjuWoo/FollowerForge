# FollowerForge 3.7.11 — a package that can prove itself

Parent: candidate 3.7.10 (never released; GitHub Latest is still v3.7.6). The 3.7.10 tree is kept
unchanged.

## What changed

- `Publish-FollowerForge.ps1` closes stray FollowerForge processes, reopens the archive it wrote,
  checks the expected entries are present and non-empty, prints size and SHA-256, and exits
  non-zero on failure. A locked executable and a masked exit status are what produced a fake
  success before.
- `FollowerForge.Cli.exe verify --plugin FILE` gates a plugin on its own bytes, so a package can be
  checked as shipped.
- `RaceFromMeshPath` and the unrestricted-hair warning: the message now names the race whose folder
  the hair mesh sits in, which is the actionable part of issue #2.

## Verification

615 Release tests. Packaged build verified from its own archive; a follower package's plugin gated
from inside the zip. No in-game check. **No real RaceMenu head export exists on this machine** (7
`.jslot` presets, 0 NIF/DDS pairs), so the real-export path is NOT RUN.

## Next

The owner has to export a head in RaceMenu and run one in-game acceptance pass; tagging and the
Nexus upload remain the owner's call.

