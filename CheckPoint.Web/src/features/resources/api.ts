import { apiDownload, apiRequest } from '../../api/client'
import type { PagedResponse, Resource, ResourceType } from '../../api/types'

export const RESOURCE_PAGE_SIZE = 10

export interface ResourceFilters {
  universityId?: number
  courseId?: number
  type?: ResourceType
  q?: string
  page: number
}

export const resourceKeys = {
  all: ['resources'] as const,
  list: (filters: ResourceFilters) => ['resources', 'list', filters] as const,
  detail: (id: number) => ['resources', 'detail', id] as const,
  mine: (page: number) => ['resources', 'mine', page] as const,
}

export function fetchResources(filters: ResourceFilters, signal?: AbortSignal) {
  return apiRequest<PagedResponse<Resource>>('/api/resources', {
    query: { ...filters, pageSize: RESOURCE_PAGE_SIZE },
    signal,
  })
}

export function fetchMyResources(page: number, signal?: AbortSignal) {
  return apiRequest<PagedResponse<Resource>>('/api/users/me/resources', {
    query: { page, pageSize: RESOURCE_PAGE_SIZE },
    signal,
  })
}

export function fetchResource(id: number, signal?: AbortSignal) {
  return apiRequest<Resource>(`/api/resources/${id}`, { signal })
}

export function downloadResource(id: number) {
  return apiDownload(`/api/resources/${id}/download`)
}

export function deleteResource(id: number) {
  return apiRequest<void>(`/api/resources/${id}`, { method: 'DELETE' })
}
