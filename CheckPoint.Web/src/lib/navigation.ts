export const RETURN_PARAM = 'donus'

export function safeReturnPath(value: string | null | undefined, fallback = '/kaynaklar'): string {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) {
    return fallback
  }
  return value
}

export function loginPathFor(returnPath: string): string {
  return `/giris?${RETURN_PARAM}=${encodeURIComponent(returnPath)}`
}
