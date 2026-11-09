# FollowerForge 3.7.9

Three fixes, all to correctness rather than features.

## The tint is written to one slot

The locator scanned every texture slot on every shape and treated any path containing `\CharGen\`
or `\facetint\` as a face tint. An exported head whose diffuse, normal or specular map also lives
under the CharGen folder therefore had that map replaced with a tint path — the face broke in a way
that looks exactly like a FaceGen problem and is not one.

Only slot 6 is written now. The head's tint slot is claimed when it is empty or already a tint; a
second shape is claimed only when its own slot 6 already reads as a tint, which is the export saying
it uses one. A skin, hair or normal map in slot 6 is left as it is.

## A partial face is inventoried as partial

`source-assets.json` listed FaceGen files only for a fully converted face, while `manifest.json`
counted a partial conversion as having one. Both now agree: partial files are listed and labelled
incomplete, and the console line, the HTML report and the manifest read the same state.

## The ship gate reads the whole file

Three damage classes the gate could not see before:

| Check | What it rejects |
|---|---|
| `ESP_MASTER_INDEX` | a FormID whose master index names a master the plugin does not declare |
| `ESP_TRUNCATED` | a record whose declared size runs past the group it sits in |
| `ESP_TRAILING_DATA` | bytes after the last group |

HEDR 1.71, per-record formVersion 44, the ESL flag and the records-plus-GRUPs convention are
unchanged.

## Verification

596 Release tests, three consecutive runs. The new regression tests were run against the previous
code first: all four tint and gate cases that target a fixed defect failed there and pass here.
Nothing was tested in game, and no real RaceMenu export exists on this machine.
