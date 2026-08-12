import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { jwtDecode } from "../../utils/jwt";
import { apiClient, refreshSessionFromCookie, SESSION_EXPIRED_EVENT } from "../../api/client";
import { authSession } from "../../utils/authSession";
import type { AuthResponse, AuthUser, LoginRequest, Role } from "../../types/auth";

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isInitializing: boolean;
  login: (request: LoginRequest) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

interface AccessTokenClaims {
  sub: string;
  name?: string;
  company_id?: string;
  role?: string | string[];
}

function userFromAccessToken(accessToken: string): AuthUser {
  const claims = jwtDecode<AccessTokenClaims>(accessToken);
  const roles = claims.role ? (Array.isArray(claims.role) ? claims.role : [claims.role]) : [];

  return {
    userId: claims.sub,
    // Falls back to the raw id only if a token predates the "name" claim (e.g. a refresh
    // token issued before this shipped) — should not happen for any freshly issued token.
    fullName: claims.name ?? claims.sub,
    companyId: claims.company_id ?? null,
    roles: roles as Role[],
  };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isInitializing, setIsInitializing] = useState(true);

  useEffect(() => {
    // The access token lives only in memory, so it's gone on every page reload by design — the
    // only way to know "is this browser still logged in?" is to ask the server, via the
    // httpOnly refresh-token cookie (if any). A first-ever visit / a logged-out browser simply
    // has no cookie, refreshSessionFromCookie() resolves null, and the user sees the login page
    // — this is the expected, normal path, not an error.
    (async () => {
      const session = await refreshSessionFromCookie();
      if (session) {
        setUser(userFromAccessToken(session.accessToken));
      }
      setIsInitializing(false);
    })();
  }, []);

  useEffect(() => {
    const handleSessionExpired = () => setUser(null);
    window.addEventListener(SESSION_EXPIRED_EVENT, handleSessionExpired);
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, handleSessionExpired);
  }, []);

  const login = useCallback(async (request: LoginRequest) => {
    const response = await apiClient.post<AuthResponse>("/auth/login", request, {
      headers: { "X-Auth-Transport": "cookie" },
    });
    authSession.setAccessToken(response.data.accessToken);
    setUser(userFromAccessToken(response.data.accessToken));
  }, []);

  const logout = useCallback(() => {
    authSession.clear();
    setUser(null);
    // Best-effort revoke; the user is logged out locally regardless of the outcome. The server
    // reads the refresh token from the httpOnly cookie itself and clears it — nothing to pass
    // in the body for the web client.
    void apiClient.post("/auth/logout", {}, { headers: { "X-Auth-Transport": "cookie" } }).catch(() => undefined);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: user !== null, isInitializing, login, logout }),
    [user, isInitializing, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider");
  }
  return context;
}
