import { useQuery } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { Link, useSearchParams } from 'react-router'
import { Page } from '../../components/Page'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/StatusViews'
import { fetchCourses } from '../courses/api'
import { fetchResources, resourceKeys } from '../resources/api'
import { ResourceCard } from '../resources/ResourceCard'
import { fetchUniversities } from '../universities/api'

const PREVIEW_SIZE = 5

function ResultSection({
  title,
  count,
  more,
  children,
}: {
  title: string
  count?: number
  more?: ReactNode
  children: ReactNode
}) {
  return (
    <section className="space-y-3">
      <div className="flex items-baseline justify-between gap-4">
        <h2 className="text-lg font-semibold text-slate-900">
          {title}
          {count !== undefined && (
            <span className="ml-2 text-sm font-normal text-slate-500">({count})</span>
          )}
        </h2>
        {more}
      </div>
      {children}
    </section>
  )
}

const listLinkClass =
  'block rounded-lg border border-slate-200 bg-white px-4 py-3 shadow-sm transition-colors hover:border-tangerine hover:bg-peach/20'

export function SearchPage() {
  const [searchParams] = useSearchParams()
  const q = (searchParams.get('q') ?? '').trim()

  const universities = useQuery({
    queryKey: ['universities', 'search', q],
    queryFn: ({ signal }) => fetchUniversities({ q, pageSize: PREVIEW_SIZE }, signal),
    enabled: q.length > 0,
  })
  const courses = useQuery({
    queryKey: ['courses', 'search', q],
    queryFn: ({ signal }) => fetchCourses({ q, pageSize: PREVIEW_SIZE }, signal),
    enabled: q.length > 0,
  })
  const resources = useQuery({
    queryKey: resourceKeys.list({ q, page: 1 }),
    queryFn: ({ signal }) => fetchResources({ q, page: 1 }, signal),
    enabled: q.length > 0,
  })

  if (!q) {
    return (
      <Page title="Arama">
        <EmptyState title="Aramak için üstteki kutuya bir şey yazın.">
          Ders, konu, kaynak veya üniversite adıyla arayabilirsiniz.
        </EmptyState>
      </Page>
    )
  }

  const nothingFound =
    universities.data?.totalCount === 0 &&
    courses.data?.totalCount === 0 &&
    resources.data?.totalCount === 0

  return (
    <Page title={`"${q}" için sonuçlar`}>
      {nothingFound && (
        <EmptyState title="Sonuç bulunamadı.">Farklı bir kelimeyle tekrar deneyin.</EmptyState>
      )}

      <ResultSection title="Üniversiteler" count={universities.data?.totalCount}>
        {universities.isPending ? (
          <LoadingState label="Üniversiteler aranıyor…" />
        ) : universities.error ? (
          <ErrorState error={universities.error} />
        ) : universities.data.items.length === 0 ? (
          <p className="text-sm text-slate-600">Eşleşen üniversite yok.</p>
        ) : (
          <ul className="grid gap-2 sm:grid-cols-2">
            {universities.data.items.map((university) => (
              <li key={university.id}>
                <Link to={`/kaynaklar?universite=${university.id}`} className={listLinkClass}>
                  <span className="font-medium text-slate-900">{university.name}</span>
                  <span className="block text-sm text-slate-600">
                    {university.city} · {university.resourceCount} kaynak
                  </span>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </ResultSection>

      <ResultSection title="Dersler" count={courses.data?.totalCount}>
        {courses.isPending ? (
          <LoadingState label="Dersler aranıyor…" />
        ) : courses.error ? (
          <ErrorState error={courses.error} />
        ) : courses.data.items.length === 0 ? (
          <p className="text-sm text-slate-600">Eşleşen ders yok.</p>
        ) : (
          <ul className="grid gap-2 sm:grid-cols-2">
            {courses.data.items.map((course) => (
              <li key={course.id}>
                <Link
                  to={`/kaynaklar?universite=${course.universityId}&ders=${course.id}`}
                  className={listLinkClass}
                >
                  <span className="font-medium text-slate-900">
                    {course.code} · {course.name}
                  </span>
                  <span className="block text-sm text-slate-600">{course.universityName}</span>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </ResultSection>

      <ResultSection
        title="Kaynaklar"
        count={resources.data?.totalCount}
        more={
          resources.data && resources.data.totalCount > PREVIEW_SIZE ? (
            <Link
              to={`/kaynaklar?q=${encodeURIComponent(q)}`}
              className="text-sm font-medium text-accent hover:underline"
            >
              Tümünü gör
            </Link>
          ) : undefined
        }
      >
        {resources.isPending ? (
          <LoadingState label="Kaynaklar aranıyor…" />
        ) : resources.error ? (
          <ErrorState error={resources.error} />
        ) : resources.data.items.length === 0 ? (
          <p className="text-sm text-slate-600">Eşleşen kaynak yok.</p>
        ) : (
          <ul className="space-y-3">
            {resources.data.items.slice(0, PREVIEW_SIZE).map((resource) => (
              <li key={resource.id}>
                <ResourceCard resource={resource} />
              </li>
            ))}
          </ul>
        )}
      </ResultSection>
    </Page>
  )
}
