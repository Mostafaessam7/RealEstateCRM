import type { Role } from "./auth";

export interface User {
  id: string;
  fullName: string;
  email: string;
  roles: Role[];
  isActive: boolean;
  managerId: string | null;
  avatarUrl: string | null;
  createdAt: string;
}

export interface CreateUserRequest {
  fullName: string;
  email: string;
  password: string;
  role: Role;
  managerId?: string | null;
}

export interface UpdateUserRoleRequest {
  role: Role;
}

export interface UpdateUserActiveRequest {
  isActive: boolean;
}
