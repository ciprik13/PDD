---
status: idea
created: 2026-04-25
last-updated: 2026-04-25
---

# WebSocket / SignalR live feed for camera frames and detections

## Why
The frontend currently polls `/api/camera/frame` every 1s and `/api/detections` every 5s. That's wasteful for a live-monitoring view, and the latency hides important detections (a critical late-blight find could sit in the queue for 5 seconds before the dashboard shows it). The frontend's `frontend/src/services/api.ts` already has a TODO comment with a sketched WebSocket hook (`connectLiveFeed`).

## Approach sketch
- Use **SignalR** (built into ASP.NET Core, integrates well with auth and Hangfire). Hub at `/hubs/live`.
- Two events: `frame` (camera frame metadata) and `detection` (new detection).
- Backend publishes from a Hangfire job (or directly from the detection ingest endpoint) using `IHubContext<LiveHub>`.
- Auth: require JWT in the connection negotiation (SignalR supports `access_token` query param).
- Frontend hook: replace the polling-based `useCameraFrame` and `useDetections` with a single subscription, falling back to polling if the socket drops.

## Dependencies / blockers
- PRs #4 (auth) and #5 (detections) must be merged — we need real auth and a real detection event source.
- Decision needed: do edge devices push frames straight to the SignalR hub, or to a REST endpoint that fans out via SignalR? Probably the latter (fewer credentials in the field).

## Ready to start when…
- The frontend team asks for live detection latency under 1s.
- OR detection throughput grows past ~5/sec, where polling becomes bandwidth-painful.

## Notes
- Investigate scaling: SignalR with a Redis backplane (`Microsoft.AspNetCore.SignalR.StackExchangeRedis`) once we have multiple API replicas behind the load balancer.
- The frontend stub for `connectLiveFeed` lives in `frontend/src/services/api.ts` — match that signature exactly so the swap is local.
