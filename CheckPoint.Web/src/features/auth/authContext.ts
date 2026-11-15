import { createContext, useContext } from 'react'
import type { LoginResponse, User } from '../../api/types'
import type { Session } from '../../lib/session'

export interface AuthContextValue {
  session: Session | null
  user: User | null
  isAdmin: boolean
  signIn: (response: LoginResponse) => void
  signOut: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext)
  if (!value) throw new Error('useAuth must be used inside AuthProvider.')
  return value
}
