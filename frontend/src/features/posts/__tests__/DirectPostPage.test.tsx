import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest'
import { act, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from '../../../app/providers/AuthProvider'
import { DirectPostPage } from '../DirectPostPage'
import { createMockPost } from '../../../shared/test/factories'
import { apiClient } from '../../../shared/api/client'
import { ApiError } from '../../../shared/api/errors'
import { emitProfileIdentityUpdated } from '../../../shared/profileIdentityEvents'
import type { ConversationResponse, SessionResponse } from '../../../shared/api/contracts'

const processApi = (globalThis as typeof globalThis & { process: any }).process

vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
  getConversationPath: (postId: string | number) => `/posts/${String(postId)}`,
  getRepliesPath: (postId: string | number) => `/posts/${String(postId)}/replies`,
}))

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useParams: () => ({ postId: '123' }),
  }
})

function makeConversation(overrides?: Partial<ConversationResponse>): ConversationResponse {
  return {
    target: {
      state: 'available',
      post: createMockPost({ id: 123, authorUsername: 'alice', authorDisplayName: 'Alice Example', body: 'This is a test post' }),
    },
    replies: [],
    nextCursor: null,
    ...overrides,
  }
}

function renderDirectPostPage({
  session = { userId: 2, username: 'bob', displayName: 'Bob Tester' } as SessionResponse | null,
} = {}) {
  return render(
    <BrowserRouter>
      <AuthProvider initialSession={session}>
        <DirectPostPage />
      </AuthProvider>
    </BrowserRouter>
  )
}

