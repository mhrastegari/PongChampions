import { HttpError } from './HttpError';

// A header value of null removes a default header for one request.
export type HeaderValue = string | null;

export interface IRequestOptions {
  body?: unknown;
  headers?: Record<string, HeaderValue>;
  signal?: AbortSignal;
}

let _baseUrl = '';
const _headers: Record<string, string> = {};
let _unauthorizedHandler: (() => void) | null = null;

function setBaseUrl(url: string) {
  _baseUrl = url.replace(/\/+$/, '');
}

function getBaseUrl() {
  return _baseUrl;
}

function setHeader(name: string, value: HeaderValue) {
  if (value === null) delete _headers[name];
  else _headers[name] = value;
}

// Called when a request that carried the default Authorization header is rejected.
function setUnauthorizedHandler(handler: (() => void) | null) {
  _unauthorizedHandler = handler;
}

function buildHeaders(options: IRequestOptions) {
  const headers: Record<string, string> = {
    Accept: 'application/json',
    ..._headers,
  };

  for (const [name, value] of Object.entries(options.headers ?? {})) {
    if (value === null) delete headers[name];
    else headers[name] = value;
  }

  if (options.body !== undefined) headers['Content-Type'] = 'application/json';

  return headers;
}

async function readBody(response: Response): Promise<unknown> {
  const text = await response.text().catch(() => '');

  if (!text) return undefined;

  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

async function request<T>(
  method: string,
  path: string,
  options: IRequestOptions = {}
): Promise<T> {
  const headers = buildHeaders(options);
  let response: Response;

  try {
    response = await fetch(`${_baseUrl}${path}`, {
      method,
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      signal: options.signal,
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error;

    throw new HttpError(0, null);
  }

  const body = await readBody(response);

  if (!response.ok) {
    const usedDefaultAuth =
      headers.Authorization !== undefined &&
      headers.Authorization === _headers.Authorization;

    if (response.status === 401 && usedDefaultAuth) _unauthorizedHandler?.();

    throw new HttpError(response.status, body);
  }

  return body as T;
}

function get<T>(path: string, options?: IRequestOptions) {
  return request<T>('GET', path, options);
}

function post<T>(path: string, options?: IRequestOptions) {
  return request<T>('POST', path, options);
}

function put<T>(path: string, options?: IRequestOptions) {
  return request<T>('PUT', path, options);
}

function del<T>(path: string, options?: IRequestOptions) {
  return request<T>('DELETE', path, options);
}

export const http = {
  get,
  post,
  put,
  del,
  setHeader,
  setBaseUrl,
  getBaseUrl,
  setUnauthorizedHandler,
};
