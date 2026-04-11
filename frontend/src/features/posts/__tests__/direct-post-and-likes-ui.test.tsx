import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider } from '../../../app/providers/AuthProvider'
import { TimelinePage } from '../../timeline/TimelinePage'
import { ProfilePage } from '../../profiles/ProfilePage'
import { DirectPostPage } from '../DirectPostPage'
import { apiClient } from '../../../shared/api/client'
import { ApiError } from '../../../shared/api/errors'
import {
  renderWithProviders,
  mockAuthenticatedSession,
} from '../../../shared/test/helpers'
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
        <AuthProvider initialSession={mockAuthenticatedSession()}>
          <Routes>
            <Route path="/u/:username" element={<ProfilePage />} />
          </Routes>
        </AuthProvider>
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
        <AuthProvider initialSession={mockAuthenticatedSession()}>
          <Routes>
            <Route path="/" element={<div>Timeline home</div>} />
            <Route path="/posts/:postId" element={<DirectPostPage />} />
          </Routes>
        </AuthProvider>
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
        <AuthProvider initialSession={mockAuthenticatedSession()}>
          <Routes>
            <Route path="/posts/:postId" element={<DirectPostPage />} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>
    )

    await waitFor(() => {
      expect(screen.getByTestId('post-page')).toBeInTheDocument()
    })

    expect(screen.getByTestId('post-card-11')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Unlike' })).toBeInTheDocument()
    expect(screen.getByTestId('post-like-count-11')).toHaveTextContent('2')
    expect(screen.getByTestId('post-permalink-11')).toBeInTheDocument()
  })

  it('renders profile and direct post surfaces in read-only mode for unauthenticated visitors', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce({
      profile: createMockProfile({ username: 'alice', isSelf: false }),
      posts: [
        createMockPost({
          id: 21,
          authorUsername: 'alice',
          authorDisplayName: 'Alice Example',
          likeCount: 4,
          canEdit: false,
          canDelete: false,
        }),
      ],
      nextCursor: null,
    })
    vi.mocked(apiClient.get).mockResolvedValueOnce(
      createMockPost({
        id: 21,
        authorUsername: 'alice',
        authorDisplayName: 'Alice Example',
        likeCount: 4,
        canEdit: false,
        canDelete: false,
        likedByViewer: false,
      })
    )

    render(
      <MemoryRouter initialEntries={['/u/alice']}>
        <AuthProvider initialSession={null}>
          <Routes>
            <Route path="/u/:username" element={<ProfilePage />} />
            <Route path="/posts/:postId" element={<DirectPostPage />} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>
    )

    await waitFor(() => {
      expect(screen.getByTestId('profile-page')).toBeInTheDocument()
    })

    expect(screen.queryByTestId('follow-unfollow-button')).not.toBeInTheDocument()
    expect(screen.queryByTestId('post-like-button-21')).not.toBeInTheDocument()
    expect(screen.getByTestId('post-like-count-21')).toHaveTextContent('4')

    await userEvent.setup().click(screen.getByTestId('post-permalink-21'))

    await waitFor(() => {
      expect(screen.getByTestId('post-page')).toBeInTheDocument()
    })

    expect(screen.queryByTestId('post-like-button-21')).not.toBeInTheDocument()
    expect(screen.getByTestId('post-like-count-21')).toHaveTextContent('4')
    expect(screen.queryByTestId('post-edit-button-21')).not.toBeInTheDocument()
    expect(screen.queryByTestId('post-delete-button-21')).not.toBeInTheDocument()
  })
})
