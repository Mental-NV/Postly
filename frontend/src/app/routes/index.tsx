import { Routes, Route, Navigate } from 'react-router-dom'
import { SignupPage } from '../../features/auth/signup/SignupPage'
import { SigninPage } from '../../features/auth/signin/SigninPage'
import { TimelinePage } from '../../features/timeline/TimelinePage'
import { ProtectedRoute } from './ProtectedRoute'

function ProfilePage() {
  return <div>Profile Page</div>
}

function DirectPostPage() {
  return <div>Direct Post Page</div>
}

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/signup" element={<SignupPage />} />
      <Route path="/signin" element={<SigninPage />} />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <TimelinePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/u/:username"
        element={
          <ProtectedRoute>
            <ProfilePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/posts/:postId"
        element={
          <ProtectedRoute>
            <DirectPostPage />
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
