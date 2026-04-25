---
status: idea
created: 2026-04-25
last-updated: 2026-04-25
---

# Image / blob storage for raw detection frames

## Why
Today, detections store metadata only — bounding boxes, predictions, plant IDs. The actual camera frames live on the edge device and are eventually overwritten. For audits, retraining, and dispute resolution ("did we really detect blight there?"), we need to keep the raw frames somewhere durable.

## Approach sketch
- Add an object store. **MinIO** for self-hosted, S3-compatible local dev (drop-in via docker-compose). **AWS S3 / Cloudflare R2** for prod.
- New `IImageStorage` interface in `Application`; `S3ImageStorage` and `MinioImageStorage` implementations in `Infrastructure`.
- Detections gain a `frameImageUrl` field (string). The edge device uploads to a signed URL; metadata lands via the existing `POST /api/detections`.
- Lifecycle: keep frames for N days (configurable per environment), then delete via a Hangfire recurring job.
- Frontend: detection detail view shows the frame; treatment/passport views can fetch on demand.

## Dependencies / blockers
- PR #3 (Hangfire) — needed for the cleanup job.
- PR #5 (detections CRUD) — needed for the metadata endpoint.
- Decision: signed-URL upload (edge → S3 directly) vs proxied upload (edge → API → S3). Signed URL is cheaper and faster but harder to validate; proxied is simpler. Probably start proxied, switch to signed when bandwidth becomes the bottleneck.

## Ready to start when…
- Audit/regulatory request lands ("show us the frame for detection X").
- OR retraining work needs labeled real-world frames.

## Notes
- Don't store frames in Postgres bytea — that path leads to slow queries and bloated backups.
- MinIO docker-compose entry can live alongside Postgres; same network.
