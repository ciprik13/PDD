---
description: Push the current branch and open a pull request against develop with the standard backend template.
---

# /create-pr

Open a PR for the current backend work.

## Inputs

`$ARGUMENTS` — optional. If text is provided, treat it as the PR title hint.

## Steps

1. **Inspect state in parallel.**
   - `git status`
   - `git rev-parse --abbrev-ref HEAD` (current branch)
   - `git log develop..HEAD --oneline` (commits to ship)
   - `git diff develop...HEAD --stat` (overall scope)
   - `git diff develop...HEAD` (the actual changes — read selectively, focus on src/ and test/ files)
   - `git ls-remote --heads origin <current-branch>` (does it already track remote?)

2. **Sanity checks.**
   - Branch must NOT be `develop` or `master`. If it is, stop and ask the user to create a feature branch first.
   - Working tree should be clean (no uncommitted changes). If not, run `/commit` first or stop and ask.
   - At least one commit must exist between `develop` and `HEAD`. If not, stop.

3. **Push the branch.**
   ```bash
   git push -u origin HEAD
   ```

4. **Read existing PRs to match the user's style.**
   ```bash
   gh pr list --base develop --state all --limit 5
   ```

5. **Draft the PR title.**
   - Mirror the dominant commit type if they all agree, e.g. `feat(api): treatments endpoint and seed data`.
   - Keep under 70 chars.

6. **Draft the PR body** using this template (HEREDOC, exact structure):

   ```bash
   gh pr create --base develop --head HEAD --title "<title>" --body "$(cat <<'EOF'
   ## Summary
   - bullet 1
   - bullet 2

   ## What changed
   - **api/**: …
   - **application/**: …
   - **infrastructure/**: …
   - **tests/**: …
   - **docs/**: …

   ## API contract changes
   <!-- If any endpoint shape that's consumed by frontend/src/services/api.ts changed,
        list affected fields here. If nothing changed, write "None." -->
   None.

   ## Test plan
   - [ ] `dotnet build` clean
   - [ ] `dotnet test` all green
   - [ ] Manual: run the new endpoint via Swagger and confirm shape
   - [ ] (if migrations) ran `dotnet ef database update` against a fresh container

   ## Notes
   <!-- screenshots, swagger snippets, links to backlog files this clears, etc. -->

   Refs: #<issue-or-backlog-file>
   EOF
   )"
   ```

7. **Print the PR URL** as the final output.

## HARD RULES

- Target branch is **`develop`**, never `master` or `main`.
- Never force-push without an explicit user instruction.
- Never push directly to `develop`.
- Don't include `Co-Authored-By: Claude` in any commit picked up by this PR.
- Never use `gh pr merge` from this command — merging is the user's call.

## Edge cases

- **Existing PR for this branch:** report the URL and stop. Don't open a duplicate.
- **`gh` not installed / not authenticated:** print `gh auth status` output and ask the user to authenticate.
