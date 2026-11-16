import { expect, type APIRequestContext, type Page } from '@playwright/test'
import { randomBytes } from 'node:crypto'

export const API_URL = process.env.E2E_API_URL ?? 'http://localhost:5199'

export const admin = {
  email: process.env.E2E_ADMIN_EMAIL ?? '',
  password: process.env.E2E_ADMIN_PASSWORD ?? '',
}

export const PASSWORD = 'E2eParola1'

export function uniqueSuffix(): string {
  return randomBytes(4).toString('hex')
}

export function newUser(prefix = 'ogrenci') {
  const suffix = uniqueSuffix()
  return {
    username: `${prefix}_${suffix}`,
    email: `${prefix}.${suffix}@example.com`,
    password: PASSWORD,
  }
}

export async function login(page: Page, email: string, password: string) {
  await page.goto('/giris')
  await page.getByLabel('E-posta').fill(email)
  await page.getByLabel('Parola').fill(password)
  await page.getByRole('button', { name: 'Giriş yap' }).click()
  await expect(page).toHaveURL(/\/kaynaklar$/)
}

export async function logout(page: Page) {
  await page.getByRole('button', { name: 'Çıkış' }).click()
  await expect(page).toHaveURL(/\/giris$/)
}

export async function register(page: Page, user: ReturnType<typeof newUser>) {
  await page.goto('/kayit')
  await page.getByLabel('Kullanıcı adı').fill(user.username)
  await page.getByLabel('E-posta').fill(user.email)
  await page.getByLabel('Parola', { exact: true }).fill(user.password)
  await page.getByLabel('Parola (tekrar)').fill(user.password)
  await page.getByRole('button', { name: 'Kayıt ol' }).click()
}

export async function apiToken(request: APIRequestContext, email: string, password: string) {
  const response = await request.post(`${API_URL}/api/auth/login`, { data: { email, password } })
  expect(response.status()).toBe(200)
  return ((await response.json()) as { token: string }).token
}

export async function apiRegister(request: APIRequestContext, user: ReturnType<typeof newUser>) {
  const response = await request.post(`${API_URL}/api/auth/register`, { data: user })
  expect(response.status()).toBe(201)
  return apiToken(request, user.email, user.password)
}

export async function apiCreateCatalog(request: APIRequestContext) {
  const token = await apiToken(request, admin.email, admin.password)
  const headers = { Authorization: `Bearer ${token}` }
  const suffix = uniqueSuffix()
  const university = await request.post(`${API_URL}/api/universities`, {
    headers,
    data: { name: `Katalog Üniversitesi ${suffix}`, city: 'Ankara' },
  })
  expect(university.status()).toBe(201)
  const universityId = ((await university.json()) as { id: number }).id
  const course = await request.post(`${API_URL}/api/courses`, {
    headers,
    data: { name: 'Katalog Dersi', code: `KAT${suffix}`.slice(0, 20), universityId },
  })
  expect(course.status()).toBe(201)
  return {
    universityId,
    universityName: `Katalog Üniversitesi ${suffix}`,
    courseId: ((await course.json()) as { id: number }).id,
  }
}

export async function apiUpload(
  request: APIRequestContext,
  token: string,
  data: { title: string; universityId: number; courseId: number },
) {
  const response = await request.post(`${API_URL}/api/resources`, {
    headers: { Authorization: `Bearer ${token}` },
    multipart: {
      Title: data.title,
      UniversityId: String(data.universityId),
      CourseId: String(data.courseId),
      Type: 'StudyQuestion',
      File: { name: 'sorular.txt', mimeType: 'text/plain', buffer: Buffer.from('Soru 1: ...') },
    },
  })
  expect(response.status()).toBe(201)
  return ((await response.json()) as { id: number }).id
}
