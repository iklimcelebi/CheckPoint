import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider, useLocation, type RouteObject } from 'react-router'
import type { LoginResponse, Resource, Role, UserStats } from '../api/types'
import { AuthProvider } from '../features/auth/AuthProvider'
import { API_BASE_URL } from '../lib/config'
import { saveSession } from '../lib/session'

export const api = (path: string) => `${API_BASE_URL}${path}`

export function loginResponse(role: Role = 'User', id = 1, username = 'ayse'): LoginResponse {
  return {
    token: `test-token-${id}`,
    expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    user: { id, username, email: `${username}@example.com`, role },
  }
}

export function signInAs(role: Role = 'User', id = 1, username = 'ayse') {
  saveSession(loginResponse(role, id, username))
}

export function makeResource(overrides: Partial<Resource> = {}): Resource {
  return {
    id: 10,
    title: 'Veri Yapıları ders notları',
    description: 'Bağlı listeler',
    university: { id: 1, name: 'Pamukkale Üniversitesi' },
    course: { id: 2, name: 'Veri Yapıları', code: 'BLG201' },
    type: 'LectureNote',
    originalFileName: 'notlar.pdf',
    fileSize: 2048,
    uploadedBy: { id: 1, username: 'ayse' },
    uploadedAt: '2026-11-15T09:30:00Z',
    viewCount: 0,
    isFavorite: false,
    ...overrides,
  }
}

function LocationProbe() {
  const location = useLocation()
  return <p data-testid="location">{`${location.pathname}${location.search}`}</p>
}

export function renderRoutes(routes: RouteObject[], initialEntry: string) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  const router = createMemoryRouter([...routes, { path: '*', element: <LocationProbe /> }], {
    initialEntries: [initialEntry],
  })

  render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </QueryClientProvider>,
  )

  return { router, user: userEvent.setup() }
}

export function currentLocation(router: ReturnType<typeof createMemoryRouter>) {
  return `${router.state.location.pathname}${router.state.location.search}`
}

export function makeStats(overrides: Partial<UserStats> = {}): UserStats {
  return {
    resourceCount: 0,
    totalViews: 0,
    favoritesReceived: 0,
    favoriteCount: 0,
    resourcesByType: [],
    topResources: [],
    ...overrides,
  }
}
