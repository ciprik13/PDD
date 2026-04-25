# Backend — Claude playbook

This file is read on every Claude Code session inside `backend/`. Treat it as enforceable rules, not suggestions.

## What this codebase is

**AgriCure backend** — .NET 9 Clean Architecture API for plant-disease detection. Receives detection events from edge devices (Jetson + YOLOv8), stores them in PostgreSQL, and serves the React/Vite dashboard living in `../frontend/` (own branch).

## Project layout

```
backend/
├── src/
│   ├── AgriCure.Domain/         # entities, value objects, enums; no deps
│   ├── AgriCure.Application/    # use cases (MediatR), DTOs, validators, interfaces (-> Domain)
│   ├── AgriCure.Infrastructure/ # EF Core, Identity, JWT, Hangfire, repos (-> Application, Domain)
│   └── AgriCure.Api/            # controllers, middleware, Program.cs (-> Application, Infrastructure)
└── tests/
    ├── AgriCure.Domain.Tests/
    ├── AgriCure.Application.Tests/
    └── AgriCure.Api.IntegrationTests/   # WebApplicationFactory + Testcontainers Postgres
```

**Reference rule:** dependencies point inward only. `Domain` depends on nothing. `Api` references `Application` + `Infrastructure` but never the other way around.

## Documentation strategy (the part frontend devs need)

The frontend team builds against the API contract **without reading our code**. Keep these in sync, in this order of precedence:

1. **Swagger / OpenAPI = source of truth.**
   - Every controller action MUST have XML doc comments (`<summary>`, `<param>`, `<response>`) and `[ProducesResponseType]` for every status code returned.
   - The `AgriCure.Api.csproj` generates the XML doc file; Swashbuckle reads it and renders types + descriptions in `/swagger`.
   - JWT bearer auth is wired into Swagger UI ("Authorize" button) so they can try authenticated calls.
2. **`backend/docs/api-contract.md`** — human overview, NOT a per-field listing. Sections: auth flow with curl examples, pagination conventions, error format (ProblemDetails), versioning policy, links to `/swagger`. Update **only** when conventions change.
3. **`backend/README.md`** — quick-start in <2 minutes (`docker compose up`).
4. **`backend/CONTRIBUTING.md`** — how to add an endpoint end-to-end.

When you add or modify an endpoint, you MUST:
- Update XML docs and `[ProducesResponseType]`.
- If the response shape changes AND the endpoint is consumed by `frontend/src/services/api.ts`, list the affected fields in the PR description under a "**API contract change**" heading.
- Do NOT invent endpoint shapes. The frontend's `api.ts` is the binding contract for endpoints already used by the dashboard.

## Commit and PR rules

- **Never add `Co-Authored-By: Claude ...`** to commit messages. The user reserves co-author trailers for real teammates. Use `/commit` (in `backend/.claude/commands/`) which already enforces this.
- Commits follow Conventional Commits: `feat(api): …`, `fix(infra): …`, `chore(docker): …`, `test(application): …`, `docs(api): …`.
- PRs target `develop`. Use `/create-pr` to open one with the standard template.

## Coding conventions

- File-scoped namespaces (`namespace X;`).
- Nullable reference types ON everywhere (configured in `Directory.Build.props`).
- `TreatWarningsAsErrors=true` — fix warnings, don't suppress unless there's a clear reason.
- camelCase JSON via the System.Text.Json default policy (matches the frontend's TS types).
- `Guid` keys for entities unless there's a domain reason to use something else.
- Async all the way down. Method names end with `Async` when they return `Task`/`ValueTask` (except for controller actions, which omit it by convention).

## Testing rules

- Unit tests in `*.Tests/`. Integration tests against the API in `AgriCure.Api.IntegrationTests/` using `WebApplicationFactory<Program>`.
- Integration tests use **Testcontainers PostgreSQL** so each run gets a clean DB. Docker must be running.
- TDD on changed behavior: write the failing integration test first, then make it pass. The `tdd-dotnet` skill enforces this loop.
- FluentAssertions for readable failures (`response.Should().BeEquivalentTo(...)`).
- Test method naming: `Method_under_test_returns_X_when_Y` — underscores are intentional and CA1707 is suppressed in `tests/.editorconfig`.

## Useful slash commands and skills

These are scoped to `backend/` (`backend/.claude/`):

| | Where | What |
|---|---|---|
| `/commit` | `commands/commit.md` | Conventional-commit message; never adds Claude co-author. |
| `/create-pr` | `commands/create-pr.md` | Push branch + open PR against `develop` with full template. |
| `/implement-ticket` | `commands/implement-ticket.md` | TDD-driven ticket execution; runs the brainstorm → plan → execute loop. |
| `add-endpoint` | `skills/add-endpoint.md` | Full walk-through for a new HTTP-callable surface. |
| `add-migration` | `skills/add-migration.md` | Safe EF Core migration playbook. |
| `update-api-docs` | `skills/update-api-docs.md` | Keep XML docs + ProducesResponseType + `api-contract.md` in lockstep. |
| `tdd-dotnet` | `skills/tdd-dotnet.md` | Red-green-refactor loop with WebApplicationFactory + Testcontainers. |

## Backlog (parked future work)

`backend/.claude/backlog/` — committed parking lot for ideas we'll get to. Don't lose them. To pick one up, change its status to `planned` and feed the file into `/implement-ticket`.

## Local settings (NOT committed)

`backend/.claude/settings.json` and `settings.local.json` are gitignored — each developer maintains their own permissions / env. Don't commit them.
