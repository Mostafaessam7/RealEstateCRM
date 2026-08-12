import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import type {
  AgentPerformance,
  CommissionReport,
  ConversionReport,
  InventoryReport,
  LeadsReport,
  SalesReport,
} from "../../types/report";

function useReport<T>(key: string, path: string) {
  return useQuery({
    queryKey: ["reports", key],
    queryFn: async () => {
      const response = await apiClient.get<T>(path);
      return response.data;
    },
  });
}

export const useLeadsReport = () => useReport<LeadsReport>("leads", "/reports/leads");
export const useSalesReport = () => useReport<SalesReport>("sales", "/reports/sales");
export const useConversionReport = () => useReport<ConversionReport>("conversion", "/reports/conversion");
export const useAgentPerformanceReport = () =>
  useReport<AgentPerformance[]>("agent-performance", "/reports/agent-performance");
export const useCommissionReport = () => useReport<CommissionReport>("commissions", "/reports/commissions");
export const useInventoryReport = () => useReport<InventoryReport>("inventory", "/reports/inventory");
