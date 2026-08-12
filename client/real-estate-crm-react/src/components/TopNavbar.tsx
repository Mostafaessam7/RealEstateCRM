import { useEffect, useRef, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { Menu, Search, LogOut, ChevronDown } from "lucide-react";
import { NotificationBell } from "./NotificationBell";

interface TopNavbarProps {
  userLabel: string;
  roleLabel?: string;
  onLogout: () => void;
  onToggleSidebar: () => void;
}

function initials(label: string): string {
  const clean = label.replace(/@.*/, "");
  const parts = clean.split(/[.\s_-]+/).filter(Boolean);
  if (parts.length === 0) return "?";
  return (parts[0][0] + (parts[1]?.[0] ?? "")).toUpperCase();
}

export function TopNavbar({ userLabel, roleLabel, onLogout, onToggleSidebar }: TopNavbarProps) {
  const [menuOpen, setMenuOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const onClickOutside = (e: MouseEvent) => {
      if (rootRef.current && !rootRef.current.contains(e.target as Node)) setMenuOpen(false);
    };
    document.addEventListener("mousedown", onClickOutside);
    return () => document.removeEventListener("mousedown", onClickOutside);
  }, []);

  return (
    <header className="navbar">
      <button type="button" className="icon-btn" onClick={onToggleSidebar} aria-label="Toggle navigation">
        <Menu size={18} />
      </button>

      <div className="navbar-search">
        <Search size={15} color="var(--color-text-faint)" />
        <input type="search" placeholder="Search leads, projects, units…" aria-label="Search" />
      </div>

      <div className="navbar-actions">
        <NotificationBell />

        <div ref={rootRef} style={{ position: "relative" }}>
          <button
            type="button"
            className="user-chip"
            onClick={() => setMenuOpen((o) => !o)}
            style={{ background: "none", border: "none", cursor: "pointer" }}
          >
            <span className="avatar">{initials(userLabel)}</span>
            <span style={{ display: "flex", flexDirection: "column", alignItems: "flex-start", lineHeight: 1.2 }}>
              <span style={{ fontSize: 13, fontWeight: 600 }}>{userLabel}</span>
              {roleLabel && <span style={{ fontSize: 11, color: "var(--color-text-muted)" }}>{roleLabel}</span>}
            </span>
            <ChevronDown size={14} color="var(--color-text-faint)" />
          </button>

          <AnimatePresence>
            {menuOpen && (
              <motion.div
                initial={{ opacity: 0, y: -6, scale: 0.97 }}
                animate={{ opacity: 1, y: 0, scale: 1 }}
                exit={{ opacity: 0, y: -6, scale: 0.97 }}
                transition={{ duration: 0.15 }}
                className="card-flat"
                style={{ position: "absolute", right: 0, top: "calc(100% + 10px)", width: 180, padding: 6, zIndex: 60, boxShadow: "var(--shadow-lg)" }}
              >
                <button
                  type="button"
                  className="sidebar-link"
                  style={{ width: "100%", color: "var(--color-danger)" }}
                  onClick={onLogout}
                >
                  <LogOut size={16} />
                  <span>Logout</span>
                </button>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      </div>
    </header>
  );
}
