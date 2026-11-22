import { ApiError } from './ApiError';

// 401: missing, invalid or expired credentials.
export class UnauthorizedError extends ApiError {
  constructor(serverMessage: string | null = null) {
    super(401, serverMessage);
    this.name = 'UnauthorizedError';
  }
}
