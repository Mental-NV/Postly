import { Routes, Route, Navigate } from 'react-router-dom'

function SignupPage() {
  return <div>Signup Page</div>
}

function SigninPage() {
  return <div>Signin Page</div>
}

function TimelinePage() {
  return <div>Timeline Page</div>
}

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
      <Route path="/" element={<TimelinePage />} />
      <Route path="/u/:username" element={<ProfilePage />} />
      <Route path="/posts/:postId" element={<DirectPostPage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
