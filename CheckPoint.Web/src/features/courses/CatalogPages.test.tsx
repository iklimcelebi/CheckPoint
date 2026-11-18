import { screen, within } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import type { Course } from '../../api/types'
import { server } from '../../test/server'
import { api, renderRoutes, signInAs } from '../../test/utils'
import { UniversitiesPage } from '../universities/UniversitiesPage'
import { CoursesPage } from './CoursesPage'

const paged = <T,>(items: T[]) => ({ items, page: 1, pageSize: 100, totalCount: items.length })

const course: Course = {
  id: 7,
  name: 'Kalkülüs I',
  code: 'MATH119',
  universityId: 3,
  universityName: 'ODTÜ',
}

let universityRequests: unknown[]

beforeEach(() => {
  universityRequests = []
  server.use(
    http.get(api('/api/universities'), () =>
      HttpResponse.json(paged([{ id: 3, name: 'ODTÜ', city: 'Ankara', resourceCount: 1 }])),
    ),
    http.get(api('/api/courses'), () => HttpResponse.json(paged([course]))),
    http.delete(api('/api/courses/7'), () =>
      HttpResponse.json(
        {
          status: 409,
          title: 'Çakışma',
          detail: 'Bu derse bağlı kaynaklar olduğu için silinemez.',
        },
        { status: 409, headers: { 'Content-Type': 'application/problem+json' } },
      ),
    ),
    http.post(api('/api/universities'), async ({ request }) => {
      universityRequests.push(await request.json())
      return HttpResponse.json(
        { id: 9, name: 'Yeni Üniversite', city: 'Erzurum', resourceCount: 0 },
        { status: 201 },
      )
    }),
  )
})

describe('CoursesPage', () => {
  it('lets regular users add courses but not edit or delete them', async () => {
    signInAs('User')
    renderRoutes([{ path: '/dersler', element: <CoursesPage /> }], '/dersler')

    expect(await screen.findByRole('link', { name: 'Kalkülüs I' })).toHaveAttribute(
      'href',
      '/kaynaklar?universite=3&ders=7',
    )
    expect(screen.getByRole('button', { name: 'Ders ekle' })).toBeInTheDocument()
    expect(screen.queryByRole('columnheader', { name: 'İşlemler' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /düzenle$/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /sil$/ })).not.toBeInTheDocument()
  })

  it('shows edit and delete actions to admins and reports a delete conflict', async () => {
    signInAs('Admin', 99, 'yonetici')
    const { user } = renderRoutes([{ path: '/dersler', element: <CoursesPage /> }], '/dersler')

    expect(
      await screen.findByRole('button', { name: 'MATH119 Kalkülüs I düzenle' }),
    ).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'MATH119 Kalkülüs I sil' }))
    const dialog = screen.getByRole('dialog', { name: 'Dersi sil' })
    await user.click(within(dialog).getByRole('button', { name: 'Sil' }))

    expect(await within(dialog).findByRole('alert')).toHaveTextContent(
      'Bu derse bağlı kaynaklar olduğu için silinemez.',
    )
  })
})

describe('UniversitiesPage', () => {
  it('picks the city from the list of 81 provinces', async () => {
    signInAs('Admin', 99, 'yonetici')
    const { user } = renderRoutes(
      [{ path: '/universiteler', element: <UniversitiesPage /> }],
      '/universiteler',
    )

    expect(await screen.findByRole('link', { name: 'ODTÜ' })).toHaveAttribute(
      'href',
      '/kaynaklar?universite=3',
    )
    await user.click(screen.getByRole('button', { name: 'Üniversite ekle' }))
    const dialog = screen.getByRole('dialog', { name: 'Üniversite ekle' })
    const city = within(dialog).getByLabelText('İl')
    expect(within(city).getAllByRole('option')).toHaveLength(82)

    await user.type(within(dialog).getByLabelText('Üniversite adı'), 'Yeni Üniversite')
    await user.click(within(dialog).getByRole('button', { name: 'Kaydet' }))
    expect(await within(dialog).findByText('Geçerli bir il seçin.')).toBeInTheDocument()
    expect(universityRequests).toHaveLength(0)

    await user.selectOptions(city, 'Erzurum')
    await user.click(within(dialog).getByRole('button', { name: 'Kaydet' }))
    expect(await screen.findByText('"Yeni Üniversite" eklendi.')).toBeInTheDocument()
    expect(universityRequests).toEqual([{ name: 'Yeni Üniversite', city: 'Erzurum' }])
  })
})
