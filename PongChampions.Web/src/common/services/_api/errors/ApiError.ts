// Base class for every failed API call. serverMessage is the ProblemDetails title, if any.
export class ApiError extends Error {
  readonly status: number;
  readonly serverMessage: string | null;

  constructor(status: number, serverMessage: string | null = null) {
    super(serverMessage ?? `Request failed with status ${status}`);
    this.name = 'ApiError';
    this.status = status;
    this.serverMessage = serverMessage;
  }
}
