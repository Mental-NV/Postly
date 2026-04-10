import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../providers/AuthProvider'

export function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return <div>Loading...</div>
  }

  if (!isAuthenticated) {
    // Redirect to signin with return URL
    const returnUrl = location.pathname + location.search
    return <Navigate to={`/signin?returnUrl=${encodeURIComponent(returnUrl)}`} replace />
  }

  return <>{children}</>
}
