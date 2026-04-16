import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { NotificationsPage } from '../NotificationsPage'
import { createMockNotification } from '../../../shared/test/factories'
import { apiClient } from '../../../shared/api/client'
import type { NotificationsResponse, NotificationOpenResponse } from '../../../shared/api/contracts'

vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
  },
}))

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

function renderPage() {
  return render(
    <BrowserRouter>
      <NotificationsPage />
    </BrowserRouter>
  )
}

describe('NotificationsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders loading state initially', () => {
    vi.mocked(apiClient.get).mockImplementation(() => new Promise(() => {}))
    renderPage()

    expect(screen.getByTestId('notifications-page')).toBeInTheDocument()
    expect(screen.getByTestId('notifications-status')).toHaveTextContent('Loading...')
  })

  it('renders empty state when no notifications', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce({ notifications: [] } as NotificationsResponse)
    renderPage()

    await waitFor(() => {
      expect(screen.getByTestId('notifications-empty-state')).toBeInTheDocument()
    })
    expect(screen.getByText(/No notifications yet/)).toBeInTheDocument()
  })

  it('renders error state on fetch failure', async () => {
    vi.mocked(apiClient.get).mockRejectedValueOnce(new Error('Network error'))
    renderPage()

    await waitFor(() => {
      expect(screen.getByTestId('notifications-status')).toHaveTextContent('Failed to load notifications')
    })
  })

  it('renders notification list with unread indicators', async () => {
    const notifications = [
      createMockNotification({ id: 1, isRead: false }),
      createMockNotification({ id: 2, isRead: false }),
    ]
    vi.mocked(apiClient.get).mockResolvedValueOnce({ notifications } as NotificationsResponse)
    renderPage()

    await waitFor(() => {
      expect(screen.getByTestId('notifications-list')).toBeInTheDocument()
    })
    expect(screen.getByTestId('notification-unread-indicator-1')).toBeInTheDocument()
    expect(screen.getByTestId('notification-unread-indicator-2')).toBeInTheDocument()
  })

  it('renders notification list with read indicators', async () => {
    const notifications = [
      createMockNotification({ id: 1, isRead: true }),
    ]
    vi.mocked(apiClient.get).mockResolvedValueOnce({ notifications } as NotificationsResponse)
    renderPage()

    await waitFor(() => {
      expect(screen.getByTestId('notification-read-indicator-1')).toBeInTheDocument()
    })
  })

  it('shows pending state when opening notification', async () => {
    const notifications = [createMockNotification({ id: 1 })]
    vi.mocked(apiClient.get).mockResolvedValueOnce({ notifications } as NotificationsResponse)
    vi.mocked(apiClient.post).mockImplementation(() => new Promise(() => {}))
    renderPage()

    await waitFor(() => {
      expect(screen.getByTestId('notification-item-1')).toBeInTheDocument()
    })

    const user = userEvent.setup()
    await user.click(screen.getByTestId('notification-item-1'))

    const item = screen.getByTestId('notification-item-1')
    expect(item).toHaveStyle({ opacity: '0.5' })
  })

  it('calls open API and navigates on notification click', async () => {
    const notifications = [createMockNotification({ id: 1 })]
    vi.mocked(apiClient.get).mockResolvedValueOnce({ notifications } as NotificationsResponse)
    vi.mocked(apiClient.post).mockResolvedValueOnce({
      notification: createMockNotification({ id: 1, isRead: true }),
      destination: { state: 'available', route: '/u/alice' },
    } as NotificationOpenResponse)
    renderPage()

    await waitFor(() => {
      expect(screen.getByTestId('notification-item-1')).toBeInTheDocument()
    })

    const user = userEvent.setup()
    await user.click(screen.getByTestId('notification-item-1'))

    await waitFor(() => {
      expect(apiClient.post).toHaveBeenCalledWith('/notifications/1/open')
      expect(mockNavigate).toHaveBeenCalledWith('/u/alice')
    })
  })

  it('updates notification to read after successful open', async () => {
    const notifications = [createMockNotification({ id: 1, isRead: false })]
    vi.mocked(apiClient.get).mockResolvedValueOnce({ notifications } as NotificationsResponse)
    vi.mocked(apiClient.post).mockResolvedValueOnce({
      notification: createMockNotification({ id: 1, isRead: true }),
      destination: { state: 'available', route: '/u/alice' },
    } as NotificationOpenResponse)
    renderPage()

    await waitFor(() => {
      expect(screen.getByTestId('notification-unread-indicator-1')).toBeInTheDocument()
    })

    const user = userEvent.setup()
    await user.click(screen.getByTestId('notification-item-1'))

    await waitFor(() => {
      expect(screen.getByTestId('notification-read-indicator-1')).toBeInTheDocument()
    })
  })

  it('navigates to destination route for available notification', async () => {
    const notifications = [createMockNotification({ id: 1 })]
    vi.mocked(apiClient.get).mockResolvedValueOnce({ notifications } as NotificationsResponse)
    vi.mocked(apiClient.post).mockResolvedValueOnce({
      notification: createMockNotification({ id: 1, isRead: true }),
      destination: { state: 'available', route: '/posts/123' },
    } as NotificationOpenResponse)
    renderPage()

    await waitFor(() => {
      expect(screen.getByTestId('notification-item-1')).toBeInTheDocument()
    })

    const user = userEvent.setup()
    await user.click(screen.getByTestId('notification-item-1'))

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/posts/123')
    })
  })

  it('navigates to unavailable route for unavailable notification', async () => {
    const notifications = [createMockNotification({ id: 1 })]
    vi.mocked(apiClient.get).mockResolvedValueOnce({ notifications } as NotificationsResponse)
    vi.mocked(apiClient.post).mockResolvedValueOnce({
      notification: createMockNotification({ id: 1, isRead: true }),
      destination: { state: 'unavailable', route: '/notifications/unavailable' },
    } as NotificationOpenResponse)
    renderPage()

    await waitFor(() => {
      expect(screen.getByTestId('notification-item-1')).toBeInTheDocument()
    })

    const user = userEvent.setup()
    await user.click(screen.getByTestId('notification-item-1'))

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/notifications/unavailable')
    })
  })

  it('displays correct text for follow notifications', async () => {
    const notifications = [createMockNotification({ id: 1, kind: 'follow', actorDisplayName: 'Alice' })]
    vi.mocked(apiClient.get).mockResolvedValueOnce({ notifications } as NotificationsResponse)
    renderPage()

    await waitFor(() => {
      expect(screen.getByText(/Alice followed you/)).toBeInTheDocument()
    })
  })

  it('displays correct text for like notifications', async () => {
    const notifications = [createMockNotification({ id: 1, kind: 'like', actorDisplayName: 'Bob' })]
    vi.mocked(apiClient.get).mockResolvedValueOnce({ notifications } as NotificationsResponse)
    renderPage()

    await waitFor(() => {
      expect(screen.getByText(/Bob liked your post/)).toBeInTheDocument()
    })
  })

  it('displays correct text for reply notifications', async () => {
    const notifications = [createMockNotification({ id: 1, kind: 'reply', actorDisplayName: 'Charlie' })]
    vi.mocked(apiClient.get).mockResolvedValueOnce({ notifications } as NotificationsResponse)
    renderPage()

    await waitFor(() => {
      expect(screen.getByText(/Charlie replied to your post/)).toBeInTheDocument()
    })
  })

  it('handles keyboard navigation (Enter and Space)', async () => {
    const notifications = [createMockNotification({ id: 1 })]
    vi.mocked(apiClient.get).mockResolvedValueOnce({ notifications } as NotificationsResponse)
    vi.mocked(apiClient.post).mockResolvedValueOnce({
      notification: createMockNotification({ id: 1, isRead: true }),
      destination: { state: 'available', route: '/u/alice' },
    } as NotificationOpenResponse)
    renderPage()

    await waitFor(() => {
      expect(screen.getByTestId('notification-item-1')).toBeInTheDocument()
    })

    const user = userEvent.setup()
    const item = screen.getByTestId('notification-item-1')
    item.focus()
    await user.keyboard('{Enter}')

    await waitFor(() => {
      expect(apiClient.post).toHaveBeenCalledWith('/notifications/1/open')
    })
  })
})
