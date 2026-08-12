import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import type { Company } from "../../types/company";

export function useCurrentCompany() {
  return useQuery({
    queryKey: ["companies", "current"],
    queryFn: async () => {
      const response = await apiClient.get<Company>("/companies/current");
      return response.data;
    },
  });
}
