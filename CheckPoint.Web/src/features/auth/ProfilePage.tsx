import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import type { UserStats } from '../../api/types'
import { Page } from '../../components/Page'
import { buttonClasses } from '../../components/ui/buttonStyles'
import { ErrorState, LoadingState } from '../../components/ui/StatusViews'
import { RESOURCE_TYPE_LABELS } from '../../lib/resourceTypes'
import { ResourceTypeBadge } from '../resources/ResourceTypeBadge'
import { fetchCurrentUser, fetchMyStats } from './api'

function StatTile({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <dt className="text-sm font-medium text-slate-600">{label}</dt>
      <dd className="mt-1 text-3xl font-semibold text-slate-900">{value}</dd>
    </div>
  )
}

function Stats({ stats }: { stats: UserStats }) {
  const largest = Math.max(1, ...stats.resourcesByType.map((group) => group.count))

  return (
    <div className="space-y-6">
      <dl className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatTile label="Yüklenen kaynak" value={stats.resourceCount} />
        <StatTile label="Toplam görüntülenme" value={stats.totalViews} />
        <StatTile label="Favorilere eklenme" value={stats.favoritesReceived} />
        <StatTile label="Favorilerim" value={stats.favoriteCount} />
      </dl>

      <div className="grid gap-4 lg:grid-cols-2">
        <section
          aria-labelledby="types-title"
          className="space-y-3 rounded-lg border border-slate-200 bg-white p-5 shadow-sm"
        >
          <h2 id="types-title" className="font-semibold text-slate-900">
            Türe göre kaynaklarım
          </h2>
          {stats.resourcesByType.length === 0 ? (
            <p className="text-sm text-slate-600">
              Henüz kaynak yüklemediniz.{' '}
              <Link to="/yukle" className="font-medium text-accent hover:underline">
                İlk kaynağınızı yükleyin
              </Link>
            </p>
          ) : (
            <ul className="space-y-3">
              {stats.resourcesByType.map((group) => (
                <li
                  key={group.type}
                  aria-label={`${RESOURCE_TYPE_LABELS[group.type]}: ${group.count}`}
                  className="space-y-1"
                >
                  <div className="flex items-center justify-between text-sm">
                    <ResourceTypeBadge type={group.type} />
                    <span className="font-medium text-slate-900">{group.count}</span>
                  </div>
                  <div className="h-2 rounded-full bg-peach/40" aria-hidden="true">
                    <div
                      className="h-2 rounded-full bg-tangerine"
                      style={{ width: `${(group.count / largest) * 100}%` }}
                    />
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section
          aria-labelledby="top-title"
          className="space-y-3 rounded-lg border border-slate-200 bg-white p-5 shadow-sm"
        >
          <h2 id="top-title" className="font-semibold text-slate-900">
            En çok görüntülenen kaynaklarım
          </h2>
          {stats.topResources.length === 0 ? (
            <p className="text-sm text-slate-600">Kaynaklarınız henüz görüntülenmedi.</p>
          ) : (
            <ol className="space-y-2">
              {stats.topResources.map((resource, index) => (
                <li key={resource.id} className="flex items-center gap-3 text-sm">
                  <span
                    aria-hidden="true"
                    className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-peach font-semibold text-accent-strong"
                  >
                    {index + 1}
                  </span>
                  <Link
                    to={`/kaynaklar/${resource.id}`}
                    className="min-w-0 flex-1 truncate font-medium text-slate-900 hover:text-accent hover:underline"
                  >
                    {resource.title}
                  </Link>
                  <span className="shrink-0 text-slate-600">{resource.viewCount} görüntülenme</span>
                </li>
              ))}
            </ol>
          )}
          <Link
            to="/benim-kaynaklarim"
            className="inline-block text-sm font-medium text-accent hover:underline"
          >
            Tüm kaynaklarımı gör
          </Link>
        </section>
      </div>
    </div>
  )
}

export function ProfilePage() {
  const user = useQuery({
    queryKey: ['users', 'me'],
    queryFn: ({ signal }) => fetchCurrentUser(signal),
  })
  const stats = useQuery({
    queryKey: ['users', 'me', 'stats'],
    queryFn: ({ signal }) => fetchMyStats(signal),
  })

  return (
    <Page title="Profil" description="Hesap bilgileriniz ve kaynak istatistikleriniz.">
      {user.isPending ? (
        <LoadingState label="Profil yükleniyor…" />
      ) : user.error ? (
        <ErrorState error={user.error} onRetry={() => void user.refetch()} />
      ) : (
        <div className="flex flex-wrap items-center justify-between gap-4 rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <div className="flex min-w-0 items-center gap-4">
            <span
              aria-hidden="true"
              className="flex h-14 w-14 shrink-0 items-center justify-center rounded-full bg-tangerine text-2xl font-semibold text-slate-900"
            >
              {user.data.username.charAt(0).toLocaleUpperCase('tr-TR')}
            </span>
            <dl className="min-w-0">
              <dt className="sr-only">Kullanıcı adı</dt>
              <dd className="truncate text-lg font-semibold text-slate-900">
                {user.data.username}
              </dd>
              <dt className="sr-only">E-posta</dt>
              <dd className="truncate text-sm text-slate-600">{user.data.email}</dd>
            </dl>
          </div>
          <Link to="/profil/parola" className={buttonClasses('secondary')}>
            Parolayı değiştir
          </Link>
        </div>
      )}

      {stats.isPending ? (
        <LoadingState label="İstatistikler yükleniyor…" />
      ) : stats.error ? (
        <ErrorState error={stats.error} onRetry={() => void stats.refetch()} />
      ) : (
        <Stats stats={stats.data} />
      )}
    </Page>
  )
}
