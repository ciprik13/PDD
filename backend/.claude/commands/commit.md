---
description: Create a Conventional Commits commit for staged + relevant unstaged changes. Never adds Claude as co-author.
---

# /commit

Stage the right files and create a single, well-formed commit.

## Inputs

`$ARGUMENTS` — optional. If the user provided text, treat it as a hint about the intended commit message (don't quote it verbatim — refine it).

## Steps

1. **Inspect state in parallel.** Run these together:
   - `git status` (no `-uall`, large repos misbehave)
   - `git diff --staged`
   - `git diff` (unstaged)
   - `git log -10 --oneline` (style of recent commits)
   - `git rev-parse --abbrev-ref HEAD` (branch — informs the commit scope)

2. **Decide what to stage.**
   - If anything is already staged, that's the commit. Don't add more unless the user asked.
   - If nothing is staged, stage the modifications relevant to the user's intent. NEVER `git add -A` or `git add .` — call out specific paths.
   - DO NOT stage anything that looks like secrets (`.env`, `*.pfx`, `*.key`, files containing strings like `BEGIN RSA PRIVATE KEY`, anything matching `appsettings.*.json` other than `Development`). Warn the user instead.

3. **Draft the commit message — Conventional Commits, subject only.**
   - One line: `<type>(<scope>): <imperative summary>` — under 72 chars.
   - Types: `feat`, `fix`, `chore`, `docs`, `test`, `refactor`, `build`, `ci`, `perf`, `style`.
   - Scopes for this repo (use the closest one): `api`, `application`, `domain`, `infra`, `tests`, `docker`, `db`, `auth`, `hangfire`, `ci`, `deps`, `claude`.
   - **No commit body.** Do not write a multi-line description, even for "interesting" commits. Context belongs in the PR description, not in `git log`. The only exceptions are when the user explicitly asks for a body, or when a `Co-Authored-By: <human>` trailer is needed.

4. **Commit with a one-line `-m`:**
   ```bash
   git commit -m "feat(api): add /api/treatments query and DTO"
   ```

5. **HARD RULES — must not violate:**
   - **Never** include `Co-Authored-By: Claude ...` (or any AI attribution) in the message. Co-authors are reserved for real human teammates.
   - **Never** append `(PR #N)`, `(#N)`, `[PR #N]`, or any similar PR-number marker to the commit subject. The subject ends after the imperative phrase. Exception: when the user squash-merges via the GitHub UI, GitHub will auto-append `(#<pr-number>)` to the resulting commit on `develop` — that is the user's choice and must NOT be amended out.
   - **Never** use `--no-verify`. If a hook fails, fix the underlying issue and create a new commit.
   - **Never** use `--amend` after a hook failure (the commit didn't happen, so amending mutates the previous one — destructive).
   - **Never** stage `.env`, `*.pfx`, secrets, or large binaries.

6. **After committing**, run `git status` and report: branch, commit subject, files committed.

## Edge cases

- **No changes:** report it; don't create empty commits.
- **Pre-commit hook failed:** read the failure, fix, re-stage, create a NEW commit (don't amend).
- **User explicitly asks to add a co-author:** allowed only for named human teammates, e.g. `Co-Authored-By: Alex Doe <alex@example.com>` — never Claude/AI.
