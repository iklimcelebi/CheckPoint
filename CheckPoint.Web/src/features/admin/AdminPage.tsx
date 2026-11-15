import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { Page } from '../../components/Page'
import { fetchCourses } from '../courses/api'
import { fetchResources } from '../resources/api'
import { fetchUniversities } from '../universities/api'

interface Section {
  key: string
  to: string
  title: string
  description: string
  load: (signal: AbortSignal) => Promise<number>
}

const SECTIONS: Section[] = [
  {
    key: 'universities',
    to: '/universiteler',
    title: 'Üniversiteler',
    description: 'Üniversite ekleyin, düzenleyin veya silin. Dersi olan üniversite silinemez.',
    load: async (signal) => (await fetchUniversities({ pageSize: 1 }, signal)).totalCount,
  },
  {
    key: 'courses',
    to: '/dersler',
    title: 'Dersler',
    description: 'Dersleri ekleyin veya düzenleyin. Ders silme bu sürümde yoktur.',
    load: async (signal) => (await fetchCourses({ pageSize: 1 }, signal)).totalCount,
  },
  {
    key: 'resources',
    to: '/kaynaklar',
    title: 'Kaynaklar',
    description: 'Yönetici olarak tüm kaynakları düzenleyebilir veya silebilirsiniz.',
    load: async (signal) => (await fetchResources({ page: 1 }, signal)).totalCount,
  },
]

function SectionCard({ section }: { section: Section }) {
  const count = useQuery({
    queryKey: [section.key, 'count'],
    queryFn: ({ signal }) => section.load(signal),
  })

  return (
    <Link
      to={section.to}
      className="block rounded-lg border border-slate-200 bg-white p-5 shadow-sm transition-colors hover:border-blue-300 hover:bg-blue-50/40"
    >
      <p className="text-sm font-medium text-slate-600">{section.title}</p>
      <p className="mt-1 text-3xl font-semibold text-slate-900">
        {count.data ?? (count.isPending ? '…' : '—')}
      </p>
      <p className="mt-2 text-sm text-slate-600">{section.description}</p>
    </Link>
  )
}

export function AdminPage() {
  return (
    <Page title="Yönetim" description="Yalnızca yöneticilerin görebildiği işlemler.">
      <div className="grid gap-4 md:grid-cols-3">
        {SECTIONS.map((section) => (
          <SectionCard key={section.key} section={section} />
        ))}
      </div>
    </Page>
  )
}
