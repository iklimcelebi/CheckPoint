import type { ResourceType } from '../../api/types'
import { RESOURCE_TYPE_BADGE_CLASSES, RESOURCE_TYPE_LABELS } from '../../lib/resourceTypes'

export function ResourceTypeBadge({ type }: { type: ResourceType }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${RESOURCE_TYPE_BADGE_CLASSES[type]}`}
    >
      {RESOURCE_TYPE_LABELS[type]}
    </span>
  )
}
