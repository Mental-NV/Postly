import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useSigninForm } from '../signin/useSigninForm'
import { ApiError } from '../../../shared/api/errors'

const mockNavigate = vi.fn()
const mockSignin = vi.fn()
let mockLocationSearch = ''

vi.mock('../../../app/providers/AuthProvider', () => ({
  useAuth: () => ({
    signin: mockSignin,
  }),
}))

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useLocation: () => ({
      search: mockLocationSearch,
    }),
    useNavigate: () => mockNavigate,
  }
})

function createSubmitEvent() {
  return { preventDefault: vi.fn() } as any
}

function createChangeEvent(name: string, value: string) {
  return {
    target: {
      name,
      value,
    },
  } as any
}

describe('useSigninForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockLocationSearch = ''
  })

  it('navigates to the requested return URL after successful signin', async () => {
    mockLocationSearch = '?returnUrl=%2Fu%2Fme'
    mockSignin.mockResolvedValueOnce(undefined)

    const { result } = renderHook(() => useSigninForm())

    act(() => {
      result.current.handleChange(createChangeEvent('username', 'alice'))
      result.current.handleChange(createChangeEvent('password', 'TestPassword123'))
    })

    await act(async () => {
      await result.current.handleSubmit(createSubmitEvent())
    })

    expect(mockSignin).toHaveBeenCalledWith('alice', 'TestPassword123')
    expect(mockNavigate).toHaveBeenCalledWith('/u/me', { replace: true })
  })

  it('shows a generic form error, preserves username, and clears password for invalid credentials', async () => {
    mockSignin.mockRejectedValueOnce(
      new ApiError(401, 'UNAUTHORIZED', 'Unauthorized')
    )

    const { result } = renderHook(() => useSigninForm())

    act(() => {
      result.current.handleChange(createChangeEvent('username', 'alice'))
      result.current.handleChange(createChangeEvent('password', 'wrongpassword'))
    })

    await act(async () => {
      await result.current.handleSubmit(createSubmitEvent())
    })

    expect(result.current.formError).toBe('Invalid username or password')
    expect(result.current.values).toEqual({
      username: 'alice',
      password: '',
    })
  })

  it('stores field-level validation errors and clears them when the field changes', async () => {
    mockSignin.mockRejectedValueOnce(
      new ApiError(
        400,
        'VALIDATION_ERROR',
        'Validation failed',
        undefined,
        { username: ['Username is required.'] }
      )
    )

    const { result } = renderHook(() => useSigninForm())

    act(() => {
      result.current.handleChange(createChangeEvent('username', ''))
      result.current.handleChange(createChangeEvent('password', 'TestPassword123'))
    })

    await act(async () => {
      await result.current.handleSubmit(createSubmitEvent())
    })

    expect(result.current.errors.username).toEqual(['Username is required.'])

    act(() => {
      result.current.handleChange(createChangeEvent('username', 'alice'))
    })

    expect(result.current.errors.username).toBeUndefined()
  })

  it('exposes a pending state while the signin request is in flight', async () => {
    let resolveSignin: (() => void) | undefined
    mockSignin.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          resolveSignin = () => resolve(undefined)
        })
    )

    const { result } = renderHook(() => useSigninForm())

    act(() => {
      result.current.handleChange(createChangeEvent('username', 'alice'))
      result.current.handleChange(createChangeEvent('password', 'TestPassword123'))
    })

    await act(async () => {
      void result.current.handleSubmit(createSubmitEvent())
    })

    await waitFor(() => {
      expect(result.current.isPending).toBe(true)
    })

    await act(async () => {
      resolveSignin?.()
      await Promise.resolve()
    })

    await waitFor(() => {
      expect(result.current.isPending).toBe(false)
    })
  })
})
