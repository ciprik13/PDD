# ops/

Production deployment artifacts. Local development still uses
`backend/docker-compose.yml` + `backend/docker-compose.override.yml` —
nothing here is meant to be run on a developer machine.

## What's in here

| File | Purpose |
|---|---|
| `docker-compose.prod.yml` | Compose file shipped to the VPS by CI. Pulls images from GHCR (no `build:`), wires `postgres` + `minio` + `api` + `frontend`. All container ports bind to **loopback only** — host nginx is the public entry point. |

The companion workflow is `.github/workflows/build-and-deploy.yml`.

## Topology

```
   https://agricure.online          ┐
   https://api.agricure.online      │                                127.0.0.1:8082 (frontend)
   https://media.agricure.online    │ ─▶  host nginx (:80, :443) ─▶  127.0.0.1:8090 (api → :8080 in container)
   https://storage.agricure.online  │                                127.0.0.1:9000 (minio S3 API)
                                    ┘                                127.0.0.1:9001 (minio console — login-protected)
```

The MinIO admin console at `https://storage.agricure.online` is
login-protected (MinIO root credentials) — use it to browse buckets and
manage objects from any browser. As a fallback if nginx is down, you can
still SSH-tunnel to it directly:

```bash
ssh -L 9001:127.0.0.1:9001 "$SSH_USER@$SSH_HOST"
# then open http://localhost:9001 in your browser
```

Nginx runs **on the VPS host** (you manage it). The compose stack only
binds container ports to `127.0.0.1` so they're not exposed to the
public internet — nginx is the only thing that talks to them.

## Deploy flow

`push` to `develop` →

1. `build-and-push` job builds and publishes two images to GHCR:
   - `ghcr.io/<owner>/agricure-api:latest` (and `:<sha>`)
   - `ghcr.io/<owner>/agricure-frontend:latest` (and `:<sha>`) — frontend image bakes in `VITE_API_BASE_URL` at build time.
2. `deploy` job assembles `.deploy/.env` from repo secrets, copies it together with `docker-compose.prod.yml` to `~/apps/agricure` on the VPS via SCP, then SSHes in to run `docker compose pull && down && up -d`.

## Pre-flight (one-time setup)

### 1. DNS

At your registrar, add four A records pointing at the VPS IP:

```
agricure.online          A    <VPS_IP>
api.agricure.online      A    <VPS_IP>
media.agricure.online    A    <VPS_IP>
storage.agricure.online  A    <VPS_IP>
```

### 2. Host nginx + TLS

Install nginx and certbot on the VPS, then drop the snippet below at
`/etc/nginx/sites-available/agricure` and symlink it into `sites-enabled`.

```nginx
# /etc/nginx/sites-available/agricure
# After saving, run:
#   sudo certbot --nginx \
#     -d agricure.online -d api.agricure.online \
#     -d media.agricure.online -d storage.agricure.online
# Certbot will inject the listen 443 / ssl_certificate lines automatically.

server {
    listen 80;
    listen [::]:80;
    server_name agricure.online;

    location / {
        proxy_pass         http://127.0.0.1:8082;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}

server {
    listen 80;
    listen [::]:80;
    server_name api.agricure.online;

    # Increase if your image-detection upload payloads are large.
    client_max_body_size 25m;

    location / {
        proxy_pass         http://127.0.0.1:8090;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;

        # Pass through SSE / long-poll without buffering.
        proxy_read_timeout 300s;
        proxy_buffering    off;
    }
}

server {
    listen 80;
    listen [::]:80;
    server_name media.agricure.online;

    # Picture uploads can be large; match or exceed the API limit.
    client_max_body_size 25m;

    location / {
        proxy_pass         http://127.0.0.1:9000;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;

        # MinIO streams object bytes — don't buffer.
        proxy_buffering    off;
        proxy_request_buffering off;
    }
}

server {
    listen 80;
    listen [::]:80;
    server_name storage.agricure.online;

    # Console can upload via the browser too.
    client_max_body_size 100m;

    # Longer timeouts — uploads through the console can be slow.
    proxy_connect_timeout 300s;
    proxy_send_timeout    300s;
    proxy_read_timeout    300s;

    location / {
        proxy_pass         http://127.0.0.1:9001;
        proxy_http_version 1.1;

        # WebSocket upgrade — the console uses WS for live updates.
        proxy_set_header   Upgrade           $http_upgrade;
        proxy_set_header   Connection        "upgrade";

        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;

        proxy_buffering           off;
        proxy_request_buffering   off;
        chunked_transfer_encoding off;
    }
}
```

Enable + obtain certs:

