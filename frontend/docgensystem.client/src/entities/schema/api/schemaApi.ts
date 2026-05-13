import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../../shared/api/client'
import type { EntitySchema } from '../model/types'

export const schemaQueryKey = ['constructor', 'schema'] as const

export async function fetchConstructorSchema() {
  const response = await apiClient.get<EntitySchema>('/api/constructor/schema')
  return response.data
}

export function useConstructorSchema() {
  return useQuery({
    queryKey: schemaQueryKey,
    queryFn: fetchConstructorSchema,
  })
}
