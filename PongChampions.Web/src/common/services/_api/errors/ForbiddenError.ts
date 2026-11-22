import { ApiError } from './ApiError';

// 403: authenticated but not allowed.
export class ForbiddenError extends ApiError {
  constructor(serverMessage: string | null = null) {
    super(403, serverMessage);
    this.name = 'ForbiddenError';
  }
}
