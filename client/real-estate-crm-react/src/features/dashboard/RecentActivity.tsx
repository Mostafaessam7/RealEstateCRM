import { Link } from "react-router-dom";
import { UserPlus, Handshake } from "lucide-react";
import { StatusBadge } from "../../components/StatusBadge";
import { useLeads } from "../leads/leadsApi";
import { useDeals } from "../deals/dealsApi";

type ActivityItem = {
  id: string;
  kind: "lead" | "deal";
  title: string;
  status: string;
  href: string;
  at: string;
};

function timeAgo(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime();
  const minutes = Math.floor(diffMs / 60_000);
  if (minutes < 1) return "just now";
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  if (days < 30) return `${days}d ago`;
  return new Date(iso).toLocaleDateString();
}

/**
 * A merged, chronological feed built from the existing Leads/Deals list endpoints (sorted by
 * createdAt) rather than a new dedicated "activity" endpoint — there's no distinct activity-log
 * concept to expose beyond what these two lists already represent, so a new backend surface
 * would just duplicate them.
 */
export function RecentActivity() {
  const leads = useLeads({ page: 1, pageSize: 5, sortBy: "createdAt", sortDirection: "desc" });
  const deals = useDeals({ page: 1, pageSize: 5, sortBy: "createdAt", sortDirection: "desc" });

  const isLoading = leads.isLoading || deals.isLoading;
  const isError = leads.isError || deals.isError;

  const items: ActivityItem[] = [
    ...(leads.data?.items.map((lead): ActivityItem => ({
      id: `lead-${lead.id}`,
      kind: "lead",
      title: lead.fullName,
      status: lead.status,
      href: `/leads/${lead.id}`,
      at: lead.createdAt,
    })) ?? []),
    ...(deals.data?.items.map((deal): ActivityItem => ({
      id: `deal-${deal.id}`,
      kind: "deal",
      title: `Deal · $${deal.dealValue.toLocaleString()}`,
      status: deal.status,
      href: "/deals",
      at: deal.createdAt,
    })) ?? []),
  ]
    .sort((a, b) => new Date(b.at).getTime() - new Date(a.at).getTime())
    .slice(0, 6);

  return (
    <div className="card">
      <h3 style={{ margin: "0 0 4px" }}>Recent Activity</h3>
      <p className="subtitle" style={{ margin: "0 0 12px" }}>
        The newest leads and deals across your pipeline.
      </p>

      {isLoading ? (
        <p className="state-message">Loading…</p>
      ) : isError ? (
        <p className="state-message">Couldn't load recent activity.</p>
      ) : items.length === 0 ? (
        <p className="state-message">Nothing yet — new leads and deals will show up here.</p>
      ) : (
        <ul style={{ listStyle: "none", margin: 0, padding: 0 }}>
          {items.map((item) => (
            <li
              key={item.id}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 10,
                padding: "9px 0",
                borderBottom: "1px solid var(--color-border)",
              }}
            >
              <span
                style={{
                  width: 30,
                  height: 30,
                  borderRadius: "50%",
                  display: "grid",
                  placeItems: "center",
                  flexShrink: 0,
                  background: item.kind === "lead" ? "var(--color-info-soft)" : "var(--color-primary-soft)",
                  color: item.kind === "lead" ? "var(--color-info)" : "var(--color-primary)",
                }}
              >
                {item.kind === "lead" ? <UserPlus size={14} /> : <Handshake size={14} />}
              </span>
              <Link to={item.href} style={{ flex: 1, minWidth: 0, fontSize: 13, fontWeight: 500 }}>
                <span
                  style={{
                    display: "block",
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                    whiteSpace: "nowrap",
                  }}
                >
                  {item.kind === "lead" ? "New lead" : "New deal"} · {item.title}
                </span>
              </Link>
              <StatusBadge status={item.status} />
              <span style={{ fontSize: 11.5, color: "var(--color-text-faint)", flexShrink: 0, minWidth: 52, textAlign: "right" }}>
                {timeAgo(item.at)}
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
