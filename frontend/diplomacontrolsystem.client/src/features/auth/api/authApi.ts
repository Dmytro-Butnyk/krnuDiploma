import { apiRequest } from '../../../shared/api/client'
import type { AuthSession, SecretaryProfile } from '../model/authSession'

interface LoginWithGoogleResponse {
  accessToken: string
  expiresInSeconds: number | string
  secretary: SecretaryProfile
}

export async function loginWithGoogle(idToken: string): Promise<AuthSession> {
  const response = await apiRequest<LoginWithGoogleResponse>('/api/auth/google', {
    method: 'POST',
    body: { idToken },
    skipAuth: true,
  })

  const expiresInSeconds = Number(response.expiresInSeconds)
  const expiresAt = Date.now() + Math.max(Number.isFinite(expiresInSeconds) ? expiresInSeconds : 0, 0) * 1000

  return {
    accessToken: response.accessToken,
    expiresAt,
    secretary: response.secretary,
  }
}
