import { apiRequest } from '../../api/client'
import type {
  ChangePasswordRequest,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  User,
  UserStats,
} from '../../api/types'

export function login(body: LoginRequest): Promise<LoginResponse> {
  return apiRequest<LoginResponse>('/api/auth/login', { method: 'POST', json: body })
}

export function register(body: RegisterRequest): Promise<User> {
  return apiRequest<User>('/api/auth/register', { method: 'POST', json: body })
}

export function fetchCurrentUser(signal?: AbortSignal): Promise<User> {
  return apiRequest<User>('/api/users/me', { signal })
}

export function changePassword(body: ChangePasswordRequest): Promise<void> {
  return apiRequest<void>('/api/users/me/password', { method: 'PUT', json: body })
}

export function fetchMyStats(signal?: AbortSignal): Promise<UserStats> {
  return apiRequest<UserStats>('/api/users/me/stats', { signal })
}
