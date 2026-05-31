import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../../shared/api/client'
import type { ConstructorScenario } from '../model/types'

export const constructorScenariosQueryKey = ['constructor', 'scenarios'] as const

export async function fetchConstructorScenarios() {
  const response = await apiClient.get<ConstructorScenario[]>('/api/constructor/scenarios')
  return response.data
}

export function useConstructorScenarios() {
  return useQuery({
    queryKey: constructorScenariosQueryKey,
    queryFn: fetchConstructorScenarios,
  })
}
