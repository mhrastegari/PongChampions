import { ApiError } from './ApiError';

// The server could not be reached.
export class NetworkError extends ApiError {
  constructor(serverMessage: string | null = null) {
    super(0, serverMessage);
    this.name = 'NetworkError';
  }
}
