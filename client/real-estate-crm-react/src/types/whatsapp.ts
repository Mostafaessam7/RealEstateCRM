export const WhatsAppMessageStatus = {
  Queued: "Queued",
  Sent: "Sent",
  Failed: "Failed",
} as const;
export type WhatsAppMessageStatus = (typeof WhatsAppMessageStatus)[keyof typeof WhatsAppMessageStatus];

export interface WhatsAppTemplate {
  id: string;
  name: string;
  body: string;
  isActive: boolean;
}

export interface CreateWhatsAppTemplateRequest {
  name: string;
  body: string;
}

export interface UpdateWhatsAppTemplateRequest {
  name: string;
  body: string;
  isActive: boolean;
}

export interface WhatsAppMessage {
  id: string;
  leadId: string;
  templateId: string | null;
  toPhone: string;
  body: string;
  status: WhatsAppMessageStatus;
  errorMessage: string | null;
  sentAt: string | null;
  createdAt: string;
}

export interface SendWhatsAppRequest {
  templateId?: string;
  body?: string;
}
