import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router'
import { Page } from '../../components/Page'
import { buttonClasses } from '../../components/ui/buttonStyles'
import { Pagination } from '../../components/ui/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/StatusViews'
import { parsePositiveInt } from '../../lib/params'
import { fetchMyResources, RESOURCE_PAGE_SIZE, resourceKeys } from './api'
import { DeleteResourceButton } from './DeleteResourceButton'
import { ResourceCard } from './ResourceCard'

export function MyResourcesPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const page = parsePositiveInt(searchParams.get('sayfa')) ?? 1
  const goToPage = (next: number) => setSearchParams(next > 1 ? { sayfa: String(next) } : {})

  const query = useQuery({
    queryKey: resourceKeys.mine(page),
    queryFn: ({ signal }) => fetchMyResources(page, signal),
    placeholderData: keepPreviousData,
  })

  return (
    <Page
      title="Benim Kaynaklarım"
      description="Yüklediğiniz kaynaklar."
      actions={
        <Link to="/yukle" className={buttonClasses('primary')}>
          Kaynak yükle
        </Link>
      }
    >
      {query.isPending ? (
        <LoadingState label="Kaynaklarınız yükleniyor…" />
      ) : query.error ? (
        <ErrorState error={query.error} onRetry={() => void query.refetch()} />
      ) : query.data.items.length === 0 ? (
        page > 1 ? (
          <EmptyState title="Bu sayfada kayıt yok.">
            <button
              type="button"
              onClick={() => goToPage(1)}
              className="font-medium text-blue-700 hover:underline"
            >
              İlk sayfaya dön
            </button>
          </EmptyState>
        ) : (
          <EmptyState title="Henüz kaynak yüklemediniz.">
            <Link to="/yukle" className="font-medium text-blue-700 hover:underline">
              İlk kaynağınızı yükleyin
            </Link>
          </EmptyState>
        )
      ) : (
        <div className="space-y-4">
          <ul className="space-y-3">
            {query.data.items.map((resource) => (
              <li key={resource.id}>
                <ResourceCard
                  resource={resource}
                  actions={
                    <>
                      <Link
                        to={`/kaynaklar/${resource.id}/duzenle`}
                        className={buttonClasses('secondary', 'small')}
                        aria-label={`${resource.title} kaynağını düzenle`}
                      >
                        Düzenle
                      </Link>
                      <DeleteResourceButton
                        resource={resource}
                        size="small"
                        onDeleted={() => {
                          if (query.data.items.length === 1 && page > 1) goToPage(page - 1)
                        }}
                      />
                    </>
                  }
                />
              </li>
            ))}
          </ul>
          <Pagination
            page={page}
            pageSize={RESOURCE_PAGE_SIZE}
            totalCount={query.data.totalCount}
            onPageChange={goToPage}
          />
        </div>
      )}
    </Page>
  )
}
