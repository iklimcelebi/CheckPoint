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
  LectureNote: 'bg-peach text-slate-900 ring-tangerine/50',
  Presentation: 'bg-blush text-slate-900 ring-blush',
  StudyQuestion: 'bg-white text-accent ring-tangerine',
  PastExamQuestion: 'bg-tangerine text-slate-900 ring-tangerine',
}

export function isResourceType(value: string | null | undefined): value is ResourceType {
  return RESOURCE_TYPES.includes(value as ResourceType)
}
