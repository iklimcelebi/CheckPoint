import { Link } from 'react-router'
import { useDocumentTitle } from '../../lib/useDocumentTitle'

export function RegisterPage() {
  useDocumentTitle('Kayıt ol')

  return (
    <div className="space-y-4">
      <h1 className="text-xl font-semibold text-slate-900">Kayıt ol</h1>
      <p className="text-sm text-slate-600">Kayıt formu yapım aşamasında.</p>
      <p className="text-sm text-slate-600">
        Zaten hesabınız var mı?{' '}
        <Link to="/giris" className="font-medium text-blue-700 hover:underline">
          Giriş yapın
        </Link>
      </p>
    </div>
  )
}
