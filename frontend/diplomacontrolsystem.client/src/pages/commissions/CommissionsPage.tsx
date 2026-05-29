import { Navigate, useSearchParams } from 'react-router-dom'

export function CommissionsPage() {
  const [searchParams] = useSearchParams()
  const query = searchParams.toString()

  return <Navigate to={`/groups${query ? `?${query}` : ''}`} replace />
}
