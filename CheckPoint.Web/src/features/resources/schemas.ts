import { z } from 'zod'
import { validateFile } from '../../lib/fileRules'
import { RESOURCE_TYPES } from '../../lib/resourceTypes'
import { selectedId } from '../../lib/validation'

const title = z
  .string()
  .trim()
  .min(1, { error: 'Başlık zorunludur.' })
  .max(200, { error: 'Başlık en fazla 200 karakter olabilir.' })

const description = z
  .string()
  .trim()
  .max(2000, { error: 'Açıklama en fazla 2000 karakter olabilir.' })

const type = z.enum(RESOURCE_TYPES, { error: 'Kaynak türünü seçin.' })

export const resourceEditSchema = z.object({ title, description, type })

export type ResourceEditForm = z.infer<typeof resourceEditSchema>

export const resourceUploadSchema = resourceEditSchema.extend({
  universityId: selectedId('Üniversite seçin.'),
  courseId: selectedId('Ders seçin.'),
  file: z.custom<FileList>().superRefine((files, context) => {
    const message = validateFile(files?.[0])
    if (message) context.addIssue({ code: 'custom', message })
  }),
})

export type ResourceUploadForm = z.infer<typeof resourceUploadSchema>
