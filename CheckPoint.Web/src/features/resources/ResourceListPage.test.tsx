import { screen, waitFor, within } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import type { Course, PagedResponse, Resource } from '../../api/types'
import { server } from '../../test/server'
import { api, currentLocation, makeResource, renderRoutes, signInAs } from '../../test/utils'
import { ResourceListPage } from './ResourceListPage'

const page = <T,>(items: T[], totalCount = items.length): PagedResponse<T> => ({
  items,
  page: 1,
  pageSize: 10,
  totalCount,
})

const COURSES: Record<string, Course[]> = {
  '1': [
    { id: 2, name: 'Veri Yapıları', code: 'BLG201', universityId: 1, universityName: 'Pamukkale' },
  ],
  '3': [{ id: 7, name: 'Kalkülüs I', code: 'MATH119', universityId: 3, universityName: 'ODTÜ' }],
}

let resourceRequests: URLSearchParams[]
let resources: Resource[]

beforeEach(() => {
  signInAs('User')
  resourceRequests = []
  resources = [
    makeResource(),
    makeResource({
      id: 11,
      title: 'Final soruları',
      type: 'PastExamQuestion',
      fileSize: 5 * 1024 * 1024,
    }),
  ]
  server.use(
    http.get(api('/api/universities'), () =>
      HttpResponse.json(
        page([
          { id: 1, name: 'Pamukkale Üniversitesi', city: 'Denizli' },
          { id: 3, name: 'Orta Doğu Teknik Üniversitesi', city: 'Ankara' },
        ]),
      ),
    ),
    http.get(api('/api/courses'), ({ request }) => {
      const universityId = new URL(request.url).searchParams.get('universityId') ?? ''
      return HttpResponse.json(page(COURSES[universityId] ?? []))
    }),
    http.get(api('/api/resources'), ({ request }) => {
      resourceRequests.push(new URL(request.url).searchParams)
      return HttpResponse.json(page(resources))
    }),
  )
})

const render = (entry = '/kaynaklar') =>
  renderRoutes([{ path: '/kaynaklar', element: <ResourceListPage /> }], entry)

const lastRequest = () => resourceRequests.at(-1)

describe('ResourceListPage', () => {
  it('shows resource cards with Turkish labels and readable sizes', async () => {
    render()

    const cards = await screen.findAllByRole('article')
    expect(cards).toHaveLength(2)
    expect(within(cards[0]).getByText('Ders Notu')).toBeInTheDocument()
    expect(cards[0]).toHaveTextContent('notlar.pdf (2 KB)')
    expect(cards[0]).toHaveTextContent('Pamukkale Üniversitesi · BLG201 Veri Yapıları')
    expect(cards[0]).toHaveTextContent('15 Kasım 2026 12:30')
    expect(within(cards[1]).getByText('Geçmiş Sınav Sorusu')).toBeInTheDocument()
    expect(cards[1]).toHaveTextContent('(5 MB)')
    expect(screen.getByText('2 kaynak bulundu.')).toBeInTheDocument()
  })

  it('loads courses for the selected university and resets them when it changes', async () => {
    const { router, user } = render()
    const course = screen.getByLabelText('Ders')
    expect(course).toBeDisabled()

    await screen.findByRole('option', { name: 'Pamukkale Üniversitesi' })
    await user.selectOptions(screen.getByLabelText('Üniversite'), 'Pamukkale Üniversitesi')
    await waitFor(() => expect(course).toBeEnabled())
    await user.selectOptions(course, 'BLG201 — Veri Yapıları')
    await waitFor(() => expect(lastRequest()?.get('courseId')).toBe('2'))
    expect(lastRequest()?.get('universityId')).toBe('1')
    expect(currentLocation(router)).toBe('/kaynaklar?universite=1&ders=2')

    await user.selectOptions(screen.getByLabelText('Üniversite'), 'Orta Doğu Teknik Üniversitesi')
    await waitFor(() => expect(lastRequest()?.get('universityId')).toBe('3'))
    expect(lastRequest()?.get('courseId')).toBeNull()
    expect(currentLocation(router)).toBe('/kaynaklar?universite=3')
    expect(await screen.findByRole('option', { name: 'MATH119 — Kalkülüs I' })).toBeInTheDocument()
  })

  it('filters by type and debounced search text', async () => {
    const { router, user } = render()

    await user.selectOptions(screen.getByLabelText('Tür'), 'Sunum')
    await waitFor(() => expect(lastRequest()?.get('type')).toBe('Presentation'))

    await user.type(screen.getByLabelText('Ara'), 'vize')
    await waitFor(() => expect(lastRequest()?.get('q')).toBe('vize'))
    expect(resourceRequests.filter((params) => params.get('q') === 'v')).toHaveLength(0)
    expect(currentLocation(router)).toBe('/kaynaklar?tur=Presentation&q=vize')
  })

  it('restores filters from the URL', async () => {
    render('/kaynaklar?universite=1&tur=StudyQuestion&q=sql&sayfa=2')

    await waitFor(() => expect(lastRequest()).toBeDefined())
    expect(Object.fromEntries(lastRequest() ?? [])).toMatchObject({
      universityId: '1',
      type: 'StudyQuestion',
      q: 'sql',
      page: '2',
    })
    expect(screen.getByLabelText('Ara')).toHaveValue('sql')
  })

  it('shows an empty state and lets the user clear filters', async () => {
    resources = []
    const { router, user } = render('/kaynaklar?tur=Presentation')

    expect(await screen.findByText('Filtrelere uyan kaynak bulunamadı.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Filtreleri temizle' }))
    expect(currentLocation(router)).toBe('/kaynaklar')
  })

  it('shows an error with a retry button', async () => {
    server.use(
      http.get(api('/api/resources'), () =>
        HttpResponse.json(
          { status: 503, detail: 'Hizmet şu anda kullanılamıyor.' },
          { status: 503, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    )
    render()

    expect(await screen.findByRole('alert')).toHaveTextContent('Hizmet şu anda kullanılamıyor.')
    expect(screen.getByRole('button', { name: 'Tekrar dene' })).toBeInTheDocument()
  })
})
