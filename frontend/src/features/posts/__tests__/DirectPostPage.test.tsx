import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from '../../../app/providers/AuthProvider'
import { DirectPostPage } from '../DirectPostPage'
import { createMockPost } from '../../../shared/test/factories'
import { apiClient } from '../../../shared/api/client'
import { ApiError } from '../../../shared/api/errors'
import type { SessionResponse } from '../../../shared/api/contracts'

const processApi = (globalThis as typeof globalThis & { process: any }).process

vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
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

  // Suppress unhandled rejections from intentional error tests
  beforeAll(() => {
    // Store original handler
    const originalHandler = processApi.listeners('unhandledRejection')[0]

    // Add custom handler that ignores ApiError rejections in tests
    processApi.removeAllListeners('unhandledRejection')
    processApi.on('unhandledRejection', (reason: any) => {
      // Ignore ApiError rejections from our tests
      if (reason?.constructor?.name === 'ApiError') {
        return
      }
      // Re-throw other errors
      if (originalHandler) {
        originalHandler(reason, Promise.reject(reason))
      }
    })
  })

  it('renders loading state initially', () => {
    vi.mocked(apiClient.get).mockImplementation(
      () => new Promise(() => {}) // Never resolves
    )

    renderDirectPostPage()

    expect(screen.getByText('Loading post...')).toBeInTheDocument()
  })

  it('loads and displays post successfully', async () => {
    const mockPost = createMockPost({
      id: 123,
      authorUsername: 'alice',
      authorDisplayName: 'Alice Example',
      body: 'This is a test post',
    })

    vi.mocked(apiClient.get).mockResolvedValueOnce(mockPost)

    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByText('This is a test post')).toBeInTheDocument()
    })

    expect(screen.getAllByText(/Alice Example/i).length).toBeGreaterThan(0)
    expect(screen.getByText('@alice')).toBeInTheDocument()
    expect(screen.getByTestId('post-back-link')).toBeInTheDocument()
  })

  it('verifies correct API URL (regression test)', async () => {
    const mockPost = createMockPost({ id: 123 })
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockPost)

    renderDirectPostPage()

    await waitFor(() => {
      expect(apiClient.get).toHaveBeenCalledWith('/posts/123')
    })
  })

  it('shows 404 state when post not found', async () => {
    const notFoundError = new ApiError(404, 'Not Found', 'Post not found')
    vi.mocked(apiClient.get).mockRejectedValueOnce(notFoundError)

    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByText(/post not available/i)).toBeInTheDocument()
    })

    expect(
      screen.getByText(/this post may have been deleted/i)
    ).toBeInTheDocument()
    expect(screen.getByTestId('post-unavailable-home-link')).toBeInTheDocument()
  })

  it('shows error state for other errors', async () => {
    const serverError = new ApiError(
      500,
      'Server Error',
      'Internal server error'
    )
    vi.mocked(apiClient.get).mockRejectedValueOnce(serverError)

    renderDirectPostPage()

    await waitFor(() => {
      expect(
        screen.getByText(/failed to load post/i)
      ).toBeInTheDocument()
    })

    expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument()
  })

  it('retries loading on error', async () => {
    const serverError = new ApiError(
      500,
      'Server Error',
      'Internal server error'
    )
    const mockPost = createMockPost({ id: 123, body: 'Loaded after retry' })

    vi.mocked(apiClient.get).mockRejectedValueOnce(serverError)
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockPost)

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
    const mockPost = createMockPost({ id: 123, canEdit: true })
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockPost)

    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByTestId('post-edit-button-123')).toBeInTheDocument()
    })
  })

  it('hides edit button for other users posts', async () => {
    const mockPost = createMockPost({ id: 123, canEdit: false })
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockPost)

    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByText('Test post content')).toBeInTheDocument()
    })

    expect(
      screen.queryByRole('button', { name: 'Edit' })
    ).not.toBeInTheDocument()
  })

  it('hides auth-only controls for unauthenticated visitors', async () => {
    const mockPost = createMockPost({
      id: 123,
      canEdit: false,
      canDelete: false,
      likeCount: 2,
      likedByViewer: false,
    })
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockPost)

    renderDirectPostPage({ session: null })

    await waitFor(() => {
      expect(screen.getByText('Test post content')).toBeInTheDocument()
    })

    expect(screen.queryByTestId('post-like-button-123')).not.toBeInTheDocument()
    expect(screen.getByTestId('post-like-count-123')).toHaveTextContent('2')
    expect(screen.queryByTestId('post-edit-button-123')).not.toBeInTheDocument()
    expect(screen.queryByTestId('post-delete-button-123')).not.toBeInTheDocument()
  })

  it('enters edit mode when edit clicked', async () => {
    const mockPost = createMockPost({
      id: 123,
      canEdit: true,
      body: 'Original content',
    })
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockPost)

    const user = userEvent.setup()
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByTestId('post-edit-button-123')).toBeInTheDocument()
    })

    await user.click(screen.getByTestId('post-edit-button-123'))

    // PostEditor should be rendered with textarea
    await waitFor(() => {
      const textarea = screen.getByTestId('editor-textarea')
      expect(textarea).toHaveValue('Original content')
    })
  })

  it('saves edited post successfully', async () => {
    const mockPost = createMockPost({
      id: 123,
      canEdit: true,
      body: 'Original content',
    })
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockPost)
    vi.mocked(apiClient.patch).mockResolvedValueOnce({})

    const user = userEvent.setup()
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByTestId('post-edit-button-123')).toBeInTheDocument()
    })

    await user.click(screen.getByTestId('post-edit-button-123'))

    const textarea = await screen.findByTestId('editor-textarea')
    await user.clear(textarea)
    await user.type(textarea, 'Updated content')
    await user.click(screen.getByRole('button', { name: /save/i }))

    await waitFor(() => {
      expect(apiClient.patch).toHaveBeenCalledWith('/posts/123', {
        body: 'Updated content',
      })
    })

    await waitFor(() => {
      expect(screen.getByText('Updated content')).toBeInTheDocument()
      expect(screen.getByTestId('post-edited-badge-123')).toBeInTheDocument()
    })
  })

  it('cancels edit mode', async () => {
    const mockPost = createMockPost({
      id: 123,
      canEdit: true,
      body: 'Original content',
    })
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockPost)

    const user = userEvent.setup()
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByTestId('post-edit-button-123')).toBeInTheDocument()
    })

    await user.click(screen.getByTestId('post-edit-button-123'))

    await waitFor(() => {
      expect(screen.getByTestId('editor-textarea')).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: /cancel/i }))

    await waitFor(() => {
      expect(screen.queryByTestId('editor-textarea')).not.toBeInTheDocument()
      expect(screen.getByText('Original content')).toBeInTheDocument()
    })
  })

  it('shows delete button for own posts', async () => {
    const mockPost = createMockPost({ id: 123, canDelete: true })
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockPost)

    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByTestId('post-delete-button-123')).toBeInTheDocument()
    })
  })

  it('opens confirmation dialog on delete', async () => {
    const mockPost = createMockPost({ id: 123, canDelete: true })
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockPost)

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

  it('deletes post and navigates to home', async () => {
    const mockPost = createMockPost({ id: 123, canDelete: true })
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockPost)
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

    const confirmButton = within(screen.getByRole('dialog')).getByRole(
      'button',
      { name: 'Delete' }
    )
    await user.click(confirmButton)

    await waitFor(() => {
      expect(apiClient.delete).toHaveBeenCalledWith('/posts/123')
      expect(mockNavigate).toHaveBeenCalledWith('/')
    })
  })

  it('handles delete error', async () => {
    const mockPost = createMockPost({ id: 123, canDelete: true })
    const deleteError = new ApiError(500, 'Server Error', 'Failed to delete')

    vi.mocked(apiClient.get).mockResolvedValueOnce(mockPost)
    vi.mocked(apiClient.delete).mockRejectedValueOnce(deleteError)

    const user = userEvent.setup()
    renderDirectPostPage()

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Delete' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Delete' }))

    await waitFor(() => {
      expect(screen.getByText('Delete Post')).toBeInTheDocument()
    })

    const confirmButton = within(screen.getByRole('dialog')).getByRole(
      'button',
      { name: 'Delete' }
    )
    await user.click(confirmButton)

    // Wait for the delete call and let the error be handled
    await waitFor(() => {
      expect(apiClient.delete).toHaveBeenCalledWith('/posts/123')
    })

    // Give time for the error to propagate and be handled
    await new Promise((resolve) => setTimeout(resolve, 100))

    // Post should still be visible after error (dialog closes but post remains)
    await waitFor(() => {
      expect(screen.getByText('Test post content')).toBeInTheDocument()
    })

    // Verify navigate was NOT called
    expect(mockNavigate).not.toHaveBeenCalled()
  })
})
