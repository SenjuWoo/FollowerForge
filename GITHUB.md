# GitHub

| | |
|--|--|
| **Repo** | https://github.com/SenjuWoo/FollowerForge |
| **Clone** | `git clone https://github.com/SenjuWoo/FollowerForge.git` |
| **Account** | SenjuWoo (the old ShugokiFable URL redirects here) |
| **Default branch** | `main` |
| **Owner work root** | workspace `FollowerForge` (versioned snapshots under this folder) |
| **Current ship tree** | `FollowerForge 3.7.4/` (see `CURRENT.txt`) |
| **This folder** | canonical publish home for this app |

## What to push

Publish FollowerForge **CURRENT** snapshot: `src/`, `docs/`, build/publish scripts,
`README.md`, `VERSION.txt` (no `bin/`, `obj/`, `dist/`). Root `README.md` + `CHANGELOG.txt`
+ `CURRENT.txt` describe the active release.

## Agent update checklist

1. Edit **only** under this owner root (no cross-agent copies).
2. If versioned: bump via `skyrim-versioned-workspace`; update `CURRENT.txt` / `CHANGELOG.txt`.
3. Stage the **CURRENT** ship tree (or paths listed above).
4. **Exclude:** secrets (`GPT_SECRET_KEY.txt`, `.env`), `bin/`, `obj/`, `build/`, `extern/` (CommonLibSSE), game masters (`Skyrim.esm` etc.), promo noise if not part of ship.
5. Commit on `main` and push:

```powershell
# from a clean staging copy of CURRENT (or this folder if it is the ship root)
git remote -v   # https://github.com/SenjuWoo/FollowerForge.git
git add -- "FollowerForge 3.7.4" CURRENT.txt STATE.md
git commit -m "Describe the change"
git push origin main
```

6. If the folder has no `.git` yet, create/link once:

```powershell
git init -b main
git remote add origin https://github.com/SenjuWoo/FollowerForge.git
# then add/commit/push as above (first push may need --force-with-lease only if rewriting intentional)
```

## Do not

- Create a second GitHub repo with a different name for the same mod.
- Push Claude/GPT/Grok twin trees to different remotes.
- Commit API keys or full game ESMs.

