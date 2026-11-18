import { screen, within } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import type { AdminUser } from '../../api/types'
import { server } from '../../test/server'
import { api, renderRoutes, signInAs } from '../../test/utils'
import { AdminPage } from './AdminPage'

const paged = <T,>(items: T[]) => ({ items, page: 1, pageSize: 10, totalCount: items.length })

let users: AdminUser[]
let deletedUsers: string[]

beforeEach(() => {
  signInAs('Admin', 99, 'yonetici')
  deletedUsers = []
  users = [
    {
      id: 99,
      username: 'yonetici',
      email: 'yonetici@example.com',
      role: 'Admin',
      createdAt: '2026-11-01T10:00:00Z',
      resourceCount: 0,
    },
    {
      id: 5,
      username: 'ogrenci',
      email: 'ogrenci@example.com',
      role: 'User',
      createdAt: '2026-11-02T10:00:00Z',
      resourceCount: 3,
    },
  ]
  server.use(
    http.get(api('/api/universities'), () => HttpResponse.json(paged([]))),
    http.get(api('/api/resources'), () => HttpResponse.json(paged([]))),
    http.get(api('/api/users'), () => HttpResponse.json(paged(users))),
    http.get(api('/api/courses'), () => HttpResponse.json(paged([]))),
    http.delete(api('/api/users/:id'), ({ params }) => {
      deletedUsers.push(String(params.id))
      users = users.filter((user) => String(user.id) !== params.id)
      return new HttpResponse(null, { status: 204 })
    }),
  )
})

const render = (entry = '/yonetim') =>
  renderRoutes([{ path: '/yonetim', element: <AdminPage /> }], entry)

describe('AdminPage', () => {
  it('deletes a regular user after confirmation but protects admin accounts', async () => {
    const { user } = render()

    const row = (await screen.findByRole('cell', { name: 'ogrenci' })).closest('tr')!
    const adminRow = screen.getByRole('cell', { name: 'yonetici' }).closest('tr')!
    expect(within(adminRow).queryByRole('button')).not.toBeInTheDocument()
    expect(within(adminRow).getByText('Silinemez')).toBeInTheDocument()

    await user.click(within(row).getByRole('button', { name: 'ogrenci hesabını sil' }))
    const dialog = screen.getByRole('dialog', { name: 'Hesabı sil' })
    expect(dialog).toHaveTextContent('yüklediği 3 kaynak')
    await user.click(within(dialog).getByRole('button', { name: 'Hesabı sil' }))

    expect(await screen.findByText('"ogrenci" hesabı silindi.')).toBeInTheDocument()
    expect(deletedUsers).toEqual(['5'])
    expect(screen.queryByRole('cell', { name: 'ogrenci' })).not.toBeInTheDocument()
  })
})
