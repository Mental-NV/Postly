import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { TimelinePage } from '../TimelinePage'
import { createMockPost } from '../../../shared/test/factories'
import { apiClient } from '../../../shared/api/client'
import { renderWithProviders, mockAuthenticatedSession } from '../../../shared/test/helpers'

const processApi = (globalThis as typeof globalThis & { process: any }).process

vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
}))

describe('TimelinePage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  // Suppress unhandled rejections from intentional error tests
  beforeAll(() => {
    const originalHandler = processApi.listeners('unhandledRejection')[0]
    processApi.removeAllListeners('unhandledRejection')
    processApi.on('unhandledRejection', (reason: any) => {
      if (reason?.constructor?.name === 'ApiError') {
        return
      }
      if (originalHandler) {
        originalHandler(reason, Promise.reject(reason))
      }
    })
  })

  // Loading & Display Tests
  it('renders loading state initially', () => {
    vi.mocked(apiClient.get).mockImplementation(() => new Promise(() => {}))
    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })
    expect(screen.getByText(/loading timeline/i)).toBeInTheDocument()
  })

  it('loads and displays timeline successfully', async () => {
    const mockData = {
      posts: [
        createMockPost({ id: 1, body: 'First post' }),
        createMockPost({ id: 2, body: 'Second post' }),
      ],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })

    await waitFor(() => {
      expect(screen.getByText('First post')).toBeInTheDocument()
      expect(screen.getByText('Second post')).toBeInTheDocument()
    })
  })

  it('shows error state on load failure', async () => {
    vi.mocked(apiClient.get).mockRejectedValueOnce(new Error('Network error'))

    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })

    await waitFor(() => {
      expect(
        screen.getByText(/failed to load timeline/i)
      ).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument()
    })
  })

  it('retries loading on error', async () => {
    const mockData = {
      posts: [createMockPost({ body: 'Loaded after retry' })],
      nextCursor: null,
    }

    vi.mocked(apiClient.get).mockRejectedValueOnce(new Error('Network error'))
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    const user = userEvent.setup()
    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })

    const retryBtn = await screen.findByRole('button', { name: /retry/i })
    await user.click(retryBtn)

    await waitFor(() => {
      expect(screen.getByText('Loaded after retry')).toBeInTheDocument()
    })
  })

  // Empty State Test
  it('shows empty state when no posts', async () => {
    const mockData = { posts: [], nextCursor: null }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })

    await waitFor(() => {
      expect(screen.getByText(/your timeline is empty/i)).toBeInTheDocument()
    })
  })

  // Post Creation Tests
  it('renders composer component', async () => {
    const mockData = { posts: [], nextCursor: null }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })

    await waitFor(() => {
      expect(
        screen.getByPlaceholderText(/what's happening/i)
      ).toBeInTheDocument()
    })
  })

  it('reloads timeline after post creation', async () => {
    const initialData = { posts: [], nextCursor: null }
    const afterPostData = {
      posts: [createMockPost({ id: 1, body: 'New post' })],
      nextCursor: null,
    }

    vi.mocked(apiClient.get).mockResolvedValueOnce(initialData)
    vi.mocked(apiClient.post).mockResolvedValueOnce({ id: 1 })
    vi.mocked(apiClient.get).mockResolvedValueOnce(afterPostData)

    const user = userEvent.setup()
    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })

    const textarea = await screen.findByPlaceholderText(/what's happening/i)
    await user.type(textarea, 'New post')
    await user.click(screen.getByRole('button', { name: /post/i }))

    await waitFor(() => {
      expect(screen.getByText('New post')).toBeInTheDocument()
    })
  })

  // Pagination Tests
  it('loads more posts with pagination', async () => {
    const initialData = {
      posts: [createMockPost({ id: 1, body: 'First post' })],
      nextCursor: 'cursor123',
    }
    const moreData = {
      posts: [createMockPost({ id: 2, body: 'Second post' })],
      nextCursor: null,
    }

    vi.mocked(apiClient.get).mockResolvedValueOnce(initialData)
    vi.mocked(apiClient.get).mockResolvedValueOnce(moreData)

    const user = userEvent.setup()
    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })

    const loadMoreBtn = await screen.findByRole('button', { name: /load more/i })
    await user.click(loadMoreBtn)

    await waitFor(() => {
      expect(screen.getByText('Second post')).toBeInTheDocument()
      expect(
        screen.queryByRole('button', { name: /load more/i })
      ).not.toBeInTheDocument()
    })
  })

  it('shows loading state while loading more', async () => {
    const initialData = {
      posts: [createMockPost({ id: 1, body: 'First post' })],
      nextCursor: 'cursor123',
    }

    vi.mocked(apiClient.get).mockResolvedValueOnce(initialData)
    vi.mocked(apiClient.get).mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 100))
    )

    const user = userEvent.setup()
    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })

    const loadMoreBtn = await screen.findByRole('button', { name: /load more/i })
    await user.click(loadMoreBtn)

    expect(screen.getByRole('button', { name: /loading/i })).toBeDisabled()
  })

  // Edit Post Tests
  it('shows edit button for own posts', async () => {
    const mockData = {
      posts: [createMockPost({ id: 1, canEdit: true })],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })

    await waitFor(() => {
      expect(screen.getByTestId('post-edit-button-1')).toBeInTheDocument()
    })
  })

  it('enters edit mode when edit clicked', async () => {
    const mockData = {
      posts: [
        createMockPost({ id: 1, canEdit: true, body: 'Original content' }),
      ],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    const user = userEvent.setup()
    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })

    const editBtn = await screen.findByTestId('post-edit-button-1')
    await user.click(editBtn)

    await waitFor(() => {
      expect(screen.getByTestId('editor-textarea')).toHaveValue('Original content')
    })
  })

  it('saves edited post successfully', async () => {
    const mockData = {
      posts: [
        createMockPost({ id: 1, canEdit: true, body: 'Original content' }),
      ],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)
    vi.mocked(apiClient.patch).mockResolvedValueOnce({})

    const user = userEvent.setup()
    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })

    const editBtn = await screen.findByTestId('post-edit-button-1')
    await user.click(editBtn)

    const textarea = await screen.findByTestId('editor-textarea')
    await user.clear(textarea)
    await user.type(textarea, 'Updated content')
    await user.click(screen.getByRole('button', { name: /save/i }))

    await waitFor(() => {
      expect(apiClient.patch).toHaveBeenCalledWith('/posts/1', {
        body: 'Updated content',
      })
    })

    await waitFor(() => {
      expect(screen.getByText('Updated content')).toBeInTheDocument()
      expect(screen.getByTestId('post-edited-badge-1')).toBeInTheDocument()
    })
  })

  // Delete Post Tests
  it('shows delete button for own posts', async () => {
    const mockData = {
      posts: [createMockPost({ id: 1, canDelete: true })],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })

    await waitFor(() => {
      expect(screen.getByTestId('post-delete-button-1')).toBeInTheDocument()
    })
  })

  it('opens confirmation dialog on delete', async () => {
    const mockData = {
      posts: [createMockPost({ id: 1, canDelete: true })],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    const user = userEvent.setup()
    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })

    const deleteBtn = await screen.findByTestId('post-delete-button-1')
    await user.click(deleteBtn)

    await waitFor(() => {
      expect(screen.getByText(/delete post/i)).toBeInTheDocument()
    })
  })

  it('deletes post successfully', async () => {
    const mockData = {
      posts: [
        createMockPost({ id: 1, body: 'Post to delete', canDelete: true }),
        createMockPost({ id: 2, body: 'Other post' }),
      ],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)
    vi.mocked(apiClient.delete).mockResolvedValueOnce(undefined)

    const user = userEvent.setup()
    renderWithProviders(<TimelinePage />, { session: mockAuthenticatedSession() })

    const deleteBtn = await screen.findByTestId('post-delete-button-1')
    await user.click(deleteBtn)

    const confirmBtn = await screen.findByTestId('confirm-delete')
    await user.click(confirmBtn)

    await waitFor(() => {
      expect(apiClient.delete).toHaveBeenCalledWith('/posts/1')
    })

    await waitFor(() => {
      expect(screen.queryByText('Post to delete')).not.toBeInTheDocument()
      expect(screen.getByText('Other post')).toBeInTheDocument()
    })
  })
})
