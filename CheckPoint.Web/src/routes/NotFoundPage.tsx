import { Link } from 'react-router'
import { useDocumentTitle } from '../lib/useDocumentTitle'

export function NotFoundPage() {
  useDocumentTitle('Sayfa bulunamadı')

  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-4 px-4 text-center">
      <p className="text-5xl font-bold text-blue-700">404</p>
      <h1 className="text-xl font-semibold text-slate-900">Sayfa bulunamadı</h1>
      <p className="text-sm text-slate-600">
        Aradığınız adres mevcut değil veya taşınmış olabilir.
      </p>
      <Link to="/kaynaklar" className="text-sm font-medium text-blue-700 hover:underline">
        Ana sayfaya dön
      </Link>
    </main>
  )
}
