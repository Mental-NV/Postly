import type { ReactNode } from 'react'
import { NavLink, Link, useNavigate } from 'react-router-dom'
import { Home, User, LogOut } from 'lucide-react'
import { useAuth } from '../../app/providers/AuthProvider'
import { Button } from './Button'

interface MainLayoutProps {
  children: ReactNode
}

export function MainLayout({ children }: MainLayoutProps): React.JSX.Element {
  const navigate = useNavigate()
  const { session, isAuthenticated } = useAuth()

  const handleSignOut = (): void => {
    // Basic sign out logic - clear local storage/session and redirect
    localStorage.removeItem('token') // Assuming a token-based system
    void navigate('/signin')
  }

  return (
    <div className="layout-shell">
      <header className="layout-left">
        <div className="nav-container">
          <Link to="/" className="brand" data-testid="brand-link">
            Postly
          </Link>
          {isAuthenticated ? <>
              <nav className="nav-links">
                <NavLink
                  to="/"
                  className={({ isActive }) =>
                    `nav-link ${isActive ? 'active' : ''}`
                  }
                  data-testid="nav-home-link"
                >
                  <Home size={24} />
                  <span className="nav-link-text">Home</span>
                </NavLink>
                <NavLink
                  to={session ? `/u/${session.username}` : '/u/me'}
                  className={({ isActive }) =>
                    `nav-link ${isActive ? 'active' : ''}`
                  }
                  data-testid="nav-profile-link"
                >
                  <User size={24} />
                  <span className="nav-link-text">Profile</span>
                </NavLink>
              </nav>
              <div className="nav-footer">
                <Button
                  variant="ghost"
                  onClick={handleSignOut}
                  className="signout-btn"
                  data-testid="nav-signout-button"
                >
                  <LogOut size={24} />
                  <span className="signout-btn-text">Sign Out</span>
                </Button>
              </div>
            </> : null}
        </div>
      </header>

      <main className="layout-middle">{children}</main>

      <aside className="layout-right">
        <div className="aside-container">
          <section className="trends-placeholder">
            <h3>What's happening</h3>
            <p className="placeholder-text">Coming soon to the MVP!</p>
          </section>
          <section className="trends-placeholder">
            <h3>Who to follow</h3>
            <p className="placeholder-text">Coming soon to the MVP!</p>
          </section>
        </div>
      </aside>
    </div>
  )
}
