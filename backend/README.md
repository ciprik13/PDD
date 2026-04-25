# AgriCure Backend

.NET 9 Clean Architecture API for the AgriCure plant-disease detection platform. REST endpoints for the dashboard, JWT auth, Hangfire background jobs, PostgreSQL persistence — all dockerized.

## Quick start

> Requirements: Docker (Compose v2). .NET 9 SDK only needed for running the test suite or `dotnet run` against the dockerized Postgres directly.

```bash
cd backend
cp .env.example .env
docker compose up --build
```

Then check:

```bash
curl http://localhost:8080/health/ready
# → {"status":"Healthy","checks":[{"name":"postgres","status":"Healthy","description":null}]}
```

`docker compose down -v` tears the stack down and drops the Postgres volume.

### Running tests

```bash
cd backend
dotnet test
```

The integration tests use [Testcontainers](https://dotnet.testcontainers.org) to spin up an ephemeral Postgres per run, so Docker must be running but no manual setup is needed.

### Running the API outside Docker

The api needs a connection string. Either:
- Bring up only Postgres (`docker compose up postgres`) and point the api at `localhost:5432` via a `ConnectionStrings__Default` env var, or
- Set the env var to any reachable Postgres instance.

```powershell
$env:ConnectionStrings__Default = "Host=localhost;Port=5432;Database=agricure;Username=agricure;Password=change-me"
dotnet run --project src/AgriCure.Api
```

Migrations run automatically on startup in the `Development` environment.

## Default ports

| Service | Port | Path |
|---|---|---|
| API | 8080 | `/` |
| Swagger UI | 8080 | `/swagger` |
| Hangfire dashboard | 8080 | `/hangfire` (admin role required) |
| PostgreSQL | 5432 | — |
| Health (liveness, "self") | 8080 | `/health` |
| Health (readiness — Postgres + Hangfire) | 8080 | `/health/ready` |

## Authentication

JWT bearer with refresh-token rotation. All endpoints under `/api/auth/*` are anonymous; everything else expects a bearer token (or is explicitly `[AllowAnonymous]`).

```bash
# Register (creates a user with the `user` role).
curl -s -X POST http://localhost:8080/api/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"email":"me@example.com","password":"P@ssw0rd!ABC"}'

# Login (same shape).
curl -s -X POST http://localhost:8080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"me@example.com","password":"P@ssw0rd!ABC"}'

# Refresh — rotates both tokens, revokes the old refresh token.
curl -s -X POST http://localhost:8080/api/auth/refresh \
  -H 'Content-Type: application/json' \
  -d '{"refreshToken":"<refresh-from-login>"}'

# Logout (idempotent).
curl -s -X POST http://localhost:8080/api/auth/logout \
  -H 'Content-Type: application/json' \
  -d '{"refreshToken":"<refresh-from-login>"}'
```

In Swagger, click **Authorize** at the top right and paste the access token (without the `Bearer ` prefix) to call protected endpoints.

The admin user is seeded on startup from `Admin:Email` / `Admin:Password` (set in `.env`). Hangfire dashboard at `/hangfire` requires that admin role.

In production, run with the override skipped so host ports aren't bound:

```bash
docker compose -f docker-compose.yml up
```

Then expose the api via your reverse proxy of choice (nginx, Caddy, etc.).

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
├── .config/dotnet-tools.json     pinned dotnet-ef for migrations
├── Dockerfile                    multi-stage build for AgriCure.Api
├── docker-compose.yml            api + postgres (prod-shaped)
├── docker-compose.override.yml   dev port mappings + ASPNETCORE_ENVIRONMENT=Development
├── .env.example                  copy to .env and customize
├── CLAUDE.md                     project rules — read this before contributing
├── CONTRIBUTING.md               how to add an endpoint end-to-end
└── README.md                     this file
```

## Logs

Serilog writes structured logs to two sinks:

- **Console** — what `docker compose logs api` and `dotnet run` show.
- **Rolling file** — `logs/agricure-YYYYMMDD.log` next to the api binary (inside the container: `/app/logs/`). Daily rolling, 14 days retained.

Inside Docker the `app` user owns `/app/logs/`; mount a volume there if you need logs persisted across container rebuilds. In production prefer shipping the structured stdout to your log aggregator and skip the file sink — it's there for local debugging convenience.

`/hangfire` shows the recurring `daily-detection-summary` job — currently a placeholder that just logs.

## Migrations

```bash
cd backend
dotnet tool restore                                                       # first time only
dotnet ef migrations add <Name> \
  --project src/AgriCure.Infrastructure \
  --startup-project src/AgriCure.Api \
  --output-dir Persistence/Migrations
```

Migrations live in `src/AgriCure.Infrastructure/Persistence/Migrations/` and apply automatically when the api starts in the `Development` environment. In production, run `dotnet ef database update` (or wire a one-shot migrator container) before promoting a build.

## API documentation

The API contract is defined by Swagger / OpenAPI. Once the API is running, browse to `/swagger` for the interactive UI, or fetch `/swagger/v1/swagger.json` to regenerate TypeScript types in the frontend project.

For human-readable conventions (auth flow, error format, pagination, versioning), see [`docs/api-contract.md`](./docs/api-contract.md).

## Contributing

See [`CONTRIBUTING.md`](./CONTRIBUTING.md) for the end-to-end "add an endpoint" walk-through.

## Branching

Stacked PRs targeting `develop`:

| PR | Branch | Status |
|---|---|---|
| #1 | `backend/01-foundation` | merged |
| #2 | `backend/02-docker-database` | this branch — Docker, Postgres, EF Core, health checks |
| #3 | `backend/03-hangfire-serilog` | next — background jobs, structured logging |
| #4 | `backend/04-auth` | ASP.NET Identity + JWT |
| #5 | `backend/05-detections` | `/api/detections` CRUD matching frontend contract |
| #6 | `backend/06-dashboard-stubs` | stubs for remaining frontend endpoints |
