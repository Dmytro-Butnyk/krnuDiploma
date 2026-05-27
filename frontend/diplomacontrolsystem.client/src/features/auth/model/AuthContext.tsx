import { useMemo, useState, type ReactNode } from 'react'
import {
  clearStoredAuthSession,
  getStoredAuthSession,
  storeAuthSession,
  type AuthSession,
} from './authSession'
import { AuthContext, type AuthContextValue } from './authContextValue'
import { loginWithGoogle as requestGoogleLogin } from '../api/authApi'

interface AuthProviderProps {
  children: ReactNode
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [session, setSession] = useState<AuthSession | null>(getStoredAuthSession)
  const secretary = session?.secretary ?? null
  const secretaryEmail = secretary?.email ?? ''

  const value = useMemo<AuthContextValue>(
    () => ({
      secretaryEmail,
      secretary,
      isAuthenticated: Boolean(session?.accessToken && secretaryEmail),
      loginWithGoogle: async (idToken) => {
        const nextSession = await requestGoogleLogin(idToken)
        storeAuthSession(nextSession)
        setSession(nextSession)
      },
      logout: () => {
        clearStoredAuthSession()
        setSession(null)
      },
    }),
    [secretary, secretaryEmail, session?.accessToken],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
