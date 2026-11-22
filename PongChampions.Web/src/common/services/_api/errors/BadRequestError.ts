import { ApiError } from './ApiError';

// 400: invalid input.
export class BadRequestError extends ApiError {
  constructor(serverMessage: string | null = null) {
    super(400, serverMessage);
    this.name = 'BadRequestError';
  }
}
