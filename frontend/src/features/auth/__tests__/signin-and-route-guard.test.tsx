import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import {
  BrowserRouter,
  MemoryRouter,
  Route,
  Routes,
  useLocation,
} from 'react-router-dom'
import { SigninPage } from '../signin/SigninPage'
import { ProtectedRoute } from '../../../app/routes/ProtectedRoute'
import { AuthProvider } from '../../../app/providers/AuthProvider'
import { apiClient } from '../../../shared/api/client'

vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    get: vi.fn(),
  },
}))

function LocationDisplay() {
  const location = useLocation()

  return <div data-testid="location-display">{location.pathname + location.search}</div>
}

describe('SigninPage', () => {
  it('renders the signin form shell', () => {
    render(
      <BrowserRouter>
        <AuthProvider initialSession={null}>
          <SigninPage />
        </AuthProvider>
      </BrowserRouter>
    )

    expect(screen.getByTestId('username-input')).toBeInTheDocument()
    expect(screen.getByTestId('password-input')).toBeInTheDocument()
    expect(screen.getByTestId('submit-button')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /sign up/i })).toBeInTheDocument()
  })
})

describe('ProtectedRoute', () => {
  it('redirects to signin with the current return URL', () => {
    render(
      <MemoryRouter initialEntries={['/private?tab=posts']}>
        <AuthProvider initialSession={null}>
          <Routes>
            <Route
              path="/private"
              element={
                <ProtectedRoute>
                  <div>Protected Content</div>
                </ProtectedRoute>
              }
            />
            <Route path="/signin" element={<LocationDisplay />} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>
    )

    expect(screen.getByTestId('location-display')).toHaveTextContent(
      '/signin?returnUrl=%2Fprivate%3Ftab%3Dposts'
    )
  })

  it('shows loading state while checking authentication', () => {
    vi.mocked(apiClient.get).mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 100))
    )

    render(
      <MemoryRouter initialEntries={['/']}>
        <AuthProvider>
          <ProtectedRoute>
            <div>Protected Content</div>
          </ProtectedRoute>
        </AuthProvider>
      </MemoryRouter>
    )

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders children when authenticated', () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <AuthProvider
          initialSession={{
            userId: 1,
            username: 'alice',
            displayName: 'Alice Example',
          }}
        >
          <ProtectedRoute>
            <div>Protected Content</div>
          </ProtectedRoute>
        </AuthProvider>
      </MemoryRouter>
    )

    expect(screen.getByText('Protected Content')).toBeInTheDocument()
  })
})
