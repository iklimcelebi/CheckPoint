import { z } from 'zod'
import { isTurkishProvince } from '../../lib/provinces'
import { trimmedText } from '../../lib/validation'

export const universitySchema = z.object({
  name: trimmedText('Üniversite adı', 2, 200),
  city: z.string().refine(isTurkishProvince, { error: 'Geçerli bir il seçin.' }),
})

export type UniversityForm = z.infer<typeof universitySchema>
