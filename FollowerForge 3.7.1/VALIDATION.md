# 3.7.1 local repair candidate — validation

## Scope and provenance
Parent: released FollowerForge 3.7.0. Snapshot: 240 files, zero SHA-256 mismatches, bin/obj/dist excluded. Source changes remain in the supplied owner workspace. No deployment or GitHub publication performed.

This is a verified repair pass, NOT an exhaustive audit or proof of perfection. Both independent review subagents failed before reviewing code due to an API weekly limit; their scopes are not counted as completed.

## Fixes and evidence
- MO2 priority: upstream https://raw.githubusercontent.com/ModOrganizer2/modorganizer/master/src/profile.cpp lines 258–260 serialize reverse priority via crbegin/crend. Parser regression failed before correction; normalized order and custom-path winning-plugin/texture checks pass. Existing tests had encoded the wrong disk ordering and were corrected.
- Missing explicit head NIF: targeted builder regression returned success before correction; now produces FACE_NIF_MISSING and does not publish.
- Explicit output guard: targeted game-Data destination test threw no exception before correction; now rejects it before writing. Intentionally chosen manager mods folders remain supported.
- Cross-volume publish: real source C: temp to Z: project output failed before correction with IOException: Source and destination path must have identical roots. Destination-side preparation and backup/rollback now pass that exact test. Locked previous-output preservation also passes. This is not crash-atomic across the two directory renames; power loss can leave a .ff-backup folder for recovery. Concurrent builds of the same follower are not tested.
- Packaging script no longer terminates every running FollowerForge instance by process name. Its own boot-check child is still terminated after the check.
- Batch failure logging now includes captured internal build logs in the test assertion. No batch production fix claimed.

## Executed verification
- Initial unchanged parent suite: 488 passed, 1 failed (Phase6Tests.Batch_BuildsAllProfiles expected 2, actual 1).
- Repeated unchanged parent suite: 489 passed. Isolated Phase6Tests with crash/hang collection: 4 passed on each snapshot. The flake reproduced a third time on unchanged 3.7.0 (488/1, background baseline run completing after the 3.7.1 work): every reproduction is on 3.7.0, zero on 3.7.1. BatchBuilder catches per-item exceptions and the 3.7.0 test discarded logs; 3.7.1's BatchTestLog sink now surfaces the internal reason on recurrence. No production batch change claimed.
- Final new suite with FFORGE_TEST_PUBLISH_ROOT set to the Z: snapshot test-output directory: 501 passed, 0 failed, 0 skipped.
- Publish-FollowerForge.ps1 -Version 3.7.1: exit 0, self-contained UI/CLI produced; staging GUI stayed alive during 12-second boot check.
- Release pre-flight: Phase6 batch-flake candidate run 10 consecutive times on 3.7.1 — 10/10 green — before pushing to GitHub CI.
- Final ZIP: CRC check passed; five entries: FollowerForge.exe, cli/FollowerForge.Cli.exe, README.md, CHANGELOG.txt, NEXUS-CHANGELOG-3.7.1.txt.
- Fresh extraction: packaged CLI sample-profile command exited 0 and produced parseable JSON (Aria Forge / FF_AriaForge.esp).
- Extracted CLI build --profile smoke-profile.json --out build-smoke --zip: exit 0, BUILD OK. Read the live Vortex environment (2932 enabled plugins), generated a 926-byte ESP with Skyrim.esm master, ship gate ESL=True / formVersion=44 / HEDR=1.71, then packaged 9 files. No custom FaceGen in this starter profile. It warned that Dynamic Detection Overhaul.esp is enabled but missing from deployed Data; no deployed files were modified.
- Git whitespace check reports four inherited Markdown hard-break lines in the copied 2026-08-18 design specification. Preserved as inherited document formatting.
- Packaged CLI --help prints usage but exits 2 as an unknown command; this inherited behavior is not corrected in this pass.

## Final packaging follow-up
- Final Publish-FollowerForge.ps1 run exited 0 after hub fixes, with boot check and ZIP creation successful. Final archive CRC check passed during extraction.
- First final smoke attempt used an incorrect positional sample-profile argument. Corrected invocation with --out then passed from a fresh extraction at C:/Users/karlo/AppData/Local/Temp/ff-verified-0yf9cxxd: sample-profile exit 0; build --profile sample.json --out work --zip exit 0, BUILD OK, 926-byte ESP, ship gate pass and 9-file follower ZIP.

## Artifact
File: dist/FollowerForge-3.7.1-win-x64.zip
Bytes: 99254250
SHA-256: 82271deaad1029c77e0a72fe2d789580e7de386f6312e9152d82ed15b4e3f8fa

## Remaining work / risks
- Full UI/CLI interaction and saved-profile audit remains incomplete. The existing headless renderer completed all five themes; inspected dark/light review at 1040x700 and teal loadout at 2560x1440. No overlapping displayed controls in those frames. This is not a complete interaction walkthrough. Renderer emitted MSB3246 and obsolete Bitmap.Save warnings; the UI still shows inherited CREATOR STUDIO 3.6 branding.
- Papyrus was read, not recompiled/decompiled or verified in Skyrim. Transformation save/reload/combat behavior remains unconfirmed. Earlier conversational claim of matching PEX was withdrawn.
- Complete FaceGen fidelity and RaceMenu overlays remain unverified.
- HubBuilder now shares DirectoryPublisher with followers. Locked-asset regression failed before repair (previous plugin deleted), then passed; empty-name and escaping-folder regressions also failed before validation was added, then passed. Empty reserved directories are preserved.
- ProfileIo still directly writes JSON; power-failure-safe saving remains a candidate improvement.
- Path guards are lexical; junction-based paths and unusual save locations have not been audited.
- GitHub main is two documentation-only commits ahead of local parent 70e28aa. Old ShugokiFable repo redirects to SenjuWoo. Preserve upstream README/community files before any future publication.
