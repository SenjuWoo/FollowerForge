# Workspace layout

Everything FollowerForge needs lives here. If you are looking for "where did that go", this file
answers it.

```
FollowerForge/
  FollowerForge-main/          the git repository (source, docs, snapshots) - the only folder that matters
  _attic/                      things kept out of the way, safe to delete whenever you like
```

## Inside the repository

| Path | What it is | Safe to delete |
|---|---|---|
| `src/` | current source, tests, UI, CLI | no |
| `FollowerForge 3.x.y/` | frozen snapshots, one per candidate or release | no - this is the history |
| `artifacts/` | the last shipped zip plus its `SHA256SUMS.txt` | yes, it is on GitHub Releases |
| `docs/`, `*.md`, `CHANGELOG.txt` | project documentation | no |
| `bin/`, `obj/`, `dist/` | build output, recreated by `dotnet build` and the publish script | yes |

Snapshot folders hold **source and docs only**. They deliberately do not carry `bin/`, `obj/` or
`dist/`: those were 1.9 GB per snapshot and are regenerable. `dotnet build` inside a snapshot
rebuilds it.

## Where the build output goes

- `dotnet build` writes `bin/` and `obj/` beside each project. Gitignored.
- `Publish-FollowerForge.ps1 -Version x.y.z` writes `dist/FollowerForge x.y.z/` (the unpacked app)
  and `dist/FollowerForge-x.y.z-win-x64.zip`. Gitignored; `dist/` is replaced on every publish.

## Keeping it small

```
pwsh tools/Clean-BuildOutput.ps1 -WhatIf   # show what would go
pwsh tools/Clean-BuildOutput.ps1           # remove bin/, obj/, dist/, publish.log
```

Run it after a release, or whenever the folder creeps past a couple of hundred megabytes. On
2026-11-09 it took the repository from 15 GB to 175 MB.

## What is in `_attic/`

- `3.7.6-unpacked/` - an old unpacked build of the app (the released 3.7.6 zip is on GitHub).
- `root-docs/` - copies of changelogs and the README that used to sit loose in the workspace root.

Nothing in `_attic/` is referenced by the build. Delete the whole folder if you want the space.
