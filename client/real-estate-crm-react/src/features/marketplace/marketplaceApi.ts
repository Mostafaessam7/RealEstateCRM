import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import type { PagedResult } from "../../types/common";
import type { PublicUnit, PublicUnitListQuery } from "../../types/marketplace";

export function usePublicUnits(query: PublicUnitListQuery) {
  return useQuery({
    queryKey: ["marketplace", "units", query],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<PublicUnit>>("/marketplace/units", { params: query });
      return response.data;
    },
  });
}
