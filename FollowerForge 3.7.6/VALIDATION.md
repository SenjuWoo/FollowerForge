# 3.7.6 placement and warning hotfix — validation

Parent: released FollowerForge 3.7.5 (GitHub v3.7.5, SHA 001cac8). Copied from 3.7.5 before the
edits; 3.7.5 is not modified.

## What was found, and how
- A 3.7.5 build of the shipped default profile was parsed directly (TES4 header + subrecords).
  The ACHR's `DATA` was 24 zero bytes: position `(0.0, 0.0, 0.0)` in `WhiterunWorld`. The CLI
  logged "Placed ... at Whiterun exterior (manual cell)". The plaza branch 3.7.4 added only runs
  when no cell is named, and Studio's default profile always names one.
- A 3.7.5 build of the same preset on its **own** race (`000801:DWKaPoTunRace.esp`) still printed
  `PRESET_HEADPART_OTHER_RACE` for `00083D:DWKaPoTunRace.esp` — same plugin, so a variant, not a
  mismatch. A warning that fires on the correct answer is worse than no warning.
- The published 3.7.5 zip was listed with `unzip -l`: five files, `README.md` dated 2026-11-01 and
  headed "FollowerForge 3.7.4 - Windows tool (not a Skyrim mod)".

## What was checked after the fix
- `dotnet test src/FollowerForge.slnx -c Release` from `FollowerForge 3.7.6`: 546 passed, 0 failed,
  0 skipped (543 parent + 3 new).
- New cases: a named Whiterun cell with no coordinates lands on `VanillaForms.WhiterunDefaultPos`;
  coordinates that are set are kept exactly; a part restricted to a race in the same plugin is not
  flagged.
- Live CLI builds on this machine's 2,955-plugin Vortex order, 3.7.6 binary:
  - default profile (Nord, cell only): `Placed Audit Nord on the Whiterun plaza (cell named, no
    coordinates given)`; parsed ACHR `DATA` = `(24606.0, -4167.0, -2986.0)`.
  - preset on its own race: no race warnings at all.
  - preset on a Nord (mismatch): both `PRESET_HEADPART_OTHER_RACE` and `PRESET_HAIR_OTHER_RACE`
    still print, naming `FemaleHairKapotun01` and its mesh path.
- Package contents verified after publish (see the release record).

## Release record
- Commit `67a840aa81fb33bd3c40cfe6a447c0f01bd67b9c` pushed to `main`. Required check `xunit suite
  (latest snapshot)` succeeded on that SHA; `Analyze (csharp)` and `Analyze (actions)` succeeded.
- Tag `v3.7.6` on `67a840a`. GitHub release `v3.7.6` is Latest.
- Asset `FollowerForge-3.7.6-win-x64.zip`: 99403591 bytes, SHA-256
  `77e511b026cd23d6b18d1314d188008aa2916f2d523a9c142f89402725536170`; GitHub reports the same
  digest for the uploaded file.
- Zip inspected with `unzip -l`: five entries — `FollowerForge.exe`, `cli\FollowerForge.Cli.exe`,
  `README.md`, `CHANGELOG.txt`, `NEXUS-CHANGELOG-3.7.6.txt`. No `Data` folder, no plugin, no
  manager metadata: the application stays out of the game and out of the mod manager, as designed.
  The README inside is 9427 bytes and carries the 3.7.6 notes.
- Binaries rebuilt from the committed tree: both exes report ProductVersion
  `3.7.6+67a840aa81fb33bd3c40cfe6a447c0f01bd67b9c`.
- Boot check: `FollowerForge.exe` from the published folder started and stayed up with
  `Responding = True` for 12 s, then was closed.
- Open Dependabot, code-scanning and secret-scanning alerts after the push: 0, 0, 0.

## What this does not claim
- No in-game check. The placement fix is proven at the record level, not by watching her stand on
  the plaza.
- The "running in place" and "gone after a cell transition" reports are *consistent with* a
  follower placed at the world origin. That is not proof that every such report had that cause.
- The head-part warnings are advisory. FollowerForge still writes the preset's parts as given.

# 3.7.5 head-part audit — validation

Parent: released FollowerForge 3.7.4 (GitHub v3.7.4, SHA 77e6b0b). This snapshot was copied from
3.7.4 before the edits; 3.7.4 was not modified.

