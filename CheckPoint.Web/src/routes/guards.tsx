import { Navigate, Outlet, useLocation, useSearchParams } from 'react-router'
import { useAuth } from '../features/auth/authContext'
import { RETURN_PARAM, loginPathFor, safeReturnPath } from '../lib/navigation'
import { getSignOutReason } from '../lib/session'
import { ForbiddenPage } from './ForbiddenPage'

export function RequireAuth() {
  const { session } = useAuth()
  const location = useLocation()

  if (!session) {
    const target =
      getSignOutReason() === 'signedOut'
        ? '/giris'
        : loginPathFor(`${location.pathname}${location.search}`)
    return <Navigate to={target} replace />
  }

  return <Outlet />
}

export function RequireAdmin() {
  const { isAdmin } = useAuth()
  return isAdmin ? <Outlet /> : <ForbiddenPage />
}

export function GuestOnly() {
  const { session } = useAuth()
  const [searchParams] = useSearchParams()

  if (session) {
    return <Navigate to={safeReturnPath(searchParams.get(RETURN_PARAM))} replace />
  }

  return <Outlet />
}
