import { Bar, BarChart, CartesianGrid, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { LeadStatus } from "../../types/lead";
import { statusVariant, type StatusVariant } from "../../utils/statusVariant";

// Same variant → color mapping StatusBadge uses everywhere else, so the chart's colors read as
// the same system as the rest of the app rather than an unrelated arbitrary palette.
const VARIANT_COLOR: Record<StatusVariant, string> = {
  success: "var(--color-success)",
  warning: "var(--color-warning)",
  danger: "var(--color-danger)",
  info: "var(--color-info)",
  neutral: "var(--color-text-faint)",
};

interface LeadsPipelineChartProps {
  byStatus: Record<string, number>;
}

export function LeadsPipelineChart({ byStatus }: LeadsPipelineChartProps) {
  // Fixed pipeline order (not alphabetical/whatever object-key order the API happens to return)
  // so the funnel reads left-to-right the way the sales process actually flows.
  const data = Object.values(LeadStatus).map((status) => ({
    status,
    count: byStatus[status] ?? 0,
    color: VARIANT_COLOR[statusVariant(status)],
  }));

  const hasAnyData = data.some((d) => d.count > 0);

  return (
    <div className="card">
      <h3 style={{ margin: "0 0 4px" }}>Leads Pipeline</h3>
      <p className="subtitle" style={{ margin: "0 0 16px" }}>
        Where every lead currently sits, from first contact to close.
      </p>
      {hasAnyData ? (
        <ResponsiveContainer width="100%" height={220}>
          <BarChart data={data} margin={{ top: 4, right: 8, left: -20, bottom: 0 }}>
            <CartesianGrid vertical={false} stroke="var(--color-border)" />
            <XAxis
              dataKey="status"
              tick={{ fontSize: 11, fill: "var(--color-text-muted)" }}
              axisLine={{ stroke: "var(--color-border)" }}
              tickLine={false}
              interval={0}
              angle={-20}
              textAnchor="end"
              height={48}
            />
            <YAxis allowDecimals={false} tick={{ fontSize: 11, fill: "var(--color-text-muted)" }} axisLine={false} tickLine={false} width={28} />
            <Tooltip
              cursor={{ fill: "var(--color-primary-soft)" }}
              contentStyle={{
                background: "var(--color-surface)",
                border: "1px solid var(--color-border)",
                borderRadius: "var(--radius-md)",
                fontSize: 12.5,
              }}
            />
            <Bar dataKey="count" radius={[4, 4, 0, 0]} maxBarSize={40}>
              {data.map((entry) => (
                <Cell key={entry.status} fill={entry.color} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      ) : (
        <p className="state-message">No leads yet.</p>
      )}
    </div>
  );
}
