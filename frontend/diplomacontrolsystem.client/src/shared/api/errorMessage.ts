import { ApiError } from './client'

const fallbackMessage = 'Виникла помилка, спробуйте ще раз'

export function getApiErrorMessages(error: unknown) {
  if (error instanceof ApiError) {
    const messages: string[] = []

    if (error.problem?.detail) {
      messages.push(error.problem.detail)
    }

    Object.entries(error.problem?.errors ?? {}).forEach(([field, fieldErrors]) => {
      fieldErrors.forEach((fieldError) => {
        messages.push(field ? `${field}: ${fieldError}` : fieldError)
      })
    })

    if (messages.length > 0) {
      return messages
    }

    return [error.problem?.title ?? error.message ?? fallbackMessage]
  }

  if (error instanceof Error) {
    return [error.message]
  }

  return [fallbackMessage]
}

export function getApiErrorMessage(error: unknown) {
  return getApiErrorMessages(error).join('\n')
}
