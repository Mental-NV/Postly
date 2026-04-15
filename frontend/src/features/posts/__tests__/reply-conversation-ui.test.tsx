import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from '../../../app/providers/AuthProvider'
import { DirectPostPage } from '../DirectPostPage'
import { createMockPost } from '../../../shared/test/factories'
import { apiClient } from '../../../shared/api/client'
import { ApiError } from '../../../shared/api/errors'
import type { ConversationResponse, PostResponse, SessionResponse } from '../../../shared/api/contracts'

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

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => vi.fn(),
    useParams: () => ({ postId: '10' }),
  }
})

const BOB_SESSION: SessionResponse = { userId: 1, username: 'bob', displayName: 'Bob Tester' }

function makeConversation(overrides?: Partial<ConversationResponse>): ConversationResponse {
  return {
    target: {
      state: 'available',
      post: createMockPost({ id: 10, authorUsername: 'alice', authorDisplayName: 'Alice Example', body: 'Parent post' }),
    },
    replies: [],
    nextCursor: null,
    ...overrides,
  }
}

function renderPage(session: SessionResponse | null = BOB_SESSION) {
  return render(
    <BrowserRouter>
      <AuthProvider initialSession={session}>
        <DirectPostPage />
      </AuthProvider>
    </BrowserRouter>
  )
}

