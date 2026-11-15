import { useState } from 'react'
import { Link, NavLink } from 'react-router'
import { useAuth } from '../features/auth/authContext'

interface NavItem {
  to: string
  label: string
  adminOnly?: boolean
}

const NAV_ITEMS: NavItem[] = [
  { to: '/kaynaklar', label: 'Kaynaklar' },
  { to: '/yukle', label: 'Yükle' },
  { to: '/benim-kaynaklarim', label: 'Benim Kaynaklarım' },
  { to: '/universiteler', label: 'Üniversiteler' },
  { to: '/dersler', label: 'Dersler' },
  { to: '/yonetim', label: 'Yönetim', adminOnly: true },
]

function linkClass({ isActive }: { isActive: boolean }) {
  return [
    'block rounded-md px-3 py-2 text-sm font-medium transition-colors',
    isActive
      ? 'bg-blue-50 text-blue-700'
      : 'text-slate-700 hover:bg-slate-100 hover:text-slate-900',
  ].join(' ')
}

export function NavBar() {
  const { user, isAdmin, signOut } = useAuth()
  const [open, setOpen] = useState(false)
  const items = NAV_ITEMS.filter((item) => !item.adminOnly || isAdmin)
  const close = () => setOpen(false)

  return (
    <header className="border-b border-slate-200 bg-white">
      <nav aria-label="Ana menü" className="mx-auto max-w-6xl px-4">
        <div className="flex h-16 items-center justify-between gap-4">
          <Link to="/kaynaklar" className="text-lg font-bold text-blue-700" onClick={close}>
            CheckPoint
          </Link>

          <ul className="hidden items-center gap-1 lg:flex">
            {items.map((item) => (
              <li key={item.to}>
                <NavLink to={item.to} className={linkClass}>
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>

          <div className="hidden items-center gap-3 lg:flex">
            <NavLink to="/profil" className={linkClass}>
              {user?.username}
            </NavLink>
            <button
              type="button"
              onClick={signOut}
              className="rounded-md border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
            >
              Çıkış
            </button>
          </div>

          <button
            type="button"
            className="rounded-md border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 lg:hidden"
            aria-expanded={open}
            aria-controls="mobile-menu"
            onClick={() => setOpen((value) => !value)}
          >
            {open ? 'Menüyü kapat' : 'Menü'}
          </button>
        </div>

        {open && (
          <div id="mobile-menu" className="border-t border-slate-200 py-3 lg:hidden">
            <ul className="space-y-1">
              {items.map((item) => (
                <li key={item.to}>
                  <NavLink to={item.to} className={linkClass} onClick={close}>
                    {item.label}
                  </NavLink>
                </li>
              ))}
              <li>
                <NavLink to="/profil" className={linkClass} onClick={close}>
                  Profil ({user?.username})
                </NavLink>
              </li>
              <li>
                <button
                  type="button"
                  onClick={signOut}
                  className="block w-full rounded-md px-3 py-2 text-left text-sm font-medium text-slate-700 hover:bg-slate-100"
                >
                  Çıkış
                </button>
              </li>
            </ul>
          </div>
        )}
      </nav>
    </header>
  )
}
