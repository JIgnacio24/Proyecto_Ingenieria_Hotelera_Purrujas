export interface AdminUser {
  adminUserId: number;
  fullName: string;
  username: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: AdminUser;
}

export interface LoginPayload {
  username: string;
  password: string;
}

export interface RegisterPayload {
  fullName: string;
  username: string;
  email: string;
  password: string;
  role?: string;
}
