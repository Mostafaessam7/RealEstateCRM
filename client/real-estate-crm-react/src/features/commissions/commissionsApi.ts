import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import type { PagedResult } from "../../types/common";
import type { Commission, CommissionListQuery, CreateCommissionRequest } from "../../types/commission";
import type { Deal } from "../../types/deal";

export function useCommissions(query: CommissionListQuery) {
  return useQuery({
    queryKey: ["commissions", query],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<Commission>>("/commissions", { params: query });
      return response.data;
    },
  });
}

export function useContractedDeals() {
  return useQuery({
    queryKey: ["deals", "contracted", "all"],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<Deal>>("/deals", {
        params: { page: 1, pageSize: 100, status: "Contracted" },
      });
      return response.data.items;
    },
  });
}

export function useCreateCommission() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateCommissionRequest) => {
      const response = await apiClient.post<Commission>("/commissions", request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["commissions"] }),
  });
}

function useCommissionAction(action: "mark-paid" | "cancel") {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const response = await apiClient.post<Commission>(`/commissions/${id}/${action}`);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["commissions"] }),
  });
}

export const useMarkCommissionPaid = () => useCommissionAction("mark-paid");
export const useCancelCommission = () => useCommissionAction("cancel");
