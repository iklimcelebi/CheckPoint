import { useQuery } from '@tanstack/react-query'
import { errorMessage } from '../../api/errors'
import { Page } from '../../components/Page'
import { Alert } from '../../components/ui/Alert'
import { fetchCurrentUser } from './api'

const ROLE_LABELS: Record<string, string> = {
  Admin: 'Yönetici',
  User: 'Kullanıcı',
}

export function ProfilePage() {
  const {
    data: user,
    isPending,
    error,
  } = useQuery({
    queryKey: ['users', 'me'],
    queryFn: ({ signal }) => fetchCurrentUser(signal),
  })

  return (
    <Page title="Profil" description="Hesap bilgileriniz.">
      {isPending && <p className="text-sm text-slate-600">Yükleniyor…</p>}
      {error && <Alert tone="error">{errorMessage(error)}</Alert>}
      {user && (
        <dl className="divide-y divide-slate-200 rounded-lg border border-slate-200 bg-white">
          {[
            ['Kullanıcı adı', user.username],
            ['E-posta', user.email],
            ['Rol', ROLE_LABELS[user.role] ?? user.role],
          ].map(([label, value]) => (
            <div key={label} className="grid gap-1 px-4 py-3 sm:grid-cols-3 sm:gap-4">
              <dt className="text-sm font-medium text-slate-600">{label}</dt>
              <dd className="text-sm text-slate-900 sm:col-span-2">{value}</dd>
            </div>
          ))}
        </dl>
      )}
    </Page>
  )
}
