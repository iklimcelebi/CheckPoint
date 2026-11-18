import { screen, within } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { server } from '../test/server'
import { api, currentLocation, makeResource, renderRoutes, signInAs } from '../test/utils'
import { FavoritesPage } from '../features/resources/FavoritesPage'
import { getSessionSnapshot } from '../lib/session'
import { AppLayout } from './AppLayout'
import { RequireAuth } from '../routes/guards'

const routes = [
  { path: '/giris', element: <h1>Giriş sayfası</h1> },
  {
    element: <RequireAuth />,
    children: [
      {
        element: <AppLayout />,
        children: [
          { path: '/', element: <h1>Ana sayfa</h1> },
          { path: '/arama', element: <h1>Arama sayfası</h1> },
          { path: '/profil', element: <h1>Profil sayfası</h1> },
          { path: '/favoriler', element: <FavoritesPage /> },
        ],
      },
    ],
  },
]

beforeEach(() => signInAs('User', 1, 'ayse'))

describe('AppLayout', () => {
  it('shows the side menu without a duplicate upload button', () => {
    renderRoutes(routes, '/')
    const nav = screen.getByRole('navigation', { name: 'Ana menü' })
    expect(
      within(nav)
        .getAllByRole('link')
        .map((link) => link.textContent),
    ).toEqual([
      '+ Kaynak Yükle',
      'Ana Sayfa',
      'Kaynaklarım',
      'Favoriler',
      'Profilim',
      'Üniversiteler',
      'Dersler',
    ])
    expect(screen.getAllByRole('link', { name: /Kaynak Yükle/ })).toHaveLength(1)
  })

  it('searches from the header', async () => {
    const { router, user } = renderRoutes(routes, '/')
    await user.type(
      screen.getByPlaceholderText('Ders, konu, kaynak veya üniversite ara...'),
      'veri yapıları{Enter}',
    )
    expect(await screen.findByRole('heading', { name: 'Arama sayfası' })).toBeInTheDocument()
    expect(currentLocation(router)).toBe('/arama?q=veri%20yap%C4%B1lar%C4%B1')
  })

  it('opens the profile and signs out from the user menu', async () => {
    const { router, user } = renderRoutes(routes, '/')

    await user.click(screen.getByRole('button', { name: 'Kullanıcı menüsü: ayse' }))
    await user.click(screen.getByRole('menuitem', { name: 'Profili görüntüle' }))
    expect(await screen.findByRole('heading', { name: 'Profil sayfası' })).toBeInTheDocument()
    expect(screen.queryByRole('menu')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Kullanıcı menüsü: ayse' }))
    await user.click(screen.getByRole('menuitem', { name: 'Çıkış yap' }))
    expect(await screen.findByRole('heading', { name: 'Giriş sayfası' })).toBeInTheDocument()
    expect(getSessionSnapshot()).toBeNull()
    expect(currentLocation(router)).toBe('/giris')
  })
})

describe('FavoritesPage', () => {
  it('lists favorites and removes one with the star button', async () => {
    let favorites = [makeResource({ id: 10, title: 'Favori not', isFavorite: true })]
    const deleted: string[] = []
    server.use(
      http.get(api('/api/users/me/favorites'), () =>
        HttpResponse.json({
          items: favorites,
          page: 1,
          pageSize: 10,
          totalCount: favorites.length,
        }),
      ),
      http.delete(api('/api/resources/:id/favorite'), ({ params }) => {
        deleted.push(String(params.id))
        favorites = []
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const { user } = renderRoutes(routes, '/favoriler')

    expect(await screen.findByText('Favori not')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Favori not: Favorilerden çıkar' }))

    expect(await screen.findByText('Henüz favori kaynağınız yok.')).toBeInTheDocument()
    expect(deleted).toEqual(['10'])
  })
})