## What was checked
- `dotnet test src/FollowerForge.slnx -c Release` from `FollowerForge 3.7.5`: 541 passed, 0 failed,
  0 skipped. Parent baseline on this machine: 532, re-run before any edit.
- The 9 new cases cover: each head part reported by slot and FormID; two parts for one slot
  flagged; parts without a Hair part flagged; a Hair part present not flagged; an appearance with
  no parts saying nothing; a part restricted to another race named; a part with no restriction not
  flagged; a part restricted to her own race not flagged; an unrestricted hair called out when the
  preset belongs to another race; the slot list and body-shape count arriving from one read of the
  preset file.
- Live run, `FollowerForge.Cli build --profile` with `Appearance.JslotPath` set to
  `Priscilla_Big_Kitty.jslot` and the sample profile's race left at `013746:Skyrim.esm` (Nord).
  The build printed, from the real preset:

      PRESET_HEADPART: Misc = 00080A:DWKaPoTunRace.esp
      PRESET_HEADPART: Face = 00080B:DWKaPoTunRace.esp
      PRESET_HEADPART: Eyes = 0009A8:DWKaPoTunRace.esp
      PRESET_HEADPART: Hair = 0009F1:DWKaPoTunRace.esp
      PRESET_HEADPART: FacialHair = 000880:DWKaPoTunRace.esp
      PRESET_HEADPART: slot 7 = 00099B:DWKaPoTunRace.esp
      [Warning] PRESET_HEADPART_OTHER_RACE: 'FemaleHeadKapotun' is a Face head part that only
        applies to 00083D:DWKaPoTunRace.esp, and she is built as 013746:Skyrim.esm ...
      [Warning] PRESET_HAIR_OTHER_RACE: The preset's hair ('FemaleHairKapotun01',
        actors\character\DWRaces\TigerKhajiit\Hair\FemaleHairKapotun01.nif) carries no race
        restriction at all ...
      SHIP_GATE_PASS: HEDR=1.71 numRec=13 formVersion=44 (6 records) ESL=True
      BUILD OK (published)
- record reads through houseCARL (`housecarl_records`, form=fields) confirm the two records the
  warning is built from: `0009F1:DWKaPoTunRace.esp` EditorID `FemaleHairKapotun01`, Type `Hair`,
  ValidRaces null, model `actors\character\DWRaces\TigerKhajiit\Hair\FemaleHairKapotun01.nif`;
  `00080B:DWKaPoTunRace.esp` (defining plugin) ValidRaces `00083D:DWKaPoTunRace.esp`.
- No in-game check. The follower was built into a scratch folder, not installed.

## Release record
- Commit `001cac8d0a1bd9c16a434eab26a0eba1fd26a149` pushed to `main`. Required check `xunit suite
  (latest snapshot)` succeeded on that SHA; `Analyze (csharp)` and `Analyze (actions)` succeeded.
- Tag `v3.7.5` on `001cac8`. GitHub release `v3.7.5` is Latest.
- Asset `FollowerForge-3.7.5-win-x64.zip`: 99402205 bytes, SHA-256
  `6c176f83f63d301fbc18008ff4d932e4a69f867f70d914c45cf4987b51e6d61f`, and the digest GitHub
  reports for the uploaded asset is the same value.
- Rebuilt from the committed tree so the binaries stamp the release SHA: both exes report
  FileVersion `3.7.5.0`, ProductVersion `3.7.5+001cac8d0a1bd9c16a434eab26a0eba1fd26a149`.
- Boot check: `FollowerForge.exe` from the published folder started and stayed up with
  `Responding = True` for 12 s, then was closed.
- Open Dependabot, code-scanning and secret-scanning alerts on the repository after the push: 0,
  0, 0.

## What this does and does not claim
- Claims: the build states which head part it writes, by slot and EditorID; a part restricted to
  another race is named; an unrestricted hair on a preset from another race is named with its
  mesh path; a preset naming two parts for one slot, or no Hair part, is flagged.
- Does not claim: that issue #2's reporter used a custom-race preset. The mechanism is proven on
  this machine's preset, not on theirs. The audit is what makes their next report decidable.
- Does not claim: that the hair is repositioned or repaired. FollowerForge still writes the
  preset's parts as given; it now says when they cannot fit.
