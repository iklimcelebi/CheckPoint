import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { RouteErrorPage } from '../routes/RouteErrorPage'
import { AppErrorBoundary } from './AppErrorBoundary'

function Broken(): never {
  throw new Error('boom')
}

describe('error handling', () => {
  it('shows a Turkish fallback instead of a blank page', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    render(
      <AppErrorBoundary>
        <Broken />
      </AppErrorBoundary>,
    )
    expect(screen.getByRole('alert')).toHaveTextContent('Beklenmeyen bir hata oluştu')
    expect(screen.queryByText('boom')).not.toBeInTheDocument()
  })

  it('shows the route error page when a page crashes', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.spyOn(console, 'warn').mockImplementation(() => {})
    const router = createMemoryRouter([
      { path: '/', element: <Broken />, errorElement: <RouteErrorPage /> },
    ])
    render(<RouterProvider router={router} />)
    expect(screen.getByRole('alert')).toHaveTextContent('Beklenmeyen bir hata oluştu')
    expect(screen.getByRole('button', { name: 'Sayfayı yenile' })).toBeInTheDocument()
  })

  it('shows the 404 page for unknown routes', () => {
    const router = createMemoryRouter(
      [{ path: '/', element: <p>Ana sayfa</p>, errorElement: <RouteErrorPage /> }],
      {
        initialEntries: ['/olmayan'],
      },
    )
    render(<RouterProvider router={router} />)
    expect(screen.getByRole('heading', { name: 'Sayfa bulunamadı' })).toBeInTheDocument()
  })
})
