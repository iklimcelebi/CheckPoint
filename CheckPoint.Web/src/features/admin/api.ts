import { apiRequest } from '../../api/client'
import type { AdminUser, PagedResponse } from '../../api/types'

export const ADMIN_PAGE_SIZE = 10

export function fetchUsers(
  query: { q?: string; page?: number; pageSize?: number },
  signal?: AbortSignal,
) {
  return apiRequest<PagedResponse<AdminUser>>('/api/users', { query: { ...query }, signal })
}

export function deleteUser(id: number) {
  return apiRequest<void>(`/api/users/${id}`, { method: 'DELETE' })
}
