import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import { NotificationUnavailablePage } from '../NotificationUnavailablePage'

function renderPage() {
  return render(
    <BrowserRouter>
      <NotificationUnavailablePage />
    </BrowserRouter>
  )
}

describe('NotificationUnavailablePage', () => {
  it('renders unavailable destination message', () => {
    renderPage()

    expect(screen.getByTestId('notification-unavailable-page')).toBeInTheDocument()
    expect(screen.getByText('Content Not Available')).toBeInTheDocument()
    expect(screen.getByText(/no longer available/)).toBeInTheDocument()
  })

  it('renders return to notifications link', () => {
    renderPage()

    const link = screen.getByRole('link', { name: /Back to Notifications/ })
    expect(link).toBeInTheDocument()
    expect(link).toHaveAttribute('href', '/notifications')
  })

  it('navigates back to notifications on link click', async () => {
    renderPage()

    const link = screen.getByRole('link', { name: /Back to Notifications/ })
    expect(link).toHaveAttribute('href', '/notifications')
  })
})
