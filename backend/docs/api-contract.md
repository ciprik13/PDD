# AgriCure API — Conventions

This is a **conventions** document, not a per-endpoint reference. The full per-endpoint contract lives in **Swagger** at `/swagger` (or `/swagger/v1/swagger.json` if you want to regenerate types).

> Frontend devs: if you have the API running locally, the easiest way to learn the contract is to open `http://localhost:8080/swagger`, click "Authorize", paste a JWT, and exercise the endpoints.

## Base URL

| Environment | Base URL |
|---|---|
| Local dev | `http://localhost:8080` |
| Production | TBD (set in `VITE_API_BASE_URL` on the frontend) |

All endpoints live under `/api/...`.

## Authentication

JSON Web Tokens (JWT) issued by the backend's `AuthController` (PR #4 onward). Two-token flow:

- **Access token** — short-lived (15 minutes). Sent via `Authorization: Bearer <token>` on every protected call.
- **Refresh token** — long-lived (7 days), rotates on use. Stored server-side so we can revoke.

```bash
# Register
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"dev@agricure.test","password":"strong-password-123!"}'

# Login
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"dev@agricure.test","password":"strong-password-123!"}'
# -> { "accessToken": "...", "refreshToken": "...", "expiresInSeconds": 900 }

# Use access token
curl http://localhost:8080/api/detections \
  -H "Authorization: Bearer <accessToken>"

# Refresh
curl -X POST http://localhost:8080/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"..."}'

# Logout (revokes the refresh token)
curl -X POST http://localhost:8080/api/auth/logout \
  -H "Authorization: Bearer <accessToken>" \
  -d '{"refreshToken":"..."}'
```

## JSON conventions

- All responses are JSON with `application/json; charset=utf-8`.
- Property casing: **camelCase** (`detectionsToday`, `topPrediction`, etc.) — matches the frontend's TypeScript types.
- Dates: ISO 8601 with timezone, e.g. `"2026-04-25T14:32:09.123Z"`.
- IDs: GUIDs as strings.
- Enums: lowercase snake_case strings (`"late_blight"`, `"early_blight"`, `"healthy"`).

## Error format — RFC 7807 ProblemDetails

Errors follow [RFC 7807](https://datatracker.ietf.org/doc/html/rfc7807):

```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Not Found",
  "status": 404,
  "detail": "Detection 'det-999' was not found.",
  "instance": "/api/detections/det-999",
  "traceId": "00-..."
}
```

Validation errors (HTTP 400) include a per-field `errors` map:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "email": ["'Email' is not a valid email address."],
    "password": ["'Password' must be at least 8 characters."]
  },
  "traceId": "00-..."
}
```

## Pagination

For list endpoints, default and max page sizes are documented per-endpoint in Swagger. Common pattern:

- `?limit=20` — max 200, default per endpoint.
- `?offset=0` — when pagination beyond simple `limit` is needed.

If an endpoint diverges, Swagger is authoritative.

## Versioning

URL-versioned: future breaking changes will land at `/api/v2/...`. The current implicit version is `v1`. Don't include `/v1/` in URLs until v2 ships.

## Rate limits

Not enforced in PR #1. To be added once we hit production traffic — track via the backlog file `backend/.claude/backlog/` (TBD).

## Health checks

- `GET /health` — liveness. Returns 200 if the process is up.
- `GET /health/ready` — readiness. Returns 200 only if Postgres (and Hangfire storage) are reachable.

Use `/health/ready` for load-balancer checks.

## Endpoints

The full list lives in **Swagger UI**. Below is a quick index of what's planned across the 6-PR series:

| PR | Endpoint | Purpose |
|---|---|---|
| #4 | `POST /api/auth/register` | Create user |
| #4 | `POST /api/auth/login` | Issue access + refresh tokens |
| #4 | `POST /api/auth/refresh` | Rotate refresh token |
| #4 | `POST /api/auth/logout` | Revoke refresh token |
| #5 | `GET /api/detections?limit=` | List recent detections |
| #5 | `GET /api/detections/{id}` | Single detection |
| #5 | `POST /api/detections` | Ingest a detection (edge devices) |
| #5 | `PUT /api/detections/{id}` | Update detection |
| #5 | `DELETE /api/detections/{id}` | Remove detection |
| #6 | `GET /api/system/status` | Device + model + sync info |
| #6 | `GET /api/dashboard/stats` | Aggregate dashboard tiles |
| #6 | `GET /api/camera/frame` | Latest camera frame metadata |
| #6 | `GET /api/stand/position` | Current stand GPS + row |
| #6 | `GET /api/passports/{plantId}` | Plant passport (event log) |
| #6 | `GET /api/treatments?diseaseClass=` | Treatment recommendations |
| #6 | `GET /api/environment/latest` | Latest environment reading |

This table is updated only when we add/remove an endpoint. The shape of each endpoint is documented in Swagger and in the controller XML docs — not duplicated here.
