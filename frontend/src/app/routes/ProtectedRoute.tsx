import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../providers/AuthContext'

export function ProtectedRoute({ children }: { children: React.ReactNode }): React.JSX.Element {
  const { isAuthenticated, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return <div>Loading...</div>
  }

  if (!isAuthenticated) {
    // Redirect to signin with return URL
    const returnUrl = location.pathname + location.search
    return (
      <Navigate
        to={`/signin?returnUrl=${encodeURIComponent(returnUrl)}`}
        replace
      />
    )
  }

  return <>{children}</>
}
