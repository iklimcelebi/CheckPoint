import { useQuery } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError, errorMessage } from '../../api/errors'
import { Page } from '../../components/Page'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { buttonClasses } from '../../components/ui/buttonStyles'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/StatusViews'
import { formatDateTime, formatFileSize } from '../../lib/format'
import { parsePositiveInt } from '../../lib/params'
import { fetchResource, resourceKeys } from './api'
import { DeleteResourceButton } from './DeleteResourceButton'
import { useCanManage, useDownloadResource } from './hooks'
import { ResourceTypeBadge } from './ResourceTypeBadge'

function NotFound() {
  return (
    <Page title="Kaynak bulunamadı">
      <EmptyState title="Bu kaynak bulunamadı veya silinmiş olabilir.">
        <Link to="/kaynaklar" className="font-medium text-blue-700 hover:underline">
          Kaynaklara dön
        </Link>
      </EmptyState>
    </Page>
  )
}

export function ResourceDetailPage() {
  const resourceId = parsePositiveInt(useParams().id)
  const navigate = useNavigate()
  const download = useDownloadResource()

  const query = useQuery({
    queryKey: resourceKeys.detail(resourceId ?? 0),
    queryFn: ({ signal }) => fetchResource(resourceId ?? 0, signal),
    enabled: resourceId !== undefined,
  })
  const resource = query.data
  const canManage = useCanManage(resource)

  if (resourceId === undefined) return <NotFound />
  if (query.isPending) return <LoadingState label="Kaynak yükleniyor…" />
  if (query.error instanceof ApiError && query.error.status === 404) return <NotFound />
  if (query.error || !resource) {
    return (
      <Page title="Kaynak">
        <ErrorState error={query.error} onRetry={() => void query.refetch()} />
      </Page>
    )
  }

  const details: [string, ReactNode][] = [
    ['Tür', <ResourceTypeBadge key="type" type={resource.type} />],
    ['Üniversite', resource.university.name],
    ['Ders', `${resource.course.code} — ${resource.course.name}`],
    [
      'Açıklama',
      resource.description ? (
        <span className="whitespace-pre-line">{resource.description}</span>
      ) : (
        <span className="text-slate-500">Açıklama eklenmemiş.</span>
      ),
    ],
    ['Dosya', `${resource.originalFileName} (${formatFileSize(resource.fileSize)})`],
    ['Yükleyen', resource.uploadedBy.username],
    [
      'Yüklenme tarihi',
      <time key="date" dateTime={resource.uploadedAt}>
        {formatDateTime(resource.uploadedAt)}
      </time>,
    ],
  ]

  return (
    <Page
      title={resource.title}
      actions={
        <div className="flex flex-wrap gap-2">
          <Button
            onClick={() => download.mutate(resource)}
            loading={download.isPending}
            loadingText="İndiriliyor…"
          >
            İndir
          </Button>
          {canManage && (
            <>
              <Link to={`/kaynaklar/${resource.id}/duzenle`} className={buttonClasses('secondary')}>
                Düzenle
              </Link>
              <DeleteResourceButton
                resource={resource}
                onDeleted={() =>
                  void navigate('/kaynaklar', {
                    replace: true,
                    state: { notice: 'Kaynak silindi.' },
                  })
                }
              />
            </>
          )}
        </div>
      }
    >
      {download.error && <Alert tone="error">{errorMessage(download.error)}</Alert>}
      <dl className="divide-y divide-slate-200 rounded-lg border border-slate-200 bg-white">
        {details.map(([label, value]) => (
          <div key={label} className="grid gap-1 px-4 py-3 sm:grid-cols-4 sm:gap-4">
            <dt className="text-sm font-medium text-slate-600">{label}</dt>
            <dd className="break-words text-sm text-slate-900 sm:col-span-3">{value}</dd>
          </div>
        ))}
      </dl>
      <Link
        to="/kaynaklar"
        className="inline-block text-sm font-medium text-blue-700 hover:underline"
      >
        ← Kaynaklara dön
      </Link>
    </Page>
  )
}
