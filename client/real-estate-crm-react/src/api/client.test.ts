import { describe, it, expect, beforeEach, vi } from "vitest";
import axios, { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from "axios";
import { apiClient, getApiErrorMessage, refreshSessionFromCookie, SESSION_EXPIRED_EVENT } from "./client";
import { authSession } from "../utils/authSession";

describe("getApiErrorMessage", () => {
  it("returns the ProblemDetails title when the error is an AxiosError carrying one", () => {
    const error = new AxiosError("Request failed", "400", undefined, undefined, {
      status: 400,
      data: { title: "That email is already registered." },
    } as AxiosResponse);

    expect(getApiErrorMessage(error)).toBe("That email is already registered.");
  });

  it("falls back to the provided default when there is no title", () => {
    const error = new AxiosError("Request failed", "500", undefined, undefined, {
      status: 500,
      data: {},
    } as AxiosResponse);

    expect(getApiErrorMessage(error, "Custom fallback.")).toBe("Custom fallback.");
  });

  it("falls back to the default generic message for a non-Axios error", () => {
    expect(getApiErrorMessage(new Error("boom"))).toBe("Something went wrong.");
  });
});

function setCsrfCookie(value: string) {
  document.cookie = `XSRF-TOKEN=${encodeURIComponent(value)}; path=/`;
}

function clearCsrfCookie() {
  document.cookie = "XSRF-TOKEN=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/";
}

/**
 * apiClient's 401 -> cookie-refresh -> retry flow, and its concurrent-refresh deduplication, are
 * critical auth/security behavior. Stubs axios' adapter function directly (same substitution
 * technique as the backend/Flutter fake-HTTP-handler tests) rather than hitting a real network —
 * a request queue per method+path, consumed once per call, so a route that needs to respond
 * differently across calls must be enqueued once per expected invocation. The refresh token
 * itself is never visible to this test (or any JS) — it lives only in the (simulated) httpOnly
 * cookie the fake adapter doesn't need to model, since the browser/adapter layer handles cookies
 * transparently in the real flow; what's under test here is that the client asks for a refresh
 * correctly and reacts correctly to the outcome.
 */
describe("apiClient auth interceptor (cookie-based refresh)", () => {
  type Responder = (config: InternalAxiosRequestConfig) => Partial<AxiosResponse> | Promise<Partial<AxiosResponse>>;

  let queue: Responder[];
  let calls: InternalAxiosRequestConfig[];

  beforeEach(() => {
    authSession.clear();
    clearCsrfCookie();
    queue = [];
    calls = [];

    const fakeAdapter = async (config: InternalAxiosRequestConfig) => {
      calls.push(config);
      const responder = queue.shift();
      if (!responder) {
        throw new Error(`No responder queued for ${config.method} ${config.url}`);
      }
      const partial = await responder(config);
      if (partial.status && partial.status >= 400) {
        throw new AxiosError("Request failed", String(partial.status), config, undefined, {
          status: partial.status,
          data: partial.data,
          config,
          headers: {},
          statusText: "",
        } as AxiosResponse);
      }
      return {
        status: 200,
        statusText: "OK",
        headers: {},
        config,
        data: partial.data,
        ...partial,
      } as AxiosResponse;
    };

    // refreshSessionFromCookie() deliberately calls the bare `axios` module (not the
    // `apiClient` instance) to avoid recursing through apiClient's own interceptors — so both
    // need the fake adapter, or the refresh call would try (and fail) to hit a real network.
    apiClient.defaults.adapter = fakeAdapter;
    axios.defaults.adapter = fakeAdapter;
  });

  it("attaches the Bearer token from in-memory session state to outgoing requests", async () => {
    authSession.setAccessToken("access-1");
    queue.push(() => ({ status: 200, data: { ok: true } }));

    await apiClient.get("/leads");

    expect(calls[0].headers.Authorization).toBe("Bearer access-1");
  });

  it("sends withCredentials + X-Auth-Transport: cookie + CSRF header on the refresh call", async () => {
    authSession.setAccessToken("expired-access");
    setCsrfCookie("csrf-token-value");
    queue.push(() => ({ status: 401, data: {} })); // /leads
    queue.push(() => ({ status: 200, data: { accessToken: "new-access", accessTokenExpiresAt: "2030-01-01", refreshToken: "" } })); // /auth/refresh
    queue.push(() => ({ status: 200, data: [] })); // retried /leads

    await apiClient.get("/leads");

    const refreshCall = calls.find((c) => c.url?.includes("/auth/refresh"));
    expect(refreshCall?.withCredentials).toBe(true);
    expect(refreshCall?.headers["X-Auth-Transport"]).toBe("cookie");
    expect(refreshCall?.headers["X-CSRF-Token"]).toBe("csrf-token-value");
  });

  it("refreshes once and retries the original request on a 401", async () => {
    authSession.setAccessToken("expired-access");
    queue.push(() => ({ status: 401, data: {} })); // original /leads request
    queue.push(() => ({ status: 200, data: { accessToken: "new-access", accessTokenExpiresAt: "2030-01-01", refreshToken: "" } })); // /auth/refresh
    queue.push(() => ({ status: 200, data: [{ id: 1 }] })); // retried /leads request

    const response = await apiClient.get("/leads");

    expect(response.data).toEqual([{ id: 1 }]);
    expect(authSession.getAccessToken()).toBe("new-access");
    // The retried request must carry the *new* token, not the stale one.
    expect(calls[2].headers.Authorization).toBe("Bearer new-access");
  });

  it("deduplicates concurrent refreshes triggered by simultaneous 401s into a single refresh call", async () => {
    authSession.setAccessToken("expired-access");
    queue.push(() => ({ status: 401, data: {} })); // /leads
    queue.push(() => ({ status: 401, data: {} })); // /deals
    queue.push(() => ({ status: 200, data: { accessToken: "new-access", accessTokenExpiresAt: "2030-01-01", refreshToken: "" } })); // /auth/refresh — should only be called once
    queue.push(() => ({ status: 200, data: ["lead"] })); // retried /leads
    queue.push(() => ({ status: 200, data: ["deal"] })); // retried /deals

    const [leads, deals] = await Promise.all([apiClient.get("/leads"), apiClient.get("/deals")]);

    expect(leads.data).toEqual(["lead"]);
    expect(deals.data).toEqual(["deal"]);
    const refreshCalls = calls.filter((c) => c.url?.includes("/auth/refresh"));
    expect(refreshCalls).toHaveLength(1);
  });

  it("clears the in-memory access token and dispatches SESSION_EXPIRED_EVENT when the refresh itself fails (no valid cookie)", async () => {
    authSession.setAccessToken("expired-access");
    queue.push(() => ({ status: 401, data: {} })); // /leads
    queue.push(() => ({ status: 401, data: {} })); // /auth/refresh fails too — e.g. no rt cookie, or it's expired

    const listener = vi.fn();
    window.addEventListener(SESSION_EXPIRED_EVENT, listener);

    await expect(apiClient.get("/leads")).rejects.toBeInstanceOf(AxiosError);

    expect(authSession.getAccessToken()).toBeNull();
    expect(listener).toHaveBeenCalledTimes(1);

    window.removeEventListener(SESSION_EXPIRED_EVENT, listener);
  });
});

describe("refreshSessionFromCookie", () => {
  beforeEach(() => {
    authSession.clear();
    clearCsrfCookie();
  });

  it("stores the returned access token in memory and returns the full session on success", async () => {
    axios.defaults.adapter = async (config: InternalAxiosRequestConfig) =>
      ({
        status: 200,
        statusText: "OK",
        headers: {},
        config,
        data: { accessToken: "fresh-access", accessTokenExpiresAt: "2030-01-01", refreshToken: "" },
      }) as AxiosResponse;

    const session = await refreshSessionFromCookie();

    expect(session?.accessToken).toBe("fresh-access");
    expect(authSession.getAccessToken()).toBe("fresh-access");
  });

  it("returns null and leaves the session cleared when there is no valid session cookie", async () => {
    axios.defaults.adapter = async (config: InternalAxiosRequestConfig) => {
      throw new AxiosError("Unauthorized", "401", config, undefined, {
        status: 401,
        data: {},
        config,
        headers: {},
        statusText: "",
      } as AxiosResponse);
    };

    const session = await refreshSessionFromCookie();

    expect(session).toBeNull();
    expect(authSession.getAccessToken()).toBeNull();
  });
});

// Sanity check that axios itself is the real module (not auto-mocked) — the interceptor tests
// above depend on real AxiosError/interceptor plumbing, only the transport adapter is stubbed.
describe("axios sanity", () => {
  it("axios.isAxiosError recognizes a real AxiosError instance", () => {
    expect(axios.isAxiosError(new AxiosError())).toBe(true);
  });
});
