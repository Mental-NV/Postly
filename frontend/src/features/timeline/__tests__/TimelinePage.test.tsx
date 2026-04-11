import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { TimelinePage } from '../TimelinePage'
import { createMockPost } from '../../../shared/test/factories'
import { apiClient } from '../../../shared/api/client'

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
    render(<BrowserRouter><TimelinePage /></BrowserRouter>)
    expect(screen.getByText('Loading timeline...')).toBeInTheDocument()
  })

  it('loads and displays timeline successfully', async () => {
    const mockData = {
      posts: [
        createMockPost({ id: 1, body: 'First post' }),
        createMockPost({ id: 2, body: 'Second post' })
      ],
      nextCursor: null
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    render(<BrowserRouter><TimelinePage /></BrowserRouter>)

    await waitFor(() => {
      expect(screen.getByText('First post')).toBeInTheDocument()
      expect(screen.getByText('Second post')).toBeInTheDocument()
    })
  })

  it('verifies correct API URL for timeline load', async () => {
    const mockData = { posts: [], nextCursor: null }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    render(<BrowserRouter><TimelinePage /></BrowserRouter>)

    await waitFor(() => {
      expect(apiClient.get).toHaveBeenCalledWith('/timeline')
    })
  })

  it('shows error state on load failure', async () => {
    vi.mocked(apiClient.get).mockRejectedValueOnce(new Error('Network error'))

    render(<BrowserRouter><TimelinePage /></BrowserRouter>)

    await waitFor(() => {
      expect(screen.getByText('Failed to load timeline. Please try again.')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument()
    })
  })

  it('retries loading on error', async () => {
    const mockData = { posts: [createMockPost({ body: 'Loaded after retry' })], nextCursor: null }

    vi.mocked(apiClient.get).mockRejectedValueOnce(new Error('Network error'))
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    const user = userEvent.setup()
    render(<BrowserRouter><TimelinePage /></BrowserRouter>)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Retry' }))

    await waitFor(() => {
      expect(screen.getByText('Loaded after retry')).toBeInTheDocument()
    })
  })

  // Empty State Test
  it('shows empty state when no posts', async () => {
    const mockData = { posts: [], nextCursor: null }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    render(<BrowserRouter><TimelinePage /></BrowserRouter>)

    await waitFor(() => {
      expect(screen.getByText('Your timeline is empty.')).toBeInTheDocument()
      expect(screen.getByText('Create a post or follow other users to see content here.')).toBeInTheDocument()
    })
  })

  // Post Creation Tests
  it('renders composer component', async () => {
    const mockData = { posts: [], nextCursor: null }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    render(<BrowserRouter><TimelinePage /></BrowserRouter>)

    await waitFor(() => {
      expect(screen.getByPlaceholderText("What's happening?")).toBeInTheDocument()
    })
  })

  it('reloads timeline after post creation', async () => {
    const initialData = { posts: [], nextCursor: null }
    const afterPostData = {
      posts: [createMockPost({ id: 1, body: 'New post' })],
      nextCursor: null
    }

    vi.mocked(apiClient.get).mockResolvedValueOnce(initialData)
    vi.mocked(apiClient.post).mockResolvedValueOnce({ id: 1 })
    vi.mocked(apiClient.get).mockResolvedValueOnce(afterPostData)

    const user = userEvent.setup()
    render(<BrowserRouter><TimelinePage /></BrowserRouter>)

    await waitFor(() => {
      expect(screen.getByPlaceholderText("What's happening?")).toBeInTheDocument()
    })

    const textarea = screen.getByPlaceholderText("What's happening?")
    await user.type(textarea, 'New post')
    await user.click(screen.getByRole('button', { name: 'Post' }))

    await waitFor(() => {
      expect(screen.getByText('New post')).toBeInTheDocument()
    })

    expect(apiClient.get).toHaveBeenCalledTimes(2) // Initial load + reload after post
  })

  // Pagination Tests
  it('loads more posts with pagination', async () => {
    const initialData = {
      posts: [createMockPost({ id: 1, body: 'First post' })],
      nextCursor: 'cursor123'
    }
    const moreData = {
      posts: [createMockPost({ id: 2, body: 'Second post' })],
      nextCursor: null
    }

    vi.mocked(apiClient.get).mockResolvedValueOnce(initialData)
    vi.mocked(apiClient.get).mockResolvedValueOnce(moreData)

    const user = userEvent.setup()
    render(<BrowserRouter><TimelinePage /></BrowserRouter>)

    await waitFor(() => {
      expect(screen.getByText('First post')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Load more' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Load more' }))

    await waitFor(() => {
      expect(screen.getByText('Second post')).toBeInTheDocument()
      expect(screen.queryByRole('button', { name: 'Load more' })).not.toBeInTheDocument()
    })

    expect(apiClient.get).toHaveBeenCalledWith('/timeline?cursor=cursor123')
  })

  it('shows loading state while loading more', async () => {
    const initialData = {
      posts: [createMockPost({ id: 1, body: 'First post' })],
      nextCursor: 'cursor123'
    }

    vi.mocked(apiClient.get).mockResolvedValueOnce(initialData)
    vi.mocked(apiClient.get).mockImplementation(() => new Promise(resolve => setTimeout(resolve, 100)))

    const user = userEvent.setup()
    render(<BrowserRouter><TimelinePage /></BrowserRouter>)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Load more' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Load more' }))

    expect(screen.getByRole('button', { name: 'Loading...' })).toBeDisabled()
  })

  // Edit Post Tests
  it('shows edit button for own posts', async () => {
    const mockData = {
      posts: [createMockPost({ id: 1, canEdit: true })],
      nextCursor: null
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    render(<BrowserRouter><TimelinePage /></BrowserRouter>)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Edit' })).toBeInTheDocument()
    })
  })

  it('enters edit mode when edit clicked', async () => {
    const mockData = {
      posts: [createMockPost({ id: 1, canEdit: true, body: 'Original content' })],
      nextCursor: null
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    const user = userEvent.setup()
    render(<BrowserRouter><TimelinePage /></BrowserRouter>)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Edit' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Edit' }))

    await waitFor(() => {
      const textarea = screen.getByTestId('editor-textarea')
      expect(textarea).toHaveValue('Original content')
    })
  })

  it('saves edited post successfully', async () => {
    const mockData = {
      posts: [createMockPost({ id: 1, canEdit: true, body: 'Original content' })],
      nextCursor: null
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)
    vi.mocked(apiClient.patch).mockResolvedValueOnce({})

    const user = userEvent.setup()
    render(<BrowserRouter><TimelinePage /></BrowserRouter>)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Edit' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Edit' }))

    const textarea = await screen.findByTestId('editor-textarea')
    await user.clear(textarea)
    await user.type(textarea, 'Updated content')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => {
      expect(apiClient.patch).toHaveBeenCalledWith('/posts/1', { body: 'Updated content' })
    })

    await waitFor(() => {
      expect(screen.getByText('Updated content')).toBeInTheDocument()
      expect(screen.getByText('(edited)')).toBeInTheDocument()
    })
  })

  // Delete Post Tests
  it('shows delete button for own posts', async () => {
    const mockData = {
      posts: [createMockPost({ id: 1, canDelete: true })],
      nextCursor: null
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    render(<BrowserRouter><TimelinePage /></BrowserRouter>)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Delete' })).toBeInTheDocument()
    })
  })

  it('opens confirmation dialog on delete', async () => {
    const mockData = {
      posts: [createMockPost({ id: 1, canDelete: true })],
      nextCursor: null
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    const user = userEvent.setup()
    render(<BrowserRouter><TimelinePage /></BrowserRouter>)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Delete' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Delete' }))

    await waitFor(() => {
      expect(screen.getByText('Delete Post')).toBeInTheDocument()
      expect(screen.getByText('Are you sure you want to delete this post? This action cannot be undone.')).toBeInTheDocument()
    })
  })

  it('deletes post successfully', async () => {
    const mockData = {
      posts: [
        createMockPost({ id: 1, body: 'Post to delete', canDelete: true }),
        createMockPost({ id: 2, body: 'Other post' })
      ],
      nextCursor: null
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)
    vi.mocked(apiClient.delete).mockResolvedValueOnce(undefined)

    const user = userEvent.setup()
    render(<BrowserRouter><TimelinePage /></BrowserRouter>)

    await waitFor(() => {
      expect(screen.getByText('Post to delete')).toBeInTheDocument()
    })

    const deleteButtons = screen.getAllByRole('button', { name: 'Delete' })
    await user.click(deleteButtons[0]!)

    await waitFor(() => {
      expect(screen.getByText('Delete Post')).toBeInTheDocument()
    })

    const confirmButton = screen.getAllByRole('button', { name: 'Delete' })[1]!
    await user.click(confirmButton)

    await waitFor(() => {
      expect(apiClient.delete).toHaveBeenCalledWith('/posts/1')
    })

    await waitFor(() => {
      expect(screen.queryByText('Post to delete')).not.toBeInTheDocument()
      expect(screen.getByText('Other post')).toBeInTheDocument()
    })
  })
})
