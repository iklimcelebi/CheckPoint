import type { ReactNode } from 'react'
import { Link } from 'react-router'
import type { Resource } from '../../api/types'
import { formatDateTime, formatFileSize } from '../../lib/format'
import { ResourceTypeBadge } from './ResourceTypeBadge'

export function ResourceCard({ resource, actions }: { resource: Resource; actions?: ReactNode }) {
  return (
    <article className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0 flex-1 space-y-1">
          <div className="flex flex-wrap items-center gap-2">
            <ResourceTypeBadge type={resource.type} />
            <h2 className="text-base font-semibold text-slate-900">
              <Link
                to={`/kaynaklar/${resource.id}`}
                className="hover:text-blue-700 hover:underline"
              >
                {resource.title}
              </Link>
            </h2>
          </div>
          <p className="text-sm text-slate-700">
            {resource.university.name} · <span className="font-medium">{resource.course.code}</span>{' '}
            {resource.course.name}
          </p>
          <p className="text-xs text-slate-500">
            <span className="break-all">{resource.originalFileName}</span> (
            {formatFileSize(resource.fileSize)}) · Yükleyen: {resource.uploadedBy.username} ·{' '}
            <time dateTime={resource.uploadedAt}>{formatDateTime(resource.uploadedAt)}</time>
          </p>
        </div>
        {actions && <div className="flex shrink-0 gap-2">{actions}</div>}
      </div>
    </article>
  )
}
