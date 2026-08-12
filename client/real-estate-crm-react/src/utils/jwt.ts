/**
 * Decodes a JWT's payload client-side for display/routing purposes only (e.g. which nav
 * items to show). This is never a security boundary — the backend independently validates
 * and enforces every claim on every request.
 */
export function jwtDecode<T>(token: string): T {
  const payload = token.split(".")[1];
  if (!payload) {
    throw new Error("Invalid JWT");
  }

  const normalized = payload.replace(/-/g, "+").replace(/_/g, "/");
  const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), "=");
  const json = decodeURIComponent(
    atob(padded)
      .split("")
      .map((c) => "%" + c.charCodeAt(0).toString(16).padStart(2, "0"))
      .join(""),
  );

  return JSON.parse(json) as T;
}
