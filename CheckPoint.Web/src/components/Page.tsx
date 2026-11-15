import type { ReactNode } from 'react'
import { useDocumentTitle } from '../lib/useDocumentTitle'

interface PageProps {
  title: string
  description?: string
  actions?: ReactNode
  children?: ReactNode
}

export function Page({ title, description, actions, children }: PageProps) {
  useDocumentTitle(title)

  return (
    <section className="space-y-6">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-slate-900">{title}</h1>
          {description && <p className="mt-1 text-sm text-slate-600">{description}</p>}
        </div>
        {actions}
      </header>
      {children}
    </section>
  )
}
