# notification-hub

Simple notification hub for public pages.

## Docker publish binding

Publish the container on the host loopback interface so only cloudflared (or another local process) can open a connection:

```bash
docker run -p 127.0.0.1:7999:7999 --env-file notification-hub.env pedrodev95/notification-hub:latest
```

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
