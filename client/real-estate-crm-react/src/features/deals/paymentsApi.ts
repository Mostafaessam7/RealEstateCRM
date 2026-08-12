import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import type { CheckoutSession, CreateCheckoutRequest, Payment } from "../../types/payment";

export function useDealPayments(dealId: string | null) {
  return useQuery({
    queryKey: ["deals", dealId, "payments"],
    queryFn: async () => {
      const response = await apiClient.get<Payment[]>(`/deals/${dealId}/payments`);
      return response.data;
    },
    enabled: !!dealId,
  });
}

export function useCreateCheckout() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ dealId, request }: { dealId: string; request?: CreateCheckoutRequest }) => {
      const response = await apiClient.post<CheckoutSession>(`/deals/${dealId}/payments/checkout`, request ?? {});
      return response.data;
    },
    onSuccess: (_data, variables) => queryClient.invalidateQueries({ queryKey: ["deals", variables.dealId, "payments"] }),
  });
}
