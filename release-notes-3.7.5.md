## What changed

A follower could come out with the wrong hair and nothing in the build report said which hair she
carried. GitHub issue #2 ("Crazy Hair") has been open since August on exactly that, and it could
not be answered because a screenshot of a displaced mesh cannot be traced back to a record.

**Every build now lists its head parts** — slot, FormID and EditorID — one line each:

```
[Info] PRESET_HEADPART: Hair = 0009F1:SomeHairMod.esp "FemaleHair01"
[Info] PRESET_HEADPART: Face = 00080B:SomeRaceMod.esp "FemaleHead01"
```

**Two arrangements the game completes differently are now reported.**

- `PRESET_HEADPART_OTHER_RACE` — the part names the races it applies to, and her race is not one of
  them, so the game leaves it off her. The warning names the part and the race.
- `PRESET_HAIR_OTHER_RACE` — the preset belongs to another race and its hair names no race at all.
  The engine then applies that hair to every race, so a mesh modelled around one skull renders
  around another. This is the mechanism behind hair sitting low or oversized around a follower's
  jaw. The warning names the hair and its mesh file.
- `PRESET_HEADPART_DUPLICATE` — two parts for one slot. The engine keeps one and drops the rest.
- `PRESET_NO_HAIR_PART` — parts, but no Hair part, so she is bald however the preset looked.

**One pass over the preset.** The body-shape count used to re-open and re-parse the same preset
file on every build; it now comes out of the same read as the appearance.

## Not in this release

- FollowerForge still writes the preset's head parts as given. It reports when they cannot fit; it
  does not substitute parts or rebuild a face.
- A part with no race restriction built on the preset's own race is correct and stays silent.
- No in-game check. The audit is verified against real records and real builds, not in a running
  game.

## Verification

543 Release tests (532 parent + 11 new). A live build against a real load order prints the audit and
both race warnings for a preset that belongs to a custom race, including the hair's mesh path.
