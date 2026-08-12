export type StatusVariant = "success" | "warning" | "danger" | "info" | "neutral";

/**
 * One shared status → color mapping so a given word (e.g. "Cancelled") always renders
 * the same color everywhere in the app, regardless of which entity it belongs to.
 */
const STATUS_VARIANTS: Record<string, StatusVariant> = {
  // Leads
  New: "info",
  Contacted: "info",
  Interested: "warning",
  Viewing: "warning",
  Negotiation: "warning",
  Reserved: "warning",
  Contracted: "success",
  Lost: "danger",

  // Projects / Units
  Planning: "neutral",
  UnderConstruction: "warning",
  Ready: "success",
  SoldOut: "info",
  OnHold: "neutral",
  Available: "success",
  Sold: "info",
  Unavailable: "neutral",

  // Deals / Commissions / Tasks
  Pending: "warning",
  Paid: "success",
  Cancelled: "danger",
  Completed: "success",

  // Generic
  Active: "success",
  Inactive: "neutral",

  // Subscriptions
  Trialing: "info",
  PastDue: "danger",

  // WhatsApp messages
  Queued: "warning",
  Sent: "success",
  Failed: "danger",

  // Campaigns
  Draft: "neutral",
};

export function statusVariant(status: string): StatusVariant {
  return STATUS_VARIANTS[status] ?? "neutral";
}
