import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter, MemoryRouter } from 'react-router-dom'
import { SigninPage } from '../signin/SigninPage'
import { ProtectedRoute } from '../../../app/routes/ProtectedRoute'
import { AuthProvider } from '../../../app/providers/AuthProvider'
import { apiClient } from '../../../shared/api/client'
import { ApiError } from '../../../shared/api/errors'

// Mock API client
vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    post: vi.fn(),
    get: vi.fn(),
  },
}))

// Mock router hooks
const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

describe('SigninPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    // Mock session check to return 401 (not authenticated)
    vi.mocked(apiClient.get).mockRejectedValue(
      new ApiError(401, 'Unauthorized', 'Unauthorized')
    )
  })

  it('renders signin form with all fields', async () => {
    render(
      <BrowserRouter>
        <AuthProvider>
          <SigninPage />
        </AuthProvider>
      </BrowserRouter>
    )

    // Wait for AuthProvider's session check to complete
    await waitFor(() => {
      expect(screen.getByTestId('username-input')).toBeInTheDocument()
    })

    expect(screen.getByTestId('password-input')).toBeInTheDocument()
    expect(screen.getByTestId('submit-button')).toBeInTheDocument()
    expect(screen.getByText(/don't have an account/i)).toBeInTheDocument()
  })

  it('shows generic error for invalid credentials', async () => {
    const user = userEvent.setup()

    vi.mocked(apiClient.post).mockRejectedValueOnce(
      new ApiError(401, 'Unauthorized', 'Unauthorized')
    )

    render(
      <BrowserRouter>
        <AuthProvider>
          <SigninPage />
        </AuthProvider>
      </BrowserRouter>
    )

    await user.type(screen.getByTestId('username-input'), 'alice')
    await user.type(screen.getByTestId('password-input'), 'wrongpassword')
    await user.click(screen.getByTestId('submit-button'))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(
        'Invalid username or password'
      )
    })
  })

  it('preserves username and clears password after error', async () => {
    const user = userEvent.setup()

    vi.mocked(apiClient.post).mockRejectedValueOnce(
      new ApiError(401, 'Unauthorized', 'Unauthorized')
    )

    render(
      <BrowserRouter>
        <AuthProvider>
          <SigninPage />
        </AuthProvider>
      </BrowserRouter>
    )

    const usernameInput = screen.getByTestId(
      'username-input'
    ) as HTMLInputElement
    const passwordInput = screen.getByTestId(
      'password-input'
    ) as HTMLInputElement

    await user.type(usernameInput, 'alice')
    await user.type(passwordInput, 'wrongpassword')
    await user.click(screen.getByTestId('submit-button'))

    await waitFor(() => {
      expect(usernameInput.value).toBe('alice')
      expect(passwordInput.value).toBe('')
    })
  })

  it('disables form during submission', async () => {
    const user = userEvent.setup()

    vi.mocked(apiClient.post).mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 100))
    )

    render(
      <BrowserRouter>
        <AuthProvider>
          <SigninPage />
        </AuthProvider>
      </BrowserRouter>
    )

    await user.type(screen.getByTestId('username-input'), 'alice')
    await user.type(screen.getByTestId('password-input'), 'password')
    await user.click(screen.getByTestId('submit-button'))

    expect(screen.getByTestId('submit-button')).toBeDisabled()
    expect(screen.getByTestId('username-input')).toBeDisabled()
    expect(screen.getByTestId('password-input')).toBeDisabled()
  })

  it('navigates to home on successful signin', async () => {
    const user = userEvent.setup()

    vi.mocked(apiClient.post).mockResolvedValueOnce({
      userId: 1,
      username: 'alice',
      displayName: 'Alice Example',
    })

    render(
      <BrowserRouter>
        <AuthProvider>
          <SigninPage />
        </AuthProvider>
      </BrowserRouter>
    )

    await user.type(screen.getByTestId('username-input'), 'alice')
    await user.type(screen.getByTestId('password-input'), 'TestPassword123')
    await user.click(screen.getByTestId('submit-button'))

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/', { replace: true })
    })
  })
})

describe('ProtectedRoute', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('redirects to signin when not authenticated', async () => {
    vi.mocked(apiClient.get).mockRejectedValue(
      new ApiError(401, 'Unauthorized', 'Unauthorized')
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

    // Wait for auth check to complete
    await waitFor(() => {
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
    })
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

  it('renders children when authenticated', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({
      userId: 1,
      username: 'alice',
      displayName: 'Alice Example',
    })

    render(
      <MemoryRouter initialEntries={['/']}>
        <AuthProvider>
          <ProtectedRoute>
            <div>Protected Content</div>
          </ProtectedRoute>
        </AuthProvider>
      </MemoryRouter>
    )

    await waitFor(() => {
      expect(screen.getByText('Protected Content')).toBeInTheDocument()
    })
  })
})
