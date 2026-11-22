// Status 0 means the request never produced an HTTP response (offline, DNS, CORS).
export class HttpError extends Error {
  readonly status: number;
  readonly body: unknown;

  constructor(status: number, body: unknown) {
    super(status === 0 ? 'Network request failed' : `HTTP ${status}`);
    this.name = 'HttpError';
    this.status = status;
    this.body = body;
  }
}
