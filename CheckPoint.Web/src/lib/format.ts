const dateTimeFormatter = new Intl.DateTimeFormat('tr-TR', {
  dateStyle: 'long',
  timeStyle: 'short',
  timeZone: 'Europe/Istanbul',
})

const numberFormatter = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 1 })

export function formatDateTime(value: string): string {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : dateTimeFormatter.format(date)
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${numberFormatter.format(bytes / 1024)} KB`
  return `${numberFormatter.format(bytes / (1024 * 1024))} MB`
}
