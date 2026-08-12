import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import type { PagedResult } from "../../types/common";
import type { CreateUnitRequest, Unit, UnitListQuery, UpdateUnitRequest } from "../../types/unit";

export function useUnits(query: UnitListQuery) {
  return useQuery({
    queryKey: ["units", query],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<Unit>>("/units", { params: query });
      return response.data;
    },
  });
}

export function useUnit(id: string | undefined) {
  return useQuery({
    queryKey: ["units", "detail", id],
    queryFn: async () => {
      const response = await apiClient.get<Unit>(`/units/${id}`);
      return response.data;
    },
    enabled: Boolean(id),
  });
}

export function useCreateUnit() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateUnitRequest) => {
      const response = await apiClient.post<Unit>("/units", request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["units"] }),
  });
}

export function useUpdateUnit(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: UpdateUnitRequest) => {
      const response = await apiClient.put<Unit>(`/units/${id}`, request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["units"] }),
  });
}

export function useDeleteUnit() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/units/${id}`);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["units"] }),
  });
}
