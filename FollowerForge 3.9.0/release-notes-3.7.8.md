# FollowerForge 3.7.8

A correctness release for what the build *reports*, not what it generates. The follower, the plugin
and the package layout are 3.7.7's.

## FaceGen states are now unambiguous

A conversion that copied the head mesh but could not rewrite its tint path used to set both
`Success` and `NeedsCreationKit`. Every surface then had to pick one: the console said the face was
custom, `manifest.json` said the follower had FaceGen, and the warning said to open the Creation Kit.
One result now carries one state:

| State | Meaning |
|---|---|
| complete | mesh written, tint copied, tint path persisted, saved NIF reopened clean |
| partial | mesh installed, tint path left exactly as exported |
| needs manual repair | nothing installed; the handoff report has the steps |
| failed | the conversion ran and produced nothing usable |

`Success` is true only for complete. The console line, the HTML report and `manifest.json` all read
the same state.

## The handoff report is inside the package

It was written one directory above the staging root. Publishing only walks the staging root, so the
file stayed behind and the warning named a path that did not exist. It is now
`ck-handoff-report.json` at the root of the follower folder.

## Missing head parts are reported

A part whose plugin is absent, or whose FormID the plugin does not hold, used to vanish silently.
`PRESET_HEADPART_MISSING` names it, with the FormKey and the slot the preset put it in, and
distinguishes "the plugin is not installed" from "the plugin is installed but does not hold it".

## Manifest truth

`CombatStyleCloned` described the request. It now describes the plugin.

## Verification

577 Release tests. Nothing here was checked in game: no follower was recruited, walked across a cell
boundary or reloaded from a save, and no face was looked at in Skyrim.
