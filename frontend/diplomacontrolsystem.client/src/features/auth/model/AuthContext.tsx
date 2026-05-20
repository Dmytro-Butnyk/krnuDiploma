import { useMemo, useState, type ReactNode } from 'react'
import {
  clearStoredSecretaryEmail,
  getStoredSecretaryEmail,
  storeSecretaryEmail,
} from './authSession'
import { AuthContext, type AuthContextValue } from './authContextValue'

interface AuthProviderProps {
  children: ReactNode
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [secretaryEmail, setSecretaryEmail] = useState(getStoredSecretaryEmail)

  const value = useMemo<AuthContextValue>(
    () => ({
      secretaryEmail,
      isAuthenticated: secretaryEmail.trim().length > 0,
      login: (email) => {
        const normalizedEmail = email.trim()
        storeSecretaryEmail(normalizedEmail)
        setSecretaryEmail(normalizedEmail)
      },
      logout: () => {
        clearStoredSecretaryEmail()
        setSecretaryEmail('')
      },
    }),
    [secretaryEmail],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
