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

JSON Web Tokens (JWT) issued by `AuthController`. Two-token flow:

- **Access token** — short-lived (15 minutes by default). Sent via `Authorization: Bearer <token>` on every protected call.
- **Refresh token** — long-lived (7 days by default), rotates on use. Stored server-side so we can revoke.

All four endpoints accept JSON request bodies and return JSON.

```bash
# Register — creates a user with the `user` role.
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"dev@agricure.test","password":"P@ssw0rd!ABC"}'

# Login — same shape.
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"dev@agricure.test","password":"P@ssw0rd!ABC"}'
# -> {
#      "accessToken": "...",
#      "accessTokenExpiresAt": "2026-04-25T14:47:09.000Z",
#      "refreshToken": "...",
#      "refreshTokenExpiresAt": "2026-05-02T14:32:09.000Z"
#    }

# Use access token on a protected call.
curl http://localhost:8080/api/detections \
  -H "Authorization: Bearer <accessToken>"

# Refresh — old refresh token is revoked, new pair issued.
curl -X POST http://localhost:8080/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"..."}'

# Logout — idempotent; revokes the refresh token.
curl -X POST http://localhost:8080/api/auth/logout \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"..."}'
```

The Hangfire dashboard at `/hangfire` requires the **admin** role; seed the admin user via `Admin:Email` / `Admin:Password` env vars.

## Detections (`/api/detections`)

Plant-disease detection events emitted by edge devices (Jetson + YOLOv8). **All actions require a JWT bearer token** (reads included).

```bash
# List newest first (limit 1–200, default 20).
curl http://localhost:8080/api/detections?limit=50 \
  -H "Authorization: Bearer <accessToken>"

# Single detection.
curl http://localhost:8080/api/detections/<id> \
  -H "Authorization: Bearer <accessToken>"

# Ingest a new detection (edge device).
curl -X POST http://localhost:8080/api/detections \
  -H "Authorization: Bearer <accessToken>" \
  -H "Content-Type: application/json" \
  -d @detection.json   # body shape below

# Replace a detection.
curl -X PUT http://localhost:8080/api/detections/<id> \
  -H "Authorization: Bearer <accessToken>" \
  -H "Content-Type: application/json" \
  -d @detection.json   # must include "id" matching the route

# Delete (idempotent — 204 even if id is missing).
curl -X DELETE http://localhost:8080/api/detections/<id> \
  -H "Authorization: Bearer <accessToken>"
```

`POST` returns **201 Created** with a `Location` header pointing to the new resource and a body `{ "id": "<guid>" }`. Auto-creates the referenced `plantId` if it does not exist.

The DTO mirrors `Detection` in `frontend/src/services/api.ts`:

```jsonc
{
  "id": "<guid>",
  "frameId": 4821,
  "timestamp": "2026-04-25T14:32:09.123Z",
  "severity": "warning",                  // "critical" | "warning" | "healthy"
  "topPrediction": {
    "diseaseClass": "early_blight",       // see DiseaseClass enum below
    "confidence": 0.876,
    "label": "Early Blight (A. solani)"
  },
  "allPredictions": [ /* same shape, ranked desc by confidence */ ],
  "boundingBox": {
    "x": 0.35, "y": 0.4, "width": 0.18, "height": 0.22,
    "depthMeters": 0.9, "affectedAreaPercent": 11
  },
  "inferenceMs": 35,
  "confidenceGatePassed": true,
  "row": 7,
  "plantId": "P023",
  "positionMeters": 12.4
}
```

`DiseaseClass` values: `late_blight`, `early_blight`, `fusarium_wilt`, `powdery_mildew`, `bacterial_spot`, `leaf_mold`, `septoria_leaf_spot`, `spider_mites`, `healthy`. Enums are serialized as snake_case_lower strings (matches the frontend TS unions exactly).

## JSON conventions

- All responses are JSON with `application/json; charset=utf-8`.
- Property casing: **camelCase** (`detectionsToday`, `topPrediction`, etc.) — matches the frontend's TypeScript types.
- Dates: ISO 8601 with timezone, e.g. `"2026-04-25T14:32:09.123Z"`.
- IDs: GUIDs as strings.
- Enums: lowercase snake_case strings (`"late_blight"`, `"early_blight"`, `"healthy"`).

## Error format — RFC 7807 ProblemDetails

Every error response is RFC 7807 ProblemDetails JSON. Field error keys are **camelCase** to match the rest of the JSON.

### Validation (HTTP 400)
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "email": ["'Email' is not a valid email address."],
    "password": [
      "The length of 'Password' must be at least 8 characters. You entered 5 characters.",
      "Password must contain an uppercase letter."
    ]
  },
  "traceId": "00-..."
}
```

Identity errors (e.g. duplicate email on register) are mapped to the right field by ASP.NET Identity's error code:
- `DuplicateEmail` / `DuplicateUserName` / `InvalidEmail` → `email`
- Anything starting with `Password` → `password`
- Anything else → empty key (general form-level error)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "email": [
      "Username 'taken@example.com' is already taken.",
      "Email 'taken@example.com' is already taken."
    ]
  },
  "traceId": "00-..."
}
```

### Authentication failure (HTTP 401)
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Authentication failed.",
  "status": 401,
  "detail": "Invalid email or password.",
  "traceId": "00-..."
}
```

For missing-token cases, the body may be empty (default JWT bearer behavior). Frontends should always treat HTTP 401 as "needs login", regardless of body shape.

### Not found (HTTP 404)
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not found.",
  "status": 404,
  "detail": "Detection '53c7221a-22d7-416c-b6cb-8780f10ed57e' was not found.",
  "traceId": "00-..."
}
```

### Frontend handling pattern
- HTTP **400**: read `errors` map, render each field's first message under the matching input. Show entries under the empty key (`""`) as a form-level banner.
- HTTP **401**: redirect to login (or show "session expired" if you had a token).
- HTTP **404**: navigate or show a not-found message; `detail` is safe to display.
- HTTP **5xx**: show a generic "something went wrong" toast; log the `traceId` to your error tracker so it can be cross-referenced with backend logs.

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
