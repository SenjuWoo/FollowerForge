# FollowerForge 3.7.4 — tint persistence, exported faces, Whiterun plaza

Status: implemented in `FollowerForge 3.7.4`. Parent `FollowerForge 3.7.3` was not edited.

- [x] Short texture sets grow to nine slots; tint writes on slot 6; an existing slot 7 specular is kept.
- [x] CharGen nifs under Exported and Presets are indexed. The same stem is kept once, root first.
- [x] A shaped head with no claimable tint is still copied. The face line says so.
- [x] No-location Whiterun drop moved to the plaza in front of the Gildergreen (24606, -4167, -2986). Package FormIDs unchanged.
- [x] 532 Release tests passed locally before the snapshot commit. In-game appearance was not checked.
- [x] GitHub release v3.7.4 from 77e6b0b. Nexus paste is still the owner's step.

# FollowerForge 3.8.0 plan — follower packs (N followers, one plugin, one zip)

Status: draft plan — owner approval pending. Nothing started. Version number provisional.
Requested in the Build a Follower comments: "create a follower pack, putting multiple followers
into one single .zip file". Package generation lives here because BAF is a runtime Papyrus tool
and cannot write plugins.

Budget, measured rather than assumed:
- ESL cap: 2048 new records universally (FormIDs 0x800-0xFFF); 4096 only on game 1.6.1130+
  (UESP; esplugin #40). EspHeaderValidator already enforces the universal range, so packs inherit it.
- Per follower: NPC + [CSTY] + RELA + ACHR = 3-4 records (fixed allocation order,
  FollowerCompiler.cs:60); dialogue (QUST/DIAL/INFO) is allocated last and only when the profile
  has it.
- A 100-follower pack is roughly 20% of the cap. "Too much" is not a realistic pack size; the
  guard below is insurance, not a design limit.

1. [ ] Not started. When it starts, copy the released 3.7.4 snapshot. Do not edit 3.7.3 or 3.7.2.
2. [ ] Prove first on a spike: two profiles -> ONE SkyrimMod -> write -> EspHeaderValidator
       (requireEsl) passes + two FaceGen swaps land. No feature work before this holds.
3. [ ] Split compile-from-mod: Compile builds its own SkyrimMod from profile.PluginName
       (FollowerCompiler.cs:56-57). Give it a caller-provided mod path (or move the per-profile
       record builders behind a PackCompiler that owns one mod). Keep the fixed per-follower
       allocation order and a stable follower order (ordinal by name) so DeterministicBuildId
       does not drift between runs.
4. [ ] Budget guard on the staged bytes: count new-record FormIDs (the validator already walks the
       file); error above 2048 with a "split into Pack A/B" message; warn at 1536 (75%).
5. [ ] Masters: union across members, deterministic order; ValidateMasterClosure /
       ValidateInstalledMasters run against the union; the checklist lists it.
6. [ ] FaceGen per member: FaceGenSwapper is already per-NPC (NpcFormId + PluginName,
       FaceGenSwapper.cs:21,29) — call it once per member with the pack plugin name; assert every
       NIF/DDS pair lands under the pack plugin's facegeom/facetint folders.
7. [ ] Assets: reuse the HubMode path — a hub ships shared assets once; pack-local mode ships only
       generated FaceGen (the shape FollowerBuilder.cs:1093 already documents).
8. [ ] Output: one staging folder -> one zip; SHARE-CHECKLIST lists every member and the master
       union; RSVexclude.ini merges member texture paths.
9. [ ] Surfaces: CLI verb "pack" beside build/batch/hub (Program.cs:57-64 switch), profiles loaded
       the batch way (BatchBuilder.LoadProfiles); Studio: select profiles -> Build pack.
10. [ ] Tests (src/Tests/): budget error >2048 / warn at 1536; masters union; per-member FaceGen
        naming; two-build determinism (byte-identical); dialogue-in-pack; hub+pack; 3-member
        end-to-end through the CLI.
11. [ ] Docs and release via the standard pipeline: README, IMPLEMENTATION-MAP, NEXUS changelog;
        CI green SHA, tag, asset bytes verified, NEXUS-UPLOAD refreshed.

Deliberately NOT in this plan:
- Non-ESL pack output. Only needed past 2048 records — split packs first; a plain ESP costs one of
  the 254 full slots. Revisit only if a real pack hits the cap.
- Cross-pack dedup or runtime merging of packs.
- Editing a built pack in place (rebuild instead), and per-member enable/disable toggles.

Owner decisions before starting:
- Release train: 3.8.0, or ride it into a later version.
- Pack plugin naming: author-prefix rule (SharePackageDocuments.NormalizeAuthorPrefix) — one
  prefix per pack?
- Hub: required for packs, or standalone-first with hub as an option?
- Studio UX: where packs live; whether member order is user-visible or build-internal only.

# FollowerForge 3.7.2 community-fix pass

- [x] Diagnose four Nexus reports against current code; reproduce relationship mis-map and head-part silent drop.
- [x] Fix: relationship/kin pickers bound to enum order (defaults now display what they write).
- [x] Fix: unresolved preset head parts counted + warned (picker + PRESET_HEADPART_UNRESOLVED).
- [x] Fix: tint-without-complexion warning (PRESET_NO_HEAD_TEXTURE); sculpt guidance made concrete.
- [x] Regression tests: 516 passed (15 new); package built, boot-checked, extracted-CLI smoke pass.
- [x] Publication: GitHub v3.7.2 published from green SHA 026a70e (repo CI success; asset bytes verified; CodeQL tracked to completion).
- [ ] Nexus upload of FollowerForge-3.7.2-win-x64.zip (owner step; changelog text delivered).

See FollowerForge 3.7.2/VALIDATION.md for exact evidence and remaining risks.

# FollowerForge 3.7.1 repair pass

- [x] Preserve released parent; verified 240-file snapshot.
- [x] Reproduce/fix MO2 priority, missing explicit mesh, output guard, cross-drive publication.
- [x] Regression tests: 501 passed, including real cross-drive output.
- [x] Local self-contained ZIP, boot check, CRC and extracted CLI sample-profile.
- [ ] Complete broad UI/CLI/Papyrus/FaceGen review: delegated reviewers failed before inspection.
- [ ] Diagnose initial intermittent parent batch failure; detailed test logging added.
- [x] GitHub v3.7.1 published from green SHA 20af6e5 (CI + CodeQL success, asset bytes verified).
- [ ] Nexus upload of FollowerForge-3.7.1-win-x64.zip (owner step; changelog text delivered).
- [ ] Visual walkthrough and in-game confirmation.

See FollowerForge 3.7.1/VALIDATION.md for exact evidence and remaining risks.

# FollowerForge 3.7.0 plan

1. [x] Snapshot 3.6.1 -> 3.7.0 (3.6.1 is released and immutable: GitHub v3.6.1). 238 files
       copied, 0 failed, 0 mismatched.
2. [x] Creature races in combat transformation, which is what the user asked for. Root cause
       was a filter in the wrong place, not a missing feature: the transform picker reused the
       identity picker's creature exclusion, which exists because a creature has no head data
       and therefore no buildable face. That constrains the race she IS, not the race she
       TURNS INTO. The comment above the offending line even said "all races here, not the
       follower-suitable subset - a beast form is the whole point"; the code did the opposite.
3. [x] `_transformRaces` (creatures first, then vanilla, then custom) feeds the focus card,
       its search box, the Expert Deck and selection restore. Those four had drifted apart -
       the deck honoured the identity page's creature checkbox and the focus card never did.
4. [x] Creature rows carry FormID + EditorID. 753 extra rows with repeating names otherwise.
5. [x] Found and fixed the actual "Must be fixed" from the 3.6.1 user report, which 3.6.1 had
       only guessed at. Armour + legacy outfit = one hard error per piece, always. The
       validator checked the generated-outfit path even when the compiler took the
       chosen-outfit path. Now it follows the same branch, and warns once instead.
6. [x] RaceMenu preset body shape (bodyMorphs) now warns instead of vanishing silently.
       Deferred from 3.6.1 as "the overlay warning"; the evidence on disk said body shape is
       the bigger and detectable half, so that is what shipped. Overlays are named in the same
       warning without pretending to detect them.
7. [x] 489 Release tests (478 inherited + 11 new). End-to-end CLI build of the reported
       combination verified, including the VMAD FormID for the creature beast race.
8. [x] Published, pushed, CI green on `eb4c2de`, tagged `v3.7.0`, release created as Latest
       with the zip attached (99,252,454 B, SHA-256 A2A1A067...). NEXUS-UPLOAD refreshed.
9. [ ] Nexus upload - the author's step. The page is still on 3.6.0, and 3.6.1 was never
       uploaded there, so this one upload delivers both.

Next, deliberately NOT in 3.7.0:
  - Detecting RaceMenu overlays specifically. Neither reference preset on this machine has an
    overlay block, so the key name cannot be confirmed from evidence and will not be guessed.
    Needs a preset that actually carries overlays.
  - MO2 `modlist.txt` priority direction is still UNVERIFIED (PluginLists.cs:69, open since
    3.2.5). Needs a real MO2 instance or a reporter's modlist.txt + a screenshot of MO2's order.
  - Publish from CI on tag, so the shipped zip is provably the one the tests ran against.
  - Whether a creature transform survives a real fight in game. SetRace() on a follower is
    engine behaviour this build cannot exercise; the plugin side is verified, the gameplay
    side is not.

# FollowerForge 3.6.1 plan

1. [x] Snapshot 3.6.0 → 3.6.1 (3.6.0 is released and immutable: GitHub v3.6.0, Nexus 187479).
2. [x] Add "Copy diagnostics" — Review page button + Ctrl+K command. Pure `DiagnosticsReport`
       renders; the window only gathers. Home paths become %USERPROFILE% / %LOCALAPPDATA% so a
       report pasted into a public Nexus comment does not publish the reporter's account name.
3. [x] 11 tests, including one that asserts the rendered report never contains
       `Environment.UserName`. Suite 461 → 472.
4. [x] Verified against the live machine, not just fixtures: rendered a real report through
       `EnvironmentDiscovery` (Vortex, 2,921/3,001 plugins) — redaction held, and it surfaced a
       genuine live warning about undeployed Vortex changes.
5. [x] CI asked for the .NET 9 SDK for net10.0 projects; green only because the runner image
       preinstalls .NET 10. Now 10.0.x.
6. [x] Deleted `FollowerForge 3.6.0/dist` — rebuilt 3 minutes after the release upload from the
       same commit and did NOT match it. Both hashes recorded in VALIDATION.md.
7. [x] Docs caught up: 3.6.0 shipped to GitHub and Nexus on 2026-08-19; PLAN/STATE said otherwise.
8. [ ] Publish 3.6.1 zip, refresh NEXUS-UPLOAD, push, tag `v3.6.1`.

Next, deliberately NOT in 3.6.1:
  - RaceMenu overlays (tattoos/warpaint) do not transfer. Ship the build-time WARNING first;
    treat actually solving it as a separate project, not a rider on a patch release.
  - MO2 `modlist.txt` priority direction is still UNVERIFIED (PluginLists.cs:69, open since
    3.2.5). Needs a real MO2 instance or a reporter's modlist.txt + a screenshot of MO2's order.
  - Publish from CI on tag, so the shipped zip is provably the one the tests ran against.

# FollowerForge 3.6.0 plan

1. [x] Inspect 3.5.0 UI and preserve user-owned root changes.
2. [x] Approve Studio → Focus Cards → Expert Deck architecture.
3. [x] Snapshot 3.5.0 → 3.6.0; leave 3.5.0 untouched.
4. [x] Review and approve the written UI design specification.
5. [x] Produce an implementation plan with test-first milestones.
6. [x] Implement theme tokens, shell/navigation, cards, dedicated pickers, and Expert Deck.
6b. [x] Fix Expert Deck single-choice crash (mode-aware selection sync) + 4 regression tests. (Kimi)
6c. [x] UI polish pass: density/clarity rework + live-theme chips and setup windows (theme-leak fix) + ThemeCoverageTests; 454 tests. (Kimi)
6d. [x] UI polish pass 3: per-theme distinct status hues + tinted pill chips (user: "still yellow"); 456 tests. (Kimi)
6e. [x] UI polish pass 4: overlay opacity, Light-theme variant fix, sidebar pills, auto-sizing lists/cards; headless screenshot harness. (Kimi)
6f. [x] Release-readiness: deck apply no longer wipes sibling slices; readiness not red on empty draft; palette Enter/arrows/Build/Paths/MO2; real EditorIDs including races; checkbox Apply; overlay-safe shortcuts. 461 tests. (Grok)
7. [x] Run exact Release tests, UI smoke checks, encoding guard, and source-path scan.
8. [x] Build the self-contained Windows release, validate archive/hash, and update docs.
9. [x] Pushed, CI green, tagged `v3.6.0`, zip attached (99,245,718 B, SHA-256 3EFA07D9…),
       and the Nexus page updated to 3.6.0 on 2026-08-19.

# FollowerForge 3.5.0 plan

1. [x] Prove werewolf non-revert from vanilla WerewolfTransformVisual + our Wait race.
2. [x] Snapshot 3.4.0 → 3.5.0.
3. [x] Failing tests, then stop attaching WerewolfChangeFX, then rewrite FF_Transform.
4. [x] Recompile PEX. 388 tests pass.
5. [x] Publish zip built (boot check passed) and attached to the GitHub v3.5.0 release.

# FollowerForge 3.4.0 plan

1. [x] Prove the three user requests are solvable in 3.3.0 before writing.
2. [x] Snapshot 3.3.0 to 3.4.0; leave 3.3.0 untouched.
3. [x] Persist xVASynth + output paths; scan Steam libraries; allow explicit Vortex/MO2 dest.
4. [x] Gender-matched wizard pronouns from the existing Sex box.
5. [x] Show gear FormIDs (and search them) on armor/weapons/ammo/belongings/skins.
6. [x] Tests for settings, locator, pronouns, FormID display, write-guard Allow, publishRoot.
7. [x] 386 Release tests pass.
8. [x] `Publish-FollowerForge.ps1` zip built (shipped inside the v3.5.0 GitHub release). Nexus upload stays manual.
