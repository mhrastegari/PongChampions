import { decodeJwt } from '@core/utils';
import type { IAuthUser } from '@models/auth';

// JwtSecurityTokenHandler shortens ClaimTypes URIs; accept both spellings.
const userIdClaims = [
  'nameid',
  'sub',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
];
const usernameClaims = [
  'unique_name',
  'name',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name',
];

function firstString(payload: Record<string, unknown>, claims: string[]) {
  for (const claim of claims) {
    const value = payload[claim];

    if (typeof value === 'string' && value) return value;
  }

  return null;
}

// Returns null for anything that is not a registered-account token, including guest tokens.
export function authUserMapper(token: string): IAuthUser | null {
  const payload = decodeJwt(token);

  if (
    !payload ||
    payload.identity_kind === 'guest' ||
    typeof payload.exp !== 'number'
  )
    return null;

  const userId = firstString(payload, userIdClaims);
  const username = firstString(payload, usernameClaims);

  if (!userId || !username) return null;

  return { userId, username, expiresAt: payload.exp * 1000 };
}
