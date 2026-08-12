import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import type { DashboardSummary } from "../../types/company";

export function useDashboardSummary() {
  return useQuery({
    queryKey: ["dashboard", "summary"],
    queryFn: async () => {
      const response = await apiClient.get<DashboardSummary>("/dashboard/summary");
      return response.data;
    },
  });
}
