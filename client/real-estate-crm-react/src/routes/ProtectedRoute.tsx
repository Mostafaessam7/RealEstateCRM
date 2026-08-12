import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../features/auth/AuthContext";

export function ProtectedRoute() {
  const { isAuthenticated, isInitializing } = useAuth();
  const location = useLocation();

  if (isInitializing) {
    return <div className="state-message">Loading…</div>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return <Outlet />;
}
