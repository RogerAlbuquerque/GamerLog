# Deployment notes

The supported deployment is the Docker Compose stack in the repository root. It is a single-host setup
suitable for a small community; scale-out notes are at the end.

## 1. Prepare

```bash
cp .env.example .env
```

Fill in strong, unique values for `POSTGRES_PASSWORD`, `S3_ACCESS_KEY` and `S3_SECRET_KEY`. Set `ALLOWED_ORIGINS`
to the public URL of the site (for example `https://gamelog.example.org`). For the first start only, set the
`SEED_ADMIN_*` variables if you want an administrator, then remove the password afterwards.

## 2. Start

```bash
docker compose up -d --build
docker compose logs -f api      # watch migrations and seeding on first start
```

## 3. Put TLS in front

The web container speaks plain HTTP. The session cookie is `Secure`, so production needs HTTPS. Any reverse
proxy works; with Caddy:

```
gamelog.example.org {
    reverse_proxy 127.0.0.1:3000
}
```

Keep the API (`127.0.0.1:5080`), PostgreSQL and MinIO bound to loopback as Compose does by default: only the web
container needs to reach the API, and it does so over the internal network.

## 4. Backups

| Data | Volume | How |
|---|---|---|
| Database | `pgdata` | `docker compose exec postgres pg_dump -U gamelog gamelog > backup.sql` |
| Uploads | `miniodata` | `mc mirror` the bucket, or snapshot the volume |
| Session keys | `dpkeys` | Snapshot the volume; losing it signs everybody out but loses no data |

## 5. Upgrades

```bash
git pull
docker compose up -d --build     # migrations run automatically at API start-up
```

`Database__AutoMigrate` can be set to `false` if you prefer to apply migrations yourself.

## Scaling out

- Run several API instances behind the proxy only after moving Data Protection keys to a shared location
  (the key ring in `DataProtection:KeysPath` must be identical on every instance) and accepting that rate limits are per instance.
- PostgreSQL and the S3 bucket are already external-friendly: point `ConnectionStrings__Default` and
  `Storage__ServiceUrl` at managed services.
