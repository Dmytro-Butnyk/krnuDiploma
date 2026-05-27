import type { EntityId } from '../../groups/api/types'

const authSessionStorageKey = 'diploma-control-auth-session'

export interface SecretaryProfile {
  id: EntityId
  email: string
  fullName: string
  specialtyId: EntityId
  specialtyName: string
  isSuperSecretary: boolean
}

export interface AuthSession {
  accessToken: string
  expiresAt: number
  secretary: SecretaryProfile
}

export function getStoredAuthSession(): AuthSession | null {
  const rawSession = window.localStorage.getItem(authSessionStorageKey)

  if (!rawSession) {
    return null
  }

  try {
    const session = JSON.parse(rawSession) as AuthSession

    if (!session.accessToken || !session.secretary?.email || session.expiresAt <= Date.now()) {
      clearStoredAuthSession()
      return null
    }

    return session
  } catch {
    clearStoredAuthSession()
    return null
  }
}

export function storeAuthSession(session: AuthSession) {
  window.localStorage.setItem(authSessionStorageKey, JSON.stringify(session))
}

export function clearStoredAuthSession() {
  window.localStorage.removeItem(authSessionStorageKey)
  window.localStorage.removeItem('diploma-control-secretary-email')
}

export function getStoredAccessToken() {
  return getStoredAuthSession()?.accessToken ?? ''
}