- Does not claim: a fix for a part with no race restriction built on the same race as the preset,
  which is correct and stays silent.

# 3.7.4 tint persistence and exported faces — validation

Parent: released FollowerForge 3.7.3 (GitHub v3.7.3, SHA 110d17e). This snapshot was copied from 3.7.3 before the edits. 3.7.3 was not modified. GitHub v3.7.4 is Latest, annotated tag on 77e6b0bca03de2038e50e27a25480d0cf070511c. Release asset FollowerForge-3.7.4-win-x64.zip is 99266848 bytes, SHA-256 a468e7dc6444c0c3e02509a5185e1122558d397f54e57f68db52ae8561691276. The downloaded asset and the GitHub digest match that hash. Both exes report FileVersion 3.7.4.0 and ProductVersion 3.7.4+77e6b0bca03de2038e50e27a25480d0cf070511c. Boot check: window stayed up. Nexus upload is still the owner's step.

## What was checked
- `dotnet test src/FollowerForge.slnx -c Release` from `FollowerForge 3.7.4`: 532 passed, 0 failed, 0 skipped. Parent baseline was 524. The new cases are the short texture set, the 9-slot specular guard, a head with no claimable tint, a nif only under Exported, the same name in the CharGen root and Exported, an MO2 overwrite nif, the empty-catalogue message, and the Whiterun plaza distance check.
- The short-set test failed first with FACEGEN_TINT_FALLBACK on MaleHeadBreton and FACEGEN_TINT_NOT_SET, then passed after the texture list is grown before the write. A later assertion requires the reloaded set to be 9 slots, not 7. That assertion failed before the grow target changed from slot+1 to 9, then passed.
- The 9-slot head already passed before that change: slot 6 received the tint and slot 7 stayed the specular map.
- A head with shapes and no claimable tint is saved. Its face line used to say "custom (from your RaceMenu export)" because Success was true and the handoff warning was ignored by the one-line summary. SkippedExport and HeadWithNoClaimableTint were turned red against the sentence "copied. The head mesh is in the follower; the tint path was left as exported. See the warning." Then FaceGenPlayerText was changed. A fully installed face with no handoff still says "custom (from your RaceMenu export)".
- No in-game check. Black face is not claimed fixed. The wizard was not clicked.
- Native houseCARL in this session (after the Grok restart) read the same four packages. 01B217 winner remains Z_Reynila.esp. PreferredSpeed is Run and Data[29].Data is 50 on both Skyrim.esm and that winner. No package constant changed.
- WhiterunWorld positions from Skyrim.esm: GildergreenXMarkerREF 02158F = (24775.639, -4061.3545, -2968). Inner gate door 01B1F3 = (19367.338, -7433.07, -3547.4492). The old default (28878, -4122, -2618) is 4102.8 units from the tree. DefaultWhiterunSpot_StandsOnThePlazaInFrontOfTheGildergreen failed on that distance, then passed after the drop moved to (24606, -4167, -2986).
- GitHub #2 screenshot (user-attachments b5bb5fd1) shows a room-sized black mesh, a chin spike, and a small red shape above the head. Xilia's jslot formIdentifiers resolved through housecarl_resolve to FemaleMouthHumanoidDefault, 00KLH_FemaleHeadImperial, 0Sky201, KoralinaEyebrowsF09, and MarksFemaleHumanoid00NoGash. 0Sky201 is Type Hair. nif inspect of meshes\KS Hairdo's\Sky201.nif: one shape NoHat, partition SBP_131_HAIR, bones NPC Head and NPC Spine2. TheEyesOfBeauty.esp is not in this load order. The parser was not changed.
- houseCARL 1.9.0 answered a stdio handshake from this machine. `housecarl_load_order_status` lookup Skyrim.esm: profile Default, instance C:\Users\karlo\AppData\Local\houseCARL-Shim, 3034 enabled mods, 3010 active plugins, Skyrim.esm active as an implicit master. `housecarl_batch_record_detail` with plugin Skyrim.esm and field EditorID:
  - 01B217 Package DefaultSandboxEditorLocation512, source Skyrim.esm, load-order winner Z_Reynila.esp
  - 09361E Package DefaultSandboxEditorLocation256, winner Skyrim.esm
  - 0956B8 Package DefaultSandboxCurrentLocation256, winner Skyrim.esm
  - 01B210 Package DefaultSleepEditorLoc24x8, winner Skyrim.esm
  VanillaForms already uses those four EditorIDs. No package constant was changed. The Z_Reynila override is this load order, not a FollowerForge FormID error.
