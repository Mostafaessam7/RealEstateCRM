export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface PagedQuery {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}

export interface ProblemDetails {
  status?: number;
  title?: string;
}
