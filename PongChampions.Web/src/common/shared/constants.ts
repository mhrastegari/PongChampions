export const constants = {
  api: {
    roomHubPath: '/hubs/rooms',
  },
  storageKeys: {
    authToken: 'pc.auth',
    guestTokens: 'pc.guestTokens',
    activeRoom: 'pc.activeRoom',
    // Also read by the pre-paint script in index.html.
    theme: 'pc.theme',
  },
  room: {
    codeLength: 6,
    codePattern: /^[A-Z0-9]{6}$/,
  },
  game: {
    defaultTargetScore: 5,
    // Geometry mirrors PongChampions.Api GameSessionService.
    hostPaddleX: 0.05,
    guestPaddleX: 0.95,
    paddleHalfHeight: 0.12,
    ballRadius: 0.015,
    // The server accepts 60 hub calls per second per connection; stay well below it.
    paddleSendIntervalMs: 40,
    keyboardPaddleSpeed: 1.4,
    // Render slightly in the past so there are two server snapshots to blend.
    interpolationDelayMs: 60,
    maxExtrapolationMs: 120,
  },
  validation: {
    usernameMin: 3,
    usernameMax: 32,
    usernamePattern: /^[A-Za-z0-9_]+$/,
    passwordMin: 10,
    passwordMax: 72,
    passwordMaxBytes: 72,
    displayNameMax: 32,
    countryMax: 64,
  },
} as const;
