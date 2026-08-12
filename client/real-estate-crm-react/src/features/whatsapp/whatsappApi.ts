import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import type {
  CreateWhatsAppTemplateRequest,
  SendWhatsAppRequest,
  UpdateWhatsAppTemplateRequest,
  WhatsAppMessage,
  WhatsAppTemplate,
} from "../../types/whatsapp";

export function useWhatsAppTemplates() {
  return useQuery({
    queryKey: ["whatsapp", "templates"],
    queryFn: async () => {
      const response = await apiClient.get<WhatsAppTemplate[]>("/whatsapp/templates");
      return response.data;
    },
  });
}

export function useCreateWhatsAppTemplate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateWhatsAppTemplateRequest) => {
      const response = await apiClient.post<WhatsAppTemplate>("/whatsapp/templates", request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["whatsapp", "templates"] }),
  });
}

export function useUpdateWhatsAppTemplate(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: UpdateWhatsAppTemplateRequest) => {
      const response = await apiClient.put<WhatsAppTemplate>(`/whatsapp/templates/${id}`, request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["whatsapp", "templates"] }),
  });
}

export function useLeadWhatsAppMessages(leadId: string | undefined) {
  return useQuery({
    queryKey: ["whatsapp", "messages", leadId],
    queryFn: async () => {
      const response = await apiClient.get<WhatsAppMessage[]>(`/whatsapp/leads/${leadId}/messages`);
      return response.data;
    },
    enabled: !!leadId,
  });
}

export function useSendWhatsApp(leadId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: SendWhatsAppRequest) => {
      const response = await apiClient.post<WhatsAppMessage>(`/whatsapp/leads/${leadId}/send`, request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["whatsapp", "messages", leadId] }),
  });
}
