---
status: idea
created: 2026-04-25
last-updated: 2026-04-25
---

# Implement the stub endpoints from PR #6 with real data

## Why
PR #6 ships placeholder controllers for every endpoint the frontend already calls, so the dashboard never 404s. But the responses are hardcoded. Each one needs a real implementation backed by real data — this file is the parent ticket; each sub-bullet becomes its own ticket as it's picked up.

## Approach sketch
Each endpoint is its own focused ticket, with its own integration tests and any necessary domain modeling. They can ship in any order; they don't depend on each other.

| Endpoint | Sub-ticket sketch |
|---|---|
| `GET /api/dashboard/stats` | Aggregate query: detections today + delta vs yesterday, avg confidence, rows scanned, plants tracked. Cache for 30s. |
| `GET /api/camera/frame` | Latest frame metadata from edge devices. Source: ingest endpoint (POST) for edge to push frames; this endpoint reads the most recent. |
| `GET /api/stand/position` | Edge device position telemetry. Source: same as above — edge pushes position updates. |
| `GET /api/passports/{plantId}` | Plant aggregate: events log + severity history. Joins detections + plant + treatments. |
| `GET /api/treatments?diseaseClass=` | Backed by a `Treatments` table. Seed data from agronomist input. |
| `GET /api/environment/latest` | Sensor reading: temp + humidity. Source: a separate ingest endpoint for environmental sensors. |
| `GET /api/system/status` | Likely the simplest — most fields can be derived from the running process + DB connectivity. |

## Dependencies / blockers
- PR #5 (detections) merged — `dashboard/stats` and `passports` lean on it.
- For camera, stand, environment: a new ingest endpoint each, since those readings come from edge devices and aren't yet modeled.
- For treatments: agronomist needs to provide the seed dataset.

## Ready to start when…
- Frontend team flags a specific stub as "we need this real now" — that endpoint becomes the next ticket.

## Notes
- For each implementation, the `add-endpoint` skill walks the full recipe.
- Don't implement them all in one mega-PR. One endpoint per PR keeps reviews focused.
- When a sub-ticket ships, leave a "✓ done in PR #N" line under the table above.
