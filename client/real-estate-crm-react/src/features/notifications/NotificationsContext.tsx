import { HubConnectionBuilder, LogLevel, type HubConnection } from "@microsoft/signalr";
import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { toast } from "sonner";
import { authSession } from "../../utils/authSession";
import { useAuth } from "../auth/AuthContext";

export interface AppNotification {
  id: string;
  type: string;
  title: string;
  message: string;
  createdAt: string;
  read: boolean;
}

interface NotificationsContextValue {
  notifications: AppNotification[];
  unreadCount: number;
  markAllRead: () => void;
  isConnected: boolean;
}

const NotificationsContext = createContext<NotificationsContextValue | undefined>(undefined);

function hubUrl(): string {
  const apiBase = import.meta.env.VITE_API_BASE_URL ?? "";
  return `${apiBase.replace(/\/api\/?$/, "")}/hubs/notifications`;
}

export function NotificationsProvider({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  const [notifications, setNotifications] = useState<AppNotification[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const connectionRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    if (!isAuthenticated) {
      connectionRef.current?.stop();
      connectionRef.current = null;
      setIsConnected(false);
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl(), { accessTokenFactory: () => authSession.getAccessToken() ?? "" })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on("ReceiveNotification", (payload: Omit<AppNotification, "id" | "read"> & { id?: string }) => {
      const notification: AppNotification = {
        id: payload.id ?? crypto.randomUUID(),
        type: payload.type,
        title: payload.title,
        message: payload.message,
        createdAt: payload.createdAt ?? new Date().toISOString(),
        read: false,
      };
      setNotifications((prev) => [notification, ...prev].slice(0, 30));
      toast(notification.title, { description: notification.message });
    });

    connection.onreconnected(() => setIsConnected(true));
    connection.onclose(() => setIsConnected(false));

    connection
      .start()
      .then(() => setIsConnected(true))
      .catch(() => setIsConnected(false));

    connectionRef.current = connection;

    return () => {
      connection.stop();
    };
  }, [isAuthenticated]);

  const markAllRead = useCallback(() => {
    setNotifications((prev) => prev.map((n) => ({ ...n, read: true })));
  }, []);

  const unreadCount = notifications.filter((n) => !n.read).length;

  const value = useMemo(
    () => ({ notifications, unreadCount, markAllRead, isConnected }),
    [notifications, unreadCount, markAllRead, isConnected],
  );

  return <NotificationsContext.Provider value={value}>{children}</NotificationsContext.Provider>;
}

export function useNotifications(): NotificationsContextValue {
  const context = useContext(NotificationsContext);
  if (!context) {
    throw new Error("useNotifications must be used within NotificationsProvider");
  }
  return context;
}
