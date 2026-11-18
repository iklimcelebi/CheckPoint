import { useQuery } from '@tanstack/react-query'
import { useState, type FormEvent, type ReactNode } from 'react'
import { Link, useNavigate } from 'react-router'
import type { ResourceType } from '../../api/types'
import { Page } from '../../components/Page'
import { Button } from '../../components/ui/Button'
import { SelectField } from '../../components/ui/SelectField'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/StatusViews'
import { isResourceType, RESOURCE_TYPE_LABELS, RESOURCE_TYPES } from '../../lib/resourceTypes'
import { useAuth } from '../auth/authContext'
import { useCourseOptions, usePopularCourses } from '../courses/api'
import { fetchResources, resourceKeys } from '../resources/api'
import { ResourceCard } from '../resources/ResourceCard'
import { useUniversityOptions } from '../universities/api'

const LATEST_COUNT = 5

function Section({
  title,
  action,
  children,
}: {
  title: string
  action?: ReactNode
  children: ReactNode
}) {
  const id = `section-${title.toLocaleLowerCase('tr-TR').replace(/\s+/g, '-')}`
  return (
    <section aria-labelledby={id} className="space-y-3">
      <div className="flex items-baseline justify-between gap-4">
        <h2 id={id} className="text-lg font-semibold text-slate-900">
          {title}
        </h2>
        {action}
      </div>
      {children}
    </section>
  )
}

function exploreLink(params: Record<string, string | number | undefined>) {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== '') search.set(key, String(value))
  }
  const query = search.toString()
  return query ? `/kaynaklar?${query}` : '/kaynaklar'
}

function FilterPanel() {
  const navigate = useNavigate()
  const [universityId, setUniversityId] = useState<number>()
  const [courseId, setCourseId] = useState<number>()
  const [type, setType] = useState<ResourceType>()
  const universities = useUniversityOptions()
  const courses = useCourseOptions(universityId)

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    void navigate(exploreLink({ universite: universityId, ders: courseId, tur: type }))
  }

  return (
    <form
      onSubmit={onSubmit}
      aria-label="Kaynak filtreleri"
      className="grid gap-4 rounded-xl border border-peach bg-white p-5 shadow-sm sm:grid-cols-2 lg:grid-cols-4 lg:items-end"
    >
      <SelectField
        label="Üniversite"
        value={universityId ?? ''}
        onChange={(event) => {
          setUniversityId(event.target.value ? Number(event.target.value) : undefined)
          setCourseId(undefined)
        }}
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
        disabled={!universityId}
        onChange={(event) =>
          setCourseId(event.target.value ? Number(event.target.value) : undefined)
        }
      >
        <option value="">{universityId ? 'Tüm dersler' : 'Önce üniversite seçin'}</option>
        {courses.data?.map((course) => (
          <option key={course.id} value={course.id}>
            {course.code} — {course.name}
          </option>
        ))}
      </SelectField>
      <SelectField
        label="Tür"
        value={type ?? ''}
        onChange={(event) =>
          setType(isResourceType(event.target.value) ? event.target.value : undefined)
        }
      >
        <option value="">Tüm türler</option>
        {RESOURCE_TYPES.map((value) => (
          <option key={value} value={value}>
            {RESOURCE_TYPE_LABELS[value]}
          </option>
        ))}
      </SelectField>
      <Button type="submit">Kaynakları göster</Button>
    </form>
  )
}

function PopularCourses() {
  const popular = usePopularCourses()

  if (popular.isPending) return <LoadingState label="Popüler dersler yükleniyor…" />
  if (popular.error) return <ErrorState error={popular.error} />
  if (popular.data.length === 0) {
    return (
      <EmptyState title="Henüz popüler ders yok.">
        Kaynaklar görüntülendikçe burada listelenir.
      </EmptyState>
    )
  }

  return (
    <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
      {popular.data.map((course, index) => (
        <li key={course.id}>
          <Link
            to={exploreLink({ universite: course.universityId, ders: course.id })}
            className="flex h-full gap-3 rounded-lg border border-slate-200 bg-white p-4 shadow-sm transition-colors hover:border-tangerine hover:bg-peach/20"
          >
            <span
              aria-hidden="true"
              className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-peach font-semibold text-accent-strong"
            >
              {index + 1}
            </span>
            <span className="min-w-0">
              <span className="block font-semibold text-slate-900">
                {course.code} · {course.name}
              </span>
              <span className="block text-sm text-slate-600">{course.universityName}</span>
              <span className="mt-1 block text-xs text-slate-500">
                {course.resourceCount} kaynak · {course.viewCount} görüntülenme
              </span>
            </span>
          </Link>
        </li>
      ))}
    </ul>
  )
}

function LatestResources() {
  const latest = useQuery({
    queryKey: resourceKeys.latest,
    queryFn: ({ signal }) => fetchResources({ page: 1 }, signal),
  })

  if (latest.isPending) return <LoadingState label="Son kaynaklar yükleniyor…" />
  if (latest.error) return <ErrorState error={latest.error} onRetry={() => void latest.refetch()} />
  if (latest.data.items.length === 0) {
    return (
      <EmptyState title="Henüz kaynak yok.">
        <Link to="/yukle" className="font-medium text-accent hover:underline">
          İlk kaynağı siz yükleyin
        </Link>
      </EmptyState>
    )
  }

  return (
    <ul className="space-y-3">
      {latest.data.items.slice(0, LATEST_COUNT).map((resource) => (
        <li key={resource.id}>
          <ResourceCard resource={resource} />
        </li>
      ))}
    </ul>
  )
}

function UniversitiesWithResources() {
  const universities = useUniversityOptions()

  if (universities.isPending) return <LoadingState label="Üniversiteler yükleniyor…" />
  if (universities.error) return <ErrorState error={universities.error} />
  if (universities.data.length === 0) return <EmptyState title="Henüz üniversite eklenmemiş." />

  const sorted = [...universities.data].sort(
    (a, b) => b.resourceCount - a.resourceCount || a.name.localeCompare(b.name, 'tr'),
  )

  return (
    <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
      {sorted.map((university) => (
        <li key={university.id}>
          <Link
            to={exploreLink({ universite: university.id })}
            className="flex h-full items-center justify-between gap-3 rounded-lg border border-slate-200 bg-white p-4 shadow-sm transition-colors hover:border-tangerine hover:bg-peach/20"
          >
            <span className="min-w-0">
              <span className="block truncate font-semibold text-slate-900">{university.name}</span>
              <span className="block text-sm text-slate-600">{university.city}</span>
            </span>
            <span className="shrink-0 rounded-full bg-blush px-3 py-1 text-sm font-semibold text-slate-900">
              {university.resourceCount} kaynak
            </span>
          </Link>
        </li>
      ))}
    </ul>
  )
}

export function HomePage() {
  const { user } = useAuth()

  return (
    <Page
      title="Ana Sayfa"
      description={
        user
          ? `Hoş geldin, ${user.username}! Aradığın kaynağı bul veya yenisini paylaş.`
          : undefined
      }
    >
      <div className="space-y-10">
        <Section title="Kaynak bul">
          <FilterPanel />
        </Section>
        <Section title="Popüler dersler">
          <PopularCourses />
        </Section>
        <Section
          title="Son eklenen kaynaklar"
          action={
            <Link to="/kaynaklar" className="text-sm font-medium text-accent hover:underline">
              Tümünü gör
            </Link>
          }
        >
          <LatestResources />
        </Section>
        <Section title="Üniversitelere göre kaynaklar">
          <UniversitiesWithResources />
        </Section>
      </div>
    </Page>
  )
}
