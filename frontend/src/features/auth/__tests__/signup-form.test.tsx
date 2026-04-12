import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import { SignupPage } from '../signup/SignupPage'
import { AuthProvider } from '../../../app/providers/AuthProvider'

function renderSignupPage() {
  return render(
    <BrowserRouter>
      <AuthProvider initialSession={null}>
        <SignupPage />
      </AuthProvider>
    </BrowserRouter>
  )
}

describe('SignupPage', () => {
  it('renders the signup form shell', () => {
    renderSignupPage()

    expect(screen.getByLabelText(/username/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/display name/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/bio/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /sign up/i })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /sign in/i })).toBeInTheDocument()
  })
})
