import { createContext, useContext, useEffect, useState } from 'react'
import { apiClient } from '../../shared/api/client'
import { isApiError } from '../../shared/api/errors'
import type { SessionResponse } from '../../shared/api/contracts'

interface AuthContextValue {
  session: SessionResponse | null
  isLoading: boolean
  isAuthenticated: boolean
  signin: (username: string, password: string) => Promise<void>
  signout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [session, setSession] = useState<SessionResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    checkSession()
  }, [])

  async function checkSession() {
    try {
      const response = await apiClient.get<SessionResponse>('/auth/session')
      setSession(response)
    } catch (error) {
      // 401 is expected when not authenticated
      if (isApiError(error) && error.status === 401) {
        setSession(null)
      }
    } finally {
      setIsLoading(false)
    }
  }

  async function signin(username: string, password: string) {
    const response = await apiClient.post<SessionResponse>('/auth/signin', {
      username,
      password
    })
    setSession(response)
  }

  async function signout() {
    await apiClient.post('/auth/signout', null)
    setSession(null)
  }

  return (
    <AuthContext.Provider
      value={{
        session,
        isLoading,
        isAuthenticated: session !== null,
        signin,
        signout
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return context
}
