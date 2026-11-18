import { useEffect, useId, useRef, useState } from 'react'
import { Link } from 'react-router'
import { useAuth } from '../../features/auth/authContext'

export function UserMenu() {
  const { user, signOut } = useAuth()
  const [open, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const buttonRef = useRef<HTMLButtonElement>(null)
  const menuId = useId()

  useEffect(() => {
    if (!open) return
    const onPointerDown = (event: PointerEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) setOpen(false)
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpen(false)
        buttonRef.current?.focus()
      }
    }
    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  if (!user) return null
  const initial = user.username.charAt(0).toLocaleUpperCase('tr-TR')

  return (
    <div ref={containerRef} className="relative">
      <button
        ref={buttonRef}
        type="button"
        aria-haspopup="menu"
        aria-expanded={open}
        aria-controls={menuId}
        aria-label={`Kullanıcı menüsü: ${user.username}`}
        onClick={() => setOpen((value) => !value)}
        className="flex items-center gap-2 rounded-full border border-peach bg-white py-1 pl-1 pr-3 text-sm font-medium text-slate-800 hover:bg-peach/40"
      >
        <span
          aria-hidden="true"
          className="flex h-8 w-8 items-center justify-center rounded-full bg-tangerine font-semibold text-slate-900"
        >
          {initial}
        </span>
        <span className="hidden max-w-32 truncate sm:inline">{user.username}</span>
        <svg viewBox="0 0 20 20" aria-hidden="true" className="h-4 w-4 text-slate-500">
          <path d="M5 8l5 5 5-5" fill="none" stroke="currentColor" strokeWidth="2" />
        </svg>
      </button>

      {open && (
        <div
          id={menuId}
          role="menu"
          className="absolute right-0 z-40 mt-2 w-56 overflow-hidden rounded-lg border border-peach bg-white shadow-lg"
        >
          <div className="border-b border-peach bg-peach/30 px-4 py-3">
            <p className="truncate text-sm font-semibold text-slate-900">{user.username}</p>
            <p className="truncate text-xs text-slate-600">{user.email}</p>
          </div>
          <Link
            to="/profil"
            role="menuitem"
            onClick={() => setOpen(false)}
            className="block px-4 py-2 text-sm text-slate-800 hover:bg-peach/40 focus:bg-peach/40"
          >
            Profili görüntüle
          </Link>
          <button
            type="button"
            role="menuitem"
            onClick={() => {
              setOpen(false)
              signOut()
            }}
            className="block w-full px-4 py-2 text-left text-sm text-slate-800 hover:bg-peach/40 focus:bg-peach/40"
          >
            Çıkış yap
          </button>
        </div>
      )}
    </div>
  )
}
