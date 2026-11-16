import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider } from 'react-router'
import { ApiError } from './api/errors'
import { AppErrorBoundary } from './components/AppErrorBoundary'
import { AuthProvider } from './features/auth/AuthProvider'
import './index.css'
import { router } from './routes/router'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: (failureCount, error) =>
        !(error instanceof ApiError && error.status >= 400 && error.status < 500) &&
        failureCount < 2,
    },
  },
})

const root = document.getElementById('root')
if (!root) throw new Error('Root element not found.')

createRoot(root).render(
  <StrictMode>
    <AppErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <RouterProvider router={router} />
        </AuthProvider>
      </QueryClientProvider>
    </AppErrorBoundary>
  </StrictMode>,
)
