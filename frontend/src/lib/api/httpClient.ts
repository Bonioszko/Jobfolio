export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);

  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(path, {
    ...init,
    credentials: "include",
    headers,
  });

  if (!response.ok) {
    const message = await response.text();
    throw new ApiError(response.status, message || `Request failed with status ${response.status}.`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export const httpClient = {
  get<T>(path: string, init?: RequestInit) {
    return request<T>(path, { ...init, method: "GET" });
  },

  post<T>(path: string, body?: unknown, init?: RequestInit) {
    return request<T>(path, {
      ...init,
      method: "POST",
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  },

  put<T>(path: string, body: unknown, init?: RequestInit) {
    return request<T>(path, {
      ...init,
      method: "PUT",
      body: JSON.stringify(body),
    });
  },
};
