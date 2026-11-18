import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router'
import { Page } from '../../components/Page'
import { Pagination } from '../../components/ui/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/StatusViews'
import { parsePositiveInt } from '../../lib/params'
import { fetchFavorites, RESOURCE_PAGE_SIZE, resourceKeys } from './api'
import { ResourceCard } from './ResourceCard'

export function FavoritesPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const page = parsePositiveInt(searchParams.get('sayfa')) ?? 1
  const goToPage = (next: number) => setSearchParams(next > 1 ? { sayfa: String(next) } : {})

  const query = useQuery({
    queryKey: resourceKeys.favorites(page),
    queryFn: ({ signal }) => fetchFavorites(page, signal),
    placeholderData: keepPreviousData,
  })

  return (
    <Page title="Favoriler" description="Favorilere eklediğiniz kaynaklar.">
      {query.isPending ? (
        <LoadingState label="Favorileriniz yükleniyor…" />
      ) : query.error ? (
        <ErrorState error={query.error} onRetry={() => void query.refetch()} />
      ) : query.data.items.length === 0 ? (
        page > 1 ? (
          <EmptyState title="Bu sayfada kayıt yok.">
            <button
              type="button"
              onClick={() => goToPage(1)}
              className="font-medium text-accent hover:underline"
            >
              İlk sayfaya dön
            </button>
          </EmptyState>
        ) : (
          <EmptyState title="Henüz favori kaynağınız yok.">
            Kaynak kartlarındaki yıldıza basarak favorilere ekleyebilirsiniz.{' '}
            <Link to="/kaynaklar" className="font-medium text-accent hover:underline">
              Kaynakları keşfet
            </Link>
          </EmptyState>
        )
      ) : (
        <div className="space-y-4">
          <ul className="space-y-3">
            {query.data.items.map((resource) => (
              <li key={resource.id}>
                <ResourceCard resource={resource} />
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
