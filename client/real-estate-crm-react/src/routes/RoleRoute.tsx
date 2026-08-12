import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../features/auth/AuthContext";
import type { Role } from "../types/auth";

interface RoleRouteProps {
  roles: Role[];
}

/**
 * UX-only gate — hides a page from roles that shouldn't see it in the nav. The backend
 * enforces the real authorization independently; this is never the source of truth.
 */
export function RoleRoute({ roles }: RoleRouteProps) {
  const { user } = useAuth();
  const allowed = user?.roles.some((role) => roles.includes(role)) ?? false;

  if (!allowed) {
    return <Navigate to="/dashboard" replace />;
  }

  return <Outlet />;
}
