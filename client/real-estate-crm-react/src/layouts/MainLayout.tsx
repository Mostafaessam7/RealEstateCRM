import { useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { Outlet, useLocation } from "react-router-dom";
import { Sidebar } from "../components/Sidebar";
import { TopNavbar } from "../components/TopNavbar";
import { useAuth } from "../features/auth/AuthContext";

export function MainLayout() {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [collapsed, setCollapsed] = useState(false);
  const { user, logout } = useAuth();
  const location = useLocation();

  return (
    <div className={`app-shell${collapsed ? " collapsed" : ""}`}>
      <Sidebar
        roles={user?.roles ?? []}
        open={sidebarOpen}
        collapsed={collapsed}
        onToggleCollapse={() => setCollapsed((c) => !c)}
      />
      {sidebarOpen && <div className="overlay-backdrop" onClick={() => setSidebarOpen(false)} />}
      <TopNavbar
        userLabel={user ? user.fullName : ""}
        roleLabel={user?.roles?.[0]}
        onLogout={logout}
        onToggleSidebar={() => setSidebarOpen((open) => !open)}
      />
      <main className="content">
        <AnimatePresence mode="wait">
          <motion.div
            key={location.pathname}
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -8 }}
            transition={{ duration: 0.18, ease: [0.22, 1, 0.36, 1] }}
          >
            <Outlet />
          </motion.div>
        </AnimatePresence>
      </main>
    </div>
  );
}
