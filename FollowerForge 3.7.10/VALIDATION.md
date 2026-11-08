# 3.7.10 group bounds and texture severity validation

## What was found

1. `Walk(start, end)` looped `while (r + 24 <= end)`, so 1-23 stray bytes inside a group ended the
   loop without a complaint and the file still passed the gate. Nested groups had the same hole.
2. A TES4 header claiming more bytes than the file holds made `CountMasters` read past the buffer
   and throw; the validator crashed instead of reporting.
3. `FaceGenSwapper` deduplicated texture paths with a `HashSet` before deciding required/optional, so
   a path first seen in an optional slot never produced the required warning even when a diffuse
   slot used it.

## What was checked after the fix

- `dotnet test src/FollowerForge.slnx -c Release`: **609 passed, 0 failed, 0 skipped**, three
  consecutive runs.
- New regression tests were run against the previous implementation first and failed there:
  `Junk_inside_a_group_is_rejected` (1, 7 and 23 bytes), `Junk_inside_a_nested_group_is_rejected`
  and `A_path_in_an_optional_slot_first_and_a_diffuse_slot_later_is_required`.
- Preserved and still passing: tint restricted to slot 6, other NIF paths untouched, geometry
  round-trip, four FaceGen statuses, partial inventory labels, head-part race resolution, HEDR 1.71
  / formVersion 44 / ESPFE checks.

## Not tested in Skyrim

No gameplay session. Unverified: hair, eyes, brows and face geometry; face and body tint; hire and
dismissal; cell transitions; save/reload; combat and transformation. This machine has **0 CharGen
head exports**, so no real RaceMenu export was converted and the live FaceGen test reported NOT RUN.

