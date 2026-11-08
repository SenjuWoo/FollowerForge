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
  verdict. A race list is read from the plugin its own FormID names, so a shared-lists plugin or a
  master works; an override by a later plugin is followed when a catalogue was built, and the report
  says which record it used.
- Judges a vampire against the race it will actually carry, rather than skipping vampires entirely.
- Reads the head part record itself from its load-order winner when the catalogue names one, because
  an override can change which race list the part uses. An override that cannot be opened is reported
  as unknown instead of falling back to the replaced record.
- Names every affected part on its own line - EditorID, FormID, and the record's own type - instead of
  reducing several bad parts to one sentence. One record used in two slots is reported once.
- Says so when the catalogue has no entry for a record, because a missing index entry is not proof
  that nothing overrides it.

## Verification

570 Release tests (546 parent + 12 net new), including five that write real plugins holding a race,
a race list and a head part — same plugin, a shared-lists plugin, a master, an override, and an
override that cannot be opened — and read them back through the resolver. The two cross-plugin
cases failed against the previous resolver and pass now.

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
