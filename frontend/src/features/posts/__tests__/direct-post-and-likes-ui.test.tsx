import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { TimelinePage } from '../../timeline/TimelinePage'
import { ProfilePage } from '../../profiles/ProfilePage'
import { DirectPostPage } from '../DirectPostPage'
import { apiClient } from '../../../shared/api/client'
import { ApiError } from '../../../shared/api/errors'
import {
  createMockPost,
  createMockProfile,
} from '../../../shared/test/factories'

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
    render(
      <MemoryRouter>
        <TimelinePage />
      </MemoryRouter>
    )

    await waitFor(() => {
      expect(screen.getByTestId('post-like-button-7')).toBeInTheDocument()
    })

    await user.click(screen.getByTestId('post-like-button-7'))

    await waitFor(() => {
      expect(apiClient.post).toHaveBeenCalledWith('/posts/7/like')
      expect(screen.getByRole('button', { name: 'Unlike' })).toBeInTheDocument()
      expect(screen.getByTestId('post-like-count-7')).toHaveTextContent(
        '1 like'
      )
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
    render(
      <MemoryRouter>
        <TimelinePage />
      </MemoryRouter>
    )

    await waitFor(() => {
      expect(screen.getByTestId('post-like-button-7')).toBeInTheDocument()
    })

    await user.click(screen.getByTestId('post-like-button-7'))

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Like' })).toBeInTheDocument()
      expect(screen.getByTestId('post-like-count-7')).toHaveTextContent(
        '0 likes'
      )
      expect(
        screen.getByText('Failed to like post. Please try again.')
      ).toBeInTheDocument()
    })
  })

  it('renders owner controls and like controls on a profile surface', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce({
      profile: createMockProfile({ username: 'bob', isSelf: true }),
      posts: [
        createMockPost({
          id: 3,
          authorUsername: 'bob',
          authorDisplayName: 'Bob Tester',
          canEdit: true,
          canDelete: true,
        }),
      ],
      nextCursor: null,
    })

    render(
      <MemoryRouter initialEntries={['/u/bob']}>
        <Routes>
          <Route path="/u/:username" element={<ProfilePage />} />
        </Routes>
      </MemoryRouter>
    )

    await waitFor(() => {
      expect(screen.getByTestId('post-card-3')).toBeInTheDocument()
    })

    expect(screen.getByTestId('post-like-button-3')).toBeInTheDocument()
    expect(screen.getByTestId('post-edit-button-3')).toBeInTheDocument()
    expect(screen.getByTestId('post-delete-button-3')).toBeInTheDocument()
  })

  it('renders a direct post and supports the unavailable-state recovery link', async () => {
    vi.mocked(apiClient.get).mockRejectedValueOnce(
      new ApiError(404, 'NOT_FOUND', 'Not found')
    )

    const user = userEvent.setup()
    render(
      <MemoryRouter initialEntries={['/posts/999999']}>
        <Routes>
          <Route path="/" element={<div>Timeline home</div>} />
          <Route path="/posts/:postId" element={<DirectPostPage />} />
        </Routes>
      </MemoryRouter>
    )

    await waitFor(() => {
      expect(screen.getByTestId('post-unavailable-state')).toBeInTheDocument()
    })

    await user.click(screen.getByTestId('post-unavailable-home-link'))

    await waitFor(() => {
      expect(screen.getByText('Timeline home')).toBeInTheDocument()
    })
  })

  it('renders a liked direct post with the shared controls', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(
      createMockPost({
        id: 11,
        authorUsername: 'alice',
        authorDisplayName: 'Alice Example',
        likeCount: 2,
        likedByViewer: true,
      })
    )

    render(
      <MemoryRouter initialEntries={['/posts/11']}>
        <Routes>
          <Route path="/posts/:postId" element={<DirectPostPage />} />
        </Routes>
      </MemoryRouter>
    )

    await waitFor(() => {
      expect(screen.getByTestId('post-page')).toBeInTheDocument()
    })

    expect(screen.getByTestId('post-card-11')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Unlike' })).toBeInTheDocument()
    expect(screen.getByTestId('post-like-count-11')).toHaveTextContent(
      '2 likes'
    )
    expect(screen.getByTestId('post-permalink-11')).toBeInTheDocument()
  })
})
