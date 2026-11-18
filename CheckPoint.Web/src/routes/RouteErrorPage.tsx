import { isRouteErrorResponse, Link, useRouteError } from 'react-router'
import { NotFoundPage } from './NotFoundPage'

export function RouteErrorPage() {
  const error = useRouteError()

  if (isRouteErrorResponse(error) && error.status === 404) return <NotFoundPage />

  return (
    <main
      role="alert"
      className="flex min-h-screen flex-col items-center justify-center gap-4 px-4 text-center"
    >
      <h1 className="text-xl font-semibold text-slate-900">Beklenmeyen bir hata oluştu</h1>
      <p className="text-sm text-slate-600">
        Sayfa görüntülenirken bir sorun çıktı. Sayfayı yenilemeyi deneyin.
      </p>
      <div className="flex gap-4">
        <button
          type="button"
          onClick={() => window.location.reload()}
          className="rounded-md bg-tangerine px-4 py-2 text-sm font-medium text-slate-900 hover:bg-tangerine-strong"
        >
          Sayfayı yenile
        </button>
        <Link to="/" className="px-4 py-2 text-sm font-medium text-accent hover:underline">
          Ana sayfaya dön
        </Link>
      </div>
    </main>
  )
}
