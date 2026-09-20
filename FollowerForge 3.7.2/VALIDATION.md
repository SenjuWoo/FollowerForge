# 3.7.2 community-reported fixes — validation

## Scope and provenance
Parent: released FollowerForge 3.7.1 (GitHub v3.7.1, Latest, SHA 20af6e5 — frozen, untouched). Snapshot: 244 files copied from the 3.7.1 source state via robocopy (bin/obj/dist/test-output excluded). All 3.7.2 work lives in the new snapshot only.

Source of the reports: Nexus page posts/bugs tabs (quoted by the owner; the pages themselves are not machine-readable, so reporter environment details were unavailable). Each fix is code-verified against the current 3.7.2 tree; version-at-report-time was not determinable.

## Reports and fixes (each with a failing-first regression test)
1. "Chose Ally, it displayed and recorded Friend (only 1 lined up)" — CONFIRMED BUG. WizardWindow.axaml.cs casts `(RelationshipRank)SelectedIndex` while the XAML listed `[Rival, Friend, Confidant, Ally, Lover]` against the enum's `[Lover, Ally, Confidant, Friend, Acquaintance, Rival, Foe, Enemy, Archnemesis]`: Ally (XAML 3) → enum 3 = Friend; Confidant was the only coincidence, exactly as reported. The untouched default displayed Friend while writing Ally. Fix: both RelationshipBox and KinRankBox now list the enum values in declaration order (SelectedIndex == enum value by construction); defaults are explicit (RelationshipBox = Ally, KinRankBox = Friend, matching the record defaults). Compiler side was already proven correct by BehaviorTests. Guard: WizardEnumOrderTests pins every enum-backed combo (RelationshipBox, KinRankBox by enum names; IdleBox, LineTimeBox, LineEmotionBox, E2ACompanyBox, TransformKindBox, StatPresetBox, ThemeBox, LineTriggerBox by verified label sequences) so the drift class cannot return silently.
2. "Preset calls for a hair mod, FF is not finding it even when hair is installed" — CONFIRMED SILENT-DROP PATH. CharGenDiscovery.ReadJslot dropped any head-part entry it could not resolve (no readable formIdentifier and no mods entry covering the raw formId's index) with no signal; the follower built green and could appear bald in game. Masters are NOT the problem — PluginWriter writes with MastersListContentOption.Iterate plus transitive chain expansion, so resolved references get their masters. Fix: unresolvable parts are counted (AppearanceSpec.UnresolvedHeadParts), warned on the face picker (QualityNotes) and in the build report (PRESET_HEADPART_UNRESOLVED), with the remedy spelled out. Note: the reporter's exact preset was not available, so the specific trigger on their machine is unconfirmed; the silent-drop path is real, reachable, and now surfaced.
3. "Face color doesn't match the body color" — GUIDANCE GAP ADDRESSED. The compiler already writes QNAM (skin tone) and FTST (head texture set) when the preset provides them, and lists appearance mods as required. The unhandled case is a preset with tint layers but no complexion: the face keeps the preset's painted colors while the body uses the race's default complexion. The build report now warns (PRESET_NO_HEAD_TEXTURE) explaining the cause and the fix. The deeper tone-matching (shipping body skin) remains out of scope by design — redistribution rules forbid copying arbitrary skin assets.
4. Sculpt-export confusion ("basically says I never exported the sculpt facegen data") — RaceMenu semantics verified against public guides: Sculpt tab [F5] Export Head writes NIF+DDS into Data\SKSE\Plugins\CharGen, exactly where discovery scans. The warning text now names the action, the console-open pitfall, the folder, and the name-matching rule. Behavior unchanged; the user's export likely never happened (console was open per their description) or the NIF was unreadable (which already produces its own note).

## Executed verification
- Full Release suite on the 3.7.2 snapshot: 516 passed, 0 failed, 0 skipped (501 in 3.7.1 + 15 new regressions).
- New regressions first failed red where applicable: both relationship combos (order mismatch), defaults test, then passed after the XAML fix; discovery-count and builder-warning tests pin new behavior.
- Publish-FollowerForge.ps1 -Version 3.7.2: exit 0, PUBLISH SUCCEEDED, boot check "window stayed up".
- Artifact verified from a fresh extraction: both exes report FileVersion 3.7.2.0; CLI sample-profile exit 0; build --profile sample.json --out work --zip exit 0 with BUILD OK and FF_AriaForge.esp generated.

## Artifact
File: dist/FollowerForge-3.7.2-win-x64.zip
Bytes: 99256131
SHA-256: fad550196d7c3f889a41f3cd8e5b53ba28171196cf48ea0a4592bd68d34db9ef
Entries: 5 (FollowerForge.exe, cli/FollowerForge.Cli.exe, README.md, CHANGELOG.txt, NEXUS-CHANGELOG-3.7.2.txt)

## Remaining work / risks
- Reporter-specific reproduction was not possible (no jslot/environment from the reporters); the fixes address verified code paths that match the reported symptoms.
- In-game behavior remains unverified (relationship rank display in-game, hair resolution with the reporter's preset, face/body tones with a complexion mod installed).
- Inherited from 3.7.1: batch flake never reproduced on 3.7.x code (3x on 3.7.0 only, log capture in place); CLI --help exits 2; inherited UI branding strings; Papyrus read but not decompiled.
- No publication performed. 3.7.1 remains the published Latest until the owner decides.
