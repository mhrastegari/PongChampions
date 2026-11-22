export interface IJwtPayload {
  exp?: number;
  [claim: string]: unknown;
}

// Reads claims without verifying the signature. Only the server can trust a token.
export function decodeJwt(token: string): IJwtPayload | null {
  const part = token.split('.')[1];

  if (!part) return null;

  try {
    const base64 = part.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(
      base64.length + ((4 - (base64.length % 4)) % 4),
      '='
    );
    const bytes = Uint8Array.from(atob(padded), (c) => c.charCodeAt(0));
    const payload: unknown = JSON.parse(new TextDecoder().decode(bytes));

    return payload !== null && typeof payload === 'object'
      ? (payload as IJwtPayload)
      : null;
  } catch {
    return null;
  }
}

export function jwtExpiresAt(token: string): number | null {
  const exp = decodeJwt(token)?.exp;

  return typeof exp === 'number' ? exp * 1000 : null;
}

export function isJwtExpired(token: string, now = Date.now()): boolean {
  const expiresAt = jwtExpiresAt(token);

  return expiresAt === null || expiresAt <= now;
}
