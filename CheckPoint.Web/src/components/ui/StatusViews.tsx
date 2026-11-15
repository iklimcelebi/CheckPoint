import type { ReactNode } from 'react'
import { errorMessage } from '../../api/errors'
import { Alert } from './Alert'
import { Button } from './Button'

export function LoadingState({ label = 'Yükleniyor…' }: { label?: string }) {
  return (
    <div role="status" aria-live="polite" className="space-y-3">
      <span className="sr-only">{label}</span>
      {[0, 1, 2].map((index) => (
        <div key={index} className="h-24 animate-pulse rounded-lg bg-slate-200/70" />
      ))}
    </div>
  )
}

export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="rounded-lg border border-dashed border-slate-300 bg-white p-8 text-center">
      <p className="font-medium text-slate-800">{title}</p>
      {children && <div className="mt-2 text-sm text-slate-600">{children}</div>}
    </div>
  )
}

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  return (
    <div className="space-y-3">
      <Alert tone="error">{errorMessage(error)}</Alert>
      {onRetry && (
        <Button variant="secondary" onClick={onRetry}>
          Tekrar dene
        </Button>
      )}
    </div>
  )
}
