import { defineConfig, devices } from '@playwright/test'
import { randomBytes } from 'node:crypto'
import { tmpdir } from 'node:os'
import { join } from 'node:path'

const API_URL = 'http://localhost:5199'
const WEB_URL = 'http://localhost:5174'

const runId = (process.env.E2E_RUN_ID ??= randomBytes(6).toString('hex'))
process.env.E2E_API_URL = API_URL
process.env.E2E_DATABASE ??= `CheckPointE2E_${runId}`
process.env.E2E_WORK_DIR ??= join(tmpdir(), `checkpoint-e2e-${runId}`)
process.env.E2E_ADMIN_EMAIL ??= `e2e.admin.${runId}@example.com`
process.env.E2E_ADMIN_PASSWORD ??= `Aa1${randomBytes(12).toString('base64url')}`
process.env.E2E_JWT_KEY ??= randomBytes(48).toString('base64')

const workDir = process.env.E2E_WORK_DIR
const apiBuildDir = join(workDir, 'api')

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 60_000,
  reporter: [['list']],
  globalTeardown: './e2e/global-teardown.ts',
  use: {
    baseURL: WEB_URL,
    locale: 'tr-TR',
    timezoneId: 'Europe/Istanbul',
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: `dotnet build ../CheckPoint.Api -c Debug -o "${apiBuildDir}" -v q --nologo && dotnet "${join(apiBuildDir, 'CheckPoint.Api.dll')}" --contentRoot "${apiBuildDir}"`,
      url: `${API_URL}/health`,
      timeout: 240_000,
      reuseExistingServer: false,
      stdout: 'ignore',
      stderr: 'pipe',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: API_URL,
        ConnectionStrings__DefaultConnection: `Server=(localdb)\\MSSQLLocalDB;Database=${process.env.E2E_DATABASE};Trusted_Connection=True;TrustServerCertificate=True`,
        Jwt__Key: process.env.E2E_JWT_KEY,
        SeedAdmin__Email: process.env.E2E_ADMIN_EMAIL,
        SeedAdmin__Password: process.env.E2E_ADMIN_PASSWORD,
        Database__AutoMigrate: 'true',
        Seed__DemoData: 'false',
        FileStorage__RootPath: join(workDir, 'uploads'),
        Cors__AllowedOrigins__0: WEB_URL,
        RateLimiting__Auth__PermitLimit: '1000',
      },
    },
    {
      command: 'npm run dev -- --port 5174 --strictPort',
      url: WEB_URL,
      timeout: 60_000,
      reuseExistingServer: false,
      env: { VITE_API_BASE_URL: API_URL },
    },
  ],
})
