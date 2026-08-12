export interface WebhookSubscription {
  id: string;
  url: string;
  eventTypes: string[];
  isActive: boolean;
  createdAt: string;
}

export interface CreatedWebhookSubscription extends WebhookSubscription {
  secret: string;
}

export interface CreateWebhookSubscriptionRequest {
  url: string;
  eventTypes: string[];
}

export interface WebhookDelivery {
  id: string;
  eventType: string;
  attemptNumber: number;
  success: boolean;
  responseStatusCode: number | null;
  errorMessage: string | null;
  createdAt: string;
  deliveredAt: string | null;
}
