import { Link } from 'react-router'
import { getSignOutReason } from '../../lib/session'
import { useDocumentTitle } from '../../lib/useDocumentTitle'

export function LoginPage() {
  useDocumentTitle('Giriş yap')
  const expired = getSignOutReason() === 'expired'

  return (
    <div className="space-y-4">
      <h1 className="text-xl font-semibold text-slate-900">Giriş yap</h1>
      {expired && (
        <p role="status" className="rounded-md bg-amber-50 p-3 text-sm text-amber-800">
          Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.
        </p>
      )}
      <p className="text-sm text-slate-600">Giriş formu yapım aşamasında.</p>
      <p className="text-sm text-slate-600">
        Hesabınız yok mu?{' '}
        <Link to="/kayit" className="font-medium text-blue-700 hover:underline">
          Kayıt olun
        </Link>
      </p>
    </div>
  )
}
