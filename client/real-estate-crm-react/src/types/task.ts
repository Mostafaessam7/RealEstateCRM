export const TaskItemStatus = {
  Pending: "Pending",
  Completed: "Completed",
  Cancelled: "Cancelled",
} as const;
export type TaskItemStatus = (typeof TaskItemStatus)[keyof typeof TaskItemStatus];

export const TaskPriority = {
  Low: "Low",
  Medium: "Medium",
  High: "High",
  Urgent: "Urgent",
} as const;
export type TaskPriority = (typeof TaskPriority)[keyof typeof TaskPriority];

export interface TaskItem {
  id: string;
  title: string;
  description: string | null;
  assignedToUserId: string;
  leadId: string | null;
  dealId: string | null;
  dueAt: string | null;
  priority: TaskPriority;
  status: TaskItemStatus;
  reminderAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTaskItemRequest {
  title: string;
  description?: string | null;
  assignedToUserId: string;
  leadId?: string | null;
  dealId?: string | null;
  dueAt?: string | null;
  priority: TaskPriority;
  reminderAt?: string | null;
}

export interface UpdateTaskItemRequest {
  title: string;
  description?: string | null;
  leadId?: string | null;
  dealId?: string | null;
  dueAt?: string | null;
  priority: TaskPriority;
  reminderAt?: string | null;
}

export interface TaskItemListQuery {
  page?: number;
  pageSize?: number;
  status?: TaskItemStatus;
  assignedToUserId?: string;
  leadId?: string;
  dealId?: string;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}

export interface AssignTaskItemRequest {
  assignedToUserId: string;
}
