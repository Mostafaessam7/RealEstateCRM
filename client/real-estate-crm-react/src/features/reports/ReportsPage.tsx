import { TrendingUp, Wallet, HandCoins, Clock } from "lucide-react";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { StatCard } from "../../components/StatCard";
import { BreakdownTable } from "./BreakdownTable";
import { formatCurrency } from "../../utils/format";
import {
  useAgentPerformanceReport,
  useCommissionReport,
  useConversionReport,
  useInventoryReport,
  useLeadsReport,
  useSalesReport,
} from "./reportsApi";

export function ReportsPage() {
  const leads = useLeadsReport();
  const sales = useSalesReport();
  const conversion = useConversionReport();
  const agents = useAgentPerformanceReport();
  const commissions = useCommissionReport();
  const inventory = useInventoryReport();

  const isLoading =
    leads.isLoading || sales.isLoading || conversion.isLoading || agents.isLoading || commissions.isLoading || inventory.isLoading;
  const isError =
    leads.isError || sales.isError || conversion.isError || agents.isError || commissions.isError || inventory.isError;

  return (
    <>
      <PageHeader title="Reports" />
      <AsyncState isLoading={isLoading} isError={isError} errorMessage="Failed to load reports.">
        <div className="kpi-grid">
          <StatCard
            index={0}
            label="Conversion Rate"
            value={conversion.data?.conversionRatePercent ?? 0}
            icon={<TrendingUp size={19} />}
            accent="success"
            suffix="%"
          />
          <StatCard
            index={1}
            label="Total Sales Value"
            value={sales.data?.totalSalesValue ?? 0}
            icon={<Wallet size={19} />}
            accent="primary"
            prefix="$"
            format={(v) => v.toLocaleString(undefined, { maximumFractionDigits: 0 })}
          />
          <StatCard
            index={2}
            label="Commissions Paid"
            value={commissions.data?.totalPaid ?? 0}
            icon={<HandCoins size={19} />}
            accent="success"
            prefix="$"
            format={(v) => v.toLocaleString(undefined, { maximumFractionDigits: 0 })}
          />
          <StatCard
            index={3}
            label="Commissions Pending"
            value={commissions.data?.totalPending ?? 0}
            icon={<Clock size={19} />}
            accent="warning"
            prefix="$"
            format={(v) => v.toLocaleString(undefined, { maximumFractionDigits: 0 })}
          />
        </div>

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16, marginBottom: 16 }}>
          <BreakdownTable title="Leads by Status" data={leads.data?.byStatus ?? {}} />
          <BreakdownTable title="Leads by Source" data={leads.data?.bySource ?? {}} />
          <BreakdownTable title="Deals by Status" data={sales.data?.byStatus ?? {}} />
          <BreakdownTable title="Units by Status" data={inventory.data?.unitsByStatus ?? {}} />
        </div>

        <div className="card">
          <h3 style={{ marginTop: 0 }}>Agent Performance</h3>
          {agents.data && agents.data.length > 0 ? (
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Agent</th>
                    <th>Leads Assigned</th>
                    <th>Deals Contracted</th>
                    <th>Commission Earned</th>
                  </tr>
                </thead>
                <tbody>
                  {agents.data.map((agent) => (
                    <tr key={agent.agentId}>
                      <td>{agent.agentName}</td>
                      <td>{agent.leadsAssigned}</td>
                      <td>{agent.dealsContracted}</td>
                      <td>{formatCurrency(agent.totalCommissionEarned)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <p className="state-message">No agent activity yet.</p>
          )}
        </div>
      </AsyncState>
    </>
  );
}
