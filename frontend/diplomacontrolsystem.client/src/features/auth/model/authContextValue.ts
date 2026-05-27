import { createContext } from 'react'
import type { SecretaryProfile } from './authSession'

export interface AuthContextValue {
  secretaryEmail: string
  secretary: SecretaryProfile | null
  isAuthenticated: boolean
  loginWithGoogle: (idToken: string) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
