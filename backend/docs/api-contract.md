# AgriCure API Reference

This is the contract for frontend developers. The full interactive spec lives at **`/swagger`** — this doc is the human-readable companion.

> **Local**: `http://localhost:8080`
> **Prod**: set `VITE_API_BASE_URL` accordingly. All paths below are relative to that base URL.

---

## Authentication

Endpoints under `/api/auth/*` are public. **Everything else requires a JWT bearer token.**

### Register or log in

| Route | Method | Auth |
|---|---|---|
| `/api/auth/register` | `POST` | none |
| `/api/auth/login` | `POST` | none |

Both accept the same request body and return the same response shape.

**Request**
```json
{
  "email": "user@example.com",
  "password": "P@ssw0rd!ABC"
}
```

Password rules: minimum 8 characters, must contain an uppercase letter, a lowercase letter, and a digit.

**Response 200** — `AuthResultDto`
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "accessTokenExpiresAt": "2026-04-25T17:15:04.000Z",
  "refreshToken": "USCZN0LqLgymgpORLdIYf+...",
  "refreshTokenExpiresAt": "2026-05-02T17:00:04.000Z"
}
```

### Use the access token

Send the access token on every protected request:

```http
GET /api/detections HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

In Swagger UI: click **Authorize**, paste the token (no `Bearer ` prefix). All "Try it out" calls then carry the header automatically.

### Refresh the access token

Access tokens expire after **15 minutes**. Refresh tokens are valid for **7 days** and rotate on every use — using a refresh token revokes it and issues a new one.

| Route | Method | Auth |
|---|---|---|
| `/api/auth/refresh` | `POST` | none (uses the refresh token in the body) |

**Request**
```json
{ "refreshToken": "<refreshToken from earlier>" }
```

**Response 200** — same `AuthResultDto` as login.
**Response 401** — refresh token unknown, expired, or already revoked.

### Sign out

| Route | Method | Auth |
|---|---|---|
| `/api/auth/logout` | `POST` | none |

**Request**
```json
{ "refreshToken": "<refreshToken>" }
```

**Response 204**. Idempotent — already-revoked tokens still get 204.

---

## Detections — `/api/detections`

Plant-disease detection events emitted by edge devices (Jetson + YOLOv8). **All actions require a bearer token.**

### List recent detections

| Route | Method | Auth |
|---|---|---|
| `/api/detections?limit=20` | `GET` | bearer |

`limit` is 1–200 (default 20). Newest first.

**Response 200** — array of `Detection`:
```json
[
  {
    "id": "7145896d-8546-49a4-baf7-4adf02fdd1bf",
    "frameId": 4821,
    "timestamp": "2026-04-25T17:28:17.803Z",
    "severity": "warning",
    "topPrediction": {
      "diseaseClass": "early_blight",
      "confidence": 0.876,
      "label": "Early Blight (A. solani)"
    },
    "allPredictions": [
      { "diseaseClass": "early_blight", "confidence": 0.876, "label": "Early Blight" },
      { "diseaseClass": "healthy", "confidence": 0.04, "label": "Healthy" }
    ],
    "boundingBox": {
      "x": 0.35,
      "y": 0.40,
      "width": 0.18,
      "height": 0.22,
      "depthMeters": 0.9,
      "affectedAreaPercent": 11.0
    },
    "inferenceMs": 35,
    "confidenceGatePassed": true,
    "row": 7,
    "plantId": "P023",
    "positionMeters": 12.4
  }
]
```

### Get a single detection

| Route | Method | Auth |
|---|---|---|
| `/api/detections/{id}` | `GET` | bearer |

`{id}` is a GUID (e.g. `7145896d-8546-49a4-baf7-4adf02fdd1bf`).

**Response 200** — single `Detection` (same shape as a list element).
**Response 404** — no detection with that id.

### Create a detection

| Route | Method | Auth |
|---|---|---|
| `/api/detections` | `POST` | bearer |

**Request** — `Detection` minus `id` (server-generated). Predictions should be ordered by confidence desc; the server preserves that order and exposes the first one as `topPrediction` on read.

