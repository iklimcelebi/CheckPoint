import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useSyncExternalStore, type ReactNode } from 'react'
import type { LoginResponse } from '../../api/types'
import { clearSession, getSessionSnapshot, saveSession, subscribe } from '../../lib/session'
import { AuthContext, type AuthContextValue } from './authContext'

const MAX_TIMEOUT_MS = 2_147_483_647

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const session = useSyncExternalStore(subscribe, getSessionSnapshot)

  useEffect(() => {
    if (!session) {
      queryClient.clear()
      return
    }

    const remaining = Date.parse(session.expiresAt) - Date.now()
    const timer = window.setTimeout(
      () => clearSession('expired'),
      Math.min(Math.max(remaining, 0), MAX_TIMEOUT_MS),
    )
    return () => window.clearTimeout(timer)
  }, [session, queryClient])

  const signIn = useCallback((response: LoginResponse) => {
    saveSession(response)
  }, [])

  const signOut = useCallback(() => clearSession('signedOut'), [])

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      user: session?.user ?? null,
      isAdmin: session?.user.role === 'Admin',
      signIn,
      signOut,
    }),
    [session, signIn, signOut],
  )

  return <AuthContext value={value}>{children}</AuthContext>
}
