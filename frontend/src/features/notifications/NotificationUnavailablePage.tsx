import { Link } from 'react-router-dom'

export function NotificationUnavailablePage(): React.JSX.Element {
  return (
    <div data-testid="notification-unavailable-destination">
      <p data-testid="notification-unavailable-message">
        This content is no longer available.
      </p>
      <Link to="/notifications" data-testid="notification-unavailable-back-link">
        Back to Notifications
      </Link>
    </div>
  )
}
