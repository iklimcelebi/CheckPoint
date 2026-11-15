import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import { ApiError, errorMessage } from '../api/errors'

export function applyApiError<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  fields: readonly Path<T>[],
): string | undefined {
  if (!(error instanceof ApiError)) return errorMessage(error)

  const unmatched: string[] = []
  for (const [key, messages] of Object.entries(error.fieldErrors)) {
    const normalizedKey = key.replace(/^\$\.?/, '').toLowerCase()
    const field = fields.find((candidate) => candidate.toLowerCase() === normalizedKey)
    const message = messages[0]
    if (!message) continue
    if (field) setError(field, { type: 'server', message })
    else unmatched.push(message)
  }

  if (Object.keys(error.fieldErrors).length === 0) return error.message
  if (unmatched.length > 0) return unmatched.join(' ')
  return error.detail
}
