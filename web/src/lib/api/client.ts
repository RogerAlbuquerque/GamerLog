import type { Problem } from "./types";

/** Every API failure surfaces as an ApiError carrying the server's problem document. */
export class ApiError extends Error {
  readonly status: number;
  readonly code: string | null;
  readonly fieldErrors: Record<string, string>;

  constructor(status: number, message: string, code: string | null = null, fieldErrors: Record<string, string> = {}) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
    this.fieldErrors = fieldErrors;
  }

  get isUnauthorized() {
    return this.status === 401;
  }

  get isNotFound() {
    return this.status === 404;
  }
}

export type Query = Record<string, string | number | boolean | null | undefined>;

export function buildQuery(query?: Query): string {
  if (!query) return "";
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === null || value === "") continue;
    params.set(key, String(value));
  }
  const text = params.toString();
  return text ? `?${text}` : "";
}

export function toApiError(status: number, body: unknown): ApiError {
  const problem = (body && typeof body === "object" ? body : {}) as Partial<Problem> & { code?: string };
  const fieldErrors: Record<string, string> = {};
  for (const [field, messages] of Object.entries(problem.errors ?? {})) {
    if (messages?.length) fieldErrors[field.charAt(0).toLowerCase() + field.slice(1)] = messages[0];
  }
  const fallback = status === 429 ? "Too many requests. Please slow down." : status >= 500 ? "Something went wrong on our side." : "Request failed.";
  return new ApiError(status, problem.title || problem.detail || fallback, problem.code ?? null, fieldErrors);
}

const BASE = "/api/v1";
let csrfToken: string | null = null;
let unauthorizedHandler: (() => void) | null = null;

export function setCsrfToken(token: string | null) {
  csrfToken = token;
}

/** Lets the session store react when the cookie expires mid-session. */
export function onUnauthorized(handler: (() => void) | null) {
  unauthorizedHandler = handler;
}

async function ensureCsrf(force = false): Promise<string> {
  if (csrfToken && !force) return csrfToken;
  const res = await fetch(`${BASE}/auth/csrf`, { credentials: "same-origin", cache: "no-store" });
  if (!res.ok) throw toApiError(res.status, null);
  csrfToken = ((await res.json()) as { token: string }).token;
  return csrfToken;
}

export interface RequestOptions {
  query?: Query;
  body?: unknown;
  form?: FormData;
  signal?: AbortSignal;
}

const SAFE_METHODS = new Set(["GET", "HEAD", "OPTIONS"]);

async function send(method: string, path: string, options: RequestOptions, retryCsrf: boolean): Promise<Response> {
  const headers: Record<string, string> = { Accept: "application/json" };
  let body: BodyInit | undefined;
  if (options.form) {
    body = options.form; // the browser sets the multipart boundary
  } else if (options.body !== undefined) {
    headers["Content-Type"] = "application/json";
    body = JSON.stringify(options.body);
  }
  if (!SAFE_METHODS.has(method)) {
    headers["X-CSRF-TOKEN"] = await ensureCsrf(!retryCsrf);
  }

  const res = await fetch(`${BASE}${path}${buildQuery(options.query)}`, {
    method,
    headers,
    body,
    credentials: "same-origin",
    signal: options.signal,
    cache: "no-store",
  });

  // An expired or rotated antiforgery token is answered with a bare 400: fetch a fresh one and retry once.
  if (res.status === 400 && !SAFE_METHODS.has(method) && retryCsrf && !(res.headers.get("content-type") ?? "").includes("json")) {
    return send(method, path, options, false);
  }
  return res;
}

export async function request<T>(method: string, path: string, options: RequestOptions = {}): Promise<T> {
  const res = await send(method, path, options, true);
  if (res.status === 204) return undefined as T;

  const isJson = (res.headers.get("content-type") ?? "").includes("json");
  const payload = isJson ? await res.json().catch(() => null) : null;
  if (!res.ok) {
    const error = toApiError(res.status, payload);
    if (error.isUnauthorized) unauthorizedHandler?.();
    throw error;
  }
  return payload as T;
}

export const http = {
  get: <T>(path: string, query?: Query, signal?: AbortSignal) => request<T>("GET", path, { query, signal }),
  post: <T>(path: string, body?: unknown) => request<T>("POST", path, { body }),
  put: <T>(path: string, body?: unknown) => request<T>("PUT", path, { body }),
  patch: <T>(path: string, body?: unknown) => request<T>("PATCH", path, { body }),
  delete: <T = void>(path: string, body?: unknown) => request<T>("DELETE", path, { body }),
  upload: <T>(path: string, form: FormData) => request<T>("PUT", path, { form }),
};
