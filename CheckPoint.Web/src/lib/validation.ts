import { z } from 'zod'

export function trimmedText(label: string, min: number, max: number) {
  return z
    .string()
    .trim()
    .min(1, { error: `${label} zorunludur.` })
    .min(min, { error: `${label} ${min} ile ${max} karakter arasında olmalıdır.` })
    .max(max, { error: `${label} ${min} ile ${max} karakter arasında olmalıdır.` })
}

export function selectedId(message: string) {
  return z.number({ error: message }).int({ error: message }).positive({ error: message })
}
