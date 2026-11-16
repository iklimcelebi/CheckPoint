import { Component, type ErrorInfo, type ReactNode } from 'react'

interface AppErrorBoundaryState {
  failed: boolean
}

export class AppErrorBoundary extends Component<{ children: ReactNode }, AppErrorBoundaryState> {
  state: AppErrorBoundaryState = { failed: false }

  static getDerivedStateFromError(): AppErrorBoundaryState {
    return { failed: true }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Unhandled UI error', error, info.componentStack)
  }

  render() {
    if (!this.state.failed) return this.props.children

    return (
      <main
        role="alert"
        className="flex min-h-screen flex-col items-center justify-center gap-4 px-4 text-center"
      >
        <h1 className="text-xl font-semibold text-slate-900">Beklenmeyen bir hata oluştu</h1>
        <p className="text-sm text-slate-600">
          Uygulama bu ekranı gösterirken bir sorunla karşılaştı. Sayfayı yenilemeyi deneyin.
        </p>
        <button
          type="button"
          onClick={() => window.location.assign('/kaynaklar')}
          className="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
        >
          Ana sayfaya dön
        </button>
      </main>
    )
  }
}
