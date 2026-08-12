import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import type { ChangePlanRequest, CompanySubscription, SubscriptionPlan } from "../../types/subscription";

export function usePlans() {
  return useQuery({
    queryKey: ["subscriptions", "plans"],
    queryFn: async () => {
      const response = await apiClient.get<SubscriptionPlan[]>("/subscriptions/plans");
      return response.data;
    },
  });
}

export function useCurrentSubscription() {
  return useQuery({
    queryKey: ["subscriptions", "current"],
    queryFn: async () => {
      const response = await apiClient.get<CompanySubscription>("/subscriptions/current");
      return response.data;
    },
  });
}

export function useChangePlan() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: ChangePlanRequest) => {
      const response = await apiClient.post<CompanySubscription>("/subscriptions/change-plan", request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["subscriptions"] }),
  });
}

export function useCancelSubscription() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async () => {
      const response = await apiClient.post<CompanySubscription>("/subscriptions/cancel");
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["subscriptions"] }),
  });
}
