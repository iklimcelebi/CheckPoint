import { screen } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { server } from '../../test/server'
import { api, loginResponse, renderRoutes, signInAs } from '../../test/utils'
import { ProfilePage } from './ProfilePage'

let requests: unknown[]

beforeEach(() => {
  signInAs('Admin', 99, 'yonetici')
  requests = []
  server.use(
    http.get(api('/api/users/me'), () =>
      HttpResponse.json(loginResponse('Admin', 99, 'yonetici').user),
    ),
    http.put(api('/api/users/me/password'), async ({ request }) => {
      requests.push(await request.json())
      return new HttpResponse(null, { status: 204 })
    }),
  )
})

function render() {
  return renderRoutes([{ path: '/profil', element: <ProfilePage /> }], '/profil')
}

async function fill(
  user: ReturnType<typeof render>['user'],
  values: { current: string; next: string; confirm?: string },
) {
  await user.type(screen.getByLabelText('Mevcut parola'), values.current)
  await user.type(screen.getByLabelText('Yeni parola'), values.next)
  await user.type(screen.getByLabelText('Yeni parola (tekrar)'), values.confirm ?? values.next)
  await user.click(screen.getByRole('button', { name: 'Parolayı değiştir' }))
}

describe('ProfilePage password change', () => {
  it('shows the profile with a Turkish role label', async () => {
    render()
    expect(await screen.findByText('yonetici')).toBeInTheDocument()
    expect(screen.getByText('Yönetici')).toBeInTheDocument()
  })

  it('changes the password and clears the form', async () => {
    const { user } = render()
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
    const { user } = render()
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
    const { user } = render()
    await fill(user, { current: 'YanlisParola1', next: 'YeniParola2' })

    const field = screen.getByLabelText('Mevcut parola')
    expect(await screen.findByText('Mevcut parola hatalı.')).toBeInTheDocument()
    expect(field).toHaveAttribute('aria-invalid', 'true')
    expect(screen.queryByText(/Parolanız değiştirildi/)).not.toBeInTheDocument()
  })
})
