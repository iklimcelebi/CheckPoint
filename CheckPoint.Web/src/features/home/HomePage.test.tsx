import { screen, within } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import type { PagedResponse, PopularCourse, University } from '../../api/types'
import { server } from '../../test/server'
import { api, currentLocation, makeResource, renderRoutes, signInAs } from '../../test/utils'
import { HomePage } from './HomePage'

const page = <T,>(items: T[]): PagedResponse<T> => ({
  items,
  page: 1,
  pageSize: 100,
  totalCount: items.length,
})

const universities: University[] = [
  { id: 1, name: 'Pamukkale Üniversitesi', city: 'Denizli', resourceCount: 2 },
  { id: 3, name: 'Orta Doğu Teknik Üniversitesi', city: 'Ankara', resourceCount: 5 },
]

const popular: PopularCourse[] = [
  {
    id: 7,
    name: 'Kalkülüs I',
    code: 'MATH119',
    universityId: 3,
    universityName: 'Orta Doğu Teknik Üniversitesi',
    resourceCount: 3,
    viewCount: 42,
  },
]

beforeEach(() => {
  signInAs('User')
  server.use(
    http.get(api('/api/universities'), () => HttpResponse.json(page(universities))),
    http.get(api('/api/courses'), () =>
      HttpResponse.json(
        page([
          {
            id: 7,
            name: 'Kalkülüs I',
            code: 'MATH119',
            universityId: 3,
            universityName: 'Orta Doğu Teknik Üniversitesi',
          },
        ]),
      ),
    ),
    http.get(api('/api/courses/popular'), () => HttpResponse.json(popular)),
    http.get(api('/api/resources'), () =>
      HttpResponse.json(page([makeResource({ title: 'En yeni not' })])),
    ),
  )
})

const render = () =>
  renderRoutes(
    [
      { path: '/', element: <HomePage /> },
      { path: '/kaynaklar', element: <h1>Keşfet sayfası</h1> },
    ],
    '/',
  )

describe('HomePage', () => {
  it('shows popular courses, latest resources and universities by resource count', async () => {
    render()

    const popularSection = await screen.findByRole('region', { name: 'Popüler dersler' })
    expect(await within(popularSection).findByText('MATH119 · Kalkülüs I')).toBeInTheDocument()
    expect(within(popularSection).getByText('3 kaynak · 42 görüntülenme')).toBeInTheDocument()

    expect(await screen.findByText('En yeni not')).toBeInTheDocument()

    const universitySection = screen.getByRole('region', { name: 'Üniversitelere göre kaynaklar' })
    const links = await within(universitySection).findAllByRole('link')
    expect(links.map((link) => link.textContent)).toEqual([
      'Orta Doğu Teknik ÜniversitesiAnkara5 kaynak',
      'Pamukkale ÜniversitesiDenizli2 kaynak',
    ])
    expect(links[0]).toHaveAttribute('href', '/kaynaklar?universite=3')
  })

  it('opens the explore page with the chosen filters', async () => {
    const { router, user } = render()
    const filters = screen.getByRole('form', { name: 'Kaynak filtreleri' })

    await screen.findByRole('option', { name: 'Orta Doğu Teknik Üniversitesi' })
    await user.selectOptions(within(filters).getByLabelText('Üniversite'), '3')
    await screen.findByRole('option', { name: 'MATH119 — Kalkülüs I' })
    await user.selectOptions(within(filters).getByLabelText('Ders'), '7')
    await user.selectOptions(within(filters).getByLabelText('Tür'), 'PastExamQuestion')
    await user.click(within(filters).getByRole('button', { name: 'Kaynakları göster' }))

    expect(await screen.findByRole('heading', { name: 'Keşfet sayfası' })).toBeInTheDocument()
    expect(currentLocation(router)).toBe('/kaynaklar?universite=3&ders=7&tur=PastExamQuestion')
  })
})
