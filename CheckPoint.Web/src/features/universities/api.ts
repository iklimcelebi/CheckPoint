import { useQuery } from '@tanstack/react-query'
import { apiRequest } from '../../api/client'
import type { PagedResponse, University } from '../../api/types'

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
