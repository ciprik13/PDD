# AgriCure Backend

.NET 9 Clean Architecture API for the AgriCure plant-disease detection platform. REST endpoints for the dashboard, JWT auth, Hangfire background jobs, PostgreSQL persistence — all dockerized.

## Quick start

> Requirements: .NET 9 SDK, Docker (for the local Postgres container in PR #2 and beyond), Git.

```bash
# from the repo root
cd backend
dotnet build
dotnet test
dotnet run --project src/AgriCure.Api
```

Open <http://localhost:5000/> — should respond with `AgriCure API up`.

Once Docker support lands (PR #2), the workflow becomes:

```bash
cp .env.example .env
docker compose up --build
```

API will be on <http://localhost:8080> and Swagger on <http://localhost:8080/swagger>.

## Default ports (after PRs #2–#6)

| Service | Port | Path |
|---|---|---|
| API | 8080 | `/` |
| Swagger UI | 8080 | `/swagger` |
| Hangfire dashboard | 8080 | `/hangfire` (admin only) |
| PostgreSQL | 5432 | — |
| Health (liveness) | 8080 | `/health` |
| Health (readiness, with DB ping) | 8080 | `/health/ready` |

## Repository layout

See `CLAUDE.md` for the full reference rules.

```
backend/
├── src/                          Clean Architecture: Domain → Application → Infrastructure → Api
├── tests/                        Domain.Tests, Application.Tests, Api.IntegrationTests
├── docs/api-contract.md          API conventions for frontend devs (Swagger is the source of truth)
├── .claude/
│   ├── commands/                 /commit, /create-pr, /implement-ticket
│   ├── skills/                   add-endpoint, add-migration, update-api-docs, tdd-dotnet
│   └── backlog/                  parked future ideas
├── CLAUDE.md                     project rules — read this before contributing
├── CONTRIBUTING.md               how to add an endpoint end-to-end
└── README.md                     this file
```

## API documentation

The API contract is defined by Swagger / OpenAPI. Once the API is running, browse to `/swagger` for the interactive UI, or fetch `/swagger/v1/swagger.json` to regenerate TypeScript types in the frontend project.

For human-readable conventions (auth flow, error format, pagination, versioning), see [`docs/api-contract.md`](./docs/api-contract.md).

## Contributing

See [`CONTRIBUTING.md`](./CONTRIBUTING.md) for the end-to-end "add an endpoint" walk-through.

## Branching

Stacked PRs targeting `develop`:

| PR | Branch | Status |
|---|---|---|
| #1 | `backend/01-foundation` | This branch — solution skeleton, .claude/, docs |
| #2 | `backend/02-docker-database` | Docker, Postgres, EF Core, health checks |
| #3 | `backend/03-hangfire-serilog` | Background jobs, structured logging |
| #4 | `backend/04-auth` | ASP.NET Identity + JWT |
| #5 | `backend/05-detections` | `/api/detections` CRUD matching frontend contract |
| #6 | `backend/06-dashboard-stubs` | Stubs for remaining frontend endpoints |
