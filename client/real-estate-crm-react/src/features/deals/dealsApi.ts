import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import type { PagedResult } from "../../types/common";
import type { CreateDealRequest, Deal, DealListQuery } from "../../types/deal";

export function useDeals(query: DealListQuery) {
  return useQuery({
    queryKey: ["deals", query],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<Deal>>("/deals", { params: query });
      return response.data;
    },
  });
}

export function useCreateDeal() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateDealRequest) => {
      const response = await apiClient.post<Deal>("/deals", request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["deals"] }),
  });
}

function useDealAction(action: "reserve" | "contract" | "cancel") {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const response = await apiClient.post<Deal>(`/deals/${id}/${action}`);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["deals"] });
      queryClient.invalidateQueries({ queryKey: ["units"] });
    },
  });
}

export const useReserveDeal = () => useDealAction("reserve");
export const useContractDeal = () => useDealAction("contract");
export const useCancelDeal = () => useDealAction("cancel");
