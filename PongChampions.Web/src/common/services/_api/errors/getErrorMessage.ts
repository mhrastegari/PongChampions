import { vocabService } from '@services/vocab';
import { ApiError } from './ApiError';
import { NetworkError } from './NetworkError';
import { ServerError } from './ServerError';
import { TooManyRequestsError } from './TooManyRequestsError';

const vocab = vocabService.get;

const hubExceptionMarker = 'HubException: ';

// The single place that turns any failure into text a player can read.
export function getErrorMessage(error: unknown): string {
  if (error instanceof NetworkError) return vocab('error_network');
  if (error instanceof TooManyRequestsError) return vocab('error_rate_limited');
  if (error instanceof ServerError) return vocab('error_server');

  // 4xx titles from this API are specific, player-facing sentences.
  if (error instanceof ApiError)
    return error.serverMessage ?? vocab('error_unexpected');

  // SignalR wraps hub errors: "...on the server. HubException: Room has ended."
  if (error instanceof Error) {
    const index = error.message.indexOf(hubExceptionMarker);

    if (index >= 0) return error.message.slice(index + hubExceptionMarker.length);
  }

  return vocab('error_unexpected');
}
