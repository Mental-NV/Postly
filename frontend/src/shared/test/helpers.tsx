import { render } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from '../../app/providers/AuthProvider'
import { apiClient } from '../api/client'
import { vi } from 'vitest'
import type { SessionResponse } from '../api/contracts'

// Mock setup helper
export function setupApiMocks() {
  vi.mocked(apiClient.get).mockClear()
  vi.mocked(apiClient.post).mockClear()
  vi.mocked(apiClient.patch).mockClear()
  vi.mocked(apiClient.putForm).mockClear()
  vi.mocked(apiClient.delete).mockClear()
}

// Render with providers
export function renderWithProviders(ui: React.ReactElement, {
  session = null as SessionResponse | null
} = {}) {
  return render(
    <BrowserRouter>
      <AuthProvider initialSession={session}>{ui}</AuthProvider>
    </BrowserRouter>
  )
}

// Mock authenticated session
export function mockAuthenticatedSession() {
  return {
    userId: 1,
    username: 'testuser',
    displayName: 'Test User',
  }
}
