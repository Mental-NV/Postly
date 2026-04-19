import { Circle, CircleDot } from 'lucide-react'
import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiClient } from '../../shared/api/client'
import type {
  NotificationsResponse,
  NotificationSummary,
  NotificationOpenResponse,
} from '../../shared/api/contracts'

export function NotificationsPage(): React.JSX.Element {
  const [notifications, setNotifications] = useState<NotificationSummary[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [openingNotificationId, setOpeningNotificationId] = useState<
    number | null
  >(null)
  const [error, setError] = useState<string | null>(null)
  const navigate = useNavigate()

  useEffect(() => {
    void loadNotifications()
  }, [])

  async function loadNotifications(): Promise<void> {
    setIsLoading(true)
    setError(null)

    try {
      const data = await apiClient.get<NotificationsResponse>('/notifications')
      setNotifications(data.notifications)
    } catch (err) {
      setError('Failed to load notifications')
      console.error(err)
    } finally {
      setIsLoading(false)
    }
  }

  async function handleNotificationClick(
    notificationId: number
  ): Promise<void> {
    setOpeningNotificationId(notificationId)

    try {
      const response = await apiClient.post<NotificationOpenResponse>(
        `/notifications/${notificationId}/open`
      )

      // Update the notification as read in local state
      setNotifications((prev) =>
        prev.map((n) =>
          n.id === notificationId ? response.notification : n
        )
      )

      // Navigate to destination
      if (response.destination.state === 'available') {
        void navigate(response.destination.route)
      } else {
        void navigate('/notifications/unavailable')
      }
    } catch (err) {
      console.error('Failed to open notification:', err)
    } finally {
      setOpeningNotificationId(null)
    }
  }

  if (isLoading) {
    return (
      <div data-testid="notifications-page">
        <h1 data-testid="notifications-heading">Notifications</h1>
        <div data-testid="notifications-status">Loading...</div>
      </div>
    )
  }

  if (error) {
    return (
      <div data-testid="notifications-page">
        <h1 data-testid="notifications-heading">Notifications</h1>
        <div data-testid="notifications-status">{error}</div>
      </div>
    )
  }

  if (notifications.length === 0) {
    return (
      <div data-testid="notifications-page">
        <h1 data-testid="notifications-heading">Notifications</h1>
        <div data-testid="notifications-empty-state">
          No notifications yet. When people interact with you, you'll see it
          here.
        </div>
      </div>
    )
  }

  return (
    <div data-testid="notifications-page">
      <h1 data-testid="notifications-heading">Notifications</h1>
      <div data-testid="notifications-list">
        {notifications.map((notification) => (
          <div
            key={notification.id}
            data-testid={`notification-item-${notification.id}`}
            role="button"
            tabIndex={0}
            onClick={() => void handleNotificationClick(notification.id)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault()
                void handleNotificationClick(notification.id)
              }
            }}
            style={{
              padding: '1rem',
              borderBottom: '1px solid #ccc',
              cursor: 'pointer',
              backgroundColor: notification.isRead
                ? 'transparent'
                : 'rgba(29, 155, 240, 0.05)',
              opacity: openingNotificationId === notification.id ? 0.5 : 1,
              display: 'flex',
              alignItems: 'center',
              gap: '0.75rem',
            }}
          >
            {notification.isRead ? (
              <span
                data-testid={`notification-read-indicator-${notification.id}`}
                aria-label="Read"
              >
                <Circle size={14} aria-hidden="true" />
              </span>
            ) : (
              <span
                data-testid={`notification-unread-indicator-${notification.id}`}
                aria-label="Unread"
              >
                <CircleDot size={14} aria-hidden="true" />
              </span>
            )}
            <span>
              {notification.actorDisplayName} {getNotificationText(notification)}
            </span>
          </div>
        ))}
      </div>
    </div>
  )
}

function getNotificationText(notification: NotificationSummary): string {
  switch (notification.kind) {
    case 'follow':
      return 'followed you'
    case 'like':
      return 'liked your post'
    case 'reply':
      return 'replied to your post'
    default:
      return 'interacted with you'
  }
}
