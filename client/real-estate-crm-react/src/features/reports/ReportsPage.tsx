import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { BreakdownTable } from "./BreakdownTable";
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
          <div className="kpi-card">
            <div className="value">{conversion.data?.conversionRatePercent ?? 0}%</div>
            <div className="label">Conversion Rate</div>
          </div>
          <div className="kpi-card">
            <div className="value">{sales.data?.totalSalesValue ?? 0}</div>
            <div className="label">Total Sales Value</div>
          </div>
          <div className="kpi-card">
            <div className="value">{commissions.data?.totalPaid ?? 0}</div>
            <div className="label">Commissions Paid</div>
          </div>
          <div className="kpi-card">
            <div className="value">{commissions.data?.totalPending ?? 0}</div>
            <div className="label">Commissions Pending</div>
          </div>
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
                      <td>{agent.totalCommissionEarned}</td>
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
