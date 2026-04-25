# AgriCure / PDD

Plant Disease Detection — an AgriCure project that combines edge-device inference (Jetson + YOLOv8) with a web dashboard so growers can see and act on field-level crop health.

## Repository layout

This repo is a monorepo split into two top-level units:

| Folder / branch | What lives there |
|---|---|
| [`backend/`](./backend) | .NET 9 Web API (Clean Architecture). REST endpoints, auth, Hangfire background jobs, PostgreSQL persistence. Dockerized. |
| [`frontend/`](./frontend) (on branch `frontend/dashboard_implementation`) | React + Vite + TypeScript dashboard ("AgriCure"). Polls REST endpoints; live camera/detection feed planned via WebSocket. |

## Getting started

- **Backend:** see [`backend/README.md`](./backend/README.md) — `docker compose up` brings up the API + Postgres.
- **Frontend:** check out the `frontend/dashboard_implementation` branch and follow `frontend/README.md`.

## Branching

- Default branch: `develop`.
- Feature work: `backend/<topic>` or `frontend/<topic>`.
- Backend rolls out in stacked PRs (`backend/01-foundation` → `backend/02-...` etc.).
