import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import type { CreateWebhookSubscriptionRequest, CreatedWebhookSubscription, WebhookDelivery, WebhookSubscription } from "../../types/webhook";

export function useWebhooks() {
  return useQuery({
    queryKey: ["webhooks"],
    queryFn: async () => {
      const response = await apiClient.get<WebhookSubscription[]>("/webhooks");
      return response.data;
    },
  });
}

export function useWebhookEventTypes() {
  return useQuery({
    queryKey: ["webhooks", "event-types"],
    queryFn: async () => {
      const response = await apiClient.get<string[]>("/webhooks/event-types");
      return response.data;
    },
  });
}

export function useCreateWebhook() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateWebhookSubscriptionRequest) => {
      const response = await apiClient.post<CreatedWebhookSubscription>("/webhooks", request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["webhooks"] }),
  });
}

export function useDeleteWebhook() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/webhooks/${id}`);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["webhooks"] }),
  });
}

export function useWebhookDeliveries(subscriptionId: string | null) {
  return useQuery({
    queryKey: ["webhooks", subscriptionId, "deliveries"],
    queryFn: async () => {
      const response = await apiClient.get<WebhookDelivery[]>(`/webhooks/${subscriptionId}/deliveries`);
      return response.data;
    },
    enabled: !!subscriptionId,
  });
}
