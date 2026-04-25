---
status: idea
created: 2026-04-25
last-updated: 2026-04-25
---

# Production secrets management

## Why
For PRs #1–#6, secrets (JWT signing key, DB password, admin seed password) live in `.env` next to the docker-compose file. That works for local dev and a single-VPS deploy, but doesn't scale to:

- Multiple environments (staging vs prod) with different secrets.
- Secret rotation without a deploy.
- Audit trail ("who pulled the JWT signing key, when").
- Multiple services that need the same secret without copy-pasting it across `.env` files.

## Approach sketch
Pick one based on where we host:

- **Hashicorp Vault** — self-hosted, language-agnostic. The .NET integration is `VaultSharp`. Heaviest option but no vendor lock-in.
- **Azure Key Vault** — if we ever land on Azure App Service. SDK: `Azure.Security.KeyVault.Secrets`. Configuration provider integration is one line.
- **AWS Secrets Manager** — if we go AWS. SDK: `AWSSDK.SecretsManager`. Configuration provider via `AWSSDK.Extensions.NETCore.Setup`.
- **Doppler / Infisical** (managed SaaS) — easiest, but a third-party dependency and ongoing cost.

Wire via `IConfigurationBuilder` so application code is unchanged: `builder.Configuration["Jwt:SigningKey"]` keeps working.

## Dependencies / blockers
- Need to decide on the deploy target first. Currently planned: Linux VPS + docker-compose. That points toward Vault (or Doppler) since neither cloud KMS makes sense.
- PR #4 (auth) — the JWT signing key is the first secret that genuinely needs rotation.

## Ready to start when…
- We onboard a second environment (staging).
- OR we need to rotate the JWT signing key after a real or suspected leak.

## Notes
- For now (PRs #1–#6), `.env` + `docker secret` (Swarm-style) is acceptable. Document the rotation manual in `backend/docs/runbooks/rotate-jwt-key.md` once that workflow becomes real.
- Don't ever commit a `.env` file. The root `.gitignore` already prevents this.
