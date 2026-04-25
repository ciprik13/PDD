# Contributing to the AgriCure backend

Most backend changes follow the same shape: a new endpoint, a schema change, or both. This file walks through the canonical recipe so your code, tests, and docs stay aligned with the rest of the project.

> If you're using Claude Code in this folder, the `add-endpoint`, `add-migration`, `update-api-docs`, and `tdd-dotnet` skills under `backend/.claude/skills/` automate most of this.

## Branching and PRs

- Branch off `develop`: `backend/<topic>` (e.g. `backend/04-auth`, `backend/feat-treatments-api`).
- One concern per PR. Smaller stacked PRs > one mega-PR.
- Use `/create-pr` (from `backend/.claude/commands/`) to open a PR with the standard template.
- Conventional commits: `feat(api):`, `fix(infra):`, `chore(docker):`, `test(application):`, `docs(api):`.
- **Never** add `Co-Authored-By: Claude ...` to commit messages.

## Adding a new endpoint, end-to-end

Suppose the frontend needs `GET /api/treatments?diseaseClass=late_blight` returning a `Treatment[]`.

1. **Check the contract.** Look at `frontend/src/services/api.ts` (on the frontend branch). If the endpoint is already typed there, mirror its shape exactly. If not, propose a shape and surface it in the PR description.
2. **Define the DTO** in `src/AgriCure.Application/Common/DTOs/TreatmentDto.cs` — camelCase JSON.
3. **Add the query / command** in `src/AgriCure.Application/Features/Treatments/GetTreatmentsQuery.cs`. Use MediatR.
4. **Add a FluentValidation validator** next to the query (`GetTreatmentsQueryValidator.cs`).
5. **Implement the handler** (`GetTreatmentsQueryHandler.cs`) — talk to the DbContext via `IApplicationDbContext`.
6. **Write the integration test FIRST** in `tests/AgriCure.Api.IntegrationTests/Treatments/GetTreatmentsTests.cs`. It should fail to compile or fail at runtime — that's what TDD wants.
7. **Add the controller action** in `src/AgriCure.Api/Controllers/TreatmentsController.cs`:
   - Route attribute matching the frontend.
   - `[ProducesResponseType(typeof(TreatmentDto[]), StatusCodes.Status200OK)]` and any error codes.
   - XML doc comments — Swagger reads them.
8. **Run the test until green.** Refactor while green.
9. **Update `docs/api-contract.md`** only if conventions changed (new error format, pagination, etc.). Do NOT list every endpoint there — Swagger is the source of truth.
10. **Open a PR** with `/create-pr`. The template prompts for "API contract changes" — fill it in honestly.

## Adding a database migration

1. Make the model change in `src/AgriCure.Infrastructure/Persistence/...` (entity config) and `src/AgriCure.Domain/`.
2. From `backend/`:
   ```bash
   dotnet ef migrations add <DescriptiveName> \
     --project src/AgriCure.Infrastructure \
     --startup-project src/AgriCure.Api
   ```
3. **Read the generated SQL.** EF Core sometimes picks unsafe operations (e.g., DROP+ADD column when a RENAME would do). Hand-edit the migration if needed.
4. Test against a fresh container and against a populated container — confirm no data loss.
5. **Never edit a migration that has been merged to `develop`.** Add a new one instead.

## Running the project locally

```bash
# Fast loop while developing
cd backend
dotnet watch --project src/AgriCure.Api

# Tests
dotnet test                                         # all
dotnet test tests/AgriCure.Api.IntegrationTests/    # integration only
```

Once Docker support lands (PR #2):

```bash
docker compose up --build
docker compose logs -f api
```

## Style

- File-scoped namespaces.
- Nullable on, warnings as errors.
- `var` for built-in / obvious types; explicit type when it improves readability.
- One public type per file; file name matches type name.
- Tests: `MethodUnderTest_when_X_returns_Y` is the preferred shape (underscores intentional).

## Where to put things

| Concern | Location |
|---|---|
| Domain entity / value object | `src/AgriCure.Domain/<Aggregate>/` |
| Use case (command / query / handler) | `src/AgriCure.Application/Features/<Area>/` |
| DTO | `src/AgriCure.Application/Common/DTOs/` |
| Validator | next to the command/query |
| EF entity config | `src/AgriCure.Infrastructure/Persistence/Configurations/` |
| Migration | `src/AgriCure.Infrastructure/Persistence/Migrations/` (generated) |
| Controller | `src/AgriCure.Api/Controllers/` |
| Middleware / extension method | `src/AgriCure.Api/Extensions/` |
| Integration test | `tests/AgriCure.Api.IntegrationTests/<Area>/` |
