## What this fixes

A correctness hotfix for the head-part audit. No new features, no changes to placement, packaging,
FaceGen handling or version stamping.

**The audit read the wrong kind of record.** A head part names the races it may be used on through a
FormList — a list record holding races — not by pointing at a race directly. 3.7.5 and 3.7.6 looked
that FormID up among the race records, found nothing, and then compared the FormList's own FormID
against the follower's race. Those two values can never be equal, so **every** restricted part was
reported as belonging to another race. 3.7.6 did not fix that: it added a rule treating two records
from the same plugin as the same race, which masked the symptom on the preset being tested and is
wrong in general, because one plugin can define two unrelated races.

Settled from the load order, not from reasoning: `00083D:DWKaPoTunRace.esp` is `HeadPartsKapotun`, a
FormList, and its members are `000801:DWKaPoTunRace.esp` and `00083C:DWKaPoTunRace.esp` — the mortal
race and its vampire variant.

What the audit does now:

- Resolves the FormList and compares the follower's race against its members.
- Reports a mismatch only when the list is readable and she is not in it, naming the list and the
  races it holds.
- Reports `PRESET_HEADPART_RACE_UNKNOWN` when the list cannot be read, instead of inventing either
  verdict. A FormList overridden by a plugin above the head part's own plugin is outside what the
  build reads, and the message says so.
- Judges a vampire against the race it will actually carry, rather than skipping vampires entirely.

## Verification

554 Release tests (546 parent + 8 net new), including three that write a real plugin holding a race,
a FormList naming it and a head part pointing at the list, and read it back through the resolver.

Live builds against a real load order: the preset's own race is silent, its vampire race is silent,
and a different race warns and names both members of the list. Generated plugins parsed directly:
HEDR 1.71, formVersion 44, ESL set, expected masters, own FormIDs at the master index after the
masters.

## Not claimed

- No in-game check. Nothing here repairs hair geometry, warpaints or faces; the audit reports and
  does not reshape a preset.
- MO2 was not exercised on this machine.

## Not released yet

This tree is built and validated locally and is **not** published. No GitHub release, no Nexus
upload until the owner approves.
