import { describe, expect, it } from 'vitest';
import { decodeJwt, isJwtExpired, jwtExpiresAt } from './jwt';
import { clamp, reflectInto } from './math';
import { safeRedirect } from './redirect';

function fakeJwt(payload: object) {
  const encode = (value: object) => {
    const bytes = new TextEncoder().encode(JSON.stringify(value));

    return btoa(String.fromCharCode(...bytes))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/, '');
  };

  return `${encode({ alg: 'HS256' })}.${encode(payload)}.signature`;
}

describe('reflectInto', () => {
  it('keeps values inside the range unchanged', () => {
    expect(reflectInto(0.4, 0, 1)).toBeCloseTo(0.4);
  });

  it('mirrors overshoot off either end', () => {
    expect(reflectInto(1.2, 0, 1)).toBeCloseTo(0.8);
    expect(reflectInto(-0.3, 0, 1)).toBeCloseTo(0.3);
  });

  it('handles several bounces', () => {
    expect(reflectInto(2.25, 0, 1)).toBeCloseTo(0.25);
  });

  it('clamps', () => {
    expect(clamp(5, 0, 1)).toBe(1);
    expect(clamp(-5, 0, 1)).toBe(0);
  });
});

describe('jwt', () => {
  it('decodes base64url payloads including non-ASCII claims', () => {
    const token = fakeJwt({ unique_name: 'Zoë', exp: 2_000_000_000 });

    expect(decodeJwt(token)).toMatchObject({ unique_name: 'Zoë' });
    expect(jwtExpiresAt(token)).toBe(2_000_000_000_000);
  });

  it('treats malformed or exp-less tokens as expired', () => {
    expect(decodeJwt('not-a-token')).toBeNull();
    expect(isJwtExpired('a.b.c')).toBe(true);
    expect(isJwtExpired(fakeJwt({ sub: 'x' }))).toBe(true);
  });

  it('compares expiry with the supplied clock', () => {
    const token = fakeJwt({ exp: 100 });

    expect(isJwtExpired(token, 99_000)).toBe(false);
    expect(isJwtExpired(token, 100_000)).toBe(true);
  });
});

describe('safeRedirect', () => {
  it('allows same-origin paths', () => {
    expect(safeRedirect('/rooms/ABC123')).toBe('/rooms/ABC123');
  });

  it('rejects external and protocol-relative targets', () => {
    expect(safeRedirect('https://evil.example')).toBeUndefined();
    expect(safeRedirect('//evil.example')).toBeUndefined();
    expect(safeRedirect('/\\evil.example')).toBeUndefined();
    expect(safeRedirect(42)).toBeUndefined();
  });
});
