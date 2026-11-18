import type { Resource } from '../../api/types'
import { errorMessage } from '../../api/errors'
import { useToggleFavorite } from './hooks'

interface FavoriteButtonProps {
  resource: Resource
  showLabel?: boolean
}

export function FavoriteButton({ resource, showLabel = false }: FavoriteButtonProps) {
  const toggle = useToggleFavorite()
  const favorite = toggle.isPending ? !resource.isFavorite : resource.isFavorite
  const label = favorite ? 'Favorilerden çıkar' : 'Favorilere ekle'

  return (
    <button
      type="button"
      aria-pressed={favorite}
      aria-label={showLabel ? undefined : `${resource.title}: ${label}`}
      title={toggle.error ? errorMessage(toggle.error) : label}
      disabled={toggle.isPending}
      onClick={() => toggle.mutate({ id: resource.id, favorite: !resource.isFavorite })}
      className={[
        'inline-flex items-center gap-1.5 rounded-md border px-2.5 py-1.5 text-sm font-medium transition-colors',
        favorite
          ? 'border-tangerine bg-peach text-accent-strong hover:bg-tangerine'
          : 'border-slate-300 bg-white text-slate-700 hover:border-tangerine hover:bg-peach/40',
      ].join(' ')}
    >
      <svg viewBox="0 0 20 20" aria-hidden="true" className="h-4 w-4">
        <path
          d="M10 1.8l2.5 5.1 5.6.8-4 3.9.9 5.6L10 14.6l-5 2.6.9-5.6-4-3.9 5.6-.8z"
          fill={favorite ? 'currentColor' : 'none'}
          stroke="currentColor"
          strokeWidth="1.5"
          strokeLinejoin="round"
        />
      </svg>
      {showLabel && label}
    </button>
  )
}
