import type { LoginResponse, User } from '../api/types'

export interface Session {
  token: string
  expiresAt: string
  user: User
}

export type SignOutReason = 'signedOut' | 'expired'

const STORAGE_KEY = 'checkpoint.session'

type Listener = () => void

const listeners = new Set<Listener>()
let current: Session | null = readStoredSession()
let lastSignOutReason: SignOutReason | null = null

function isSession(value: unknown): value is Session {
  if (typeof value !== 'object' || value === null) return false
  const candidate = value as Partial<Session>
  return (
    typeof candidate.token === 'string' &&
    typeof candidate.expiresAt === 'string' &&
    typeof candidate.user === 'object' &&
    candidate.user !== null
  )
}

export function isExpired(session: Session, now = Date.now()): boolean {
  const expiresAt = Date.parse(session.expiresAt)
  return Number.isNaN(expiresAt) || expiresAt <= now
}

function readStoredSession(): Session | null {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const parsed: unknown = JSON.parse(raw)
    if (!isSession(parsed) || isExpired(parsed)) {
      window.localStorage.removeItem(STORAGE_KEY)
      return null
    }
    return parsed
  } catch {
    return null
  }
}

function notify() {
  for (const listener of listeners) listener()
}

export function getSession(): Session | null {
  if (current && isExpired(current)) clearSession('expired')
  return current
}

export function getSessionSnapshot(): Session | null {
  return current
}

export function saveSession(response: LoginResponse): Session {
  const session: Session = {
    token: response.token,
    expiresAt: response.expiresAt,
    user: response.user,
  }
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
  current = session
  lastSignOutReason = null
  notify()
  return session
}

export function clearSession(reason: SignOutReason) {
  window.localStorage.removeItem(STORAGE_KEY)
  if (current === null) return
  current = null
  lastSignOutReason = reason
  notify()
}

export function getSignOutReason(): SignOutReason | null {
  return lastSignOutReason
}

export function subscribe(listener: Listener): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

window.addEventListener('storage', (event) => {
  if (event.key !== STORAGE_KEY) return
  current = readStoredSession()
  notify()
})
