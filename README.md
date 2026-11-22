# Pong Champions

Server-authoritative multiplayer Pong on .NET SDK **10.0.401**, ASP.NET Core, SignalR, EF Core and PostgreSQL. Application code stays in `PongChampions.Api` and `PongChampions.Data`; `PongChampions.Tests` contains automated verification.

## Run with Docker

1. Copy `.env.example` to `.env`, choose a database password and generate a random JWT signing key of at least 32 bytes. Keep `.env` private.
2. Run `docker compose up --build -d`.
3. Check `http://localhost:8080/health/ready`.

Compose waits for PostgreSQL, runs the `migrate` service to completion, then starts the API. Existing database volumes are retained. A failed migration prevents API startup. The API runs as a non-root user. Production ports bind to loopback: expose the API through a TLS reverse proxy with WebSocket support. TLS terminates at that proxy; container HTTPS redirection is disabled. Set `CLIENT_ORIGIN` to the exact browser origin. Do not enable wildcard credentialed CORS or expose PostgreSQL publicly.

`GET /health/live` checks process liveness. `GET /health/ready` checks database connectivity and applied migrations. Logs use JSON and include room codes and failure context, never credentials. The development test page and OpenAPI document are available only in Development.

## Run locally

Start PostgreSQL with `docker compose up -d postgres`. Configure development settings through user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=pongchampions;Username=YOUR_USER;Password=YOUR_PASSWORD" --project PongChampions.Api
dotnet user-secrets set "Jwt:Key" "YOUR_RANDOM_SIGNING_KEY_AT_LEAST_32_BYTES" --project PongChampions.Api
dotnet user-secrets set "Jwt:Issuer" "PongChampions" --project PongChampions.Api
dotnet user-secrets set "Jwt:Audience" "PongChampions" --project PongChampions.Api
dotnet run --project PongChampions.Api -- --migrate-only
dotnet run --project PongChampions.Api --launch-profile https
```

The equivalent production environment variables are `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Jwt__Issuer` and `Jwt__Audience`. The EF CLI factory reads `ConnectionStrings__DefaultConnection`, or `POSTGRES_USER`/`POSTGRES_PASSWORD` for localhost. No schema updates happen implicitly during ordinary application startup.

## Client contract

- `POST /api/auth/register`: `username` (3–32 letters/digits/underscores), `password` (10–72 characters, at most 72 UTF-8 bytes), `displayName` (1–32), optional `country` (up to 64).
- `POST /api/auth/login`: `username`, `password`; returns `{ token }`. Account JWTs expire after seven days.
- `POST /api/rooms`: account bearer token required; returns a room. The default target score is five.
- `GET /api/rooms/{code}`: public room state.
- `POST /api/rooms/{code}/join`: optional account bearer token. Returns `{ room, guestToken }`. An anonymous join receives a four-hour guest JWT **only in this response**. Registered guests use their account JWT and receive `guestToken: null`. Keep guest tokens private.
- `POST /api/rooms/{code}/start`: host account required; room must have a guest.
- `POST /api/rooms/{code}/leave`: current guest account/guest JWT required. Before start, frees the guest slot and revokes its existing socket permissions. During a game, cancels the room.
- `POST /api/rooms/{code}/close`: host account required; cancels the room and removes its live session.

Connect SignalR to `/hubs/rooms` using `accessTokenFactory` (JavaScript) or `AccessTokenProvider` (.NET). A blank token permits spectating. Invoke `JoinRoom(code)`; **player GUIDs are never accepted as proof of identity**. `MovePaddle(code, y)` accepts finite coordinates between zero and one and controls only the authenticated participant's paddle. Server coordinates clamp paddle centers to keep paddles on the board. `LeaveRoom(code)` leaves the socket group; use the HTTP leave endpoint to vacate the guest slot.

Listen for `JoinedRoom`, `LeftRoom`, `PlayerJoined`, `PlayerLeft`, `MatchStarted`, `GameStateUpdated`, `PaddleUpdated`, `MatchFinished`, and `RoomClosed`. Reconnect using the same credential and invoke `JoinRoom(code)` again; the server sends the current active state immediately. Expired authentication closes the connection. The sample development page follows this contract.

The server ticks every 50 ms. A disconnected participant has a 60-second grace period; the simulation continues. Reconnecting restores control. If the grace expires before scoring finishes, the room is cancelled. Multiple connections for the same participant are supported. Waiting/ready rooms expire 30 minutes after creation. Configure `Game__TickMilliseconds`, `Game__ReconnectGraceSeconds`, `Game__LobbyLifetimeMinutes` and `Game__ServeDelayMilliseconds` to change these defaults.

Board coordinates are normalized to `[0, 1]` on both axes; ball velocity is in board units **per second**, so simulation speed does not depend on the tick interval. Each point starts with the ball held at the center for one second (velocity zero), then served at a random 15–35° angle toward the player who just scored. The ball has a radius of 0.015; paddle faces sit at x = 0.05 and x = 0.95 with a half-height of 0.12. The return angle depends on where the ball meets the paddle: center hits return nearly flat, edge hits up to 50°. Serves travel at 0.45 units/s, and each paddle return speeds the ball up by 7%, capped at 1.2 units/s. Movement is sub-stepped, so fast balls cannot pass through paddles or walls.

Finished rooms have exactly one match with scores, winner and finish time. Cancellation never creates a match. Persistence and room status update share a database transaction. A unique room index plus a PostgreSQL transaction advisory lock prevent duplicate results and conflicting room operations. The initial single-instance design serializes room mutations for simplicity; gameplay ticks use separate in-memory locking. Completed states remain queued and retry every two seconds after database failures. SignalR events are live notifications, not a durable event log.

HTTP errors use ProblemDetails: invalid input `400`, invalid/missing authentication `401`, insufficient permission `403`, missing room `404`, conflicting lifecycle operation/full room `409`, throttling `429`, unexpected failure `500`. Authentication endpoints have an additional IP rate limit; hub calls are limited per connection. Unexpected details are logged rather than returned.

## Verification and releases

```powershell
dotnet build PongChampions.slnx -c Release --warnaserror
dotnet test PongChampions.slnx -c Release
```

Tests require Docker and provision their own disposable PostgreSQL container. They do not touch the development database. Coverage includes physics, finite inputs, paddle permissions, guest credential scope/revocation, reconnects, concurrent joins, transactional completion, duplicate persistence, failure/retry, cancellation, stale rooms, shutdown, and real HTTP/SignalR clients.

GitHub Actions builds, tests, builds the container, and publishes a versioned GHCR image after a successful push to `main`. Publishing does not alter a running server. On a deployment host, set `API_IMAGE` to the published image, then run `bash scripts/deploy.sh` from the checkout. Use an immutable commit tag for repeatable releases. Configure the host's TLS proxy and secrets before exposing it. The script waits for `/health/ready` after rollout. Back up PostgreSQL before schema changes; never use `docker compose down -v` on data you need.

The separate manual `deploy` workflow uses the GitHub `production` environment. Configure its `DEPLOY_HOST`, `DEPLOY_USER`, `DEPLOY_SSH_KEY`, `DEPLOY_KNOWN_HOSTS` secrets and `DEPLOY_PATH` variable. The host must have this checkout, a private `.env`, Docker Compose, curl, and GHCR pull access if the package is private. Supply the immutable GHCR image with the 40-character commit tag from a successful build; the workflow deploys it over verified SSH. Configure environment reviewers if you want a release approval. No deployment destination or secrets are assumed by this repository.

## Operating boundary

Run **one API replica**. Active sessions live in memory. On graceful shutdown, completed states are flushed where possible and active games are cancelled. A restart cancels stale in-game rooms rather than inventing results. A crash before an unpersisted final state is saved can lose that result. Durable recovery and multiple replicas require additional storage/coordination and are outside this MVP. Database outages retry completion while the process remains alive; readiness reports database failure.

Leaderboards, profiles, match-history APIs, matchmaking, Redis, admin tools, AI opponents and a React client are intentionally deferred by the roadmap.
