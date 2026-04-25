---
description: Take a ticket description and execute it end-to-end with TDD, plan-first, and skill-driven workflow. Never skip the brainstorm step.
---

# /implement-ticket

Implement a backend ticket the careful way: brainstorm → plan → write failing test → make it pass → keep docs in sync → offer to open a PR.

## Inputs

`$ARGUMENTS` — REQUIRED. The ticket description (free-form text or a path to a backlog file like `backend/.claude/backlog/001-websocket-live-feed.md`).

If `$ARGUMENTS` is empty, stop and ask the user for the ticket description.

## Steps

### 1. Restate
In your own words, summarize what the ticket asks for in 2–4 bullets. List any assumptions you're making. STOP and ask the user if any assumption seems load-bearing.

### 2. Brainstorm (don't skip)
If the `superpowers:brainstorming` skill is available, invoke it with the ticket. Otherwise:
- Ask 1–3 clarifying questions about scope, edge cases, and success criteria.
- Identify which existing code is affected (use Grep / Glob — read the relevant files).
- Note whether this changes the frontend contract (`frontend/src/services/api.ts`).

### 3. Plan
If `superpowers:writing-plans` is available, invoke it. Otherwise produce a short plan:
- Files to create / modify
- Domain/Application/Infrastructure/Api split
- Migration needed? (If yes, queue the `add-migration` skill.)
- New endpoint? (If yes, queue the `add-endpoint` skill.)
- Test plan (which integration tests, what they assert)

Get user approval before touching code.

### 4. TDD loop
Invoke the `tdd-dotnet` skill (`backend/.claude/skills/tdd-dotnet.md`):
- Write the failing integration test first against `WebApplicationFactory<Program>`.
- Watch it fail (compile error or assertion failure — both count).
- Implement the minimum to make it pass.
- Refactor while green.
- For multi-step tickets, repeat per slice rather than writing all tests upfront.

### 5. Keep docs in sync
Whenever a controller action is added/changed:
- Invoke the `update-api-docs` skill — it walks XML docs, `[ProducesResponseType]`, and `docs/api-contract.md` (only if conventions changed).
- If response shape changed AND endpoint is consumed by `frontend/src/services/api.ts`, prepare a "**API contract change**" bullet for the eventual PR description.

### 6. Verify
- `dotnet build` — clean.
- `dotnet test` — all green.
- If you added an endpoint: hit it manually via Swagger or curl and paste the result in the conversation.

### 7. Wrap up
- Show a final summary: what shipped, what's tested, any contract changes.
- If the ticket originated from a backlog file, offer to mark it `status: done` (the user decides whether to delete the file or keep it as a record).
- End with: "Ready to commit and open a PR? Run `/create-pr`." Don't run it yourself — let the user trigger it.

## HARD RULES

- Never skip the brainstorm step, even for "simple" tickets — assumptions hide there.
- Never write implementation code before the failing test exists.
- Never invent endpoint shapes that contradict `frontend/src/services/api.ts`. If a divergence is required, surface it explicitly and let the user decide.
- Never add `Co-Authored-By: Claude` to commits this command produces (`/commit` enforces this).
- Never run `/create-pr` from inside this command — the user triggers it.

## Edge cases

- **Ticket is too big to brainstorm in one go:** propose splitting it into stacked sub-PRs, like the original 6-PR series.
- **Ticket conflicts with the backlog:** if the work is already parked in `backend/.claude/backlog/`, point at that file and confirm we're picking it up now.
- **Ticket lacks a frontend mention but touches an existing API shape:** assume frontend cares; surface the contract impact anyway.
