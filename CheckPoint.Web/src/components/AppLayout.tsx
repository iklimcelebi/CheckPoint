import { useEffect, useState } from 'react'
import { Link, Outlet, useLocation } from 'react-router'
import { GlobalSearch } from './layout/GlobalSearch'
import { SideNav } from './layout/SideNav'
import { UserMenu } from './layout/UserMenu'

export function AppLayout() {
  const [menuOpen, setMenuOpen] = useState(false)
  const location = useLocation()
  const [lastPath, setLastPath] = useState(location.pathname)
  if (location.pathname !== lastPath) {
    setLastPath(location.pathname)
    setMenuOpen(false)
  }

  useEffect(() => {
    if (!menuOpen) return
    const onKeyDown = (event: KeyboardEvent) => event.key === 'Escape' && setMenuOpen(false)
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [menuOpen])

  return (
    <div className="min-h-screen bg-peach/15">
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-50 focus:rounded-md focus:bg-white focus:px-4 focus:py-2 focus:shadow"
      >
        İçeriğe geç
      </a>

      <header className="sticky top-0 z-30 border-b border-peach bg-white">
        <div className="flex flex-wrap items-center gap-x-3 gap-y-2 py-2 pr-4 lg:h-16 lg:flex-nowrap lg:py-0 lg:pr-8">
          <div className="flex shrink-0 items-center gap-2 pl-4 lg:w-60">
            <button
              type="button"
              className="rounded-md border border-slate-300 px-2.5 py-1.5 text-sm font-medium text-slate-700 lg:hidden"
              aria-expanded={menuOpen}
              aria-controls="mobile-nav"
              onClick={() => setMenuOpen(true)}
            >
              Menü
            </button>
            <Link to="/" className="text-xl font-bold tracking-tight text-accent">
              Check<span className="text-tangerine-strong">Point</span>
            </Link>
          </div>
          <div className="order-last w-full min-w-0 pl-4 lg:order-none lg:w-auto lg:flex-1 lg:pl-8">
            <GlobalSearch />
          </div>
          <div className="ml-auto lg:ml-0">
            <UserMenu />
          </div>
        </div>
      </header>

      <div className="flex">
        <aside className="sticky top-16 hidden h-[calc(100vh-4rem)] w-60 shrink-0 overflow-y-auto border-r border-peach bg-white lg:block">
          <SideNav />
        </aside>

        {menuOpen && (
          <div className="fixed inset-0 z-40 lg:hidden">
            <button
              type="button"
              aria-label="Menüyü kapat"
              className="absolute inset-0 bg-slate-900/40"
              onClick={() => setMenuOpen(false)}
            />
            <aside
              id="mobile-nav"
              className="absolute inset-y-0 left-0 w-72 max-w-[85vw] overflow-y-auto bg-white shadow-xl"
            >
              <div className="flex h-16 items-center justify-between border-b border-peach px-4">
                <span className="text-xl font-bold text-accent">CheckPoint</span>
                <button
                  type="button"
                  onClick={() => setMenuOpen(false)}
                  className="rounded-md border border-slate-300 px-2.5 py-1.5 text-sm font-medium text-slate-700"
                >
                  Kapat
                </button>
              </div>
              <SideNav onNavigate={() => setMenuOpen(false)} />
            </aside>
          </div>
        )}

        <main id="main-content" tabIndex={-1} className="min-w-0 flex-1 px-4 py-8 lg:px-8">
          <div className="mx-auto max-w-5xl lg:mx-0">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}
