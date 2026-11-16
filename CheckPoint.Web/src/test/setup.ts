import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterAll, afterEach, beforeAll, vi } from 'vitest'
import { clearSession } from '../lib/session'
import { server } from './server'

HTMLDialogElement.prototype.showModal = function showModal(this: HTMLDialogElement) {
  this.setAttribute('open', '')
}
HTMLDialogElement.prototype.close = function close(this: HTMLDialogElement) {
  this.removeAttribute('open')
}

window.scrollTo = vi.fn()
URL.createObjectURL = vi.fn(() => 'blob:test')
URL.revokeObjectURL = vi.fn()

beforeAll(() => server.listen({ onUnhandledFrame: 'error' }))
afterEach(() => {
  cleanup()
  server.resetHandlers()
  clearSession('signedOut')
  window.localStorage.clear()
})
afterAll(() => server.close())
