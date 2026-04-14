import { useEffect, useState } from 'react'
import { apiClient } from '../../shared/api/client'
import { isApiError } from '../../shared/api/errors'
import type { SessionResponse } from '../../shared/api/contracts'
import { AuthContext } from './AuthContext'

export function AuthProvider({
  children,
  initialSession,
}: {
  children: React.ReactNode
  initialSession?: SessionResponse | null
}): React.JSX.Element {
  const hasInitialSession = initialSession !== undefined
  const [session, setSession] = useState<SessionResponse | null>(
    initialSession ?? null
  )
  const [isLoading, setIsLoading] = useState(!hasInitialSession)

  useEffect(() => {
    if (!hasInitialSession) {
      void checkSession()
    }
  }, [hasInitialSession])

  async function checkSession(): Promise<void> {
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

  async function signin(username: string, password: string): Promise<void> {
    const response = await apiClient.post<SessionResponse>('/auth/signin', {
      username,
      password,
    })
    setSession(response)
  }

  async function signout(): Promise<void> {
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
        signout,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}
