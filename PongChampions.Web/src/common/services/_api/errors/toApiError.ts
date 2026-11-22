import { HttpError } from '@core/http';
import type { IProblemDetailsDto } from '../dtos';
import { ApiError } from './ApiError';
import { BadRequestError } from './BadRequestError';
import { ConflictError } from './ConflictError';
import { ForbiddenError } from './ForbiddenError';
import { NetworkError } from './NetworkError';
import { NotFoundError } from './NotFoundError';
import { ServerError } from './ServerError';
import { TooManyRequestsError } from './TooManyRequestsError';
import { UnauthorizedError } from './UnauthorizedError';

function serverMessageOf(body: unknown): string | null {
  if (body === null || typeof body !== 'object') return null;

  const problem = body as IProblemDetailsDto;
  // Model validation failures carry the specific message under `errors`.
  const firstValidationError = Object.values(problem.errors ?? {}).flat()[0];

  return firstValidationError ?? problem.title ?? null;
}

export function toApiError(error: HttpError): ApiError {
  const message = serverMessageOf(error.body);

  switch (error.status) {
    case 0:
      return new NetworkError();
    case 400:
      return new BadRequestError(message);
    case 401:
      return new UnauthorizedError(message);
    case 403:
      return new ForbiddenError(message);
    case 404:
      return new NotFoundError(message);
    case 409:
      return new ConflictError(message);
    case 429:
      return new TooManyRequestsError(message);
    default:
      return new ServerError(error.status, message);
  }
}

// Wraps a transport call so callers only ever see typed ApiErrors.
export async function callApi<T>(request: Promise<T>): Promise<T> {
  try {
    return await request;
  } catch (error) {
    throw error instanceof HttpError ? toApiError(error) : error;
  }
}
