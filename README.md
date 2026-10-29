# notification-hub

Simple notification hub for public pages.

## Configuration

The app reads the Postgres connection string from `ConnectionStrings:ApplicationDbConnection` (`IConfiguration.GetConnectionString("ApplicationDbConnection")` in `ApplicationDbContext`). In Docker that value has to come from the environment, because it is not in `appsettings.json`:

```bash
ConnectionStrings__ApplicationDbConnection=Server=host;Port=5432;Database=notification-hub;Username=...;Password=...
```

Design-time `dotnet ef` reads `src/NotificationHub.Persistence/local.config.json`, which is gitignored. Copy `local.config.example.json` to that path and put in a local password before adding a migration. The container runs as the image's `app` user and listens on an unprivileged port (publish `127.0.0.1:7999`).

## SMTP passwords at rest

SMTP passwords are encrypted with ASP.NET Data Protection when `Security__DataProtectionKeyRingPath` is set and either `Security__EncryptExistingPasswords=true` or the process is started with `--encrypt-smtp-passwords`. The application name is fixed to `NotificationHub`. A value that is not ciphertext is still treated as a plaintext password, so a deploy without the rewrite keeps sending.

Before the rewrite:

1. Back up the column outside the repo and outside the image: `SELECT "Id", "UserName", "Password" FROM "SmtpConfigurations";`
2. Create a host directory for the key ring and give it to the container user (`docker run --rm mcr.microsoft.com/dotnet/aspnet:10.0 id app`, often uid 1654).
3. Mount that directory at `Security__DataProtectionKeyRingPath` and run once with `Security__EncryptExistingPasswords=true`.
4. Send one message. Then delete the plaintext backup. Keep a copy of the key ring with the database backups. Losing the keys makes every later send fail.

The flag is idempotent. Leaving it on does not re-encrypt ciphertext.

## Origin and CORS

`Cors:Origins` is the browser allowlist. The committed default is `https://dconstructions.ch`, `https://www.dconstructions.ch`, `https://pedroteixeira.dev`, and `https://www.pedroteixeira.dev`. Override with `Cors__Origins__0` and so on.

When a request sends `Origin`, its host is compared to `Applications.Domain` after dropping the scheme, a leading `www.`, a trailing slash, and case. A missing `Origin` is allowed, so the portfolio server route (no `Origin` header) keeps working. Check the stored domains before deploying:

```sql
SELECT "ExternalId", "Domain" FROM "Applications";
```

## Image tags

A push to `main` publishes one `pedrodev95/notification-hub` image under three tags:

- `0.1.<run number>` — `github.run_number` of that Deploy run
- `sha-<short sha>` — `sha-` plus the first 7 characters of the commit
- `latest`

The three tags share one digest. Open the run under Actions → Deploy and read the `build-push-docker` summary for the version tag and the `sha256:` digest. Pin a deploy with both:

```bash
pedrodev95/notification-hub:0.1.<run number>@sha256:<digest>
```

The image also carries `org.opencontainers.image.source`, `org.opencontainers.image.revision` (full commit), and `org.opencontainers.image.version` (`0.1.<run number>`). Pull requests run the build, the vulnerable-package scan, and tests, and do not push. Those runs use other numbers in the same `github.run_number` sequence, so `0.1.N` on `main` is not consecutive.

## Docker publish binding

Publish the container on the host loopback interface so only cloudflared (or another local process) can open a connection:

```bash
docker run -p 127.0.0.1:7999:7999 --env-file notification-hub.env pedrodev95/notification-hub:0.1.<run number>@sha256:<digest>
```

`latest` is that same image. The version tag and digest in the Deploy summary are what a pinned deploy uses.

Do not publish `0.0.0.0:7999`. A LAN-wide bind lets any host on the network set `CF-Connecting-IP` and pick a fresh rate-limit key. Run cloudflared on the same machine, pointed at `http://127.0.0.1:7999`.

Inside the container the peer address is usually the Docker bridge gateway (often `172.17.0.1`), not `127.0.0.1`. After the first request, the "too many requests" / request log shows that peer. Put only that address in the trusted list:

```bash
RateLimit__TrustedProxies__0=172.17.0.1
```

`CF-Connecting-IP` is read only when `Connection.RemoteIpAddress` is in that list. Otherwise the limiter keys on the peer itself. `X-Forwarded-For` is never read.

The limit key is `(x-app-id, client ip)`. `RateLimit__MaxRequests` (default 5) allows 6 requests per window (`count > MaxRequests`), and the window starts at the first hit. Later hits do not extend it. `RateLimit__WindowMinutes` defaults to 30.

Portfolio submissions come from the Next.js server, so they share one egress address. Give that application its own ceiling (the key is `Applications.ExternalId`):

```bash
RateLimit__AppLimits__<portfolio-external-id>=30
```

Unlisted applications use `MaxRequests`. Requests with no app id still count against the client IP.

## HELO and Message-Id

`Email__LocalDomain` (default `notification-hub.pedroteixeira.dev`) is the SMTP EHLO/HELO name and the domain on the right-hand side of `Message-Id`. It does not change the public IP the receiving server records on `Received`.
