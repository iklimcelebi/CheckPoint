import { screen, waitFor, within } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import type { Role } from '../../api/types'
import { server } from '../../test/server'
import { api, currentLocation, makeResource, renderRoutes, signInAs } from '../../test/utils'
import { ResourceDetailPage } from './ResourceDetailPage'
import { ResourceListPage } from './ResourceListPage'

const resource = makeResource({ id: 10, uploadedBy: { id: 1, username: 'ayse' } })

beforeEach(() => {
  server.use(
    http.get(api('/api/resources/10'), () => HttpResponse.json(resource)),
    http.get(api('/api/universities'), () =>
      HttpResponse.json({ items: [], page: 1, pageSize: 100, totalCount: 0 }),
    ),
    http.get(api('/api/resources'), () =>
      HttpResponse.json({ items: [], page: 1, pageSize: 10, totalCount: 0 }),
    ),
  )
})

const render = () =>
  renderRoutes(
    [
      { path: '/kaynaklar/:id', element: <ResourceDetailPage /> },
      { path: '/kaynaklar', element: <ResourceListPage /> },
    ],
    '/kaynaklar/10',
  )

describe('ResourceDetailPage role-based actions', () => {
  it.each<[string, Role, number, boolean]>([
    ['the owner', 'User', 1, true],
    ['another user', 'User', 2, false],
    ['an admin', 'Admin', 99, true],
  ])('for %s shows edit/delete: %s', async (_, role, id, canManage) => {
    signInAs(role, id, `kullanici${id}`)
    render()

    expect(await screen.findByRole('heading', { name: resource.title })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'İndir' })).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Düzenle' }) !== null).toBe(canManage)
    expect(screen.queryByRole('button', { name: 'Sil' }) !== null).toBe(canManage)
  })

  it('deletes after confirmation and returns to the list with a notice', async () => {
    signInAs('User', 1)
    let deleted = false
    server.use(
      http.delete(api('/api/resources/10'), () => {
        deleted = true
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const { router, user } = render()

    await user.click(await screen.findByRole('button', { name: 'Sil' }))
    const dialog = screen.getByRole('dialog', { name: 'Kaynağı sil' })
    await user.click(within(dialog).getByRole('button', { name: 'Vazgeç' }))
    expect(deleted).toBe(false)

    await user.click(screen.getByRole('button', { name: 'Sil' }))
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Sil' }))

    await waitFor(() => expect(currentLocation(router)).toBe('/kaynaklar'))
    expect(deleted).toBe(true)
    expect(await screen.findByText('Kaynak silindi.')).toBeInTheDocument()
  })

  it('keeps the dialog open and shows 403 errors', async () => {
    signInAs('Admin', 99)
    server.use(
      http.delete(api('/api/resources/10'), () =>
        HttpResponse.json(
          { status: 403, detail: 'Bu kaynağı silme yetkiniz yok.' },
          { status: 403, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    )
    const { user } = render()

    await user.click(await screen.findByRole('button', { name: 'Sil' }))
    const dialog = screen.getByRole('dialog')
    await user.click(within(dialog).getByRole('button', { name: 'Sil' }))
    expect(await within(dialog).findByRole('alert')).toHaveTextContent(
      'Bu kaynağı silme yetkiniz yok.',
    )
  })

  it('downloads the file with the name from Content-Disposition', async () => {
    signInAs('User', 2)
    server.use(
      http.get(api('/api/resources/10/download'), () =>
        HttpResponse.text('%PDF-1.4', {
          headers: {
            'Content-Type': 'application/pdf',
            'Content-Disposition':
              "attachment; filename=Calisma.pdf; filename*=UTF-8''%C3%87al%C4%B1%C5%9Fma.pdf",
            'Access-Control-Expose-Headers': 'Content-Disposition',
          },
        }),
      ),
    )
    const clicked: string[] = []
    HTMLAnchorElement.prototype.click = function click(this: HTMLAnchorElement) {
      clicked.push(this.download)
    }
    const { user } = render()

    await user.click(await screen.findByRole('button', { name: 'İndir' }))
    await waitFor(() => expect(clicked).toEqual(['Çalışma.pdf']))
  })

  it('reports a missing file', async () => {
    signInAs('User', 2)
    server.use(
      http.get(api('/api/resources/10/download'), () =>
        HttpResponse.json(
          { status: 404, detail: 'Kaynak dosyası bulunamadı.' },
          { status: 404, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    )
    const { user } = render()

    await user.click(await screen.findByRole('button', { name: 'İndir' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Kaynak dosyası bulunamadı.')
  })

  it('shows a not-found page for unknown resources', async () => {
    signInAs('User', 2)
    server.use(
      http.get(api('/api/resources/10'), () =>
        HttpResponse.json(
          { status: 404, detail: 'Kaynak bulunamadı.' },
          { status: 404, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    )
    render()
    expect(await screen.findByRole('heading', { name: 'Kaynak bulunamadı' })).toBeInTheDocument()
  })
})
