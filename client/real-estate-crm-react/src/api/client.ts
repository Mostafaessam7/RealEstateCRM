import axios, { type AxiosError, type InternalAxiosRequestConfig } from "axios";
import { authSession } from "../utils/authSession";
import type { AuthResponse } from "../types/auth";

// withCredentials: the refresh token lives in an httpOnly cookie (see WebAuthCookies on the
// backend) — the browser must be told to send/accept cookies on requests to a different origin
// than the page itself (the API is a separate origin from the SPA in both dev and prod).
export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  withCredentials: true,
});

/**
 * Fired when the refresh token itself is rejected — the session is over and the app must
 * send the user back to login. The auth feature listens for this; client.ts stays router-free.
 */
export const SESSION_EXPIRED_EVENT = "crm:session-expired";

apiClient.interceptors.request.use((config) => {
  const token = authSession.getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

let refreshPromise: Promise<string | null> | null = null;

/**
 * Calls /auth/refresh using the httpOnly refresh-token cookie (never the deprecated
 * localStorage-token body flow) plus the CSRF double-submit header. Exported so AuthContext can
 * reuse it verbatim for the app-boot "is there already a session?" check — there is deliberately
 * only one place this request is built.
 */
export async function refreshSessionFromCookie(): Promise<AuthResponse | null> {
  try {
    const response = await axios.post<AuthResponse>(
      `${import.meta.env.VITE_API_BASE_URL}/auth/refresh`,
      {},
      {
        withCredentials: true,
        headers: {
          "X-Auth-Transport": "cookie",
          ...(authSession.getCsrfToken() ? { "X-CSRF-Token": authSession.getCsrfToken()! } : {}),
        },
      },
    );
    authSession.setAccessToken(response.data.accessToken);
    return response.data;
  } catch {
    return null;
  }
}

interface RetryableConfig extends InternalAxiosRequestConfig {
  _retried?: boolean;
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const config = error.config as RetryableConfig | undefined;

    if (error.response?.status !== 401 || !config || config._retried) {
      return Promise.reject(error);
    }

    config._retried = true;

    refreshPromise ??= (async () => {
      const result = await refreshSessionFromCookie();
      return result?.accessToken ?? null;
    })().finally(() => {
      refreshPromise = null;
    });

    const newAccessToken = await refreshPromise;

    if (!newAccessToken) {
      authSession.clear();
      window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT));
      return Promise.reject(error);
    }

    config.headers.Authorization = `Bearer ${newAccessToken}`;
    return apiClient(config);
  },
);

export function getApiErrorMessage(error: unknown, fallback = "Something went wrong."): string {
  if (axios.isAxiosError(error)) {
    const title = (error.response?.data as { title?: string } | undefined)?.title;
    if (title) {
      return title;
    }
  }
  return fallback;
}
