export interface LeadsReport {
  totalLeads: number;
  newLeadsLast30Days: number;
  byStatus: Record<string, number>;
  bySource: Record<string, number>;
}

export interface SalesReport {
  totalDeals: number;
  contractedDeals: number;
  totalSalesValue: number;
  byStatus: Record<string, number>;
}

export interface ConversionReport {
  totalLeads: number;
  convertedLeads: number;
  conversionRatePercent: number;
}

export interface AgentPerformance {
  agentId: string;
  agentName: string;
  leadsAssigned: number;
  dealsContracted: number;
  totalCommissionEarned: number;
}

export interface CommissionReport {
  totalPending: number;
  totalPaid: number;
  totalCancelled: number;
}

export interface InventoryReport {
  totalProjects: number;
  totalUnits: number;
  unitsByStatus: Record<string, number>;
}
