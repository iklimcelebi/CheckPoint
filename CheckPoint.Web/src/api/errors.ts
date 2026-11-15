export interface ProblemDetails {
  type?: string | null
  title?: string | null
  status?: number | null
  detail?: string | null
  instance?: string | null
  errors?: Record<string, string[]>
}

export const NETWORK_ERROR_STATUS = 0

const FALLBACK_MESSAGES: Record<number, string> = {
  [NETWORK_ERROR_STATUS]:
    'Sunucuya ulaşılamadı. İnternet bağlantınızı ve API sunucusunun çalıştığını kontrol edin.',
  400: 'Gönderilen bilgiler geçersiz.',
  401: 'Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.',
  403: 'Bu işlem için yetkiniz yok.',
  404: 'Aradığınız kayıt bulunamadı.',
  409: 'Bu kayıt mevcut verilerle çakışıyor.',
  413: 'Dosya izin verilen boyut sınırını aşıyor.',
  415: 'Bu dosya türü desteklenmiyor.',
  429: 'Çok fazla deneme yaptınız. Lütfen biraz bekleyip tekrar deneyin.',
  503: 'Hizmet şu anda kullanılamıyor. Lütfen daha sonra tekrar deneyin.',
}

export class ApiError extends Error {
  readonly status: number
  readonly title: string | undefined
  readonly detail: string | undefined
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, problem?: ProblemDetails) {
    const detail = problem?.detail ?? undefined
    const fieldErrors = problem?.errors ?? {}
    const firstFieldError = Object.values(fieldErrors).flat()[0]
    super(
      detail ??
        firstFieldError ??
        FALLBACK_MESSAGES[status] ??
        (status >= 500 ? 'Beklenmeyen bir sunucu hatası oluştu.' : 'İstek işlenemedi.'),
    )
    this.name = 'ApiError'
    this.status = status
    this.title = problem?.title ?? undefined
    this.detail = detail
    this.fieldErrors = fieldErrors
  }

  get messages(): string[] {
    const fieldMessages = Object.values(this.fieldErrors).flat()
    const all = this.detail ? [this.detail, ...fieldMessages] : fieldMessages
    return all.length > 0 ? [...new Set(all)] : [this.message]
  }

  fieldError(field: string): string | undefined {
    const key = Object.keys(this.fieldErrors).find(
      (candidate) => candidate.toLowerCase() === field.toLowerCase(),
    )
    return key ? this.fieldErrors[key]?.[0] : undefined
  }
}

export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.message
  return 'Beklenmeyen bir hata oluştu.'
}
