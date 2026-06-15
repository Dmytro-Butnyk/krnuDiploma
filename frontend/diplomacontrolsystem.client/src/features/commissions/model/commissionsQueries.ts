import { queryOptions } from '@tanstack/react-query'
import type { EducationLevel, EntityId } from '../../groups/api/types'
import {
  getCommissionHeads,
  getDiplomaExaminationCommissionOptions,
  getDiplomaExaminationCommissions,
} from '../api/commissionsApi'

export const commissionQueryKeys = {
  all: ['commissions'] as const,
  details: () => [...commissionQueryKeys.all, 'detail'] as const,
  detail: (secretaryEmail: string, educationLevel: EducationLevel, defenseYear: string) =>
    [...commissionQueryKeys.details(), secretaryEmail, educationLevel, defenseYear] as const,
  heads: (secretaryEmail: string) => [...commissionQueryKeys.all, 'heads', secretaryEmail] as const,
  options: (
    secretaryEmail: string,
    commissionId?: EntityId,
  ) => [...commissionQueryKeys.all, 'options', secretaryEmail, commissionId ?? null] as const,
}

export function commissionsQuery(secretaryEmail: string, educationLevel: EducationLevel, defenseYear: string) {
  return queryOptions({
    queryKey: commissionQueryKeys.detail(secretaryEmail, educationLevel, defenseYear),
    queryFn: () => getDiplomaExaminationCommissions(secretaryEmail, educationLevel, defenseYear),
    enabled: Boolean(secretaryEmail && defenseYear),
  })
}

export function commissionOptionsQuery(
  secretaryEmail: string,
  commissionId?: EntityId,
) {
  return queryOptions({
    queryKey: commissionQueryKeys.options(secretaryEmail, commissionId),
    queryFn: () => getDiplomaExaminationCommissionOptions(secretaryEmail, commissionId),
    enabled: Boolean(secretaryEmail),
  })
}

export function commissionHeadsQuery(secretaryEmail: string) {
  return queryOptions({
    queryKey: commissionQueryKeys.heads(secretaryEmail),
    queryFn: () => getCommissionHeads(secretaryEmail),
    enabled: Boolean(secretaryEmail),
  })
}
