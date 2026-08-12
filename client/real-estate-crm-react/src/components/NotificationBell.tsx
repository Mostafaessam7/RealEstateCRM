import { useState, useRef, useEffect } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { Bell, BellRing, CheckCheck } from "lucide-react";
import { useNotifications } from "../features/notifications/NotificationsContext";

function timeAgo(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diffMs / 60000);
  if (mins < 1) return "just now";
  if (mins < 60) return `${mins}m ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.floor(hours / 24)}d ago`;
}

export function NotificationBell() {
  const { notifications, unreadCount, markAllRead } = useNotifications();
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const onClickOutside = (e: MouseEvent) => {
      if (rootRef.current && !rootRef.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", onClickOutside);
    return () => document.removeEventListener("mousedown", onClickOutside);
  }, []);

  return (
    <div ref={rootRef} style={{ position: "relative" }}>
      <button
        type="button"
        className="icon-btn"
        aria-label="Notifications"
        onClick={() => {
          setOpen((o) => !o);
          if (!open) markAllRead();
        }}
      >
        {unreadCount > 0 ? <BellRing size={18} /> : <Bell size={18} />}
        {unreadCount > 0 && <span className="dot" />}
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, y: -6, scale: 0.97 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -6, scale: 0.97 }}
            transition={{ duration: 0.15 }}
            className="card-flat"
            style={{
              position: "absolute",
              right: 0,
              top: "calc(100% + 10px)",
              width: 320,
              maxHeight: 380,
              overflowY: "auto",
              zIndex: 60,
              padding: 0,
              boxShadow: "var(--shadow-lg)",
            }}
          >
            <div
              style={{
                padding: "12px 16px",
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
                borderBottom: "1px solid var(--color-border)",
              }}
            >
              <strong style={{ fontSize: 13.5 }}>Notifications</strong>
              {notifications.length > 0 && (
                <button
                  type="button"
                  onClick={markAllRead}
                  style={{ display: "flex", alignItems: "center", gap: 4, fontSize: 11.5, color: "var(--color-text-muted)", background: "none", border: "none", cursor: "pointer" }}
                >
                  <CheckCheck size={13} /> Mark all read
                </button>
              )}
            </div>

            {notifications.length === 0 ? (
              <div style={{ padding: "28px 16px", textAlign: "center", color: "var(--color-text-muted)", fontSize: 13 }}>
                No notifications yet
              </div>
            ) : (
              notifications.map((n) => (
                <div
                  key={n.id}
                  style={{
                    padding: "10px 16px",
                    borderBottom: "1px solid var(--color-border)",
                    background: n.read ? "transparent" : "var(--color-primary-soft)",
                  }}
                >
                  <div style={{ fontSize: 12.5, fontWeight: 600 }}>{n.title}</div>
                  <div style={{ fontSize: 12, color: "var(--color-text-muted)", marginTop: 2 }}>{n.message}</div>
                  <div style={{ fontSize: 10.5, color: "var(--color-text-faint)", marginTop: 4 }}>{timeAgo(n.createdAt)}</div>
                </div>
              ))
            )}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
