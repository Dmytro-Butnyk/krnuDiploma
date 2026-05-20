import { createContext } from 'react'

export interface AuthContextValue {
  secretaryEmail: string
  isAuthenticated: boolean
  login: (secretaryEmail: string) => void
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
