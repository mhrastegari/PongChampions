import { ApiError } from './ApiError';

// 5xx, or any status the client has no specific handling for.
export class ServerError extends ApiError {
  constructor(status: number, serverMessage: string | null = null) {
    super(status, serverMessage);
    this.name = 'ServerError';
  }
}
