# FollowerForge 3.7.10 — groups that end where they say

Parent: candidate 3.7.9 (never released; GitHub Latest is still v3.7.6). The 3.7.9 tree is kept
unchanged.

## What changed

- `EspHeaderValidator` requires every group, nested included, to end exactly at its declared size;
  stray bytes are `ESP_GRUP_LEFTOVER`. A TES4 header claiming more than the file holds is
  `ESP_TES4_SIZE` instead of an unhandled exception.
- `FaceGenSwapper` aggregates every slot that references a texture path before deciding severity, so
  a path used in a diffuse or normal slot is a warning even when an optional slot saw it first.
- Existing behaviour untouched: slot-6-only tint writes, NIF geometry round-trip, the four FaceGen
  statuses, partial inventory labels, head-part race resolution, HEDR 1.71 / formVersion 44 / ESPFE
  checks, Vortex and MO2 indexing, packaging.

## Verification

609 Release tests, three consecutive runs. Five of the new regression tests were run against the
previous code and failed there. Live CLI builds and generated-plugin bytes are recorded in
VALIDATION.md. No in-game check; no real RaceMenu export on this machine.

## Next

Tag, release and Nexus upload are the owner's call.

