import { beforeAll, describe, expect, it } from 'vitest';
import { vocabService } from '@services/vocab';
import { authUserMapper } from './authMapper';
import { validateRegistration } from './authValidation';

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

const valid = {
  username: 'ada_l',
  displayName: 'Ada',
  password: 'correct horse',
  confirmPassword: 'correct horse',
  country: '',
};

beforeAll(() => vocabService.init());

describe('authUserMapper', () => {
  it('reads the short claim names JwtSecurityTokenHandler writes', () => {
    expect(
      authUserMapper(fakeJwt({ nameid: 'u1', unique_name: 'ada', exp: 10 }))
    ).toEqual({
      userId: 'u1',
      username: 'ada',
      expiresAt: 10_000,
    });
  });

  it('refuses guest tokens and tokens without identity', () => {
    expect(
      authUserMapper(fakeJwt({ nameid: 'p1', identity_kind: 'guest', exp: 10 }))
    ).toBeNull();
    expect(authUserMapper(fakeJwt({ exp: 10 }))).toBeNull();
    expect(authUserMapper('garbage')).toBeNull();
  });
});

describe('validateRegistration', () => {
  it('accepts input that satisfies the API rules', () => {
    expect(validateRegistration(valid)).toEqual({});
  });

  it('mirrors the server constraints', () => {
    const errors = validateRegistration({
      username: 'a!',
      displayName: ' ',
      password: 'short',
      confirmPassword: 'other',
      country: 'x'.repeat(65),
    });

    expect(Object.keys(errors).sort()).toEqual(
      ['confirmPassword', 'country', 'displayName', 'password', 'username'].sort()
    );
  });

  it('counts password bytes, not characters', () => {
    const password = '€'.repeat(25);

    expect(
      validateRegistration({ ...valid, password, confirmPassword: password })
        .password
    ).toBeDefined();
  });
});

describe('vocabService', () => {
  it('returns the key when a translation is missing', () => {
    expect(vocabService.get('no_such_key')).toBe('no_such_key');
  });

  it('fills placeholders in order', () => {
    expect(vocabService.format('final_score', 5, 3)).toBe('Final score 5 – 3');
  });
});
