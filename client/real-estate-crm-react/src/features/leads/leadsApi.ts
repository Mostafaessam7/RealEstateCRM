import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import type { PagedResult } from "../../types/common";
import type {
  AssignLeadRequest,
  CreateLeadActivityRequest,
  CreateLeadRequest,
  Lead,
  LeadActivity,
  LeadListQuery,
  UpdateLeadRequest,
} from "../../types/lead";
import type { UnitRecommendation } from "../../types/recommendation";
import type { AiLeadInsight } from "../../types/aiInsight";

const leadsKey = (query?: LeadListQuery) => ["leads", query] as const;
const leadKey = (id: string) => ["leads", "detail", id] as const;
const leadTimelineKey = (id: string) => ["leads", id, "activities"] as const;

export function useLeads(query: LeadListQuery) {
  return useQuery({
    queryKey: leadsKey(query),
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<Lead>>("/leads", { params: query });
      return response.data;
    },
  });
}

export function useLead(id: string | undefined) {
  return useQuery({
    queryKey: id ? leadKey(id) : ["leads", "detail", "none"],
    queryFn: async () => {
      const response = await apiClient.get<Lead>(`/leads/${id}`);
      return response.data;
    },
    enabled: Boolean(id),
  });
}

export function useCreateLead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateLeadRequest) => {
      const response = await apiClient.post<Lead>("/leads", request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["leads"] }),
  });
}

export function useUpdateLead(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: UpdateLeadRequest) => {
      const response = await apiClient.put<Lead>(`/leads/${id}`, request);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["leads"] });
    },
  });
}

export function useAssignLead(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: AssignLeadRequest) => {
      const response = await apiClient.post<Lead>(`/leads/${id}/assign`, request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["leads"] }),
  });
}

export function useTransferLead(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: AssignLeadRequest) => {
      const response = await apiClient.post<Lead>(`/leads/${id}/transfer`, request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["leads"] }),
  });
}

export function useDeleteLead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/leads/${id}`);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["leads"] }),
  });
}

export function useLeadTimeline(leadId: string | undefined) {
  return useQuery({
    queryKey: leadId ? leadTimelineKey(leadId) : ["leads", "none", "activities"],
    queryFn: async () => {
      const response = await apiClient.get<LeadActivity[]>(`/leads/${leadId}/activities`);
      return response.data;
    },
    enabled: Boolean(leadId),
  });
}

export function useAddLeadActivity(leadId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateLeadActivityRequest) => {
      const response = await apiClient.post<LeadActivity>(`/leads/${leadId}/activities`, request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: leadTimelineKey(leadId) }),
  });
}

export function useLeadRecommendations(leadId: string | undefined) {
  return useQuery({
    queryKey: leadId ? ["leads", leadId, "recommendations"] : ["leads", "none", "recommendations"],
    queryFn: async () => {
      const response = await apiClient.get<UnitRecommendation[]>(`/leads/${leadId}/recommendations`);
      return response.data;
    },
    enabled: Boolean(leadId),
  });
}

export function useGenerateAiInsight(leadId: string) {
  return useMutation({
    mutationFn: async () => {
      const response = await apiClient.get<AiLeadInsight>(`/leads/${leadId}/ai-insight`);
      return response.data;
    },
  });
}

export function useUpcomingFollowUps(days = 7) {
  return useQuery({
    queryKey: ["leads", "follow-ups", "upcoming", days],
    queryFn: async () => {
      const response = await apiClient.get<LeadActivity[]>("/leads/follow-ups/upcoming", { params: { days } });
      return response.data;
    },
  });
}
