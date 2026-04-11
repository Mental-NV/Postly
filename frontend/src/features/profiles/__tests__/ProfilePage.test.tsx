import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { ProfilePage } from '../ProfilePage'
import {
  createMockProfile,
  createMockPost,
} from '../../../shared/test/factories'
import { apiClient } from '../../../shared/api/client'
import { ApiError } from '../../../shared/api/errors'

const processApi = (globalThis as typeof globalThis & { process: any }).process

vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
  },
}))

function renderProfilePage(username: string) {
  return render(
    <MemoryRouter initialEntries={[`/u/${username}`]}>
      <Routes>
        <Route path="/u/:username" element={<ProfilePage />} />
      </Routes>
    </MemoryRouter>
  )
}

describe('ProfilePage', () => {
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
    renderProfilePage('alice')
    expect(screen.getByText('Loading profile...')).toBeInTheDocument()
  })

  it('loads and displays profile successfully', async () => {
    const mockData = {
      profile: createMockProfile({
        username: 'alice',
        displayName: 'Alice Example',
      }),
      posts: [createMockPost()],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    renderProfilePage('alice')

    await waitFor(() => {
      expect(
        screen.getAllByRole('heading', { name: /Alice Example/i }).length
      ).toBeGreaterThan(0)
    })
  })

  it('verifies correct API URL for profile load', async () => {
    const mockData = {
      profile: createMockProfile(),
      posts: [],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    renderProfilePage('alice')

    await waitFor(() => {
      expect(apiClient.get).toHaveBeenCalledWith('/profiles/alice')
    })
  })

  it('shows 404 error when user not found', async () => {
    vi.mocked(apiClient.get).mockRejectedValueOnce(
      new ApiError(404, 'Not Found', 'User not found')
    )

    renderProfilePage('nonexistent')

    await waitFor(() => {
      expect(screen.getByText('User not found')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument()
    })
  })

  it('shows generic error for other failures', async () => {
    vi.mocked(apiClient.get).mockRejectedValueOnce(
      new ApiError(500, 'Server Error', 'Internal error')
    )

    renderProfilePage('alice')

    await waitFor(() => {
      expect(
        screen.getByText('Failed to load profile. Please try again.')
      ).toBeInTheDocument()
    })
  })

  // Self vs Other Profile Tests
  it('displays self profile with Edit Profile button', async () => {
    const mockData = {
      profile: createMockProfile({ isSelf: true }),
      posts: [],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    renderProfilePage('alice')

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /edit profile/i })).toBeInTheDocument()
      expect(
        screen.queryByRole('button', { name: /^follow$/i })
      ).not.toBeInTheDocument()
    })
  })

  it('displays other profile with follow button', async () => {
    const mockData = {
      profile: createMockProfile({ isSelf: false, isFollowedByViewer: false }),
      posts: [],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    renderProfilePage('alice')

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Follow' })).toBeInTheDocument()
    })
  })

  it('shows unfollow button when already following', async () => {
    const mockData = {
      profile: createMockProfile({ isSelf: false, isFollowedByViewer: true }),
      posts: [],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    renderProfilePage('alice')

    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: 'Unfollow' })
      ).toBeInTheDocument()
    })
  })

  // Follow/Unfollow Tests
  it('follows user with optimistic update', async () => {
    const mockData = {
      profile: createMockProfile({
        isFollowedByViewer: false,
        followerCount: 5,
      }),
      posts: [],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)
    vi.mocked(apiClient.post).mockResolvedValueOnce({})

    const user = userEvent.setup()
    renderProfilePage('alice')

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Follow' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Follow' }))

    // Optimistic update - button changes to Unfollow and count increments
    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: 'Unfollow' })
      ).toBeInTheDocument()
      expect(screen.getByText('6')).toBeInTheDocument()
    })

    expect(apiClient.post).toHaveBeenCalledWith('/profiles/alice/follow')
  })

  it('reverts follow on error', async () => {
    const mockData = {
      profile: createMockProfile({
        isFollowedByViewer: false,
        followerCount: 5,
      }),
      posts: [],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)
    vi.mocked(apiClient.post).mockRejectedValueOnce(
      new ApiError(500, 'Error', 'Failed')
    )

    const user = userEvent.setup()
    renderProfilePage('alice')

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Follow' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Follow' }))

    // Should revert to original state
    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Follow' })).toBeInTheDocument()
      expect(screen.getByText('5')).toBeInTheDocument()
      expect(screen.getByRole('alert')).toHaveTextContent(
        'Failed to follow user. Please try again.'
      )
    })
  })

  it('unfollows user with optimistic update', async () => {
    const mockData = {
      profile: createMockProfile({
        isFollowedByViewer: true,
        followerCount: 6,
      }),
      posts: [],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)
    vi.mocked(apiClient.delete).mockResolvedValueOnce(undefined)

    const user = userEvent.setup()
    renderProfilePage('alice')

    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: 'Unfollow' })
      ).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Unfollow' }))

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Follow' })).toBeInTheDocument()
      expect(screen.getByText('5')).toBeInTheDocument()
    })

    expect(apiClient.delete).toHaveBeenCalledWith('/profiles/alice/follow')
  })

  it('reverts unfollow on error', async () => {
    const mockData = {
      profile: createMockProfile({
        isFollowedByViewer: true,
        followerCount: 6,
      }),
      posts: [],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)
    vi.mocked(apiClient.delete).mockRejectedValueOnce(
      new ApiError(500, 'Error', 'Failed')
    )

    const user = userEvent.setup()
    renderProfilePage('alice')

    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: 'Unfollow' })
      ).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Unfollow' }))

    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: 'Unfollow' })
      ).toBeInTheDocument()
      expect(screen.getByText('6')).toBeInTheDocument()
      expect(screen.getByRole('alert')).toHaveTextContent(
        'Failed to unfollow user. Please try again.'
      )
    })
  })

  it('prevents concurrent follow operations', async () => {
    const mockData = {
      profile: createMockProfile({ isFollowedByViewer: false }),
      posts: [],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)
    vi.mocked(apiClient.post).mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 100))
    )

    const user = userEvent.setup()
    renderProfilePage('alice')

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Follow' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: /^follow$/i }))

    // Button should be disabled during operation
    expect(screen.getByRole('button', { name: '...' })).toBeDisabled()

    // Verify only one API call was made
    await waitFor(() => {
      expect(apiClient.post).toHaveBeenCalledTimes(1)
    })
  })

  it('displays follower and following counts', async () => {
    const mockData = {
      profile: createMockProfile({
        followerCount: 42,
        followingCount: 13,
      }),
      posts: [],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    renderProfilePage('alice')

    await waitFor(() => {
      expect(screen.getByText('13')).toBeInTheDocument()
      expect(screen.getByText('Following')).toBeInTheDocument()
      expect(screen.getByText('42')).toBeInTheDocument()
      expect(screen.getByText('Followers')).toBeInTheDocument()
    })
  })

  // Posts & Pagination Tests
  it('displays posts for profile', async () => {
    const mockData = {
      profile: createMockProfile(),
      posts: [
        createMockPost({ id: 1, body: 'First post' }),
        createMockPost({ id: 2, body: 'Second post' }),
      ],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    renderProfilePage('alice')

    await waitFor(() => {
      expect(screen.getByText('First post')).toBeInTheDocument()
      expect(screen.getByText('Second post')).toBeInTheDocument()
    })
  })

  it('shows empty state for self profile with no posts', async () => {
    const mockData = {
      profile: createMockProfile({ isSelf: true }),
      posts: [],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    renderProfilePage('alice')

    await waitFor(() => {
      expect(screen.getByText(/you haven't posted yet/i)).toBeInTheDocument()
    })
  })

  it('shows empty state for other profile with no posts', async () => {
    const mockData = {
      profile: createMockProfile({
        isSelf: false,
        displayName: 'Alice Example',
      }),
      posts: [],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    renderProfilePage('alice')

    await waitFor(() => {
      expect(
        screen.getByText(/no posts yet/i)
      ).toBeInTheDocument()
    })
  })

  it('loads more posts with pagination', async () => {
    const initialData = {
      profile: createMockProfile(),
      posts: [createMockPost({ id: 1, body: 'First post' })],
      nextCursor: 'cursor123',
    }
    const moreData = {
      profile: createMockProfile(),
      posts: [createMockPost({ id: 2, body: 'Second post' })],
      nextCursor: null,
    }

    vi.mocked(apiClient.get).mockResolvedValueOnce(initialData)
    vi.mocked(apiClient.get).mockResolvedValueOnce(moreData)

    const user = userEvent.setup()
    renderProfilePage('alice')

    await waitFor(() => {
      expect(screen.getByText('First post')).toBeInTheDocument()
      expect(
        screen.getByRole('button', { name: 'Load more' })
      ).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Load more' }))

    await waitFor(() => {
      expect(screen.getByText('Second post')).toBeInTheDocument()
      expect(
        screen.queryByRole('button', { name: 'Load more' })
      ).not.toBeInTheDocument()
    })

    expect(apiClient.get).toHaveBeenCalledWith(
      '/profiles/alice?cursor=cursor123'
    )
  })

  // Self Profile API Route Tests
  it('route /u/me calls apiClient.get("/profiles/me")', async () => {
    const mockData = {
      profile: createMockProfile({
        username: 'alice',
        displayName: 'Alice Example',
        isSelf: true,
      }),
      posts: [],
      nextCursor: null,
    }
    vi.mocked(apiClient.get).mockResolvedValueOnce(mockData)

    renderProfilePage('me')

    await waitFor(() => {
      expect(apiClient.get).toHaveBeenCalledWith('/profiles/me')
    })
  })

  it('route /u/me with pagination calls apiClient.get("/profiles/me?cursor=...")', async () => {
    const initialData = {
      profile: createMockProfile({ isSelf: true }),
      posts: [createMockPost({ id: 1, body: 'First post' })],
      nextCursor: 'cursor456',
    }
    const moreData = {
      profile: createMockProfile({ isSelf: true }),
      posts: [createMockPost({ id: 2, body: 'Second post' })],
      nextCursor: null,
    }

    vi.mocked(apiClient.get).mockResolvedValueOnce(initialData)
    vi.mocked(apiClient.get).mockResolvedValueOnce(moreData)

    const user = userEvent.setup()
    renderProfilePage('me')

    await waitFor(() => {
      expect(screen.getByText('First post')).toBeInTheDocument()
      expect(
        screen.getByRole('button', { name: 'Load more' })
      ).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Load more' }))

    await waitFor(() => {
      expect(apiClient.get).toHaveBeenCalledWith(
        '/profiles/me?cursor=cursor456'
      )
    })
  })
})
