import { expect, test } from '@playwright/test'
import {
  apiCreateCatalog,
  apiRegister,
  apiUpload,
  HOME_URL,
  login,
  logout,
  navLink,
  newUser,
  register,
  uniqueSuffix,
  userMenu,
} from './support'

test('a user changes the password and only the new password works afterwards', async ({ page }) => {
  const user = newUser('parola')
  const newPassword = 'YeniParola789'
  await register(page, user)
  await expect(page).toHaveURL(HOME_URL)

  await userMenu(page, user.username).click()
  await page.getByRole('menuitem', { name: 'Profili görüntüle' }).click()
  await expect(page.getByText('Yüklenen kaynak')).toBeVisible()
  await page.getByRole('link', { name: 'Parolayı değiştir' }).click()
  await page.getByLabel('Mevcut parola').fill('YanlisParola1')
  await page.getByLabel('Yeni parola', { exact: true }).fill(newPassword)
  await page.getByLabel('Yeni parola (tekrar)').fill(newPassword)
  await page.getByRole('button', { name: 'Parolayı değiştir' }).click()
  await expect(page.getByText('Mevcut parola hatalı.')).toBeVisible()

  await page.getByLabel('Mevcut parola').fill(user.password)
  await page.getByRole('button', { name: 'Parolayı değiştir' }).click()
  await expect(page.getByText(/Parolanız değiştirildi/)).toBeVisible()
  await logout(page)

  await page.getByLabel('E-posta').fill(user.email)
  await page.getByLabel('Parola').fill(user.password)
  await page.getByRole('button', { name: 'Giriş yap' }).click()
  await expect(page.getByRole('alert')).toHaveText('E-posta veya parola hatalı.')

  await login(page, user.email, newPassword)
  await expect(userMenu(page, user.username)).toBeVisible()
})

test('a user finds a resource with the header search and adds it to favorites', async ({
  page,
  request,
}) => {
  const catalog = await apiCreateCatalog(request)
  const ownerToken = await apiRegister(request, newUser('sahip'))
  const title = `Favori kaynak ${uniqueSuffix()}`
  await apiUpload(request, ownerToken, { ...catalog, title })

  const user = newUser('favori')
  await register(page, user)
  await expect(page).toHaveURL(HOME_URL)
  await expect(
    page.getByRole('region', { name: 'Son eklenen kaynaklar' }).getByRole('link', { name: title }),
  ).toBeVisible()

  await page.getByPlaceholder('Ders, konu, kaynak veya üniversite ara...').fill(title)
  await page.keyboard.press('Enter')
  await expect(page.getByRole('heading', { name: `"${title}" için sonuçlar` })).toBeVisible()
  await page.getByRole('button', { name: `${title}: Favorilere ekle` }).click()
  await expect(page.getByRole('button', { name: `${title}: Favorilerden çıkar` })).toBeVisible()

  await navLink(page, 'Favoriler').click()
  await expect(page.getByRole('heading', { name: 'Favoriler', level: 1 })).toBeVisible()
  await expect(page.getByRole('link', { name: title })).toBeVisible()
})
