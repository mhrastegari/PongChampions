import { http } from '@core/http';
import type { IAuthResponseDto, IUserLoginDto, IUserRegisterDto } from './dtos';
import { callApi } from './errors';

// A stale account token would make the API reject these anonymous calls with 401.
const anonymous = { Authorization: null };

function login(dto: IUserLoginDto) {
  return callApi(
    http.post<IAuthResponseDto>('/api/auth/login', { body: dto, headers: anonymous })
  );
}

async function register(dto: IUserRegisterDto) {
  await callApi(
    http.post<void>('/api/auth/register', { body: dto, headers: anonymous })
  );
}

export const authApi = {
  login,
  register,
};
