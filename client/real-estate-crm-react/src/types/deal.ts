export const DealStatus = {
  Pending: "Pending",
  Reserved: "Reserved",
  Contracted: "Contracted",
  Cancelled: "Cancelled",
} as const;
export type DealStatus = (typeof DealStatus)[keyof typeof DealStatus];

export interface Deal {
  id: string;
  leadId: string;
  unitId: string;
  salesAgentId: string;
  dealValue: number;
  status: DealStatus;
  reservationDate: string | null;
  contractDate: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateDealRequest {
  leadId: string;
  unitId: string;
  salesAgentId?: string | null;
  dealValue: number;
  notes?: string | null;
}

export interface UpdateDealRequest {
  dealValue: number;
  notes?: string | null;
}

export interface DealListQuery {
  page?: number;
  pageSize?: number;
  status?: DealStatus;
  leadId?: string;
  unitId?: string;
  salesAgentId?: string;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}
