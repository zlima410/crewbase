import { env } from "./env";
import { supabase } from "./supabase";

export class ApiError extends Error {
  readonly status: number;
  readonly problem: unknown;

  constructor(status: number, problem: unknown, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }
}

function messageFor(status: number, problem: unknown): string {
  if (problem && typeof problem === "object") {
    const { title, detail } = problem as { title?: unknown; detail?: unknown };

    if (typeof detail === "string" && detail.trim()) return detail;
    if (typeof title === "string" && title.trim()) return title;
  }

  if (status === 429) return "Too many requests. Please wait a moment and try again.";
  if (status >= 500) return "Something went wrong on our end. Please try again.";

  return `Request failed (${status})`;
}

async function send(path: string, init: RequestInit, token: string | undefined) {
  return fetch(`${env.apiBaseUrl}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  });
}

export async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const { data } = await supabase.auth.getSession();
  let response = await send(path, init, data.session?.access_token);

  if (response.status === 401) {
    const refreshed = await supabase.auth.refreshSession();

    if (refreshed.data.session?.access_token && !refreshed.error) {
      response = await send(path, init, refreshed.data.session.access_token);
    }

    if (response.status === 401) {
      await supabase.auth.signOut();
      throw new ApiError(401, null, "Your session has expired. Please sign in again.");
    }
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new ApiError(response.status, problem, messageFor(response.status, problem));
  }

  if (response.status === 204) return undefined as T;

  return response.json() as Promise<T>;
}
