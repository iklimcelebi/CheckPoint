import { Link, NavLink } from 'react-router'
import { useAuth } from '../../features/auth/authContext'
import { buttonClasses } from '../ui/buttonStyles'

interface NavItem {
  to: string
  label: string
  end?: boolean
}

interface NavGroup {
  title?: string
  items: NavItem[]
  adminOnly?: boolean
}

const GROUPS: NavGroup[] = [
  {
    items: [{ to: '/', label: 'Ana Sayfa', end: true }],
  },
  {
    title: 'Hesabım',
    items: [
      { to: '/benim-kaynaklarim', label: 'Kaynaklarım' },
      { to: '/favoriler', label: 'Favoriler' },
      { to: '/profil', label: 'Profilim' },
    ],
  },
  {
    title: 'Katalog',
    items: [
      { to: '/universiteler', label: 'Üniversiteler' },
      { to: '/dersler', label: 'Dersler' },
    ],
  },
  {
    title: 'Yönetici',
    adminOnly: true,
    items: [{ to: '/yonetim', label: 'Yönetim' }],
  },
]

function linkClass({ isActive }: { isActive: boolean }) {
  return [
    'block rounded-md px-3 py-2 text-sm font-medium transition-colors',
    isActive
      ? 'bg-peach text-accent-strong'
      : 'text-slate-700 hover:bg-peach/40 hover:text-slate-900',
  ].join(' ')
}

export function SideNav({ onNavigate }: { onNavigate?: () => void }) {
  const { isAdmin } = useAuth()

  return (
    <nav aria-label="Ana menü" className="space-y-6 p-4">
      <Link
        to="/yukle"
        onClick={onNavigate}
        className={buttonClasses('primary', 'normal', 'w-full gap-2')}
      >
        <span aria-hidden="true">+</span> Kaynak Yükle
      </Link>
      {GROUPS.filter((group) => !group.adminOnly || isAdmin).map((group) => (
        <div key={group.title ?? 'main'}>
          {group.title && (
            <p className="mb-2 px-3 text-xs font-semibold uppercase tracking-wide text-slate-500">
              {group.title}
            </p>
          )}
          <ul className="space-y-1">
            {group.items.map((item) => (
              <li key={item.to}>
                <NavLink to={item.to} end={item.end} className={linkClass} onClick={onNavigate}>
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </div>
      ))}
    </nav>
  )
}
