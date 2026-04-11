import { render } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from '../../app/providers/AuthProvider'
import { apiClient } from '../api/client'
import { vi } from 'vitest'

// Mock setup helper
export function setupApiMocks() {
  vi.mocked(apiClient.get).mockClear()
  vi.mocked(apiClient.post).mockClear()
  vi.mocked(apiClient.patch).mockClear()
  vi.mocked(apiClient.delete).mockClear()
}

// Render with providers
export function renderWithProviders(ui: React.ReactElement) {
  return render(
    <BrowserRouter>
      <AuthProvider>{ui}</AuthProvider>
    </BrowserRouter>
  )
}

// Mock authenticated session
export function mockAuthenticatedSession() {
  vi.mocked(apiClient.get).mockResolvedValueOnce({
    userId: 1,
    username: 'testuser',
    displayName: 'Test User',
  })
}
