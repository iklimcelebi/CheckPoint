import type { ReactNode } from 'react'

type Tone = 'error' | 'warning' | 'success' | 'info'

const TONE_CLASSES: Record<Tone, string> = {
  error: 'border-red-200 bg-red-50 text-red-800',
  warning: 'border-amber-200 bg-amber-50 text-amber-800',
  success: 'border-green-200 bg-green-50 text-green-800',
  info: 'border-peach bg-peach/40 text-accent-strong',
}

export function Alert({ tone, children }: { tone: Tone; children: ReactNode }) {
  return (
    <div
      role={tone === 'error' ? 'alert' : 'status'}
      className={`rounded-md border p-3 text-sm ${TONE_CLASSES[tone]}`}
    >
      {children}
    </div>
  )
}
