/**
 * A problem response from the engine (RFC 9457). Validation errors are keyed by camelCase property path,
 * e.g. `state.bands[1].frequency`.
 */
export class EngineApiError extends Error {
  readonly status: number;
  readonly errors: Record<string, string[]>;

  constructor(status: number, message: string, errors: Record<string, string[]> = {}) {
    super(message);
    this.name = "EngineApiError";
    this.status = status;
    this.errors = errors;
  }
}

let accessToken: string | undefined;

/**
 * Sets the token sent with every API request and the SignalR connection.
 */
export function setAccessToken(token: string | undefined) {
  accessToken = token;
}

export function getAccessToken() {
  return accessToken;
}

/**
 * The fetch function of the generated API client: adds the access token, returns the parsed body and throws an
 * {@link EngineApiError} for error responses.
 */
export async function engineFetch<T>(url: string, options: RequestInit): Promise<T> {
  const headers = new Headers(options.headers);
  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  const response = await fetch(url, { ...options, headers });
  const text = await response.text();
  const body: unknown = text ? JSON.parse(text) : undefined;

  if (!response.ok) {
    const problem = (body ?? {}) as { title?: string; detail?: string; errors?: Record<string, string[]> };
    throw new EngineApiError(
      response.status,
      problem.detail ?? problem.title ?? response.statusText,
      problem.errors ?? {}
    );
  }

  return body as T;
}
