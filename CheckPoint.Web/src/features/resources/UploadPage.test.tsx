import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { server } from '../../test/server'
import { api, currentLocation, makeResource, renderRoutes, signInAs } from '../../test/utils'
import { UploadPage } from './UploadPage'

const CRLF = '\r\n'
let uploads: string[]

beforeEach(() => {
  signInAs('User')
  uploads = []
  server.use(
    http.get(api('/api/universities'), () =>
      HttpResponse.json({
        items: [{ id: 1, name: 'Pamukkale Üniversitesi', city: 'Denizli' }],
        page: 1,
        pageSize: 100,
        totalCount: 1,
      }),
    ),
    http.get(api('/api/courses'), () =>
      HttpResponse.json({
        items: [
          {
            id: 2,
            name: 'Veri Yapıları',
            code: 'BLG201',
            universityId: 1,
            universityName: 'Pamukkale',
          },
        ],
        page: 1,
        pageSize: 100,
        totalCount: 1,
      }),
    ),
    http.post(api('/api/resources'), async ({ request }) => {
      uploads.push(await request.text())
      return HttpResponse.json(makeResource({ id: 42 }), { status: 201 })
    }),
  )
})

function renderUpload() {
  const result = renderRoutes([{ path: '/yukle', element: <UploadPage /> }], '/yukle')
  return { ...result, user: userEvent.setup({ applyAccept: false }) }
}

async function fillValidFields(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText('Başlık'), 'Vize notları')
  await screen.findByRole('option', { name: 'Pamukkale Üniversitesi' })
  await user.selectOptions(screen.getByLabelText('Üniversite'), 'Pamukkale Üniversitesi')
  const course = screen.getByLabelText('Ders')
  await waitFor(() => expect(course).toBeEnabled())
  await user.selectOptions(course, 'BLG201 — Veri Yapıları')
  await user.selectOptions(screen.getByLabelText('Tür'), 'Ders Notu')
}

const fileInput = () => screen.getByLabelText('Dosya')
const submit = (user: ReturnType<typeof userEvent.setup>) =>
  user.click(screen.getByRole('button', { name: 'Yükle' }))

describe('UploadPage', () => {
  it('requires every field before sending anything', async () => {
    const { user } = renderUpload()
    await submit(user)

    for (const message of [
      'Başlık zorunludur.',
      'Üniversite seçin.',
      'Ders seçin.',
      'Kaynak türünü seçin.',
      'Yüklenecek dosyayı seçin.',
    ]) {
      expect(await screen.findByText(message)).toBeInTheDocument()
    }
    expect(uploads).toHaveLength(0)
  })

  it.each([
    [new File(['MZ'], 'program.exe'), 'Bu dosya türü desteklenmiyor'],
    [new File([], 'bos.txt'), 'Boş dosya yüklenemez.'],
  ])('rejects %s on the client', async (file, message) => {
    const { user } = renderUpload()
    await fillValidFields(user)
    await user.upload(fileInput(), file)
    await submit(user)

    await waitFor(() =>
      expect(fileInput()).toHaveAccessibleDescription(expect.stringContaining(message) as string),
    )
    expect(uploads).toHaveLength(0)
  })

  it('rejects files over 20 MB on the client', async () => {
    const { user } = renderUpload()
    await fillValidFields(user)
    const big = new File(['x'], 'buyuk.txt')
    Object.defineProperty(big, 'size', { value: 20 * 1024 * 1024 + 1 })
    await user.upload(fileInput(), big)
    await submit(user)

    await waitFor(() =>
      expect(fileInput()).toHaveAccessibleDescription(
        expect.stringContaining('20 MB sınırını aşıyor') as string,
      ),
    )
    expect(uploads).toHaveLength(0)
  })

  it('uploads valid files with the expected content type and opens the new resource', async () => {
    const { router, user } = renderUpload()
    await fillValidFields(user)
    await user.type(screen.getByLabelText('Açıklama (isteğe bağlı)'), '1-4. haftalar')
    await user.upload(fileInput(), new File(['slayt'], 'Sunum.pptx', { type: '' }))
    await submit(user)

    await waitFor(() => expect(currentLocation(router)).toBe('/kaynaklar/42'))
    const body = uploads[0]
    for (const [name, value] of [
      ['Title', 'Vize notları'],
      ['Description', '1-4. haftalar'],
      ['UniversityId', '1'],
      ['CourseId', '2'],
      ['Type', 'LectureNote'],
    ]) {
      expect(body).toContain(`name="${name}"${CRLF}${CRLF}${value}${CRLF}`)
    }
    expect(body).toContain(
      [
        'name="File"; filename="Sunum.pptx"',
        'Content-Type: application/vnd.openxmlformats-officedocument.presentationml.presentation',
        '',
        'slayt',
      ].join(CRLF),
    )
  })

  it('shows server-side 415 errors in Turkish', async () => {
    server.use(
      http.post(api('/api/resources'), () =>
        HttpResponse.json(
          { status: 415, detail: 'Dosya içeriği, belirtilen dosya türüyle eşleşmiyor.' },
          { status: 415, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    )
    const { user } = renderUpload()
    await fillValidFields(user)
    await user.upload(fileInput(), new File(['metin'], 'sahte.pdf'))
    await submit(user)

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Dosya içeriği, belirtilen dosya türüyle eşleşmiyor.',
    )
    expect(screen.getByRole('button', { name: 'Yükle' })).toBeEnabled()
  })
})
