import { expect, test } from '@playwright/test'
import { login, logout, newUser, register } from './support'

test('a user changes the password and only the new password works afterwards', async ({ page }) => {
  const user = newUser('parola')
  const newPassword = 'YeniParola789'
  await register(page, user)
  await expect(page).toHaveURL(/\/kaynaklar$/)

  await page.getByRole('link', { name: user.username }).click()
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
  await expect(page.getByRole('link', { name: user.username })).toBeVisible()
})
