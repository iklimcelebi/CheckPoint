import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router'
import { errorMessage } from '../../api/errors'
import type { AdminUser } from '../../api/types'
import { Page } from '../../components/Page'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { ConfirmDialog } from '../../components/ui/ConfirmDialog'
import { Pagination } from '../../components/ui/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/StatusViews'
import { TextField } from '../../components/ui/TextField'
import { formatDateTime } from '../../lib/format'
import { useSearchState } from '../../lib/useSearchState'
import { useAuth } from '../auth/authContext'
import { fetchCourses } from '../courses/api'
import { fetchResources } from '../resources/api'
import { fetchUniversities } from '../universities/api'
import { ADMIN_PAGE_SIZE, deleteUser, fetchUsers } from './api'

interface Summary {
  key: string
  to?: string
  title: string
  load: (signal: AbortSignal) => Promise<number>
}

const SUMMARIES: Summary[] = [
  {
    key: 'universities',
    to: '/universiteler',
    title: 'Üniversiteler',
    load: async (signal) => (await fetchUniversities({ pageSize: 1 }, signal)).totalCount,
  },
  {
    key: 'courses',
    to: '/dersler',
    title: 'Dersler',
    load: async (signal) => (await fetchCourses({ pageSize: 1 }, signal)).totalCount,
  },
  {
    key: 'resources',
    to: '/kaynaklar',
    title: 'Kaynaklar',
    load: async (signal) => (await fetchResources({ page: 1 }, signal)).totalCount,
  },
  {
    key: 'users',
    title: 'Kullanıcılar',
    load: async (signal) => (await fetchUsers({ pageSize: 1 }, signal)).totalCount,
  },
]

const ROLE_LABELS: Record<string, string> = { Admin: 'Yönetici', User: 'Kullanıcı' }

function SummaryCard({ summary }: { summary: Summary }) {
  const count = useQuery({
    queryKey: ['admin', 'count', summary.key],
    queryFn: ({ signal }) => summary.load(signal),
  })
  const content = (
    <>
      <p className="text-sm font-medium text-slate-600">{summary.title}</p>
      <p className="mt-1 text-3xl font-semibold text-slate-900">
        {count.data ?? (count.isPending ? '…' : '—')}
      </p>
    </>
  )
  const className = 'block rounded-lg border border-slate-200 bg-white p-5 shadow-sm'

  return summary.to ? (
    <Link
      to={summary.to}
      className={`${className} transition-colors hover:border-tangerine hover:bg-peach/30`}
    >
      {content}
    </Link>
  ) : (
    <div className={className}>{content}</div>
  )
}

function useRefreshAll() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries()
}

const tableClass = 'w-full text-left text-sm'
const headClass = 'bg-slate-50 text-slate-600'
const cellClass = 'px-4 py-3'

function UsersPanel() {
  const { user: currentUser } = useAuth()
  const { searchInput, onSearchChange, q, page, setPage } = useSearchState()
  const [deleting, setDeleting] = useState<AdminUser | null>(null)
  const [notice, setNotice] = useState<string>()
  const refreshAll = useRefreshAll()

  const list = useQuery({
    queryKey: ['admin', 'users', q, page],
    queryFn: ({ signal }) =>
      fetchUsers({ q: q || undefined, page, pageSize: ADMIN_PAGE_SIZE }, signal),
    placeholderData: keepPreviousData,
  })

  const deletion = useMutation({
    mutationFn: (user: AdminUser) => deleteUser(user.id),
    onSuccess: async (_, user) => {
      setDeleting(null)
      setNotice(`"${user.username}" hesabı silindi.`)
      if (list.data?.items.length === 1 && page > 1) setPage(page - 1)
      await refreshAll()
    },
  })

  return (
    <div className="space-y-4">
      {notice && <Alert tone="success">{notice}</Alert>}
      <TextField
        label="Kullanıcı ara"
        type="search"
        placeholder="Kullanıcı adı veya e-posta"
        maxLength={100}
        className="max-w-md"
        value={searchInput}
        onChange={(event) => onSearchChange(event.target.value)}
      />
      {list.isPending ? (
        <LoadingState label="Kullanıcılar yükleniyor…" />
      ) : list.error ? (
        <ErrorState error={list.error} onRetry={() => void list.refetch()} />
      ) : list.data.items.length === 0 ? (
        <EmptyState title="Aramanıza uyan kullanıcı bulunamadı." />
      ) : (
        <div className="space-y-4">
          <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white">
            <table className={tableClass}>
              <thead className={headClass}>
                <tr>
                  <th scope="col" className={`${cellClass} font-medium`}>
                    Kullanıcı adı
                  </th>
                  <th scope="col" className={`${cellClass} font-medium`}>
                    E-posta
                  </th>
                  <th scope="col" className={`${cellClass} font-medium`}>
                    Rol
                  </th>
                  <th scope="col" className={`${cellClass} font-medium`}>
                    Kaynak
                  </th>
                  <th scope="col" className={`${cellClass} font-medium`}>
                    Kayıt tarihi
                  </th>
                  <th scope="col" className={`${cellClass} text-right font-medium`}>
                    İşlemler
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {list.data.items.map((user) => {
                  const protectedAccount = user.role === 'Admin' || user.id === currentUser?.id
                  return (
                    <tr key={user.id}>
                      <td className={`${cellClass} font-medium text-slate-900`}>{user.username}</td>
                      <td className={`${cellClass} text-slate-700`}>{user.email}</td>
                      <td className={`${cellClass} text-slate-700`}>
                        {ROLE_LABELS[user.role] ?? user.role}
                      </td>
                      <td className={`${cellClass} text-slate-700`}>{user.resourceCount}</td>
                      <td className={`${cellClass} whitespace-nowrap text-slate-700`}>
                        {formatDateTime(user.createdAt)}
                      </td>
                      <td className={`${cellClass} text-right`}>
                        {protectedAccount ? (
                          <span className="text-xs text-slate-500">Silinemez</span>
                        ) : (
                          <Button
                            variant="danger"
                            size="small"
                            aria-label={`${user.username} hesabını sil`}
                            onClick={() => {
                              setNotice(undefined)
                              deletion.reset()
                              setDeleting(user)
                            }}
                          >
                            Sil
                          </Button>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
          <Pagination
            page={page}
            pageSize={ADMIN_PAGE_SIZE}
            totalCount={list.data.totalCount}
            onPageChange={setPage}
          />
        </div>
      )}
      <ConfirmDialog
        open={deleting !== null}
        title="Hesabı sil"
        confirmLabel="Hesabı sil"
        busyLabel="Siliniyor…"
        busy={deletion.isPending}
        error={deletion.error ? errorMessage(deletion.error) : undefined}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && deletion.mutate(deleting)}
      >
        <p>
          <strong>{deleting?.username}</strong> hesabı kalıcı olarak silinecek.
          {deleting && deleting.resourceCount > 0 && (
            <> Kullanıcının yüklediği {deleting.resourceCount} kaynak ve dosyaları da silinir.</>
          )}{' '}
          Bu işlem geri alınamaz.
        </p>
      </ConfirmDialog>
    </div>
  )
}

export function AdminPage() {
  return (
    <Page title="Yönetim" description="Kullanıcı hesaplarını yönetin.">
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {SUMMARIES.map((summary) => (
          <SummaryCard key={summary.key} summary={summary} />
        ))}
      </div>
      <section aria-labelledby="users-title" className="space-y-4">
        <h2 id="users-title" className="text-lg font-semibold text-slate-900">
          Kullanıcılar
        </h2>
        <UsersPanel />
      </section>
    </Page>
  )
}
