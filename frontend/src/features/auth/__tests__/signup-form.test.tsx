import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { SignupPage } from '../signup/SignupPage'
import { apiClient } from '../../../shared/api/client'
import { ApiError } from '../../../shared/api/errors'

vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    post: vi.fn(),
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
      <SignupPage />
    </BrowserRouter>
  )
}

describe('SignupPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
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

  it('preserves non-password values after validation error', async () => {
    const mockError = new ApiError(
      400,
      'https://tools.ietf.org/html/rfc9110#section-15.5.1',
      'One or more validation errors occurred.',
      undefined,
      {
        password: ['Password must be at least 8 characters.'],
      }
    )
    vi.mocked(apiClient.post).mockRejectedValueOnce(mockError)

    const user = userEvent.setup()
    renderSignupPage()

    await user.type(screen.getByTestId('username-input'), 'testuser')
    await user.type(screen.getByTestId('displayName-input'), 'Test User')
    await user.type(screen.getByTestId('bio-input'), 'Test bio')
    await user.type(screen.getByTestId('password-input'), 'short')
    await user.click(screen.getByTestId('submit-button'))

    await waitFor(() => {
      expect(screen.getByTestId('password-error')).toBeInTheDocument()
    })

    expect(screen.getByTestId('username-input')).toHaveValue('testuser')
    expect(screen.getByTestId('displayName-input')).toHaveValue('Test User')
    expect(screen.getByTestId('bio-input')).toHaveValue('Test bio')
    expect(screen.getByTestId('password-input')).toHaveValue('')
  })

  it('clears password field after error', async () => {
    const mockError = new ApiError(
      409,
      'https://tools.ietf.org/html/rfc9110#section-15.5.10',
      'Username already exists.'
    )
    vi.mocked(apiClient.post).mockRejectedValueOnce(mockError)

    const user = userEvent.setup()
    renderSignupPage()

    await user.type(screen.getByTestId('password-input'), 'TestPassword123')
    await user.click(screen.getByTestId('submit-button'))

    await waitFor(() => {
      expect(screen.getByTestId('password-input')).toHaveValue('')
    })
  })

  it('navigates to home timeline on success', async () => {
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
      expect(mockNavigate).toHaveBeenCalledWith('/')
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
