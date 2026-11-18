import { expect, test } from '@playwright/test'
import { readFile } from 'node:fs/promises'
import {
  admin,
  HOME_URL,
  login,
  logout,
  navLink,
  newUser,
  register,
  uniqueSuffix,
  userMenu,
} from './support'

test('register, manage catalog, upload, find, download, edit and delete a resource', async ({
  page,
}) => {
  const suffix = uniqueSuffix()
  const universityName = `E2E Üniversitesi ${suffix}`
  const courseCode = `E2E${suffix}`.toUpperCase().slice(0, 20)
  const title = `Uçtan uca ders notu ${suffix}`
  const fileContent = `Çalışma notları ${suffix}`

  await test.step('admin adds a university', async () => {
    await login(page, admin.email, admin.password)
    await navLink(page, 'Üniversiteler').click()
    await page.getByRole('button', { name: 'Üniversite ekle' }).click()
    const dialog = page.getByRole('dialog', { name: 'Üniversite ekle' })
    await dialog.getByLabel('Üniversite adı').fill(universityName)
    await dialog.getByLabel('İl').selectOption('İzmir')
    await dialog.getByRole('button', { name: 'Kaydet' }).click()
    await expect(page.getByText(`"${universityName}" eklendi.`)).toBeVisible()
    await logout(page)
  })

  await test.step('a new student registers and is signed in', async () => {
    const student = newUser()
    await register(page, student)
    await expect(page).toHaveURL(HOME_URL)
    await expect(userMenu(page, student.username)).toBeVisible()
  })

  await test.step('the student adds a course', async () => {
    await navLink(page, 'Dersler').click()
    await page.getByRole('button', { name: 'Ders ekle' }).click()
    const dialog = page.getByRole('dialog', { name: 'Ders ekle' })
    await dialog.getByLabel('Üniversite').selectOption({ label: universityName })
    await dialog.getByLabel('Ders adı').fill('Uçtan Uca Test Dersi')
    await dialog.getByLabel('Ders kodu').fill(courseCode.toLowerCase())
    await dialog.getByRole('button', { name: 'Kaydet' }).click()
    await expect(page.getByRole('cell', { name: courseCode, exact: true })).toBeVisible()
  })

  await test.step('the student uploads a file', async () => {
    await navLink(page, 'Kaynak Yükle').click()
    await page.getByLabel('Başlık').fill(title)
    await page.getByLabel('Açıklama (isteğe bağlı)').fill('Uçtan uca test açıklaması')
    await page.getByLabel('Üniversite', { exact: true }).selectOption({ label: universityName })
    await page
      .getByLabel('Ders', { exact: true })
      .selectOption({ label: `${courseCode} — Uçtan Uca Test Dersi` })
    await page.getByLabel('Tür', { exact: true }).selectOption({ label: 'Ders Notu' })
    await page.getByLabel('Dosya').setInputFiles({
      name: 'Çalışma Notları.txt',
      mimeType: 'text/plain',
      buffer: Buffer.from(fileContent),
    })
    await page.getByRole('button', { name: 'Yükle', exact: true }).click()
    await expect(page.getByText('Kaynak yüklendi.')).toBeVisible()
    await expect(page.getByRole('heading', { name: title })).toBeVisible()
  })

  await test.step('the resource is found with filters and search', async () => {
    await navLink(page, 'Üniversiteler').click()
    await page.getByLabel('Ara', { exact: true }).fill(universityName)
    await page.getByRole('link', { name: universityName, exact: true }).click()
    await expect(page.getByLabel('Üniversite', { exact: true })).toHaveValue(/\d+/)
    await expect(
      page.getByLabel('Üniversite', { exact: true }).locator('option:checked'),
    ).toHaveText(universityName)
    await page.getByLabel('Tür', { exact: true }).selectOption({ label: 'Ders Notu' })
    await page.getByLabel('Ara', { exact: true }).fill(`ders notu ${suffix}`)
    await expect(page).toHaveURL(new RegExp(`q=ders\\+notu\\+${suffix}`))
    await expect(page.getByText('1 kaynak bulundu.')).toBeVisible()
    await page.getByRole('link', { name: title }).click()
  })

  await test.step('the file downloads with its original name and content', async () => {
    const downloadPromise = page.waitForEvent('download')
    await page.getByRole('button', { name: 'İndir' }).click()
    const download = await downloadPromise
    expect(download.suggestedFilename()).toBe('Çalışma Notları.txt')
    expect(await readFile(await download.path(), 'utf8')).toBe(fileContent)
  })

  await test.step('the owner edits and deletes the resource', async () => {
    await page.getByRole('link', { name: 'Düzenle' }).click()
    await page.getByLabel('Tür', { exact: true }).selectOption({ label: 'Çalışma Sorusu' })
    await page.getByRole('button', { name: 'Kaydet' }).click()
    await expect(page.getByText('Değişiklikler kaydedildi.')).toBeVisible()
    await expect(page.getByText('Çalışma Sorusu')).toBeVisible()

    await page.getByRole('button', { name: 'Sil' }).click()
    await page
      .getByRole('dialog', { name: 'Kaynağı sil' })
      .getByRole('button', { name: 'Sil' })
      .click()
    await expect(page).toHaveURL(/\/kaynaklar$/)
    await expect(page.getByText('Kaynak silindi.')).toBeVisible()
    await page.getByLabel('Ara', { exact: true }).fill(suffix)
    await expect(page.getByText('Filtrelere uyan kaynak bulunamadı.')).toBeVisible()
  })
})
