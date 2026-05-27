import { getStoredAccessToken } from '../../features/auth/model/authSession'

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  status: number
  problem?: ProblemDetails

  constructor(status: number, message: string, problem?: ProblemDetails) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

type QueryValue = string | number | boolean | null | undefined

interface ApiRequestOptions extends Omit<RequestInit, 'body'> {
  query?: Record<string, QueryValue>
  body?: unknown
  skipAuth?: boolean
}

const rawApiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7007'
const apiBaseUrl = rawApiBaseUrl.replace(/\/$/, '')

function buildUrl(path: string, query?: Record<string, QueryValue>) {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  const url = new URL(`${apiBaseUrl}${normalizedPath}`)

  Object.entries(query ?? {}).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      url.searchParams.set(key, String(value))
    }
  })

  return url.toString()
}

async function readResponse<T>(response: Response): Promise<T> {
  if (response.status === 204) {
    return undefined as T
  }

  const contentType = response.headers.get('content-type') ?? ''
  if (contentType.includes('application/json') || contentType.includes('application/problem+json')) {
    return (await response.json()) as T
  }

  return (await response.text()) as T
}

export async function apiRequest<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  const { body, headers, query, skipAuth, ...init } = options
  const requestHeaders = new Headers(headers)
  let requestBody: BodyInit | undefined

  if (body instanceof FormData) {
    requestBody = body
  } else if (body !== undefined) {
    requestHeaders.set('content-type', 'application/json')
    requestBody = JSON.stringify(body)
  }

  const accessToken = skipAuth ? '' : getStoredAccessToken()

  if (accessToken && !requestHeaders.has('authorization')) {
    requestHeaders.set('authorization', `Bearer ${accessToken}`)
  }

  const response = await fetch(buildUrl(path, query), {
    ...init,
    headers: requestHeaders,
    body: requestBody,
  })

  if (!response.ok) {
    const problem = await readResponse<ProblemDetails | string>(response).catch(() => undefined)
    const parsedProblem = typeof problem === 'object' ? problem : undefined
    const fallbackMessage = typeof problem === 'string' && problem ? problem : 'Виникла помилка, спробуйте ще раз'
    throw new ApiError(response.status, parsedProblem?.detail ?? parsedProblem?.title ?? fallbackMessage, parsedProblem)
  }

  return readResponse<T>(response)
}
