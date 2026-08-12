export const CommissionStatus = {
  Pending: "Pending",
  Paid: "Paid",
  Cancelled: "Cancelled",
} as const;
export type CommissionStatus = (typeof CommissionStatus)[keyof typeof CommissionStatus];

export interface Commission {
  id: string;
  dealId: string;
  agentId: string;
  commissionPercentage: number;
  commissionAmount: number;
  companyCommission: number;
  status: CommissionStatus;
  paymentDate: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCommissionRequest {
  dealId: string;
  commissionPercentage: number;
  companyCommissionPercentage: number;
}

export interface CommissionListQuery {
  page?: number;
  pageSize?: number;
  status?: CommissionStatus;
  dealId?: string;
  agentId?: string;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}
