import { ApiError } from './ApiError';

// 404: the room or resource does not exist.
export class NotFoundError extends ApiError {
  constructor(serverMessage: string | null = null) {
    super(404, serverMessage);
    this.name = 'NotFoundError';
  }
}
