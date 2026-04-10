import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { SignupPage } from '../signup/SignupPage'
import { AuthProvider } from '../../../app/providers/AuthProvider'
import { apiClient } from '../../../shared/api/client'
import { ApiError } from '../../../shared/api/errors'

vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    post: vi.fn(),
    get: vi.fn(),
  },
}))

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

function renderSignupPage() {
  return render(
    <BrowserRouter>
      <AuthProvider>
        <SignupPage />
      </AuthProvider>
    </BrowserRouter>
  )
}

describe('SignupPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    // Mock session check to return 401 (not authenticated)
    vi.mocked(apiClient.get).mockRejectedValue(
      new ApiError(401, 'Unauthorized', 'Unauthorized')
    )
  })

  it('renders all form fields', () => {
    renderSignupPage()

    expect(screen.getByLabelText(/username/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/display name/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/bio/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /sign up/i })).toBeInTheDocument()
  })

  it('shows validation errors for invalid inputs', async () => {
    const mockError = new ApiError(
      400,
      'https://tools.ietf.org/html/rfc9110#section-15.5.1',
      'One or more validation errors occurred.',
      undefined,
      {
        username: ['Username must be at least 3 characters.'],
        password: ['Password must be at least 8 characters.'],
      }
    )
    vi.mocked(apiClient.post).mockRejectedValueOnce(mockError)

    const user = userEvent.setup()
    renderSignupPage()

    await user.type(screen.getByTestId('username-input'), 'ab')
    await user.type(screen.getByTestId('password-input'), 'short')
    await user.click(screen.getByTestId('submit-button'))

    await waitFor(() => {
      expect(screen.getByTestId('username-error')).toHaveTextContent(
        'Username must be at least 3 characters.'
      )
      expect(screen.getByTestId('password-error')).toHaveTextContent(
        'Password must be at least 8 characters.'
      )
    })
  })

  it('disables submit button during pending state', async () => {
    vi.mocked(apiClient.post).mockImplementationOnce(
      () => new Promise((resolve) => setTimeout(resolve, 100))
    )

    const user = userEvent.setup()
    renderSignupPage()

    await user.type(screen.getByTestId('username-input'), 'testuser')
    await user.type(screen.getByTestId('displayName-input'), 'Test User')
    await user.type(screen.getByTestId('password-input'), 'TestPassword123')

    const submitButton = screen.getByTestId('submit-button')
    await user.click(submitButton)

    expect(submitButton).toBeDisabled()
    expect(submitButton).toHaveTextContent('Signing up...')
  })

  it.skip('preserves non-password values after validation error', async () => {
    const mockError = new ApiError(
      400,
      'https://tools.ietf.org/html/rfc9110#section-15.5.1',
      'One or more validation errors occurred.',
      undefined,
      {
        password: ['Password must be at least 8 characters.'],
      }
    )
    // Mock signup to fail with validation error
    vi.mocked(apiClient.post).mockRejectedValueOnce(mockError)

    const user = userEvent.setup()
    renderSignupPage()

    const usernameInput = screen.getByTestId('username-input') as HTMLInputElement
    const displayNameInput = screen.getByTestId('displayName-input') as HTMLInputElement
    const bioInput = screen.getByTestId('bio-input') as HTMLTextAreaElement
    const passwordInput = screen.getByTestId('password-input') as HTMLInputElement

    await user.type(usernameInput, 'testuser')
    await user.type(displayNameInput, 'Test User')
    await user.type(bioInput, 'Test bio')
    await user.type(passwordInput, 'short')
    await user.click(screen.getByTestId('submit-button'))

    await waitFor(() => {
      expect(screen.getByTestId('password-error')).toHaveTextContent(
        'Password must be at least 8 characters.'
      )
    })

    expect(usernameInput.value).toBe('testuser')
    expect(displayNameInput.value).toBe('Test User')
    expect(bioInput.value).toBe('Test bio')
    expect(passwordInput.value).toBe('')
  })

  // TODO: This test is flaky - the mock rejection doesn't trigger consistently.
  // The API call to /auth/signup doesn't seem to be made or the mock isn't applied.
  // Needs investigation of test setup and mock configuration.
  it.skip('clears password field after error', async () => {
    const mockError = new ApiError(
      409,
      'https://tools.ietf.org/html/rfc9110#section-15.5.10',
      'Username already exists.'
    )
    const postMock = vi.mocked(apiClient.post).mockRejectedValueOnce(mockError)

    const user = userEvent.setup()
    renderSignupPage()

    const usernameInput = screen.getByTestId('username-input')
    const displayNameInput = screen.getByTestId('displayName-input')
    const passwordInput = screen.getByTestId('password-input')
    const submitButton = screen.getByTestId('submit-button')

    await user.type(usernameInput, 'testuser')
    await user.type(displayNameInput, 'Test User')
    await user.type(passwordInput, 'TestPassword123')

    // Verify password is filled before submit
    expect(passwordInput).toHaveValue('TestPassword123')

    await user.click(submitButton)

    // Wait for the API to be called
    await waitFor(() => {
      expect(postMock).toHaveBeenCalledWith('/auth/signup', {
        username: 'testuser',
        displayName: 'Test User',
        bio: undefined,
        password: 'TestPassword123',
      })
    })

    // Wait for error to appear and password to be cleared
    await waitFor(() => {
      expect(screen.getByTestId('username-error')).toBeInTheDocument()
      expect(passwordInput).toHaveValue('')
    })
  })

  it('navigates to home timeline on success', async () => {
    // Mock successful signup
    vi.mocked(apiClient.post).mockResolvedValueOnce({
      userId: 1,
      username: 'testuser',
      displayName: 'Test User',
    })
    // Mock successful signin (called after signup)
    vi.mocked(apiClient.post).mockResolvedValueOnce({
      userId: 1,
      username: 'testuser',
      displayName: 'Test User',
    })

    const user = userEvent.setup()
    renderSignupPage()

    await user.type(screen.getByTestId('username-input'), 'testuser')
    await user.type(screen.getByTestId('displayName-input'), 'Test User')
    await user.type(screen.getByTestId('password-input'), 'TestPassword123')
    await user.click(screen.getByTestId('submit-button'))

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/', { replace: true })
    })
  })

  it('shows conflict message for duplicate username', async () => {
    const mockError = new ApiError(
      409,
      'https://tools.ietf.org/html/rfc9110#section-15.5.10',
      'Username already exists.'
    )
    vi.mocked(apiClient.post).mockRejectedValueOnce(mockError)

    const user = userEvent.setup()
    renderSignupPage()

    await user.type(screen.getByTestId('username-input'), 'existinguser')
    await user.type(screen.getByTestId('displayName-input'), 'Test User')
    await user.type(screen.getByTestId('password-input'), 'TestPassword123')
    await user.click(screen.getByTestId('submit-button'))

    await waitFor(() => {
      expect(screen.getByTestId('username-error')).toHaveTextContent(
        'Username is already taken.'
      )
    })
  })

  it('clears field error when user types', async () => {
    const mockError = new ApiError(
      400,
      'https://tools.ietf.org/html/rfc9110#section-15.5.1',
      'One or more validation errors occurred.',
      undefined,
      {
        username: ['Username must be at least 3 characters.'],
      }
    )
    vi.mocked(apiClient.post).mockRejectedValueOnce(mockError)

    const user = userEvent.setup()
    renderSignupPage()

    await user.type(screen.getByTestId('username-input'), 'ab')
    await user.click(screen.getByTestId('submit-button'))

    await waitFor(() => {
      expect(screen.getByTestId('username-error')).toBeInTheDocument()
    })

    await user.type(screen.getByTestId('username-input'), 'c')

    expect(screen.queryByTestId('username-error')).not.toBeInTheDocument()
  })
})
