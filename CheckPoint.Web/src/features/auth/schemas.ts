import { z } from 'zod'

const PASSWORD_MAX_BYTES = 72
const utf8 = new TextEncoder()

const email = z
  .string()
  .trim()
  .min(1, { error: 'E-posta adresi zorunludur.' })
  .max(256, { error: 'E-posta adresi en fazla 256 karakter olabilir.' })
  .pipe(z.email({ error: 'Geçerli bir e-posta adresi girin.' }))

export const loginSchema = z.object({
  email,
  password: z.string().min(1, { error: 'Parola zorunludur.' }),
})

export type LoginForm = z.infer<typeof loginSchema>

export const registerSchema = z
  .object({
    username: z
      .string()
      .min(1, { error: 'Kullanıcı adı zorunludur.' })
      .refine((value) => value.trim() === value, {
        error: 'Kullanıcı adının başında veya sonunda boşluk olamaz.',
      })
      .refine((value) => value.length >= 3 && value.length <= 50, {
        error: 'Kullanıcı adı 3 ile 50 karakter arasında olmalıdır.',
      }),
    email,
    password: z
      .string()
      .min(1, { error: 'Parola zorunludur.' })
      .refine((value) => value.length >= 8 && value.length <= 64, {
        error: 'Parola 8 ile 64 karakter arasında olmalıdır.',
      })
      .refine((value) => /\p{L}/u.test(value) && /\p{Nd}/u.test(value), {
        error: 'Parola en az bir harf ve bir rakam içermelidir.',
      })
      .refine((value) => utf8.encode(value).length <= PASSWORD_MAX_BYTES, {
        error:
          'Parola çok uzun: ğ, ü, ş gibi Türkçe karakterler 2 bayt sayıldığından parola en fazla 72 bayt olabilir.',
      }),
    confirmPassword: z.string().min(1, { error: 'Parolayı tekrar girin.' }),
  })
  .refine((value) => value.password === value.confirmPassword, {
    path: ['confirmPassword'],
    error: 'Parolalar eşleşmiyor.',
  })

export type RegisterForm = z.infer<typeof registerSchema>
