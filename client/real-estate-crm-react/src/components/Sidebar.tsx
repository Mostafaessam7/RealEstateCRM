import { NavLink } from "react-router-dom";
import { ChevronsLeft, ChevronsRight, Building } from "lucide-react";
import { navItems } from "../routes/navConfig";
import type { Role } from "../types/auth";

interface SidebarProps {
  roles: Role[];
  open: boolean;
  collapsed: boolean;
  onToggleCollapse: () => void;
}

export function Sidebar({ roles, open, collapsed, onToggleCollapse }: SidebarProps) {
  const visibleItems = navItems.filter(
    (item) => !item.roles || item.roles.some((role) => roles.includes(role)),
  );

  return (
    <aside className={`sidebar${open ? " open" : ""}`}>
      <div className="brand">
        <span className="brand-mark">
          <Building size={17} color="#fff" strokeWidth={2.2} />
        </span>
        {!collapsed && <span>Real Estate CRM</span>}
      </div>

      <nav>
        {visibleItems.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink
              key={item.to}
              to={item.to}
              title={collapsed ? item.label : undefined}
              className={({ isActive }) => `sidebar-link${isActive ? " active" : ""}`}
            >
              <Icon size={18} strokeWidth={2} />
              {!collapsed && <span>{item.label}</span>}
            </NavLink>
          );
        })}
      </nav>

      <div className="sidebar-footer">
        <button type="button" className="sidebar-collapse-btn" onClick={onToggleCollapse}>
          {collapsed ? <ChevronsRight size={16} /> : <ChevronsLeft size={16} />}
          {!collapsed && <span style={{ fontSize: 12.5 }}>Collapse</span>}
        </button>
      </div>
    </aside>
  );
}
