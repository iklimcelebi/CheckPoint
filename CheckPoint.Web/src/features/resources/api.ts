import { apiDownload, apiRequest, apiUpload } from '../../api/client'
import type { PagedResponse, Resource, ResourceType, UpdateResourceRequest } from '../../api/types'

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
  favorites: (page: number) => ['resources', 'favorites', page] as const,
  latest: ['resources', 'latest'] as const,
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

export interface NewResource {
  title: string
  description?: string
  universityId: number
  courseId: number
  type: ResourceType
  file: File
}

export function createResource(resource: NewResource, onProgress?: (percent: number) => void) {
  const form = new FormData()
  form.set('Title', resource.title)
  if (resource.description) form.set('Description', resource.description)
  form.set('UniversityId', String(resource.universityId))
  form.set('CourseId', String(resource.courseId))
  form.set('Type', resource.type)
  form.set('File', resource.file, resource.file.name)
  return apiUpload<Resource>('/api/resources', form, onProgress)
}

export function updateResource(id: number, body: UpdateResourceRequest) {
  return apiRequest<void>(`/api/resources/${id}`, { method: 'PUT', json: body })
}

export function fetchFavorites(page: number, signal?: AbortSignal) {
  return apiRequest<PagedResponse<Resource>>('/api/users/me/favorites', {
    query: { page, pageSize: RESOURCE_PAGE_SIZE },
    signal,
  })
}

export function setFavorite(id: number, favorite: boolean) {
  return apiRequest<void>(`/api/resources/${id}/favorite`, { method: favorite ? 'PUT' : 'DELETE' })
}

export function recordView(id: number) {
  return apiRequest<void>(`/api/resources/${id}/view`, { method: 'POST' })
}
