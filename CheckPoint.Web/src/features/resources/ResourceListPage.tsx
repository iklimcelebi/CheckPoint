import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { useLocation, useSearchParams } from 'react-router'
import { Page } from '../../components/Page'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { Pagination } from '../../components/ui/Pagination'
import { SelectField } from '../../components/ui/SelectField'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/StatusViews'
import { TextField } from '../../components/ui/TextField'
import { parsePositiveInt } from '../../lib/params'
import { isResourceType, RESOURCE_TYPE_LABELS, RESOURCE_TYPES } from '../../lib/resourceTypes'
import { useCourseOptions } from '../courses/api'
import { useUniversityOptions } from '../universities/api'
import { fetchResources, RESOURCE_PAGE_SIZE, resourceKeys, type ResourceFilters } from './api'
import { ResourceCard } from './ResourceCard'

const SEARCH_DELAY_MS = 300

export function ResourceListPage() {
  const location = useLocation()
  const notice = (location.state as { notice?: string } | null)?.notice
  const [searchParams, setSearchParams] = useSearchParams()

  const universityId = parsePositiveInt(searchParams.get('universite'))
  const courseId = universityId ? parsePositiveInt(searchParams.get('ders')) : undefined
  const typeParam = searchParams.get('tur')
  const type = isResourceType(typeParam) ? typeParam : undefined
  const q = searchParams.get('q') ?? ''
  const page = parsePositiveInt(searchParams.get('sayfa')) ?? 1
  const filters: ResourceFilters = { universityId, courseId, type, q: q || undefined, page }
  const hasFilters = Boolean(universityId || type || q)

  const [searchInput, setSearchInput] = useState(q)
  const [syncedQ, setSyncedQ] = useState(q)
  if (q !== syncedQ) {
    setSyncedQ(q)
    setSearchInput(q)
  }

  const searchTimer = useRef<number | undefined>(undefined)
  useEffect(() => () => window.clearTimeout(searchTimer.current), [])

  const updateParams = (
    changes: Record<string, string | undefined>,
    { resetPage = true, replace = false } = {},
  ) => {
    setSearchParams(
      (previous) => {
        const next = new URLSearchParams(previous)
        for (const [key, value] of Object.entries(changes)) {
          if (value) next.set(key, value)
          else next.delete(key)
        }
        if (resetPage) next.delete('sayfa')
        return next
      },
      { replace },
    )
  }

  const onSearchChange = (value: string) => {
    setSearchInput(value)
    window.clearTimeout(searchTimer.current)
    searchTimer.current = window.setTimeout(
      () => updateParams({ q: value.trim() || undefined }, { replace: true }),
      SEARCH_DELAY_MS,
    )
  }

  const universities = useUniversityOptions()
  const courses = useCourseOptions(universityId)
  const resources = useQuery({
    queryKey: resourceKeys.list(filters),
    queryFn: ({ signal }) => fetchResources(filters, signal),
    placeholderData: keepPreviousData,
  })

  return (
    <Page title="Kaynaklar" description="Üniversite, ders ve türe göre paylaşılan kaynaklar.">
      {notice && <Alert tone="success">{notice}</Alert>}

      <section
        aria-label="Filtreler"
        className="grid gap-4 rounded-lg border border-slate-200 bg-white p-4 sm:grid-cols-2 lg:grid-cols-4"
      >
        <SelectField
          label="Üniversite"
          value={universityId ?? ''}
          onChange={(event) => updateParams({ universite: event.target.value, ders: undefined })}
          disabled={universities.isPending}
        >
          <option value="">Tüm üniversiteler</option>
          {universities.data?.map((university) => (
            <option key={university.id} value={university.id}>
              {university.name}
            </option>
          ))}
        </SelectField>
        <SelectField
          label="Ders"
          value={courseId ?? ''}
          onChange={(event) => updateParams({ ders: event.target.value })}
          disabled={!universityId || courses.isPending}
          hint={universityId ? undefined : 'Önce üniversite seçin.'}
        >
          <option value="">
            {universityId && courses.isPending ? 'Yükleniyor…' : 'Tüm dersler'}
          </option>
          {courses.data?.map((course) => (
            <option key={course.id} value={course.id}>
              {course.code} — {course.name}
            </option>
          ))}
        </SelectField>
        <SelectField
          label="Tür"
          value={type ?? ''}
          onChange={(event) => updateParams({ tur: event.target.value })}
        >
          <option value="">Tüm türler</option>
          {RESOURCE_TYPES.map((value) => (
            <option key={value} value={value}>
              {RESOURCE_TYPE_LABELS[value]}
            </option>
          ))}
        </SelectField>
        <TextField
          label="Ara"
          type="search"
          placeholder="Başlık veya açıklamada ara"
          maxLength={100}
          value={searchInput}
          onChange={(event) => onSearchChange(event.target.value)}
        />
        {hasFilters && (
          <div className="sm:col-span-2 lg:col-span-4">
            <Button
              variant="secondary"
              size="small"
              onClick={() => {
                window.clearTimeout(searchTimer.current)
                setSearchParams({})
              }}
            >
              Filtreleri temizle
            </Button>
          </div>
        )}
      </section>

      {universities.error && <ErrorState error={universities.error} />}

      {resources.isPending ? (
        <LoadingState label="Kaynaklar yükleniyor…" />
      ) : resources.error ? (
        <ErrorState error={resources.error} onRetry={() => void resources.refetch()} />
      ) : resources.data.items.length === 0 ? (
        page > 1 && resources.data.totalCount > 0 ? (
          <EmptyState title="Bu sayfada kayıt yok.">
            <Button variant="secondary" onClick={() => updateParams({})}>
              İlk sayfaya dön
            </Button>
          </EmptyState>
        ) : (
          <EmptyState
            title={hasFilters ? 'Filtrelere uyan kaynak bulunamadı.' : 'Henüz kaynak yok.'}
          >
            {hasFilters
              ? 'Filtreleri değiştirmeyi veya temizlemeyi deneyin.'
              : 'İlk kaynağı siz yükleyin.'}
          </EmptyState>
        )
      ) : (
        <div className="space-y-4" aria-busy={resources.isFetching}>
          <p className="text-sm text-slate-600" aria-live="polite">
            {resources.data.totalCount} kaynak bulundu.
          </p>
          <ul className="space-y-3">
            {resources.data.items.map((resource) => (
              <li key={resource.id}>
                <ResourceCard resource={resource} />
              </li>
            ))}
          </ul>
          <Pagination
            page={page}
            pageSize={RESOURCE_PAGE_SIZE}
            totalCount={resources.data.totalCount}
            onPageChange={(next) => {
              updateParams({ sayfa: next > 1 ? String(next) : undefined }, { resetPage: false })
              window.scrollTo({ top: 0 })
            }}
          />
        </div>
      )}
    </Page>
  )
}