describe('Conversation UI states', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  // Target available
  it('renders conversation-target when target is available', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation())
    renderPage()

    await waitFor(() => {
      expect(screen.getByTestId('conversation-target')).toBeInTheDocument()
    })
    expect(screen.getByText('Parent post')).toBeInTheDocument()
  })

  // Target unavailable
  it('renders conversation-target-unavailable when target state is unavailable', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation({
      target: { state: 'unavailable' },
    }))
    renderPage()

    await waitFor(() => {
      expect(screen.getByTestId('conversation-target-unavailable')).toBeInTheDocument()
    })
    expect(screen.queryByTestId('conversation-target')).not.toBeInTheDocument()
  })

  // Reply composer visible when authenticated and target available
  it('shows reply-composer when authenticated and target available', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation())
    renderPage(BOB_SESSION)

    await waitFor(() => {
      expect(screen.getByTestId('reply-composer')).toBeInTheDocument()
      expect(screen.getByTestId('reply-composer-input')).toBeInTheDocument()
      expect(screen.getByTestId('reply-submit-button')).toBeInTheDocument()
    })
  })

  // Reply composer hidden when unauthenticated
  it('hides reply-composer when unauthenticated', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation())
    renderPage(null)

    await waitFor(() => {
      expect(screen.getByTestId('conversation-target')).toBeInTheDocument()
    })
    expect(screen.queryByTestId('reply-composer')).not.toBeInTheDocument()
    expect(screen.queryByTestId('reply-submit-button')).not.toBeInTheDocument()
  })

  // Reply composer unavailable when target is unavailable
  it('shows reply-composer-unavailable when target is unavailable', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation({
      target: { state: 'unavailable' },
    }))
    renderPage(BOB_SESSION)

    await waitFor(() => {
      expect(screen.getByTestId('reply-composer-unavailable')).toBeInTheDocument()
    })
    expect(screen.queryByTestId('reply-submit-button')).not.toBeInTheDocument()
  })

  // Reply submit pending state
  it('shows pending state during reply submission', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation())
    vi.mocked(apiClient.post).mockImplementationOnce(() => new Promise(() => {}))

    const user = userEvent.setup()
    renderPage(BOB_SESSION)

    await waitFor(() => {
      expect(screen.getByTestId('reply-composer-input')).toBeInTheDocument()
    })

    await user.type(screen.getByTestId('reply-composer-input'), 'My reply')
    await user.click(screen.getByTestId('reply-submit-button'))

    expect(screen.getByTestId('reply-submit-button')).toBeDisabled()
    expect(screen.getByTestId('reply-submit-button')).toHaveTextContent('Posting…')
  })

  // Reply validation — submit disabled when empty
  it('disables reply-submit-button when input is empty', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation())
    renderPage(BOB_SESSION)

    await waitFor(() => {
      expect(screen.getByTestId('reply-submit-button')).toBeDisabled()
    })
  })

  // Reply appears in conversation after submit
  it('adds reply to conversation after successful submit', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation())
    const newReply = createMockPost({ id: 99, authorUsername: 'bob', body: 'New reply', isReply: true, replyToPostId: 10, canEdit: true, canDelete: true })
    vi.mocked(apiClient.post).mockResolvedValueOnce({ post: newReply } as PostResponse)

    const user = userEvent.setup()
    renderPage(BOB_SESSION)

    await waitFor(() => {
      expect(screen.getByTestId('reply-composer-input')).toBeInTheDocument()
    })

    await user.type(screen.getByTestId('reply-composer-input'), 'New reply')
    await user.click(screen.getByTestId('reply-submit-button'))

    await waitFor(() => {
      expect(screen.getByText('New reply')).toBeInTheDocument()
    })
    expect(screen.getByTestId('reply-composer-input')).toHaveValue('')
  })

  // Reply error state
  it('shows reply-form-status on reply submission failure', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation())
    vi.mocked(apiClient.post).mockRejectedValueOnce(new ApiError(400, 'error', 'Bad request', 'Body is required'))

    const user = userEvent.setup()
    renderPage(BOB_SESSION)

    await waitFor(() => {
      expect(screen.getByTestId('reply-composer-input')).toBeInTheDocument()
    })

    await user.type(screen.getByTestId('reply-composer-input'), 'x')
    await user.click(screen.getByTestId('reply-submit-button'))

    await waitFor(() => {
      expect(screen.getByTestId('reply-form-status')).toBeInTheDocument()
    })
  })

  // Deleted reply placeholder
  it('renders deleted-reply-placeholder for deleted replies', async () => {
    const deletedReply = createMockPost({ id: 55, state: 'deleted', body: null, authorUsername: null, authorDisplayName: null, canEdit: false, canDelete: false })
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation({ replies: [deletedReply] }))
    renderPage(BOB_SESSION)

    await waitFor(() => {
      expect(screen.getByTestId('deleted-reply-placeholder-55')).toBeInTheDocument()
    })
    expect(screen.queryByTestId('post-card-55')).not.toBeInTheDocument()
    expect(screen.queryByTestId('post-edit-button-55')).not.toBeInTheDocument()
    expect(screen.queryByTestId('post-delete-button-55')).not.toBeInTheDocument()
  })

  // Non-authored replies have no edit/delete controls
  it('does not render edit/delete controls for non-authored replies', async () => {
    const aliceReply = createMockPost({ id: 77, authorUsername: 'alice', body: 'Alice reply', isReply: true, canEdit: false, canDelete: false })
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation({ replies: [aliceReply] }))
    renderPage(BOB_SESSION)

    await waitFor(() => {
      expect(screen.getByTestId('post-card-77')).toBeInTheDocument()
    })
    expect(screen.queryByTestId('post-edit-button-77')).not.toBeInTheDocument()
    expect(screen.queryByTestId('post-delete-button-77')).not.toBeInTheDocument()
  })

  // Authored replies have edit/delete controls
  it('renders edit/delete controls for authored replies', async () => {
    const bobReply = createMockPost({ id: 88, authorUsername: 'bob', body: 'Bob reply', isReply: true, canEdit: true, canDelete: true })
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation({ replies: [bobReply] }))
    renderPage(BOB_SESSION)

    await waitFor(() => {
      expect(screen.getByTestId('post-edit-button-88')).toBeInTheDocument()
      expect(screen.getByTestId('post-delete-button-88')).toBeInTheDocument()
    })
  })

  // Reply inline edit
  it('enters inline edit mode for a reply', async () => {
    const bobReply = createMockPost({ id: 88, authorUsername: 'bob', body: 'Bob reply', isReply: true, canEdit: true, canDelete: true })
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation({ replies: [bobReply] }))

    const user = userEvent.setup()
    renderPage(BOB_SESSION)

    await waitFor(() => {
      expect(screen.getByTestId('post-edit-button-88')).toBeInTheDocument()
    })
    await user.click(screen.getByTestId('post-edit-button-88'))

    await waitFor(() => {
      expect(screen.getByTestId('post-editor-body-input-88')).toHaveValue('Bob reply')
    })
  })

  // Replies list rendered
  it('renders conversation-replies region', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(makeConversation())
    renderPage(BOB_SESSION)

    await waitFor(() => {
      expect(screen.getByTestId('conversation-replies')).toBeInTheDocument()
    })
  })
})
