---
status: idea
created: 2026-04-25
last-updated: 2026-04-25
---

# CI/CD via GitHub Actions

## Why
Right now, "did the tests pass?" lives in whoever pushed the PR's terminal. As the team grows, we need automated build + test on every PR and an image-push pipeline on merge to `develop`. Catches regressions, makes deploys deterministic.

## Approach sketch
- **`.github/workflows/backend-ci.yml`** — triggered on PRs touching `backend/**`.
  - `dotnet restore`, `dotnet build` (Release), `dotnet test` with coverage upload.
  - Cache NuGet packages.
  - Run on `ubuntu-latest`. Postgres for integration tests via service container or Testcontainers.
- **`.github/workflows/backend-publish.yml`** — triggered on push to `develop`.
  - Build + push Docker image to GHCR (`ghcr.io/<org>/agricure-backend`).
  - Tag with both `:develop` and `:sha-<short>`.
  - (Later) trigger deploy to the VPS via SSH or webhook.
- **Branch protection on `develop`**: require backend-ci to pass, require 1 review.

## Dependencies / blockers
- PR #2 (Docker) — workflow needs the Dockerfile.
- Decision: GHCR vs Docker Hub vs ACR. GHCR is free for public/internal, ties auth to the GH org — simplest.

## Ready to start when…
- Right after PR #6 merges (the boilerplate is done and stable).
- OR when a second developer joins and we need protection on `develop`.

## Notes
- Use `actions/cache` for NuGet (`~/.nuget/packages`) and `dotnet build` artifacts.
- Run `dotnet format --verify-no-changes` to enforce formatting; fail the build if it would reformat anything.
