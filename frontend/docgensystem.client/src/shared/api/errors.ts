import axios from 'axios'

type ProblemDetails = {
  title?: string | null
  detail?: string | null
  errors?: Record<string, string[]>
}

export function getApiErrorMessage(error: unknown, fallback = 'Сталася помилка. Спробуйте ще раз.') {
  if (!axios.isAxiosError<ProblemDetails>(error)) {
    return error instanceof Error ? error.message : fallback
  }

  const data = error.response?.data
  const validationErrors = data?.errors ? Object.values(data.errors).flat() : []

  if (validationErrors.length > 0) return validationErrors.join('\n')
  if (data?.detail) return data.detail
  if (data?.title) return data.title
  if (error.message) return error.message

  return fallback
}
