import { describe, it, expect, beforeEach } from "vitest";
import { authSession } from "./authSession";

function setCsrfCookie(value: string | null) {
  if (value === null) {
    document.cookie = "XSRF-TOKEN=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/";
    return;
  }
  document.cookie = `XSRF-TOKEN=${encodeURIComponent(value)}; path=/`;
}

describe("authSession", () => {
  beforeEach(() => {
    authSession.clear();
    setCsrfCookie(null);
  });

  it("has no access token until one is set", () => {
    expect(authSession.getAccessToken()).toBeNull();
  });

  it("round-trips an access token", () => {
    authSession.setAccessToken("access-123");
    expect(authSession.getAccessToken()).toBe("access-123");
  });

  it("clear() removes the access token", () => {
    authSession.setAccessToken("access-123");
    authSession.clear();
    expect(authSession.getAccessToken()).toBeNull();
  });

  it("never persists to localStorage — only ever lives in the module's memory", () => {
    authSession.setAccessToken("access-123");
    expect(localStorage.getItem("crm.accessToken")).toBeNull();
    expect(Object.keys(localStorage)).toHaveLength(0);
  });

  it("reads the CSRF token from the non-httpOnly XSRF-TOKEN cookie", () => {
    setCsrfCookie("csrf-abc-123");
    expect(authSession.getCsrfToken()).toBe("csrf-abc-123");
  });

  it("returns null when no CSRF cookie is present", () => {
    expect(authSession.getCsrfToken()).toBeNull();
  });

  it("URL-decodes the CSRF cookie value", () => {
    setCsrfCookie("value/with+special=chars");
    expect(authSession.getCsrfToken()).toBe("value/with+special=chars");
  });
});
