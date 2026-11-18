import { act, screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import type { RouteObject } from 'react-router'
import { describe, expect, it } from 'vitest'
import { clearSession, getSessionSnapshot } from '../../lib/session'
import { GuestOnly, RequireAuth } from '../../routes/guards'
import { server } from '../../test/server'
import { api, currentLocation, loginResponse, renderRoutes, signInAs } from '../../test/utils'
import { LoginPage } from './LoginPage'
import { RegisterPage } from './RegisterPage'

const routes: RouteObject[] = [
  {
    element: <GuestOnly />,
    children: [
      { path: '/giris', element: <LoginPage /> },
      { path: '/kayit', element: <RegisterPage /> },
    ],
  },
  {
    element: <RequireAuth />,
    children: [
      { path: '/', element: <h1>Ana sayfa</h1> },
      { path: '/kaynaklar', element: <h1>Kaynaklar sayfası</h1> },
      { path: '/profil', element: <h1>Profil sayfası</h1> },
    ],
  },
]

describe('LoginPage', () => {
  it('shows Turkish validation errors without calling the API', async () => {
    let calls = 0
    server.use(http.post(api('/api/auth/login'), () => void calls++))
    const { user } = renderRoutes(routes, '/giris')

    await user.click(screen.getByRole('button', { name: 'Giriş yap' }))

    expect(await screen.findByText('E-posta adresi zorunludur.')).toBeInTheDocument()
    expect(screen.getByText('Parola zorunludur.')).toBeInTheDocument()
    await user.type(screen.getByLabelText('E-posta'), 'abc')
    await user.click(screen.getByRole('button', { name: 'Giriş yap' }))
    expect(await screen.findByText('Geçerli bir e-posta adresi girin.')).toBeInTheDocument()
    expect(calls).toBe(0)
  })

  it('shows the API message for wrong credentials', async () => {
    server.use(
      http.post(api('/api/auth/login'), () =>
        HttpResponse.json(
          {
            status: 401,
            title: 'Kimlik doğrulaması gerekli',
            detail: 'E-posta veya parola hatalı.',
          },
          { status: 401, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    )
    const { user } = renderRoutes(routes, '/giris')

    await user.type(screen.getByLabelText('E-posta'), 'ayse@example.com')
    await user.type(screen.getByLabelText('Parola'), 'YanlisParola1')
    await user.click(screen.getByRole('button', { name: 'Giriş yap' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('E-posta veya parola hatalı.')
    expect(getSessionSnapshot()).toBeNull()
  })

  it('signs in and returns to the requested page', async () => {
    server.use(http.post(api('/api/auth/login'), () => HttpResponse.json(loginResponse())))
    const { router, user } = renderRoutes(routes, '/profil')
    expect(currentLocation(router)).toBe('/giris?donus=%2Fprofil')

    await user.type(screen.getByLabelText('E-posta'), 'ayse@example.com')
    await user.type(screen.getByLabelText('Parola'), 'Parola123')
    await user.click(screen.getByRole('button', { name: 'Giriş yap' }))

    expect(await screen.findByRole('heading', { name: 'Profil sayfası' })).toBeInTheDocument()
    expect(getSessionSnapshot()?.user.username).toBe('ayse')
    expect(window.localStorage.getItem('checkpoint.session')).toContain('test-token-1')
  })

  it('explains that an expired session needs a new sign-in', async () => {
    signInAs('User')
    const { router } = renderRoutes(routes, '/kaynaklar')
    expect(await screen.findByRole('heading', { name: 'Kaynaklar sayfası' })).toBeInTheDocument()

    act(() => clearSession('expired'))

    expect(await screen.findByText(/Oturumunuzun süresi doldu/)).toBeInTheDocument()
    expect(currentLocation(router)).toBe('/giris?donus=%2Fkaynaklar')
  })
})

describe('RegisterPage', () => {
  async function fillForm(
    user: ReturnType<typeof renderRoutes>['user'],
    { password = 'Parola123', confirm = password }: { password?: string; confirm?: string } = {},
  ) {
    await user.type(screen.getByLabelText('Kullanıcı adı'), 'yeni_ogrenci')
    await user.type(screen.getByLabelText('E-posta'), 'yeni@example.com')
    await user.type(screen.getByLabelText('Parola', { exact: true }), password)
    await user.type(screen.getByLabelText('Parola (tekrar)'), confirm)
    await user.click(screen.getByRole('button', { name: 'Kayıt ol' }))
  }

  it.each([
    ['sadeceharf', 'Parola en az bir harf ve bir rakam içermelidir.'],
    ['Kisa1', 'Parola 8 ile 64 karakter arasında olmalıdır.'],
    [`${'ğ'.repeat(36)}1`, 'Parola çok uzun'],
  ])('applies the backend password policy to %s', async (password, message) => {
    const { user } = renderRoutes(routes, '/kayit')
    await fillForm(user, { password })
    expect(await screen.findByText(new RegExp(message))).toBeInTheDocument()
  })

  it('requires matching passwords', async () => {
    const { user } = renderRoutes(routes, '/kayit')
    await fillForm(user, { confirm: 'Parola124' })
    expect(await screen.findByText('Parolalar eşleşmiyor.')).toBeInTheDocument()
  })

  it('shows 409 conflicts from the API', async () => {
    server.use(
      http.post(api('/api/auth/register'), () =>
        HttpResponse.json(
          { status: 409, detail: 'Bu kullanıcı adı veya e-posta adresi zaten kayıtlı.' },
          { status: 409, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    )
    const { user } = renderRoutes(routes, '/kayit')
    await fillForm(user)
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Bu kullanıcı adı veya e-posta adresi zaten kayıtlı.',
    )
  })

  it('maps server field errors to the matching input', async () => {
    server.use(
      http.post(api('/api/auth/register'), () =>
        HttpResponse.json(
          { status: 400, errors: { Email: ['Geçerli bir e-posta adresi girin.'] } },
          { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    )
    const { user } = renderRoutes(routes, '/kayit')
    await fillForm(user)
    const email = screen.getByLabelText('E-posta')
    await waitFor(() => expect(email).toHaveAttribute('aria-invalid', 'true'))
    expect(email).toHaveAccessibleDescription('Geçerli bir e-posta adresi girin.')
  })

  it('signs the new user in automatically', async () => {
    const calls: string[] = []
    server.use(
      http.post(api('/api/auth/register'), async ({ request }) => {
        calls.push(`register:${JSON.stringify(await request.json())}`)
        return HttpResponse.json(loginResponse().user, { status: 201 })
      }),
      http.post(api('/api/auth/login'), () => {
        calls.push('login')
        return HttpResponse.json(loginResponse('User', 7, 'yeni_ogrenci'))
      }),
    )
    const { router, user } = renderRoutes(routes, '/kayit')
    await fillForm(user)

    expect(await screen.findByRole('heading', { name: 'Ana sayfa' })).toBeInTheDocument()
    expect(currentLocation(router)).toBe('/')
    expect(calls).toEqual([
      'register:{"username":"yeni_ogrenci","email":"yeni@example.com","password":"Parola123"}',
      'login',
    ])
  })
})
