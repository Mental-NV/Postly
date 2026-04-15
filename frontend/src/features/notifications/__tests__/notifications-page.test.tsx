import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { NotificationsPage } from '../NotificationsPage'
import { apiClient } from '../../../shared/api/client'
import type { NotificationsResponse } from '../../../shared/api/contracts'

vi.mock('../../../shared/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
  },
}))

describe('NotificationsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows empty state when no notifications exist', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({
      notifications: [],
    } as NotificationsResponse)

    render(
      <MemoryRouter>
        <NotificationsPage />
      </MemoryRouter>
    )

    await waitFor(() => {
      expect(screen.getByTestId('notifications-empty-state')).toBeInTheDocument()
    })
  })

  it('displays unread and read notifications with correct indicators', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({
      notifications: [
        {
          id: 1,
          kind: 'follow',
          actorUsername: 'alice',
          actorDisplayName: 'Alice',
          createdAtUtc: new Date().toISOString(),
          isRead: false,
          destinationKind: 'profile',
          destinationRoute: '/u/alice',
          destinationState: 'available',
        },
        {
          id: 2,
          kind: 'like',
          actorUsername: 'charlie',
          actorDisplayName: 'Charlie',
          createdAtUtc: new Date().toISOString(),
          isRead: true,
          destinationKind: 'post',
          destinationRoute: '/posts/1',
          destinationState: 'available',
        },
      ],
    } as NotificationsResponse)

    render(
      <MemoryRouter>
        <NotificationsPage />
      </MemoryRouter>
    )

    await waitFor(() => {
      expect(screen.getByTestId('notification-unread-indicator-1')).toBeInTheDocument()
      expect(screen.getByTestId('notification-read-indicator-2')).toBeInTheDocument()
    })
  })
})
