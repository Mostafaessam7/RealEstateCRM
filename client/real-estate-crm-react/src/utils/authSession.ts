// The access token lives only in memory — never localStorage/sessionStorage — so it cannot be
// read by any script (XSS or otherwise) after this page's JS context is gone, and it isn't
// sitting in browser storage for the lifetime of the tab either. It is deliberately lost on a
// full page reload; AuthProvider's bootstrap effect re-fetches a fresh one via the httpOnly
// refresh-token cookie (see api/client.ts's refreshSessionFromCookie). The refresh token itself
// never touches this module, or any other JS-reachable place, at all — it only ever exists in
// the httpOnly "rt" cookie the backend sets (see WebAuthCookies on the backend). See
// docs/auth.md#web-cookie-auth for the full design and why this differs from Flutter/API
// clients, which keep using OS-level secure storage + JSON-body tokens unchanged.
let accessToken: string | null = null;

const CSRF_COOKIE_NAME = "XSRF-TOKEN";

export const authSession = {
  getAccessToken: (): string | null => accessToken,
  setAccessToken: (token: string | null): void => {
    accessToken = token;
  },
  clear: (): void => {
    accessToken = null;
  },
  /**
   * Reads the non-httpOnly CSRF cookie the server sets alongside the httpOnly refresh-token
   * cookie, so it can be echoed back as a header (double-submit pattern) on cookie-authenticated
   * auth requests. Not a secret in itself — its security property comes from a cross-site
   * attacker being unable to read it (same-origin policy), not from it being hidden from this
   * page's own JS.
   */
  getCsrfToken: (): string | null => {
    const match = document.cookie.match(new RegExp(`(?:^|; )${CSRF_COOKIE_NAME}=([^;]*)`));
    return match ? decodeURIComponent(match[1]) : null;
  },
};