```json
{
  "frameId": 4821,
  "timestamp": "2026-04-25T17:28:17.803Z",
  "severity": "warning",
  "predictions": [
    { "diseaseClass": "early_blight", "confidence": 0.876, "label": "Early Blight" },
    { "diseaseClass": "healthy", "confidence": 0.04, "label": "Healthy" }
  ],
  "boundingBox": {
    "x": 0.35,
    "y": 0.40,
    "width": 0.18,
    "height": 0.22,
    "depthMeters": 0.9,
    "affectedAreaPercent": 11.0
  },
  "inferenceMs": 35,
  "confidenceGatePassed": true,
  "row": 7,
  "plantId": "P023",
  "positionMeters": 12.4
}
```

**Response 201** — `{ "id": "<guid>" }` and `Location: /api/detections/<guid>`.

If the referenced `plantId` doesn't exist, it's auto-created.

### Replace a detection

| Route | Method | Auth |
|---|---|---|
| `/api/detections/{id}` | `PUT` | bearer |

PUT semantics — full replacement, not patch. The body must include `id` matching the route.

```json
{
  "id": "7145896d-8546-49a4-baf7-4adf02fdd1bf",
  "frameId": 4900,
  "timestamp": "2026-04-25T18:00:00Z",
  "severity": "critical",
  "predictions": [...],
  "boundingBox": {...},
  "inferenceMs": 30,
  "confidenceGatePassed": true,
  "row": 7,
  "plantId": "P023",
  "positionMeters": 12.4
}
```

**Response 204** on success.
**Response 400** if route id and body id don't match.
**Response 404** if no detection with that id.

### Delete a detection

| Route | Method | Auth |
|---|---|---|
| `/api/detections/{id}` | `DELETE` | bearer |

**Response 204** — idempotent. Missing ids still return 204.

---

## Plants — `/api/plants`

| Route | Method | Auth | Roles |
|---|---|---|---|
| `/api/plants` | `GET` | bearer | `admin`, `agriculture` |

All plants the caller can see, each with its latest disease status (derived from the most recent detection). Agriculture users see only plants they own; admin sees all. Newest-seen first.

**Response 200** — array of `PlantSummary`:
```json
[
  {
    "plantId": "P023",
    "row": 7,
    "latestSeverity": "warning",
    "latestLabel": "Early Blight (A. solani)",
    "latestDiseaseClass": "early_blight",
    "lastSeenAt": "2026-06-08T12:01:00Z",
    "detectionCount": 12
  }
]
```
Fields except `plantId`/`detectionCount` are `null` for a plant with no detections yet.

## Plant passport — `/api/passports/{plantId}`

| Route | Method | Auth | Roles |
|---|---|---|---|
| `/api/passports/{plantId}` | `GET` | bearer | `admin`, `agriculture` |

Full life history for one plant: identity, current status, a chronological `events[]` timeline (scans + applied treatments), and a daily `severityHistory[]` sparkline (`value` is 0–100). **404** when the plant doesn't exist or isn't visible to the caller.

## Dashboard — `/api/dashboard/stats`

| Route | Method | Auth | Roles |
|---|---|---|---|
| `/api/dashboard/stats` | `GET` | bearer | `admin`, `agriculture` |

`{ detectionsToday, detectionsDelta, avgConfidence (0–1), rowsScanned, totalRows, plantsTracked }` — aggregated over the caller's visible data.

## System status — `/api/system/status`

| Route | Method | Auth | Roles |
|---|---|---|---|
| `/api/system/status` | `GET` | bearer | `admin`, `agriculture` |

`{ deviceStatus, cameraConnected, gpsActive, modelLoaded, modelName, syncedAt, pendingAlerts }`. `deviceStatus` is `online` when a detection was ingested in the last 5 minutes, else `offline`. `modelName` is `null` (no real source yet).

## Treatments — `/api/treatments`

| Route | Method | Auth | Roles |
|---|---|---|---|
| `/api/treatments?diseaseClass=late_blight` | `GET` | bearer | `admin`, `agriculture` |
| `/api/treatments/applied` | `GET` | bearer | `admin`, `agriculture` |
| `/api/treatments/applied` | `POST` | bearer | `admin`, `agriculture` |

`GET /api/treatments?diseaseClass=` returns the recommendation catalog for a disease (snake_case class), ordered by `rank` (biological-first). **422** on an unknown class.

`GET /api/treatments/applied?limit=&plantId=` returns applied-treatment history (tenant-scoped, newest first).

`POST /api/treatments/applied` records an application:
```json
{ "treatmentId": "<guid>", "plantId": "P023", "appliedAt": "2026-06-08T12:00:00Z", "notes": "full row" }
```
**201** `{ "id": "<guid>" }`. **422** if the treatment is unknown or the plant isn't owned by the caller.

