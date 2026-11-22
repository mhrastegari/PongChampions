import { ApiError } from './ApiError';

// 429: rate limited.
export class TooManyRequestsError extends ApiError {
  constructor(serverMessage: string | null = null) {
    super(429, serverMessage);
    this.name = 'TooManyRequestsError';
  }
}
