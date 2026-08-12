import {
  LayoutDashboard,
  Contact,
  KanbanSquare,
  Building2,
  DoorOpen,
  Handshake,
  ListChecks,
  Wallet,
  BarChart3,
  UserCog,
  Settings,
  CreditCard,
  MessageCircle,
  Megaphone,
  Key,
  Webhook,
  type LucideIcon,
} from "lucide-react";
import { Roles, type Role } from "../types/auth";

export interface NavItem {
  to: string;
  label: string;
  icon: LucideIcon;
  /** Omit to show for every authenticated role. */
  roles?: Role[];
}

export const navItems: NavItem[] = [
  { to: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  { to: "/leads", label: "Leads", icon: Contact },
  { to: "/pipeline", label: "Pipeline", icon: KanbanSquare },
  { to: "/projects", label: "Projects", icon: Building2 },
  { to: "/units", label: "Units", icon: DoorOpen },
  { to: "/deals", label: "Deals", icon: Handshake },
  { to: "/tasks", label: "Tasks", icon: ListChecks },
  {
    to: "/commissions",
    label: "Commissions",
    icon: Wallet,
    roles: [Roles.CompanyAdmin, Roles.SalesManager, Roles.SuperAdmin],
  },
  { to: "/reports", label: "Reports", icon: BarChart3 },
  {
    to: "/whatsapp-templates",
    label: "WhatsApp",
    icon: MessageCircle,
    roles: [Roles.CompanyAdmin, Roles.SalesManager, Roles.SuperAdmin],
  },
  {
    to: "/marketing-campaigns",
    label: "Marketing",
    icon: Megaphone,
    roles: [Roles.CompanyAdmin, Roles.SalesManager, Roles.SuperAdmin],
  },
  {
    to: "/users",
    label: "Users",
    icon: UserCog,
    roles: [Roles.CompanyAdmin, Roles.SuperAdmin],
  },
  {
    to: "/company-settings",
    label: "Company Settings",
    icon: Settings,
    roles: [Roles.CompanyAdmin, Roles.SuperAdmin],
  },
  {
    to: "/billing",
    label: "Billing",
    icon: CreditCard,
    roles: [Roles.CompanyAdmin, Roles.SuperAdmin],
  },
  {
    to: "/api-keys",
    label: "API Keys",
    icon: Key,
    roles: [Roles.CompanyAdmin, Roles.SuperAdmin],
  },
  {
    to: "/webhooks",
    label: "Webhooks",
    icon: Webhook,
    roles: [Roles.CompanyAdmin, Roles.SuperAdmin],
  },
];
