import { Routes, Route, Navigate } from 'react-router-dom'
import { SignupPage } from '../../features/auth/signup/SignupPage'
import { SigninPage } from '../../features/auth/signin/SigninPage'
import { TimelinePage } from '../../features/timeline/TimelinePage'
import { ProfilePage } from '../../features/profiles/ProfilePage'
import { DirectPostPage } from '../../features/posts/DirectPostPage'
import { ProtectedRoute } from './ProtectedRoute'
import { MainLayout } from '../../shared/components/MainLayout'

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/signup" element={<SignupPage />} />
      <Route path="/signin" element={<SigninPage />} />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <MainLayout>
              <TimelinePage />
            </MainLayout>
          </ProtectedRoute>
        }
      />
      <Route
        path="/u/:username"
        element={
          <MainLayout>
            <ProfilePage />
          </MainLayout>
        }
      />
      <Route
        path="/posts/:postId"
        element={
          <MainLayout>
            <DirectPostPage />
          </MainLayout>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
