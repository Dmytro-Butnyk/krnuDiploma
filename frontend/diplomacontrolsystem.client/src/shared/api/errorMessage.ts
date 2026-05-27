import { ApiError } from './client'

const fallbackMessage = 'Виникла помилка, спробуйте ще раз'

const errorTranslations = new Map<string, string>([
  ['Commission head specialty must match secretary specialty.', 'Спеціальність голови комісії може відрізнятися від спеціальності секретаря. Зверніться до адміністратора API, якщо помилка повторюється.'],
  ['Commission head specialty must match secretary specialty', 'Спеціальність голови комісії може відрізнятися від спеціальності секретаря. Зверніться до адміністратора API, якщо помилка повторюється.'],
  ['Bad Request', 'Некоректний запит'],
  ['Forbidden', 'Доступ заборонено'],
  ['Not Found', 'Не знайдено'],
  ['Conflict', 'Конфлікт даних'],
  ['Unauthorized', 'Потрібна авторизація'],
])

function translateApiMessage(message: string) {
  const normalized = message.trim()

  return errorTranslations.get(normalized) ?? message
}

export function getApiErrorMessages(error: unknown) {
  if (error instanceof ApiError) {
    const messages: string[] = []

    if (error.problem?.detail) {
      messages.push(translateApiMessage(error.problem.detail))
    }

    Object.entries(error.problem?.errors ?? {}).forEach(([field, fieldErrors]) => {
      fieldErrors.forEach((fieldError) => {
        const translatedError = translateApiMessage(fieldError)
        messages.push(field ? `${field}: ${translatedError}` : translatedError)
      })
    })

    if (messages.length > 0) {
      return messages
    }

    return [translateApiMessage(error.problem?.title ?? error.message ?? fallbackMessage)]
  }

  if (error instanceof Error) {
    return [translateApiMessage(error.message)]
  }

  return [fallbackMessage]
}

export function getApiErrorMessage(error: unknown) {
  return getApiErrorMessages(error).join('\n')
}