```bash
sudo ln -s /etc/nginx/sites-available/agricure /etc/nginx/sites-enabled/agricure
sudo nginx -t && sudo systemctl reload nginx
sudo certbot --nginx \
  -d agricure.online -d api.agricure.online \
  -d media.agricure.online -d storage.agricure.online
```

Certbot's package installs a renewal timer automatically; verify with
`systemctl list-timers | grep certbot`.

### 3. VPS firewall

Open `80` and `443` for inbound; the API/frontend host ports
(`8082`, `8090`) stay closed because they're already bound to
`127.0.0.1` only.

```bash
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
```

### 4. Docker on the VPS

The SSH user must be in the `docker` group (`sudo usermod -aG docker $USER`, re-login). Verify with `docker compose version`.

## Required GitHub configuration

### Secrets — Settings → Secrets and variables → Actions → Secrets

| Secret | Used for | Example |
|---|---|---|
| `SSH_HOST` | VPS IP / hostname | `144.91.110.197` |
| `SSH_USER` | VPS SSH user | `root` |
| `SSH_PASSWORD` | VPS SSH password | — |
| `POSTGRES_DB` | Database name | `agricure` |
| `POSTGRES_USER` | Database user | `agricure` |
| `POSTGRES_PASSWORD` | Database password | strong random |
| `JWT_ISSUER` | Maps to `Jwt__Issuer` | `agricure-api` |
| `JWT_AUDIENCE` | Maps to `Jwt__Audience` | `agricure-dashboard` |
| `JWT_SIGNING_KEY` | Maps to `Jwt__SigningKey`; ≥32 chars (validated at startup) | `openssl rand -base64 48` |
| `ADMIN_EMAIL` | Optional initial admin seed | `admin@agricure.online` |
| `ADMIN_PASSWORD` | Optional initial admin seed; must include uppercase + digit (Identity policy) | strong random |
| `CORS_ALLOWED_ORIGIN_0` | Production frontend origin (no trailing slash) | `https://agricure.online` |
| `MINIO_ROOT_USER` | MinIO root admin username | `agricure` |
| `MINIO_ROOT_PASSWORD` | MinIO root admin password; ≥8 chars | `openssl rand -base64 24` |
| `STORAGE_ACCESS_KEY` | API → MinIO access key. Use the same value as `MINIO_ROOT_USER` unless you've provisioned a service account. | — |
| `STORAGE_SECRET_KEY` | API → MinIO secret key. Use the same value as `MINIO_ROOT_PASSWORD` unless you've provisioned a service account. | — |
| `STORAGE_PUBLIC_BASE_URL` | Public URL the **browser** uses to load objects. Must match the media nginx server block. | `https://media.agricure.online` |

### Variables — Settings → Secrets and variables → Actions → Variables

| Variable | Value | Default |
|---|---|---|
| `VITE_API_BASE_URL` | `https://api.agricure.online` | — (required) |
| `API_HOST_PORT` | Loopback port the API binds to | `8090` |
| `FRONTEND_HOST_PORT` | Loopback port nginx-in-frontend-container binds to | `8082` |
| `MINIO_API_HOST_PORT` | Loopback port nginx proxies `media.agricure.online` to | `9000` |
| `MINIO_CONSOLE_HOST_PORT` | Loopback port for the MinIO admin console (SSH-tunnel only) | `9001` |
| `STORAGE_DEFAULT_BUCKET` | Bucket the picture layer uses | `agricure-frames` |

If you change any `*_HOST_PORT`, also update the corresponding `proxy_pass`
line in your nginx config.

## Smoke test after first deploy

```bash
ssh "$SSH_USER@$SSH_HOST" "cd ~/apps/agricure && docker compose ps"
# postgres, minio, api, frontend should all be Up.

# Containers are loopback-only — verify from the VPS shell:
ssh "$SSH_USER@$SSH_HOST" "curl -s http://127.0.0.1:8090/health/ready"
ssh "$SSH_USER@$SSH_HOST" "curl -sI http://127.0.0.1:8082/"
ssh "$SSH_USER@$SSH_HOST" "curl -s http://127.0.0.1:9000/minio/health/live"

# Then publicly, through nginx + TLS:
curl https://api.agricure.online/health/ready    # → "Healthy"
curl -I https://agricure.online/                 # → 200
curl -I https://media.agricure.online/minio/health/live  # → 200
```

Then open `https://agricure.online` in a browser and exercise the login
flow. If you see CORS errors in the console, `CORS_ALLOWED_ORIGIN_0`
doesn't match the browser's `Origin` header — fix the secret and re-run
the workflow.
