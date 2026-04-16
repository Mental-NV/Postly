import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { MainLayout } from '../MainLayout'
import { AuthContext, type AuthContextValue } from '../../../app/providers/AuthContext'

function createAuthValue(
  overrides: Partial<AuthContextValue> = {}
): AuthContextValue {
  return {
    session: {
      userId: 1,
      username: 'alice',
      displayName: 'Alice Example',
    },
    isLoading: false,
    isAuthenticated: true,
    signin: vi.fn(),
    signout: vi.fn(),
    ...overrides,
  }
}

function renderMainLayout(
  authValue: AuthContextValue,
  initialEntry = '/'
): void {
  render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <AuthContext.Provider value={authValue}>
        <MainLayout>
          <div>Content</div>
        </MainLayout>
      </AuthContext.Provider>
    </MemoryRouter>
  )
}

describe('MainLayout navigation', () => {
  it('renders a notifications nav link for authenticated users', () => {
    renderMainLayout(createAuthValue())

    const notificationsLink = screen.getByTestId('nav-notifications-link')

    expect(notificationsLink).toBeInTheDocument()
    expect(notificationsLink).toHaveAttribute('href', '/notifications')
    expect(notificationsLink).toHaveTextContent('Notifications')
    expect(notificationsLink).toHaveClass('nav-link')
  })

  it('marks notifications link as active when on /notifications', () => {
    renderMainLayout(createAuthValue(), '/notifications')

    expect(screen.getByTestId('nav-notifications-link')).toHaveClass('active')
  })

  it('does not render notifications nav link for signed-out users', () => {
    renderMainLayout(
      createAuthValue({
        isAuthenticated: false,
        session: null,
      })
    )

    expect(screen.queryByTestId('nav-notifications-link')).not.toBeInTheDocument()
  })
})
