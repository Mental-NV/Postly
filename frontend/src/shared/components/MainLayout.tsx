import { ReactNode } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { Button } from './Button'

interface MainLayoutProps {
  children: ReactNode
}

export function MainLayout({ children }: MainLayoutProps) {
  const navigate = useNavigate()

  const handleSignOut = () => {
    // Basic sign out logic - clear local storage/session and redirect
    localStorage.removeItem('token') // Assuming a token-based system
    void navigate('/signin')
  }

  return (
    <div className="layout-shell">
      <header className="layout-left">
        <div className="nav-container">
          <div className="brand">Postly</div>
          <nav className="nav-links">
            <NavLink
              to="/"
              className={({ isActive }) =>
                `nav-link ${isActive ? 'active' : ''}`
              }
            >
              Home
            </NavLink>
            <NavLink
              to="/u/me"
              className={({ isActive }) =>
                `nav-link ${isActive ? 'active' : ''}`
              }
            >
              Profile
            </NavLink>
          </nav>
          <div className="nav-footer">
            <Button
              variant="ghost"
              onClick={handleSignOut}
              className="signout-btn"
            >
              Sign Out
            </Button>
          </div>
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
