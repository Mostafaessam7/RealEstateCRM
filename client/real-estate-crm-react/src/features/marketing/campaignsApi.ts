import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import type { Campaign, CampaignRecipient, CreateCampaignRequest } from "../../types/campaign";

export function useCampaigns() {
  return useQuery({
    queryKey: ["campaigns"],
    queryFn: async () => {
      const response = await apiClient.get<Campaign[]>("/campaigns");
      return response.data;
    },
  });
}

export function useCreateCampaign() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateCampaignRequest) => {
      const response = await apiClient.post<Campaign>("/campaigns", request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["campaigns"] }),
  });
}

export function useSendCampaign() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const response = await apiClient.post<Campaign>(`/campaigns/${id}/send`);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["campaigns"] }),
  });
}

export function useCampaignRecipients(campaignId: string | null) {
  return useQuery({
    queryKey: ["campaigns", campaignId, "recipients"],
    queryFn: async () => {
      const response = await apiClient.get<CampaignRecipient[]>(`/campaigns/${campaignId}/recipients`);
      return response.data;
    },
    enabled: !!campaignId,
  });
}
