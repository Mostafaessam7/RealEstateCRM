import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import type { PagedResult } from "../../types/common";
import type { CreateProjectRequest, Project, ProjectListQuery, UpdateProjectRequest } from "../../types/project";

export function useProjects(query: ProjectListQuery) {
  return useQuery({
    queryKey: ["projects", query],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<Project>>("/projects", { params: query });
      return response.data;
    },
  });
}

/** Unfiltered list, used to populate "which project" selects elsewhere (e.g. Units form). */
export function useAllProjects() {
  return useQuery({
    queryKey: ["projects", "all"],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<Project>>("/projects", { params: { page: 1, pageSize: 100 } });
      return response.data.items;
    },
  });
}

export function useCreateProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateProjectRequest) => {
      const response = await apiClient.post<Project>("/projects", request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["projects"] }),
  });
}

export function useUpdateProject(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: UpdateProjectRequest) => {
      const response = await apiClient.put<Project>(`/projects/${id}`, request);
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["projects"] }),
  });
}

export function useDeleteProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/projects/${id}`);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["projects"] }),
  });
}
