import { Link } from 'react-router-dom'

export function NotificationUnavailablePage(): React.JSX.Element {
  return (
    <div data-testid="notification-unavailable-page">
      <h1>Content Not Available</h1>
      <p>The content you're looking for is no longer available.</p>
      <Link to="/notifications">← Back to Notifications</Link>
    </div>
  )
}