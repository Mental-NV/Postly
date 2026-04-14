import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useSignupForm } from '../signup/useSignupForm'
import { apiClient } from '../../../shared/api/client'
import { ApiError } from '../../../shared/api/errors'

const mockNavigate = vi.fn()
const mockSignin = vi.fn()

vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    post: vi.fn(),
  },
}))

vi.mock('../../../app/providers/AuthContext', () => ({
  useAuth: () => ({
    signin: mockSignin,
  }),
}))

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

function createSubmitEvent() {
  return { preventDefault: vi.fn() } as any
}

describe('useSignupForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockSignin.mockResolvedValue(undefined)
  })

  it('submits signup data, signs in, and navigates home', async () => {
    vi.mocked(apiClient.post).mockResolvedValueOnce({})

    const { result } = renderHook(() => useSignupForm())

    act(() => {
      result.current.handleChange('username', 'testuser')
      result.current.handleChange('displayName', 'Test User')
      result.current.handleChange('bio', '')
      result.current.handleChange('password', 'TestPassword123')
    })

    await act(async () => {
      await result.current.handleSubmit(createSubmitEvent())
    })

    expect(apiClient.post).toHaveBeenCalledWith('/auth/signup', {
      username: 'testuser',
      displayName: 'Test User',
      bio: undefined,
      password: 'TestPassword123',
    })
    expect(mockSignin).toHaveBeenCalledWith('testuser', 'TestPassword123')
    expect(mockNavigate).toHaveBeenCalledWith('/', { replace: true })
  })

  it('preserves non-password values and shows validation errors after a failed signup', async () => {
    vi.mocked(apiClient.post).mockRejectedValueOnce(
      new ApiError(
        400,
        'VALIDATION_ERROR',
        'Validation failed',
        undefined,
        { password: ['Password must be at least 8 characters.'] }
      )
    )

    const { result } = renderHook(() => useSignupForm())

    act(() => {
      result.current.handleChange('username', 'testuser')
      result.current.handleChange('displayName', 'Test User')
      result.current.handleChange('bio', 'Test bio')
      result.current.handleChange('password', 'short')
    })

    await act(async () => {
      await result.current.handleSubmit(createSubmitEvent())
    })

    expect(result.current.errors.password).toEqual([
      'Password must be at least 8 characters.',
    ])
    expect(result.current.values).toEqual({
      username: 'testuser',
      displayName: 'Test User',
      bio: 'Test bio',
      password: '',
    })
  })

  it('maps duplicate usernames to the username field and clears the password', async () => {
    vi.mocked(apiClient.post).mockRejectedValueOnce(
      new ApiError(409, 'CONFLICT', 'Username already exists.')
    )

    const { result } = renderHook(() => useSignupForm())

    act(() => {
      result.current.handleChange('username', 'existinguser')
      result.current.handleChange('displayName', 'Test User')
      result.current.handleChange('password', 'TestPassword123')
    })

    await act(async () => {
      await result.current.handleSubmit(createSubmitEvent())
    })

    expect(result.current.errors.username).toEqual(['Username is already taken.'])
    expect(result.current.values.password).toBe('')
  })

  it('clears field errors when the user edits that field', async () => {
    vi.mocked(apiClient.post).mockRejectedValueOnce(
      new ApiError(
        400,
        'VALIDATION_ERROR',
        'Validation failed',
        undefined,
        { username: ['Username must be at least 3 characters.'] }
      )
    )

    const { result } = renderHook(() => useSignupForm())

    act(() => {
      result.current.handleChange('username', 'ab')
      result.current.handleChange('displayName', 'Test User')
      result.current.handleChange('password', 'TestPassword123')
    })

    await act(async () => {
      await result.current.handleSubmit(createSubmitEvent())
    })

    expect(result.current.errors.username).toEqual([
      'Username must be at least 3 characters.',
    ])

    act(() => {
      result.current.handleChange('username', 'abc')
    })

    expect(result.current.errors.username).toBeUndefined()
  })

  it('ignores duplicate submits while signup is pending', async () => {
    let resolveSignup: (() => void) | undefined
    vi.mocked(apiClient.post).mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          resolveSignup = () => resolve({})
        })
    )

    const { result } = renderHook(() => useSignupForm())

    act(() => {
      result.current.handleChange('username', 'testuser')
      result.current.handleChange('displayName', 'Test User')
      result.current.handleChange('password', 'TestPassword123')
    })

    await act(async () => {
      void result.current.handleSubmit(createSubmitEvent())
    })

    await waitFor(() => {
      expect(result.current.isPending).toBe(true)
    })

    await act(async () => {
      await result.current.handleSubmit(createSubmitEvent())
    })

    expect(apiClient.post).toHaveBeenCalledTimes(1)

    await act(async () => {
      resolveSignup?.()
      await Promise.resolve()
    })

    await waitFor(() => {
      expect(result.current.isPending).toBe(false)
    })
  })
})