describe('DirectPostPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  beforeAll(() => {
    const originalHandler = processApi.listeners('unhandledRejection')[0]
    processApi.removeAllListeners('unhandledRejection')
    processApi.on('unhandledRejection', (reason: any) => {
      if (reason?.constructor?.name === 'ApiError') return
      if (originalHandler) originalHandler(reason, Promise.reject(reason))
    })
  })

  it('renders loading state initially', () => {
    vi.mocked(apiClient.get).mockImplementation(() => new Promise(() => {}))
    renderDirectPostPage()
    expect(screen.getByText('Loading post...')).toBeInTheDocument()
  })

  it('loads and displays post successfully', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation())
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByText('This is a test post')).toBeInTheDocument()
    })
    expect(screen.getAllByText(/Alice Example/i).length).toBeGreaterThan(0)
    expect(screen.getByTestId('conversation-target')).toBeInTheDocument()
  })

  it('refreshes the visible post identity when a profile identity update is emitted', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(
      makeConversation({
        target: {
          state: 'available',
          post: createMockPost({ id: 123, authorUsername: 'bob', authorDisplayName: 'Bob Tester', authorAvatarUrl: null }),
        },
      })
    )
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByText('Bob Tester')).toBeInTheDocument()
    })

    await act(async () => {
      emitProfileIdentityUpdated({
        username: 'bob',
        displayName: 'Bob Updated',
        bio: 'Updated bio',
        avatarUrl: '/api/profiles/bob/avatar?v=5',
        hasCustomAvatar: true,
        followerCount: 0,
        followingCount: 0,
        isSelf: true,
        isFollowedByViewer: false,
      })
    })

    await waitFor(() => {
      expect(screen.getByText('Bob Updated')).toBeInTheDocument()
    })
  })

  it('shows 404 state when post not found', async () => {
    vi.mocked(apiClient.get).mockRejectedValueOnce(new ApiError(404, 'Not Found', 'Post not found'))
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByText(/post not available/i)).toBeInTheDocument()
    })
    expect(screen.getByTestId('post-unavailable-home-link')).toBeInTheDocument()
  })

  it('shows error state for other errors', async () => {
    vi.mocked(apiClient.get).mockRejectedValueOnce(new ApiError(500, 'Server Error', 'Internal server error'))
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByText(/failed to load post/i)).toBeInTheDocument()
    })
    expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument()
  })

  it('retries loading on error', async () => {
    vi.mocked(apiClient.get).mockRejectedValueOnce(new ApiError(500, 'Server Error', 'Internal server error'))
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation({
      target: { state: 'available', post: createMockPost({ id: 123, body: 'Loaded after retry' }) },
    }))

    const user = userEvent.setup()
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument()
    })
    await user.click(screen.getByRole('button', { name: 'Retry' }))

    await waitFor(() => {
      expect(screen.getByText('Loaded after retry')).toBeInTheDocument()
    })
  })

  it('shows edit button for own posts', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation({
      target: { state: 'available', post: createMockPost({ id: 123, canEdit: true }) },
    }))
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByTestId('post-edit-button-123')).toBeInTheDocument()
    })
  })

  it('hides edit button for other users posts', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation({
      target: { state: 'available', post: createMockPost({ id: 123, canEdit: false }) },
    }))
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByText('Test post content')).toBeInTheDocument()
    })
    expect(screen.queryByRole('button', { name: 'Edit' })).not.toBeInTheDocument()
  })

  it('enters edit mode when edit clicked', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation({
      target: { state: 'available', post: createMockPost({ id: 123, canEdit: true, body: 'Original content' }) },
    }))

    const user = userEvent.setup()
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByTestId('post-edit-button-123')).toBeInTheDocument()
    })
    await user.click(screen.getByTestId('post-edit-button-123'))

    await waitFor(() => {
      expect(screen.getByTestId('post-editor-body-input-123')).toHaveValue('Original content')
    })
  })

  it('saves edited post successfully', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation({
      target: { state: 'available', post: createMockPost({ id: 123, canEdit: true, body: 'Original content' }) },
    }))
    vi.mocked(apiClient.patch).mockResolvedValueOnce({
      post: createMockPost({ id: 123, body: 'Updated content', isEdited: true }),
    })

    const user = userEvent.setup()
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByTestId('post-edit-button-123')).toBeInTheDocument()
    })
    await user.click(screen.getByTestId('post-edit-button-123'))

    const textarea = await screen.findByTestId('post-editor-body-input-123')
    await user.clear(textarea)
    await user.type(textarea, 'Updated content')
    await user.click(screen.getByRole('button', { name: /save/i }))

    await waitFor(() => {
      expect(apiClient.patch).toHaveBeenCalledWith('/posts/123', { body: 'Updated content' })
    })
    await waitFor(() => {
      expect(screen.getByText('Updated content')).toBeInTheDocument()
    })
  })

  it('cancels edit mode', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation({
      target: { state: 'available', post: createMockPost({ id: 123, canEdit: true, body: 'Original content' }) },
    }))

    const user = userEvent.setup()
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByTestId('post-edit-button-123')).toBeInTheDocument()
    })
    await user.click(screen.getByTestId('post-edit-button-123'))

    await waitFor(() => {
      expect(screen.getByTestId('post-editor-body-input-123')).toBeInTheDocument()
    })
    await user.click(screen.getByRole('button', { name: /cancel/i }))

    await waitFor(() => {
      expect(screen.queryByTestId('post-editor-body-input-123')).not.toBeInTheDocument()
      expect(screen.getByText('Original content')).toBeInTheDocument()
    })
  })

  it('shows delete button for own posts', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation({
      target: { state: 'available', post: createMockPost({ id: 123, canDelete: true }) },
    }))
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByTestId('post-delete-button-123')).toBeInTheDocument()
    })
  })

  it('opens confirmation dialog on delete', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation({
      target: { state: 'available', post: createMockPost({ id: 123, canDelete: true }) },
    }))

    const user = userEvent.setup()
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByTestId('post-delete-button-123')).toBeInTheDocument()
    })
    await user.click(screen.getByTestId('post-delete-button-123'))

    await waitFor(() => {
      expect(screen.getByText(/delete post/i)).toBeInTheDocument()
    })
  })

  it('deletes target post and navigates to home', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation({
      target: { state: 'available', post: createMockPost({ id: 123, canDelete: true }) },
    }))
    vi.mocked(apiClient.delete).mockResolvedValueOnce(undefined)

    const user = userEvent.setup()
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Delete' })).toBeInTheDocument()
    })
    await user.click(screen.getByRole('button', { name: 'Delete' }))

    await waitFor(() => {
      expect(screen.getByText('Delete Post')).toBeInTheDocument()
    })

    const confirmButton = within(screen.getByRole('dialog')).getByRole('button', { name: 'Delete' })
    await user.click(confirmButton)

    await waitFor(() => {
      expect(apiClient.delete).toHaveBeenCalledWith('/posts/123')
      expect(mockNavigate).toHaveBeenCalledWith('/')
    })
  })
})
