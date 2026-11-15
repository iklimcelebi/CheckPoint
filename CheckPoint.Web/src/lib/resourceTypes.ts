import type { ResourceType } from '../api/types'

export const RESOURCE_TYPES: readonly ResourceType[] = [
  'LectureNote',
  'Presentation',
  'StudyQuestion',
  'PastExamQuestion',
]

export const RESOURCE_TYPE_LABELS: Record<ResourceType, string> = {
  LectureNote: 'Ders Notu',
  Presentation: 'Sunum',
  StudyQuestion: 'Çalışma Sorusu',
  PastExamQuestion: 'Geçmiş Sınav Sorusu',
}

export const RESOURCE_TYPE_BADGE_CLASSES: Record<ResourceType, string> = {
  LectureNote: 'bg-blue-50 text-blue-800 ring-blue-200',
  Presentation: 'bg-violet-50 text-violet-800 ring-violet-200',
  StudyQuestion: 'bg-emerald-50 text-emerald-800 ring-emerald-200',
  PastExamQuestion: 'bg-amber-50 text-amber-800 ring-amber-200',
}

export function isResourceType(value: string | null | undefined): value is ResourceType {
  return RESOURCE_TYPES.includes(value as ResourceType)
}
