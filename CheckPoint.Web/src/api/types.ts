import type { components } from './schema'

type Schemas = components['schemas']

export type User = Schemas['UserResponse']
export type LoginRequest = Schemas['LoginRequest']
export type LoginResponse = Schemas['LoginResponse']
export type RegisterRequest = Schemas['RegisterRequest']
export type University = Schemas['UniversityResponse']
export type UniversityRequest = Schemas['UniversityRequest']
export type Course = Schemas['CourseResponse']
export type CourseRequest = Schemas['CourseRequest']
export type CourseUpdateRequest = Schemas['CourseUpdateRequest']
export type Resource = Schemas['ResourceResponse']
export type ResourceType = Schemas['ResourceType']
export type UpdateResourceRequest = Schemas['UpdateResourceRequest']

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export type Role = 'User' | 'Admin'
