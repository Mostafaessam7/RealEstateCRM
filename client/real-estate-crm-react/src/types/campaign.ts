export const CampaignChannel = {
  Email: "Email",
  WhatsApp: "WhatsApp",
} as const;
export type CampaignChannel = (typeof CampaignChannel)[keyof typeof CampaignChannel];

export const CampaignStatus = {
  Draft: "Draft",
  Sent: "Sent",
} as const;
export type CampaignStatus = (typeof CampaignStatus)[keyof typeof CampaignStatus];

export interface Campaign {
  id: string;
  name: string;
  channel: CampaignChannel;
  subject: string | null;
  body: string;
  targetStatus: string | null;
  targetSource: string | null;
  status: CampaignStatus;
  sentAt: string | null;
  recipientCount: number;
  successCount: number;
  failureCount: number;
  createdAt: string;
}

export interface CreateCampaignRequest {
  name: string;
  channel: CampaignChannel;
  subject?: string;
  body: string;
  targetStatus?: string;
  targetSource?: string;
}

export interface CampaignRecipient {
  id: string;
  leadId: string;
  success: boolean;
  errorMessage: string | null;
  sentAt: string;
}
