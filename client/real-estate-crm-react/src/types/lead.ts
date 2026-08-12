export const LeadStatus = {
  New: "New",
  Contacted: "Contacted",
  Interested: "Interested",
  Viewing: "Viewing",
  Negotiation: "Negotiation",
  Reserved: "Reserved",
  Contracted: "Contracted",
  Lost: "Lost",
} as const;
export type LeadStatus = (typeof LeadStatus)[keyof typeof LeadStatus];

export const LeadSource = {
  Website: "Website",
  Facebook: "Facebook",
  Instagram: "Instagram",
  Google: "Google",
  Referral: "Referral",
  WalkIn: "WalkIn",
  Phone: "Phone",
  Other: "Other",
} as const;
export type LeadSource = (typeof LeadSource)[keyof typeof LeadSource];

export const LeadActivityType = {
  Call: "Call",
  WhatsApp: "WhatsApp",
  Email: "Email",
  Meeting: "Meeting",
  Viewing: "Viewing",
  Note: "Note",
  FollowUp: "FollowUp",
} as const;
export type LeadActivityType = (typeof LeadActivityType)[keyof typeof LeadActivityType];

export interface Lead {
  id: string;
  fullName: string;
  phone: string | null;
  email: string | null;
  source: LeadSource;
  status: LeadStatus;
  budgetMin: number | null;
  budgetMax: number | null;
  preferredLocation: string | null;
  propertyType: string | null;
  assignedAgentId: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateLeadRequest {
  fullName: string;
  phone?: string | null;
  email?: string | null;
  source: LeadSource;
  budgetMin?: number | null;
  budgetMax?: number | null;
  preferredLocation?: string | null;
  propertyType?: string | null;
  assignedAgentId?: string | null;
  notes?: string | null;
}

export interface UpdateLeadRequest extends CreateLeadRequest {
  status: LeadStatus;
}

export interface LeadListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: LeadStatus;
  assignedAgentId?: string;
  source?: LeadSource;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}

export interface AssignLeadRequest {
  agentId: string;
}

export interface LeadActivity {
  id: string;
  leadId: string;
  userId: string;
  type: LeadActivityType;
  description: string | null;
  activityDate: string;
  createdAt: string;
}

export interface CreateLeadActivityRequest {
  type: LeadActivityType;
  description?: string | null;
  activityDate?: string | null;
}
