import { useQuery } from '@tanstack/react-query'
import { apiRequest } from '../../api/client'
import type {
  Course,
  CourseRequest,
  CourseUpdateRequest,
  PagedResponse,
  PopularCourse,
} from '../../api/types'
import { OPTIONS_PAGE_SIZE } from '../universities/api'

export interface CourseQuery {
  universityId?: number
  q?: string
  page?: number
  pageSize?: number
}

export function fetchCourses(query: CourseQuery, signal?: AbortSignal) {
  return apiRequest<PagedResponse<Course>>('/api/courses', { query: { ...query }, signal })
}

export function useCourseOptions(universityId: number | undefined) {
  return useQuery({
    queryKey: ['courses', 'options', universityId],
    queryFn: ({ signal }) => fetchCourses({ universityId, pageSize: OPTIONS_PAGE_SIZE }, signal),
    select: (data) => data.items,
    enabled: universityId !== undefined,
    staleTime: 5 * 60_000,
  })
}

export function createCourse(body: CourseRequest) {
  return apiRequest<Course>('/api/courses', { method: 'POST', json: body })
}

export function updateCourse(id: number, body: CourseUpdateRequest) {
  return apiRequest<void>(`/api/courses/${id}`, { method: 'PUT', json: body })
}

export function usePopularCourses(take = 6) {
  return useQuery({
    queryKey: ['courses', 'popular', take],
    queryFn: ({ signal }) =>
      apiRequest<PopularCourse[]>('/api/courses/popular', { query: { take }, signal }),
  })
}

export function deleteCourse(id: number) {
  return apiRequest<void>(`/api/courses/${id}`, { method: 'DELETE' })
}
