import { z } from 'zod'
import { selectedId, trimmedText } from '../../lib/validation'

export const courseEditSchema = z.object({
  name: trimmedText('Ders adı', 2, 200),
  code: trimmedText('Ders kodu', 1, 20).transform((value) => value.toLocaleUpperCase('en-US')),
})

export const courseCreateSchema = courseEditSchema.extend({
  universityId: selectedId('Üniversite seçin.'),
})

export type CourseEditFormInput = z.input<typeof courseEditSchema>
export type CourseEditForm = z.output<typeof courseEditSchema>
export type CourseCreateFormInput = z.input<typeof courseCreateSchema>
export type CourseCreateForm = z.output<typeof courseCreateSchema>
