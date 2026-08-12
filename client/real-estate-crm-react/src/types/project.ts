export const ProjectStatus = {
  Planning: "Planning",
  UnderConstruction: "UnderConstruction",
  Ready: "Ready",
  SoldOut: "SoldOut",
  OnHold: "OnHold",
} as const;
export type ProjectStatus = (typeof ProjectStatus)[keyof typeof ProjectStatus];

export interface Project {
  id: string;
  name: string;
  developer: string | null;
  location: string | null;
  description: string | null;
  startingPrice: number | null;
  status: ProjectStatus;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProjectRequest {
  name: string;
  developer?: string | null;
  location?: string | null;
  description?: string | null;
  startingPrice?: number | null;
  status: ProjectStatus;
}

export type UpdateProjectRequest = CreateProjectRequest;

export interface ProjectListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: ProjectStatus;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}

export interface ProjectImage {
  id: string;
  projectId: string;
  url: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  createdAt: string;
}
