import { Page } from './Page'

export function PlaceholderPage({ title, description }: { title: string; description: string }) {
  return (
    <Page title={title} description={description}>
      <p className="rounded-lg border border-dashed border-slate-300 bg-white p-6 text-sm text-slate-600">
        Bu sayfa yapım aşamasında.
      </p>
    </Page>
  )
}
