import { queryOptions } from '@tanstack/react-query'
import type { EducationLevel, EntityId } from '../../groups/api/types'
import {
  getDiplomaExaminationCommissionOptions,
  getDiplomaExaminationCommissions,
} from '../api/commissionsApi'

export const commissionQueryKeys = {
  all: ['commissions'] as const,
  lists: () => [...commissionQueryKeys.all, 'list'] as const,
  list: (secretaryEmail: string, educationLevel: EducationLevel, defenseYear: string) =>
    [...commissionQueryKeys.lists(), secretaryEmail, educationLevel, defenseYear] as const,
  options: (
    secretaryEmail: string,
    educationLevel: EducationLevel,
    defenseYear: string,
    commissionId?: EntityId,
  ) => [...commissionQueryKeys.all, 'options', secretaryEmail, educationLevel, defenseYear, commissionId ?? null] as const,
}

export function commissionsQuery(secretaryEmail: string, educationLevel: EducationLevel, defenseYear: string) {
  return queryOptions({
    queryKey: commissionQueryKeys.list(secretaryEmail, educationLevel, defenseYear),
    queryFn: () => getDiplomaExaminationCommissions(secretaryEmail, educationLevel, defenseYear),
    enabled: Boolean(secretaryEmail && defenseYear),
  })
}

export function commissionOptionsQuery(
  secretaryEmail: string,
  educationLevel: EducationLevel,
  defenseYear: string,
  commissionId?: EntityId,
) {
  return queryOptions({
    queryKey: commissionQueryKeys.options(secretaryEmail, educationLevel, defenseYear, commissionId),
    queryFn: () =>
      getDiplomaExaminationCommissionOptions(secretaryEmail, educationLevel, defenseYear, commissionId),
    enabled: Boolean(secretaryEmail && defenseYear),
  })
}
