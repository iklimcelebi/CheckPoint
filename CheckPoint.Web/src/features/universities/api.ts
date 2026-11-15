import { useQuery } from '@tanstack/react-query'
import { apiRequest } from '../../api/client'
import type { PagedResponse, University, UniversityRequest } from '../../api/types'

export const OPTIONS_PAGE_SIZE = 100

export interface UniversityQuery {
  q?: string
  page?: number
  pageSize?: number
}

export function fetchUniversities(query: UniversityQuery, signal?: AbortSignal) {
  return apiRequest<PagedResponse<University>>('/api/universities', {
    query: { ...query },
    signal,
  })
}

export function useUniversityOptions() {
  return useQuery({
    queryKey: ['universities', 'options'],
    queryFn: ({ signal }) => fetchUniversities({ pageSize: OPTIONS_PAGE_SIZE }, signal),
    select: (data) => data.items,
    staleTime: 5 * 60_000,
  })
}

export function createUniversity(body: UniversityRequest) {
  return apiRequest<University>('/api/universities', { method: 'POST', json: body })
}

export function updateUniversity(id: number, body: UniversityRequest) {
  return apiRequest<void>(`/api/universities/${id}`, { method: 'PUT', json: body })
}

export function deleteUniversity(id: number) {
  return apiRequest<void>(`/api/universities/${id}`, { method: 'DELETE' })
}
