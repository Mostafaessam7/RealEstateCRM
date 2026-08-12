import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import type { PagedResult } from "../../types/common";
import type { CreateTaskItemRequest, TaskItem, TaskItemListQuery } from "../../types/task";

export function useTasks(query: TaskItemListQuery) {
  return useQuery({
    queryKey: ["tasks", query],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<TaskItem>>("/tasks", { params: query });
      return response.data;
    },
  });
}

export function useCreateTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateTaskItemRequest) => {
      const response = await apiClient.post<TaskItem>("/tasks", request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["tasks"] }),
  });
}

function useTaskAction(action: "complete" | "cancel") {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const response = await apiClient.post<TaskItem>(`/tasks/${id}/${action}`);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["tasks"] }),
  });
}

export const useCompleteTask = () => useTaskAction("complete");
export const useCancelTask = () => useTaskAction("cancel");