- The houseCARL reads above were made in this session after the restart. SSEEdit and the Creation Kit were not launched. Rick7's MaleHeadBreton.nif was not on the deployed CharGen tree (0 nifs there earlier in this work). The tint tests use a nifly-generated head.
- GitHub CI run 37536673926 on 77e6b0b tested snapshot FollowerForge 3.7.4 and reported 532 passed, 0 failed. CodeQL run 37536673797 (C# and Actions) completed success on the same SHA. Publish-FollowerForge.ps1 then produced the zip above. In-game appearance was not checked.

# 3.7.3 face-tint slot — validation

Parent: released FollowerForge 3.7.2 (GitHub v3.7.2, SHA 026a70e). Snapshot copy: 246 files, 0 failed (bin/obj/dist excluded). 3.7.2 was not modified.

## What was checked
- `dotnet test src/FollowerForge.slnx -c Release` on this snapshot: 524 passed, 0 failed, 0 skipped (516 from 3.7.2 + 8 new).
- Live read of two installed head meshes through NiflySharp 2.0.4:
  - AAA_GirlHeads (Monique): slot 6 `textures\Data\SKSE\Plugins\CharGen\Monique2.dds`, slot 7 `femalehead_s.dds`.
  - MaleHeadArgonian: slot 6 `...\FaceTint\MASSEESL.esp\00000812.dds`, slot 7 `ArgonianMaleHead_s.dds`.
- 3.7.2 swap of the Argonian head with an unrelated DDS name: FACEGEN_TINT_FALLBACK, slot 7 overwritten, slot 6 unchanged. That is the bug.
- 3.7.3 swap of both heads: slot 6 becomes `textures\actors\character\facegendata\facetint\FF_Probe.esp\00000800.dds`, slot 7 stays the specular file. NeedsCreationKit false.

## Package
Local only. Not uploaded to GitHub or Nexus.
- Publish-FollowerForge.ps1 -Version 3.7.3: tests 524 passed, both publishes succeeded, boot check "window stayed up". Compress-Archive then failed because the exe was still locked; the zip was created immediately after the process released it.
- File: dist/FollowerForge-3.7.3-win-x64.zip
- Bytes: 99263297
- SHA-256: ed520ea8296d9ad24fd5ad17b5c8e07eeddca5b08e25f18bdc974284a9d71dee
- Entries: FollowerForge.exe, cli/FollowerForge.Cli.exe, README.md, CHANGELOG.txt, NEXUS-CHANGELOG-3.7.3.txt
- Both exes report FileVersion 3.7.3.0

## Release
- Tag v3.7.3 is the annotated tag whose commit is 110d17eef718a5d975cf094c459f2a18d239eab3.
- CI xunit suite and both CodeQL analyses completed success on that SHA before the tag.
- Release: https://github.com/SenjuWoo/FollowerForge/releases/tag/v3.7.3
- Downloaded asset SHA-256 matches the local zip: ed520ea8296d9ad24fd5ad17b5c8e07eeddca5b08e25f18bdc974284a9d71dee, 99263297 bytes.

## Not done
- Nexus upload is still the owner's step. GitHub Latest is v3.7.3.
- No in-game look at a rebuilt follower. Ctrl+F4 was not run. Creation Kit was not launched.
- The reporter's exact NIF was not attached, so this pass proves the slot bug and the warning text, not that their particular mesh now converts.

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
- No gameplay reproduction was performed; 3.7.1 remains available at v3.7.1 (SHA 20af6e5).

## Release
- Pushed SHA 026a70ea7fa95234b7772bc870d11e93909bba96 (fast-forward, no upstream divergence). Repo CI (xunit suite) completed success on that SHA. Tag v3.7.2 annotated at that SHA (verified via API deref). Release published as Latest with the tested ZIP; downloaded-asset SHA-256 re-verified byte-identical (fad550196d7c3f889a41f3cd8e5b53ba28171196cf48ea0a4592bd68d34db9ef, 99256131 bytes).
