# Pong Champions — web client

React 18 + TypeScript + Vite + Tailwind CSS 3, TanStack Router, and the SignalR JavaScript client. The client consumes the contract in the [root README](../README.md); the server is authoritative for all game state.

## Develop

Start the API first (see the root README), then:

```powershell
npm install
npm run dev
```

The dev server runs on `http://localhost:3000` and proxies `/api` and `/hubs` (including WebSockets) to `https://localhost:7273`, the API's `https` launch profile. With the `http` profile, set `VITE_DEV_PROXY_TARGET=http://localhost:5051` in `.env.local`. Port 3000 matches the default `CLIENT_ORIGIN`.

| Script                            | Purpose                                                      |
| --------------------------------- | ------------------------------------------------------------ |
| `npm run dev`                     | Dev server with proxy and hot reload                         |
| `npm run build`                   | Type-check and produce `dist/`                               |
| `npm run lint`                    | ESLint (flat config, typescript-eslint, React hooks/refresh) |
| `npm run format` / `format:check` | Prettier with Tailwind class sorting                         |
| `npm test`                        | Vitest unit tests for the pure game, mapping and auth logic  |

`npm test` goes through `scripts/vitest.mjs`. Windows terminals can report a lowercase drive (`c:\`) while modules resolve to `C:\`, which makes Vitest load itself twice; the script re-launches from the canonical path.

## Deploy

`npm run build` emits static files. Either serve them from the same origin as the API behind a reverse proxy that forwards `/api` and `/hubs` with WebSocket upgrades (no CORS needed), or host them separately: build with `VITE_API_BASE_URL=https://api.example.com` and set the API's `CLIENT_ORIGIN` to the client's exact origin. Route every unknown path to `index.html` so deep links such as `/rooms/ABC123` load the app.

## How it plays

- The server ticks at 20 Hz. The court is drawn on a canvas each animation frame from a small snapshot buffer, about 60 ms behind real time, so the ball interpolates smoothly between ticks instead of stepping. Scoring resets are never blended.
- The viewer's own paddle follows local input immediately; positions go to the hub at most every 40 ms, one call in flight, well under the server's 60-calls-per-second limit.
- Controls: pointer over the court (mouse, touch drag, pen), or ↑/↓ and W/S.
- Anyone with the link can watch. A host must have an account; a guest can join anonymously and receives a room-scoped token, kept in `localStorage` for reconnects. The API has no "my rooms" endpoint, so the client remembers the room this browser is playing in and offers to return to it.
- SignalR serializes room status as a number and HTTP as a name; `roomMapper` accepts both.

## Structure

Follows the team React conventions: `src/common/{components,hooks,models,pages,services,shared,widgets}` and `src/core/{http,storage,utils}`, one folder per unit with an `index.ts` barrel, and path aliases (`@components/*`, `@services/*`, …) defined in `tsconfig.app.json` and mirrored in `vite.config.ts`. Raw transport lives in `services/_api` (DTOs, one error class per status, HTTP and hub clients); domain services wrap it. All UI copy goes through `vocabService`, currently backed by the English dictionary in `shared/vocabs.ts` because the API does not serve translations yet. Redux and shadcn/ui are intentionally not used: shared state is limited to the session and theme, both small `useSyncExternalStore` services.
