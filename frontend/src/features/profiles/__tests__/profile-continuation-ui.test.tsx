import { act, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider } from '../../../app/providers/AuthProvider'
import { ProfilePage } from '../ProfilePage'
import {
  createMockPost,
  createMockProfile,
} from '../../../shared/test/factories'
import { apiClient } from '../../../shared/api/client'
import { ApiError } from '../../../shared/api/errors'
import { installMockIntersectionObserver } from '../../../shared/test/intersection-observer'
import { createOneShotContinuationFailure } from '../../../shared/test/fetch-mock'
import type { SessionResponse } from '../../../shared/api/contracts'

vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
    putForm: vi.fn(),
    delete: vi.fn(),
  },
  getProfilePath: (username: string, cursor?: string | null) =>
    cursor != null
      ? `/profiles/${username}?cursor=${cursor}`
      : `/profiles/${username}`,
}))

function renderProfileContinuationPage() {
  return render(
    <MemoryRouter initialEntries={['/u/alice']}>
      <AuthProvider
        initialSession={
          {
            userId: 2,
            username: 'bob',
            displayName: 'Bob Tester',
          } satisfies SessionResponse
        }
      >
        <Routes>
          <Route path="/u/:username" element={<ProfilePage />} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  )
}

describe('Profile continuation UI', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('preserves visible posts on continuation failure and appends posts after retry', async () => {
    const observer = installMockIntersectionObserver()
    const user = userEvent.setup()

    vi.mocked(apiClient.get).mockResolvedValueOnce({
      profile: createMockProfile({
        username: 'alice',
        displayName: 'Alice Example',
      }),
      posts: [createMockPost({ id: 1, body: 'Initial profile post' })],
      nextCursor: 'cursor-1',
    })

    const continuationLoad = vi.fn(
      createOneShotContinuationFailure(async () => ({
        profile: createMockProfile({
          username: 'alice',
          displayName: 'Alice Example',
        }),
        posts: [createMockPost({ id: 2, body: 'Recovered profile post' })],
        nextCursor: null,
      }),
      new ApiError(
        500,
        'CONTINUATION_FAILED',
        'Unable to load more content',
        'Profile continuation failed once.'
      )
    ))
    vi.mocked(apiClient.get).mockImplementation((path: string) => {
      if (path.includes('cursor=')) {
        return continuationLoad() as Promise<any>
      }

      return Promise.resolve({
        profile: createMockProfile({
          username: 'alice',
          displayName: 'Alice Example',
        }),
        posts: [createMockPost({ id: 1, body: 'Initial profile post' })],
        nextCursor: 'cursor-1',
      })
    })

    renderProfileContinuationPage()

    await screen.findByText('Initial profile post')
    const sentinel = await screen.findByTestId('collection-continuation-sentinel')

    await act(async () => {
      observer.trigger(sentinel)
    })

    await waitFor(() => {
      expect(
        screen.getByTestId('collection-continuation-error')
      ).toBeInTheDocument()
    })
    expect(screen.getByText('Initial profile post')).toBeInTheDocument()

    await user.click(screen.getByTestId('collection-continuation-retry'))

    await waitFor(() => {
      expect(screen.getByText('Recovered profile post')).toBeInTheDocument()
    })
    expect(screen.getByText('Initial profile post')).toBeInTheDocument()
    expect(screen.getByTestId('collection-end-state')).toBeInTheDocument()
    expect(continuationLoad).toHaveBeenCalledTimes(2)
  })
})
