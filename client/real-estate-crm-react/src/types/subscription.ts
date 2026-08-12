export const SubscriptionStatus = {
  Trialing: "Trialing",
  Active: "Active",
  PastDue: "PastDue",
  Cancelled: "Cancelled",
} as const;
export type SubscriptionStatus = (typeof SubscriptionStatus)[keyof typeof SubscriptionStatus];

export interface SubscriptionPlan {
  id: string;
  code: string;
  name: string;
  monthlyPrice: number;
  maxUsers: number;
  maxLeads: number;
  maxUnits: number;
  isActive: boolean;
}

export interface SubscriptionUsage {
  userCount: number;
  leadCount: number;
  unitCount: number;
}

export interface CompanySubscription {
  id: string;
  plan: SubscriptionPlan;
  status: SubscriptionStatus;
  trialEndsAt: string;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  cancelledAt: string | null;
  usage: SubscriptionUsage;
}

export interface ChangePlanRequest {
  planCode: string;
}
