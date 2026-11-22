import { ApiError } from './ApiError';

// 409: the operation conflicts with the room's lifecycle, or the room is full.
export class ConflictError extends ApiError {
  constructor(serverMessage: string | null = null) {
    super(409, serverMessage);
    this.name = 'ConflictError';
  }
}
