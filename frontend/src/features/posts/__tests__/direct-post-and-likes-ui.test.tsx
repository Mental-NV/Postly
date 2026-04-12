import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { TimelinePage } from '../../timeline/TimelinePage'
import { apiClient } from '../../../shared/api/client'
import { ApiError } from '../../../shared/api/errors'
import {
  renderWithProviders,
  mockAuthenticatedSession,
} from '../../../shared/test/helpers'
import { createMockPost } from '../../../shared/test/factories'

vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
}))

describe('Direct post and likes UI', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('likes a timeline post with optimistic UI and persisted server state', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce({
      posts: [createMockPost({ id: 7, likeCount: 0, likedByViewer: false })],
      nextCursor: null,
    })
    vi.mocked(apiClient.post).mockResolvedValueOnce({
      postId: 7,
      likeCount: 1,
      likedByViewer: true,
    })

    const user = userEvent.setup()
    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })

    await waitFor(() => {
      expect(screen.getByTestId('post-like-button-7')).toBeInTheDocument()
    })

    await user.click(screen.getByTestId('post-like-button-7'))

    await waitFor(() => {
      expect(apiClient.post).toHaveBeenCalledWith('/posts/7/like')
      expect(screen.getByRole('button', { name: 'Unlike' })).toBeInTheDocument()
      expect(screen.getByTestId('post-like-count-7')).toHaveTextContent('1')
    })
  })

  it('rolls back a failed optimistic like on the timeline', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce({
      posts: [createMockPost({ id: 7, likeCount: 0, likedByViewer: false })],
      nextCursor: null,
    })
    vi.mocked(apiClient.post).mockRejectedValueOnce(
      new ApiError(500, 'HTTP_ERROR', 'HTTP 500')
    )

    const user = userEvent.setup()
    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })

    await waitFor(() => {
      expect(screen.getByTestId('post-like-button-7')).toBeInTheDocument()
    })

    await user.click(screen.getByTestId('post-like-button-7'))

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Like' })).toBeInTheDocument()
      expect(screen.getByTestId('post-like-count-7')).toBeEmptyDOMElement()
      expect(
        screen.getByText('Failed to like post. Please try again.')
      ).toBeInTheDocument()
    })
  })
})
