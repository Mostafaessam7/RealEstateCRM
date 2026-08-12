export interface Company {
  id: string;
  name: string;
  slug: string;
  phone: string | null;
  email: string | null;
  isActive: boolean;
}

export interface DashboardSummary {
  totalLeads: number;
  newLeadsLast30Days: number;
  conversionRatePercent: number;
  totalDeals: number;
  totalActiveDeals: number;
  totalSalesValue: number;
  upcomingFollowUps: number;
  totalAvailableUnits: number;
}
