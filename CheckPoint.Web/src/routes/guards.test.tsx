import { screen } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import type { RouteObject } from 'react-router'
import { describe, expect, it } from 'vitest'
import { ProfilePage } from '../features/auth/ProfilePage'
import { LoginPage } from '../features/auth/LoginPage'
import { getSessionSnapshot } from '../lib/session'
import { server } from '../test/server'
import { api, currentLocation, renderRoutes, signInAs } from '../test/utils'
import { GuestOnly, RequireAdmin, RequireAuth } from './guards'

const routes: RouteObject[] = [
  { element: <GuestOnly />, children: [{ path: '/giris', element: <LoginPage /> }] },
  {
    element: <RequireAuth />,
    children: [
      { path: '/kaynaklar', element: <h1>Kaynaklar sayfası</h1> },
      { path: '/profil', element: <ProfilePage /> },
      {
        element: <RequireAdmin />,
        children: [{ path: '/yonetim', element: <h1>Yönetim içeriği</h1> }],
      },
    ],
  },
]

describe('route guards', () => {
  it('sends anonymous visitors to the login page with a return path', () => {
    const { router } = renderRoutes(routes, '/profil?sekme=1')
    expect(currentLocation(router)).toBe('/giris?donus=%2Fprofil%3Fsekme%3D1')
  })

  it('hides admin pages from regular users', async () => {
    signInAs('User')
    renderRoutes(routes, '/yonetim')
    expect(await screen.findByRole('heading', { name: 'Erişim reddedildi' })).toBeInTheDocument()
    expect(screen.queryByText('Yönetim içeriği')).not.toBeInTheDocument()
  })

  it('shows admin pages to admins', async () => {
    signInAs('Admin', 99, 'yonetici')
    renderRoutes(routes, '/yonetim')
    expect(await screen.findByRole('heading', { name: 'Yönetim içeriği' })).toBeInTheDocument()
  })

  it('redirects signed-in users away from the login page', () => {
    signInAs('User')
    const { router } = renderRoutes(routes, '/giris')
    expect(currentLocation(router)).toBe('/kaynaklar')
  })

  it('signs out and asks for a new login when the API returns 401', async () => {
    signInAs('User')
    server.use(
      http.get(api('/api/users/me'), () =>
        HttpResponse.json(
          { status: 401, title: 'Kimlik doğrulaması gerekli' },
          { status: 401, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    )
    const { router } = renderRoutes(routes, '/profil')

    expect(await screen.findByText(/Oturumunuzun süresi doldu/)).toBeInTheDocument()
    expect(currentLocation(router)).toBe('/giris?donus=%2Fprofil')
    expect(getSessionSnapshot()).toBeNull()
  })

  it('shows a Turkish message when the API cannot be reached', async () => {
    signInAs('User')
    server.use(http.get(api('/api/users/me'), () => HttpResponse.error()))
    renderRoutes(routes, '/profil')
    expect(await screen.findByRole('alert')).toHaveTextContent('Sunucuya ulaşılamadı')
  })
})
