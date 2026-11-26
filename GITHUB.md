# GitHub

| | |
|--|--|
| **Repo** | https://github.com/SenjuWoo/FollowerForge |
| **Clone** | `git clone https://github.com/SenjuWoo/FollowerForge.git` |
| **Account** | SenjuWoo. The old login was released and is unclaimed: link only to `github.com/SenjuWoo/...` |
| **Default branch** | `main` |
| **Owner work root** | workspace `FollowerForge` (versioned snapshots under this folder) |
| **Current ship tree** | the snapshot named in `CURRENT.txt` (now `FollowerForge 3.9.0/`) |
| **This folder** | canonical publish home for this app |

## What to push

Publish FollowerForge **CURRENT** snapshot: `src/`, `docs/`, build/publish scripts,
`README.md`, `VERSION.txt` (no `bin/`, `obj/`, `dist/`). Root `README.md` + `CHANGELOG.txt`
+ `CURRENT.txt` describe the active release.

## Agent update checklist

1. Edit **only** under this owner root (no cross-agent copies).
2. If versioned: bump via `skyrim-versioned-workspace`; update `CURRENT.txt` / `CHANGELOG.txt`, and the two
   snapshot paths in `.github/dependabot.yml` (CI fails until they name the newest snapshot).
3. Stage the **CURRENT** ship tree (or paths listed above).
4. **Exclude:** secrets (`GPT_SECRET_KEY.txt`, `.env`), `bin/`, `obj/`, `build/`, `extern/` (CommonLibSSE), game masters (`Skyrim.esm` etc.), promo noise if not part of ship.
5. Commit on `main` and push:

```powershell
# from a clean staging copy of CURRENT (or this folder if it is the ship root)
git remote -v   # https://github.com/SenjuWoo/FollowerForge.git
git add -- "FollowerForge 3.9.0" .github/dependabot.yml CURRENT.txt STATE.md CHANGELOG.txt README.md
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
- Push per-assistant twin trees to different remotes.
- Commit API keys or full game ESMs.

