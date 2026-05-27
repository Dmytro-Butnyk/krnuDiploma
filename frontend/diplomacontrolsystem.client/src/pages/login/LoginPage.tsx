import { useCallback, useEffect, useRef, useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../../features/auth/model/useAuth'
import { getApiErrorMessage } from '../../shared/api/errorMessage'

interface GoogleCredentialResponse {
  credential?: string
}

interface GoogleAccountsId {
  initialize: (options: {
    client_id: string
    callback: (response: GoogleCredentialResponse) => void
  }) => void
  renderButton: (
    parent: HTMLElement,
    options: {
      theme: 'outline' | 'filled_blue' | 'filled_black'
      size: 'large' | 'medium' | 'small'
      text: 'signin_with' | 'signup_with' | 'continue_with' | 'signin'
      shape: 'pill' | 'rectangular' | 'circle' | 'square'
      width: number
      locale: string
    },
  ) => void
}

declare global {
  interface Window {
    google?: {
      accounts: {
        id: GoogleAccountsId
      }
    }
  }
}

const googleClientId = (import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined)?.trim()
const googleScriptUrl = 'https://accounts.google.com/gsi/client'

function loadGoogleIdentityScript() {
  const existingScript = document.querySelector<HTMLScriptElement>(`script[src="${googleScriptUrl}"]`)

  if (window.google?.accounts.id) {
    return Promise.resolve()
  }

  if (existingScript) {
    return new Promise<void>((resolve, reject) => {
      existingScript.addEventListener('load', () => resolve(), { once: true })
      existingScript.addEventListener('error', () => reject(new Error('Не вдалося завантажити Google Sign-In')), {
        once: true,
      })
    })
  }

  return new Promise<void>((resolve, reject) => {
    const script = document.createElement('script')
    script.src = googleScriptUrl
    script.async = true
    script.defer = true
    script.onload = () => resolve()
    script.onerror = () => reject(new Error('Не вдалося завантажити Google Sign-In'))
    document.head.append(script)
  })
}

export function LoginPage() {
  const navigate = useNavigate()
  const { isAuthenticated, loginWithGoogle } = useAuth()
  const googleButtonRef = useRef<HTMLDivElement | null>(null)
  const [error, setError] = useState(googleClientId ? '' : 'Не задано VITE_GOOGLE_CLIENT_ID у .env')
  const [isLoading, setIsLoading] = useState(false)

  const handleGoogleCredential = useCallback(
    async (response: GoogleCredentialResponse) => {
      if (!response.credential) {
        setError('Google не повернув токен входу')
        return
      }

      setIsLoading(true)
      setError('')

      try {
        await loginWithGoogle(response.credential)
        navigate('/groups', { replace: true })
      } catch (loginError) {
        setError(getApiErrorMessage(loginError))
      } finally {
        setIsLoading(false)
      }
    },
    [loginWithGoogle, navigate],
  )

  useEffect(() => {
    document.documentElement.classList.add('login-page-document')
    document.body.classList.add('login-page-document')

    return () => {
      document.documentElement.classList.remove('login-page-document')
      document.body.classList.remove('login-page-document')
    }
  }, [])

  useEffect(() => {
    let isMounted = true

    if (!googleClientId) {
      return
    }

    const loadTimeout = window.setTimeout(() => {
      if (isMounted && !window.google?.accounts.id) {
        setError('Google Sign-In не завантажився. Перевірте доступ до accounts.google.com і перезапустіть dev server після зміни .env.')
      }
    }, 8000)

    loadGoogleIdentityScript()
      .then(() => {
        if (!isMounted || !window.google || !googleButtonRef.current) {
          return
        }

        window.clearTimeout(loadTimeout)
        googleButtonRef.current.innerHTML = ''
        window.google.accounts.id.initialize({
          client_id: googleClientId,
          callback: handleGoogleCredential,
        })
        window.google.accounts.id.renderButton(googleButtonRef.current, {
          theme: 'outline',
          size: 'large',
          text: 'signin_with',
          shape: 'pill',
          width: 320,
          locale: 'uk',
        })
      })
      .catch((scriptError: unknown) => {
        if (scriptError instanceof Error) {
          setError(scriptError.message)
        } else {
          setError('Не вдалося завантажити Google Sign-In')
        }
      })

    return () => {
      isMounted = false
      window.clearTimeout(loadTimeout)
    }
  }, [handleGoogleCredential])

  if (isAuthenticated) {
    return <Navigate to="/groups" replace />
  }

  return (
    <main className="login-page grid h-[100dvh] w-screen overflow-hidden bg-[radial-gradient(circle_at_20%_10%,#dbe5ff_0,transparent_38%),radial-gradient(circle_at_74%_72%,#e1f8ff_0,transparent_34%),#edf4ff] p-4">
      <section className="m-auto flex h-[min(430px,calc(100dvh-32px))] w-[min(720px,calc(100vw-32px))] flex-col items-center justify-center rounded-[22px] bg-white/60 px-[clamp(18px,6vw,80px)] py-[clamp(22px,6vh,48px)] text-center shadow-sm">
        <div className="grid h-[clamp(48px,11vh,64px)] w-full max-w-[560px] place-items-center rounded-full border-[clamp(3px,1vh,6px)] border-slate-100 bg-blue-600 px-5 text-[clamp(15px,2.4vw,20px)] font-bold leading-tight text-white shadow-[0_2px_10px_rgba(71,85,105,0.35)]">
          Система контролю процесу дипломування
        </div>

        <div className="mt-[clamp(28px,9vh,64px)] flex w-full max-w-[470px] flex-col items-center">
          <p className="text-[clamp(14px,2vw,16px)] font-medium text-slate-500">
            Увійдіть через Google-акаунт зареєстрованого секретаря
          </p>
          <div className="mt-6 flex min-h-11 justify-center">
            <div ref={googleButtonRef} />
          </div>
          <div className="mt-3 min-h-6 max-w-full whitespace-pre-wrap text-sm font-semibold text-red-500">
            {isLoading ? 'Перевіряємо доступ...' : error}
          </div>
        </div>
      </section>
    </main>
  )
}
