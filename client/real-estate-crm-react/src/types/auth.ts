export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export const Roles = {
  SuperAdmin: "SuperAdmin",
  CompanyAdmin: "CompanyAdmin",
  SalesManager: "SalesManager",
  SalesAgent: "SalesAgent",
} as const;

export type Role = (typeof Roles)[keyof typeof Roles];

export interface AuthUser {
  userId: string;
  fullName: string;
  companyId: string | null;
  roles: Role[];
}
