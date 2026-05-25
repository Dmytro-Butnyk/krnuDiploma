import { apiRequest } from '../../../shared/api/client'
import type { EducationLevel, EntityId } from '../../groups/api/types'
import type {
  CommissionHeadDto,
  CreateCommissionHeadRequest,
  CreateDiplomaExaminationCommissionRequest,
  DiplomaExaminationCommissionResponse,
  GetDiplomaExaminationCommissionOptionsResponse,
  UpdateDiplomaExaminationCommissionRequest,
} from './types'

export function getDiplomaExaminationCommissions(
  secretaryEmail: string,
  educationLevel: EducationLevel,
  defenseYear: string,
) {
  return apiRequest<DiplomaExaminationCommissionResponse>('/api/diploma-examination-commissions', {
    query: {
      SecretaryEmail: secretaryEmail,
      EducationLevel: educationLevel,
      DefenseYear: defenseYear,
    },
  })
}

export function getDiplomaExaminationCommissionOptions(
  secretaryEmail: string,
  commissionId?: EntityId,
) {
  return apiRequest<GetDiplomaExaminationCommissionOptionsResponse>(
    '/api/diploma-examination-commissions/options',
    {
      query: {
        SecretaryEmail: secretaryEmail,
        CommissionId: commissionId,
      },
    },
  )
}

export function createCommissionHead(request: CreateCommissionHeadRequest) {
  return apiRequest<CommissionHeadDto>('/api/commission-heads', {
    method: 'POST',
    body: request,
  })
}

export function createDiplomaExaminationCommission(request: CreateDiplomaExaminationCommissionRequest) {
  return apiRequest<DiplomaExaminationCommissionResponse>('/api/diploma-examination-commissions', {
    method: 'POST',
    body: request,
  })
}

export function updateDiplomaExaminationCommission(
  commissionId: EntityId,
  request: UpdateDiplomaExaminationCommissionRequest,
) {
  return apiRequest<DiplomaExaminationCommissionResponse>(
    `/api/diploma-examination-commissions/${commissionId}`,
    {
      method: 'PUT',
      body: request,
    },
  )
}

export function deleteDiplomaExaminationCommission(commissionId: EntityId, secretaryEmail: string) {
  return apiRequest<void>(`/api/diploma-examination-commissions/${commissionId}`, {
    method: 'DELETE',
    query: { secretaryEmail },
  })
}