## Camera — `/api/camera`

| Route | Method | Auth | Roles |
|---|---|---|---|
| `/api/camera/frame` | `GET` | bearer | `admin`, `agriculture` |
| `/api/camera/token` | `GET` | bearer | `admin`, `agriculture` |
| `/api/camera/stream?token=` | `GET` | stream token | — |
| `/api/camera/gps` | `GET` | bearer | `admin`, `agriculture` |
| `/api/stand/position` | `GET` | bearer | `admin`, `agriculture` |

`frame`/`position` derive real fields (row, position, depth) from the latest detection; GPS/height/speed are `null`.

The live feed is proxied from the on-prem Jetson over a private mesh network (Tailscale). `GET /api/camera/token` issues a single-use 60s token; the browser then loads `GET /api/camera/stream?token=…` (MJPEG). `/api/camera/gps` proxies live GPS. The device URLs are server config (`Jetson:StreamUrl`, `Jetson:BaseUrl`) and never exposed to the browser. **502** when the device is unreachable.

> Environmental sensor readings (temperature/humidity) are not modeled — no endpoint exists.

---

## Conventions

### IDs

- Most entity IDs are **GUIDs** (lowercase strings): `"7145896d-8546-49a4-baf7-4adf02fdd1bf"`.
- `plantId` is a **short string code**, max 64 characters: `"P023"`.

### JSON

- All field names are **camelCase**.
- All enum values are **snake_case_lower** strings.
- Timestamps are **ISO 8601 with timezone**: `"2026-04-25T14:32:09.123Z"`.

### Enums

**`severity`**
`critical` · `warning` · `healthy`

**`diseaseClass`**
`late_blight` · `early_blight` · `fusarium_wilt` · `powdery_mildew` · `bacterial_spot` · `leaf_mold` · `septoria_leaf_spot` · `spider_mites` · `healthy`

---

## Errors

Every error is **RFC 7807 ProblemDetails** JSON.

### 400 — Validation

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "email": ["Email is not valid."],
    "password": ["Password must contain a digit."]
  },
  "traceId": "00-..."
}
```

`errors` is keyed by the **camelCase field name** of the offending input. An empty key (`""`) means a form-level error not tied to one specific field — render it above the form.

Common register-time errors are mapped to fields for you:

| What happened | Key | Example message |
|---|---|---|
| Duplicate email | `email` | `"An account with this email already exists."` |
| Invalid email format | `email` | `"Email is not valid."` |
| Password too weak | `password` | `"Password must contain a digit."` |

### 401 — Authentication failed

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Authentication failed.",
  "status": 401,
  "detail": "Invalid email or password.",
  "traceId": "00-..."
}
```

When the bearer token is missing or expired, the response may be empty — treat any 401 as "needs login".

### 404 — Not found

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not found.",
  "status": 404,
  "detail": "Detection 'a7e3...' was not found.",
  "traceId": "00-..."
}
```

### 5xx — Server error

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal server error.",
  "status": 500,
  "detail": "An unexpected error occurred while processing your request.",
  "traceId": "00-..."
}
```

Show the user a generic toast and capture `traceId` to your error tracker — backend logs use the same id.

### Recommended frontend handling

| Status | Frontend action |
|---|---|
| **200 / 201 / 204** | Success path. |
| **400** | For each `errors[field][0]`, render under the matching input. Empty-key errors → form-level banner. |
| **401** | Redirect to login (or show "session expired" if a token was set). |
| **404** | Navigate or show a not-found message. `detail` is safe to display. |
| **5xx** | Generic toast. Log `traceId` to your error tracker. |

---

## Health & operations

| Route | Method | Auth | Purpose |
|---|---|---|---|
| `/health` | `GET` | none | Liveness — 200 if the API process is up. |
| `/health/ready` | `GET` | none | Readiness — 200 only if Postgres + Hangfire storage are reachable. |
| `/swagger` | `GET` | none in Development; admin role otherwise | Interactive Swagger UI. |
| `/hangfire` | `GET` | admin role | Background job dashboard. |

> Outside Development, both `/swagger` and `/hangfire` require an admin JWT bearer token. Browsers don't attach the header automatically — admins inspecting prod typically use a browser extension (e.g. ModHeader) or hit the spec at `/swagger/v1/swagger.json` from a tool that can send `Authorization: Bearer <token>`.

The frontend usually doesn't call these — they're for orchestrators and operators.
