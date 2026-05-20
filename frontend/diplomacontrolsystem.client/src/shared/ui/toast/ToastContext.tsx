/* eslint-disable react-refresh/only-export-components */
import { X } from 'lucide-react'
import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react'

interface ToastItem {
  id: number
  type: 'error' | 'success'
  messages: string[]
}

export interface ToastContextValue {
  showError: (message: string | string[]) => void
  showSuccess: (message?: string | string[]) => void
}

export const ToastContext = createContext<ToastContextValue | null>(null)

interface ToastProviderProps {
  children: ReactNode
}

export function ToastProvider({ children }: ToastProviderProps) {
  const [toasts, setToasts] = useState<ToastItem[]>([])

  const dismiss = useCallback((id: number) => {
    setToasts((current) => current.filter((toast) => toast.id !== id))
  }, [])

  const showToast = useCallback(
    (type: ToastItem['type'], message: string | string[]) => {
      const id = Date.now()
      const messages = Array.isArray(message) ? message : [message]
      setToasts((current) => [...current, { id, type, messages }])
      window.setTimeout(() => dismiss(id), 15000)
    },
    [dismiss],
  )

  const showError = useCallback((message: string | string[]) => showToast('error', message), [showToast])

  const showSuccess = useCallback(
    (message: string | string[] = 'Операція виконана успішно') => showToast('success', message),
    [showToast],
  )

  const value = useMemo(() => ({ showError, showSuccess }), [showError, showSuccess])

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div className="pointer-events-none fixed right-0 top-[210px] z-[70] flex w-full flex-col items-end gap-3">
        {toasts.map((toast) => (
          <div
            key={toast.id}
            className={[
              'pointer-events-auto ml-auto flex min-h-20 w-fit max-w-[min(760px,calc(100vw-24px))] items-center justify-between gap-8 rounded-l-full border-2 px-8 py-5 text-2xl font-bold shadow-lg backdrop-blur',
              toast.type === 'success'
                ? 'border-green-500 bg-green-50/95 text-green-600'
                : 'border-red-500 bg-red-50/95 text-red-500',
            ].join(' ')}
          >
            {toast.messages.length === 1 ? (
              <span className="whitespace-pre-wrap break-words">{toast.messages[0]}</span>
            ) : (
              <ul className="list-disc space-y-1 pl-7 text-left">
                {toast.messages.map((message) => (
                  <li key={message} className="whitespace-pre-wrap break-words">
                    {message}
                  </li>
                ))}
              </ul>
            )}
            <button
              type="button"
              aria-label="Закрити повідомлення"
              onClick={() => dismiss(toast.id)}
              className={[
                'grid size-10 shrink-0 place-items-center transition',
                toast.type === 'success' ? 'text-green-600 hover:text-green-700' : 'text-red-500 hover:text-red-600',
              ].join(' ')}
            >
              <X size={36} />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}

export function useToast() {
  const context = useContext(ToastContext)

  if (!context) {
    throw new Error('useToast must be used inside ToastProvider')
  }

  return context
}
