import { z } from 'zod'
import { trimmedText } from '../../lib/validation'

export const universitySchema = z.object({
  name: trimmedText('Üniversite adı', 2, 200),
  city: trimmedText('Şehir', 2, 100),
})

export type UniversityForm = z.infer<typeof universitySchema>
