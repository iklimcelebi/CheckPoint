import { expect, test } from '@playwright/test'
import {
  admin,
  API_URL,
  apiCreateCatalog,
  apiRegister,
  apiToken,
  apiUpload,
  login,
  newUser,
  register,
  uniqueSuffix,
} from './support'

test("another user cannot edit or delete someone else's resource", async ({ page, request }) => {
  const catalog = await apiCreateCatalog(request)
  const owner = newUser('sahip')
  const ownerToken = await apiRegister(request, owner)
  const title = `Sahibin kaynağı ${uniqueSuffix()}`
  const resourceId = await apiUpload(request, ownerToken, { ...catalog, title })

  const other = newUser('diger')
  const otherToken = await apiRegister(request, other)
  await login(page, other.email, other.password)

  await page.goto(`/kaynaklar/${resourceId}`)
  await expect(page.getByRole('heading', { name: title })).toBeVisible()
  await expect(page.getByRole('button', { name: 'İndir' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Düzenle' })).toHaveCount(0)
  await expect(page.getByRole('button', { name: 'Sil' })).toHaveCount(0)

  await page.goto(`/kaynaklar/${resourceId}/duzenle`)
  await expect(page.getByRole('heading', { name: 'Erişim reddedildi' })).toBeVisible()

  const forbidden = await request.delete(`${API_URL}/api/resources/${resourceId}`, {
    headers: { Authorization: `Bearer ${otherToken}` },
  })
  expect(forbidden.status()).toBe(403)
  expect(((await forbidden.json()) as { detail: string }).detail).toBe(
    'Bu kaynağı silme yetkiniz yok.',
  )
  const stillThere = await request.get(`${API_URL}/api/resources/${resourceId}`, {
    headers: { Authorization: `Bearer ${otherToken}` },
  })
  expect(stillThere.status()).toBe(200)

  await page.goto('/yonetim')
  await expect(page.getByRole('heading', { name: 'Erişim reddedildi' })).toBeVisible()
  await page.goto('/universiteler')
  await expect(page.getByRole('heading', { name: 'Üniversiteler' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Üniversite ekle' })).toHaveCount(0)
})

test('shows Turkish error messages for 401, 409, 415 and 404', async ({ page, request }) => {
  await page.goto('/kaynaklar')
  await expect(page).toHaveURL(/\/giris\?donus=%2Fkaynaklar$/)

  const student = newUser()
  await page.getByLabel('E-posta').fill(student.email)
  await page.getByLabel('Parola').fill('YanlisParola1')
  await page.getByRole('button', { name: 'Giriş yap' }).click()
  await expect(page.getByRole('alert')).toHaveText('E-posta veya parola hatalı.')

  await apiRegister(request, student)
  await register(page, { ...student, username: `${student.username}x` })
  await expect(page.getByRole('alert')).toHaveText(
    'Bu kullanıcı adı veya e-posta adresi zaten kayıtlı.',
  )

  const catalog = await apiCreateCatalog(request)
  await login(page, student.email, student.password)
  await page.getByRole('link', { name: 'Yükle', exact: true }).click()
  await page.getByLabel('Başlık').fill('Sahte PDF')
  await page
    .getByLabel('Üniversite', { exact: true })
    .selectOption({ label: catalog.universityName })
  await page.getByLabel('Ders', { exact: true }).selectOption({ index: 1 })
  await page.getByLabel('Tür', { exact: true }).selectOption({ label: 'Sunum' })
  await page.getByLabel('Dosya').setInputFiles({
    name: 'sunum.pdf',
    mimeType: 'application/pdf',
    buffer: Buffer.from('bu bir PDF değil'),
  })
  await page.getByRole('button', { name: 'Yükle', exact: true }).click()
  await expect(page.getByRole('alert')).toHaveText(
    'Dosya içeriği, belirtilen dosya türüyle eşleşmiyor.',
  )

  await page.goto('/olmayan-sayfa')
  await expect(page.getByRole('heading', { name: 'Sayfa bulunamadı' })).toBeVisible()
})

test('admin sees a 409 when deleting a university that has courses', async ({ page, request }) => {
  const catalog = await apiCreateCatalog(request)
  expect(await apiToken(request, admin.email, admin.password)).toBeTruthy()
  await login(page, admin.email, admin.password)
  await page.getByRole('link', { name: 'Üniversiteler' }).click()
  await page.getByLabel('Ara').fill(catalog.universityName)
  await page.getByRole('button', { name: `${catalog.universityName} sil` }).click()
  const dialog = page.getByRole('dialog', { name: 'Üniversiteyi sil' })
  await dialog.getByRole('button', { name: 'Sil' }).click()
  await expect(dialog.getByRole('alert')).toHaveText(
    'Bu üniversiteye bağlı dersler olduğu için silinemez.',
  )
})
