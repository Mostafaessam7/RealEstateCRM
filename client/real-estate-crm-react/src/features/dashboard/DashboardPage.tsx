import { Users, UserPlus, TrendingUp, Handshake, Wallet, CalendarClock, DoorOpen } from "lucide-react";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { CardGridSkeleton } from "../../components/Skeleton";
import { StatCard } from "../../components/StatCard";
import { useDashboardSummary } from "./dashboardApi";
import { useLeadsReport } from "../reports/reportsApi";
import { LeadsPipelineChart } from "./LeadsPipelineChart";
import { RecentActivity } from "./RecentActivity";

export function DashboardPage() {
  const { data, isLoading, isError } = useDashboardSummary();
  const leadsReport = useLeadsReport();

  return (
    <>
      <PageHeader title="Dashboard" subtitle="A live snapshot of your pipeline and performance." />
      <AsyncState
        isLoading={isLoading}
        isError={isError}
        errorMessage="Failed to load dashboard summary."
        skeleton={<CardGridSkeleton count={7} />}
      >
        <div className="kpi-grid">
          <StatCard index={0} label="Total Leads" value={data?.totalLeads ?? 0} icon={<Users size={19} />} accent="primary" />
          <StatCard index={1} label="New Leads (30d)" value={data?.newLeadsLast30Days ?? 0} icon={<UserPlus size={19} />} accent="info" />
          <StatCard
            index={2}
            label="Conversion Rate"
            value={data?.conversionRatePercent ?? 0}
            icon={<TrendingUp size={19} />}
            accent="success"
            suffix="%"
          />
          <StatCard index={3} label="Total Deals" value={data?.totalDeals ?? 0} icon={<Handshake size={19} />} accent="primary" />
          <StatCard
            index={4}
            label="Total Sales Value"
            value={data?.totalSalesValue ?? 0}
            icon={<Wallet size={19} />}
            accent="success"
            prefix="$"
            format={(v) => v.toLocaleString(undefined, { maximumFractionDigits: 0 })}
          />
          <StatCard index={5} label="Upcoming Follow-ups" value={data?.upcomingFollowUps ?? 0} icon={<CalendarClock size={19} />} accent="warning" />
          <StatCard index={6} label="Available Units" value={data?.totalAvailableUnits ?? 0} icon={<DoorOpen size={19} />} accent="info" />
        </div>

        <div className="dashboard-panels">
          <LeadsPipelineChart byStatus={leadsReport.data?.byStatus ?? {}} />
          <RecentActivity />
        </div>
      </AsyncState>
    </>
  );
}
