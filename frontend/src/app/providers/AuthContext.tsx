import { createContext, useContext } from 'react'
import type { SessionResponse } from '../../shared/api/contracts'

export interface AuthContextValue {
  session: SessionResponse | null
  isLoading: boolean
  isAuthenticated: boolean
  signin: (username: string, password: string) => Promise<void>
  signout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return context
}
