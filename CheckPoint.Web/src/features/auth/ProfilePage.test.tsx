import { screen, within } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { server } from '../../test/server'
import {
  api,
  currentLocation,
  loginResponse,
  makeStats,
  renderRoutes,
  signInAs,
} from '../../test/utils'
import { ChangePasswordPage } from './ChangePasswordPage'
import { ProfilePage } from './ProfilePage'

let requests: unknown[]

beforeEach(() => {
  signInAs('Admin', 99, 'yonetici')
  requests = []
  server.use(
    http.get(api('/api/users/me'), () =>
      HttpResponse.json(loginResponse('Admin', 99, 'yonetici').user),
    ),
    http.get(api('/api/users/me/stats'), () =>
      HttpResponse.json(
        makeStats({
          resourceCount: 3,
          totalViews: 42,
          favoritesReceived: 5,
          favoriteCount: 2,
          resourcesByType: [
            { type: 'LectureNote', count: 2 },
            { type: 'PastExamQuestion', count: 1 },
          ],
          topResources: [{ id: 10, title: 'En popüler not', viewCount: 30 }],
        }),
      ),
    ),
    http.put(api('/api/users/me/password'), async ({ request }) => {
      requests.push(await request.json())
      return new HttpResponse(null, { status: 204 })
    }),
  )
})

function render(entry = '/profil') {
  return renderRoutes(
    [
      { path: '/profil', element: <ProfilePage /> },
      { path: '/profil/parola', element: <ChangePasswordPage /> },
    ],
    entry,
  )
}

describe('ProfilePage', () => {
  it('shows account details and statistics without the role', async () => {
    render()
    expect(await screen.findByText('yonetici')).toBeInTheDocument()
    expect(screen.getByText('yonetici@example.com')).toBeInTheDocument()
    expect(screen.queryByText('Yönetici')).not.toBeInTheDocument()
    expect(screen.queryByText('Rol')).not.toBeInTheDocument()

    const tiles = await screen.findAllByRole('definition')
    expect(tiles.map((tile) => tile.textContent)).toEqual(
      expect.arrayContaining(['3', '42', '5', '2']),
    )
    expect(screen.getByRole('listitem', { name: 'Ders Notu: 2' })).toBeInTheDocument()
    expect(screen.getByRole('listitem', { name: 'Geçmiş Sınav Sorusu: 1' })).toBeInTheDocument()
    const top = screen.getByRole('region', { name: 'En çok görüntülenen kaynaklarım' })
    expect(within(top).getByRole('link', { name: 'En popüler not' })).toHaveAttribute(
      'href',
      '/kaynaklar/10',
    )
    expect(screen.queryByLabelText('Mevcut parola')).not.toBeInTheDocument()
  })

  it('opens the password page from a button', async () => {
    const { router, user } = render()
    await user.click(await screen.findByRole('link', { name: 'Parolayı değiştir' }))
    expect(await screen.findByLabelText('Mevcut parola')).toBeInTheDocument()
    expect(currentLocation(router)).toBe('/profil/parola')
  })
})

async function fill(
  user: ReturnType<typeof render>['user'],
  values: { current: string; next: string; confirm?: string },
) {
  await user.type(screen.getByLabelText('Mevcut parola'), values.current)
  await user.type(screen.getByLabelText('Yeni parola'), values.next)
  await user.type(screen.getByLabelText('Yeni parola (tekrar)'), values.confirm ?? values.next)
  await user.click(screen.getByRole('button', { name: 'Parolayı değiştir' }))
}

describe('ChangePasswordPage', () => {
  it('changes the password and clears the form', async () => {
    const { user } = render('/profil/parola')
    await fill(user, { current: 'EskiParola1', next: 'YeniParola2' })

    expect(await screen.findByText(/Parolanız değiştirildi/)).toBeInTheDocument()
    expect(requests).toEqual([{ currentPassword: 'EskiParola1', newPassword: 'YeniParola2' }])
    expect(screen.getByLabelText('Mevcut parola')).toHaveValue('')
  })

  it.each([
    [
      { current: 'EskiParola1', next: 'sadeceharf' },
      'Parola en az bir harf ve bir rakam içermelidir.',
    ],
    [{ current: 'EskiParola1', next: 'YeniParola2', confirm: 'Baska123' }, 'Parolalar eşleşmiyor.'],
    [
      { current: 'AyniParola1', next: 'AyniParola1' },
      'Yeni parola mevcut paroladan farklı olmalıdır.',
    ],
  ])('validates %o on the client', async (values, message) => {
    const { user } = render('/profil/parola')
    await fill(user, values)

    expect(await screen.findByText(message)).toBeInTheDocument()
    expect(requests).toHaveLength(0)
  })

  it('shows a wrong current password under the field', async () => {
    server.use(
      http.put(api('/api/users/me/password'), () =>
        HttpResponse.json(
          { status: 400, errors: { CurrentPassword: ['Mevcut parola hatalı.'] } },
          { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    )
    const { user } = render('/profil/parola')
    await fill(user, { current: 'YanlisParola1', next: 'YeniParola2' })

    const field = screen.getByLabelText('Mevcut parola')
    expect(await screen.findByText('Mevcut parola hatalı.')).toBeInTheDocument()
    expect(field).toHaveAttribute('aria-invalid', 'true')
    expect(screen.queryByText(/Parolanız değiştirildi/)).not.toBeInTheDocument()
  })

  it('links back to the profile', async () => {
    const { router, user } = render('/profil/parola')
    await user.click(screen.getByRole('link', { name: '← Profile dön' }))
    expect(await screen.findByText('yonetici@example.com')).toBeInTheDocument()
    expect(currentLocation(router)).toBe('/profil')
  })
})
